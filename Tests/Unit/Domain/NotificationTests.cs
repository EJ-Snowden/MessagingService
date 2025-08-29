using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace Tests.Unit.Domain;

public sealed class NotificationTests
{
    private static Notification New() => new(ChannelType.Email, "to@example.com", "hi", "s");

    [Fact]
    public void MarkSent_sets_expected_state()
    {
        var n = New();

        n.MarkSent();

        n.Status.Should().Be(NotificationStatus.Sent);
        n.LastError.Should().BeNull();
        n.NextAttemptAt.Should().BeNull();
        n.Attempts.Should().Be(1);
    }

    [Fact]
    public void MarkDelayed_sets_expected_state()
    {
        var n = New();
        var next = DateTime.UtcNow.AddMinutes(1);

        n.MarkDelayed("oops", next);

        n.Status.Should().Be(NotificationStatus.Delayed);
        n.LastError.Should().Be("oops");
        n.NextAttemptAt.Should().BeCloseTo(next, TimeSpan.FromSeconds(1));
        n.Attempts.Should().Be(1);
    }

    [Fact]
    public void MarkFailed_sets_expected_state()
    {
        var n = New();

        n.MarkFailed("bad");

        n.Status.Should().Be(NotificationStatus.Failed);
        n.LastError.Should().Be("bad");
        n.NextAttemptAt.Should().BeNull();
        n.Attempts.Should().Be(1);
    }
}