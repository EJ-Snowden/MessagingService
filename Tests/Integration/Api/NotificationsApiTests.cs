using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tests.Integration.Support;

namespace Tests.Integration.Api;

public sealed class NotificationsApiTests(SuccessAppFactory factory) : IClassFixture<SuccessAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Post_send_then_get_returns_sent_status()
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
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();

        post.Headers.Location!.ToString().Should().Contain($"/notifications/{created.Id}");

        var get = await _client.GetAsync($"/notifications/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await get.Content.ReadFromJsonAsync<StatusDto>();
        status!.Id.Should().Be(created.Id);
        status.Channel.Should().Be("Email");
        status.Status.Should().Be("Sent");
    }
    
    [Fact]
    public async Task Send_with_missing_fields_returns_400()
    {
        var bad = new { channel = "Email", recipient = "", message = "" };

        var res = await _client.PostAsJsonAsync("/notifications/send", bad);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        res.Content.Headers.ContentType!.ToString().Should().Contain("application/problem+json");
    }

    [Fact]
    public async Task Get_returns_enum_values_as_strings()
    {
        var post = await _client.PostAsJsonAsync("/notifications/send", new {
            channel = "Email",
            recipient = "someone@example.com",
            message = "hello",
            subject = "test"
        });

        var created = await post.Content.ReadFromJsonAsync<IdDto>();
        created!.Id.Should().NotBeEmpty();

        var res = await _client.GetAsync($"/notifications/{created.Id}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await res.Content.ReadAsStringAsync();
        json.Should().Contain("\"channel\":\"Email\"");
        json.Should().Contain("\"status\":\"Sent\"");
    }

    private sealed record IdDto(Guid Id);
    private sealed record StatusDto(Guid Id, string Channel, string Status, int Attempts, DateTime CreatedAt, DateTime? NextAttemptAt, string? LastError);
}