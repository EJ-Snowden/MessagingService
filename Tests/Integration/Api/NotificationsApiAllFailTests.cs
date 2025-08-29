using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tests.Integration.Support;

namespace Tests.Integration.Api;

public sealed class NotificationsApiAllFailTests(AllFailAppFactory factory) : IClassFixture<AllFailAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Post_send_then_get_returns_delayed_and_has_last_error()
    {
        var body = new
        {
            channel = "Email",
            recipient = "someone@example.com",
            message = "hello",
            subject = "test"
        };

        var post = await _client.PostAsJsonAsync("/notifications/send", body);
        post.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var created = await post.Content.ReadFromJsonAsync<IdDto>();
        created!.Id.Should().NotBeEmpty();

        var get = await _client.GetAsync($"/notifications/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await get.Content.ReadFromJsonAsync<StatusDto>();
        status!.Status.Should().Be("Delayed");
        status.LastError.Should().NotBeNullOrEmpty();
        status.NextAttemptAt.Should().NotBeNull();
    }

    private sealed record IdDto(Guid Id);
    private sealed record StatusDto(Guid Id, string Channel, string Status, int Attempts, DateTime CreatedAt, DateTime? NextAttemptAt, string? LastError);
}