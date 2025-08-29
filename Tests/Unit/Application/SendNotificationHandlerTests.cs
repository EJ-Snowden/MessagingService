using Application.UseCases;
using Application.UseCases.Handlers;
using Domain.Enums;
using FluentAssertions;

namespace Tests.Unit.Application;

public sealed class SendNotificationHandlerTests
{
    private static SendNotificationRequest Req(ChannelType ch = ChannelType.Email)
        => new(ch, "to@example.com", "hi", "subject");

    [Fact]
    public async Task Picks_first_successful_provider_and_stops()
    {
        var repo = new StubNotificationRepository();
        var queue = new StubRetryQueue();
        var clock = new FakeClock(new DateTime(2025, 8, 29, 20, 0, 0, DateTimeKind.Utc));

        var p1 = new FakeProviderSuccess(ChannelType.Email, priority: 1);
        var p2 = new FakeProviderSuccess(ChannelType.Email, priority: 2);

        var sut = new SendNotificationHandler([p1, p2], repo, queue, clock);

        var id = await sut.HandleAsync(Req());
        var saved = repo.GetOrThrow(id);

        saved.Status.Should().Be(NotificationStatus.Sent);
        saved.LastError.Should().BeNull();
        (await queue.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Fails_over_when_first_provider_fails_then_marks_sent()
    {
        var repo = new StubNotificationRepository();
        var queue = new StubRetryQueue();
        var clock = new FakeClock(new DateTime(2025, 8, 29, 20, 0, 0, DateTimeKind.Utc));

        var p1 = new FakeProviderFail(ChannelType.Email, "p1 failed", isTransient: true, priority: 1);
        var p2 = new FakeProviderSuccess(ChannelType.Email, priority: 2);

        var sut = new SendNotificationHandler([p1, p2], repo, queue, clock);

        var id = await sut.HandleAsync(Req());
        var saved = repo.GetOrThrow(id);

        saved.Status.Should().Be(NotificationStatus.Sent);
        saved.LastError.Should().BeNull();
        (await queue.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task When_all_providers_fail_marks_delayed_and_enqueues_about_one_minute()
    {
        var repo = new StubNotificationRepository();
        var queue = new StubRetryQueue();
        var now = new DateTime(2025, 8, 29, 20, 0, 0, DateTimeKind.Utc);
        var clock = new FakeClock(now);

        var p1 = new FakeProviderFail(ChannelType.Email, "p1 down", isTransient: true, priority: 1);
        var p2 = new FakeProviderFail(ChannelType.Email, "p2 down", isTransient: true, priority: 2);

        var sut = new SendNotificationHandler([p1, p2], repo, queue, clock);

        var id = await sut.HandleAsync(Req());
        var saved = repo.GetOrThrow(id);

        saved.Status.Should().Be(NotificationStatus.Delayed);
        saved.LastError.Should().Be("p2 down");
        saved.NextAttemptAt.Should().NotBeNull();

        var due = queue.LastEnqueued!.Value.Due;
        (due - now).TotalSeconds.Should().BeInRange(50, 70);
    }
}