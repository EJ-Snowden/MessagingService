using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Providers;
using Infrastructure.Repositories;

namespace Tests.Unit.Infrastructure;

public sealed class ProviderRegistryTests
{
    private sealed class TwilioSmsProvider : INotificationProvider
    {
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; } = 5;
        public bool CanHandle(ChannelType c) => c == ChannelType.Sms;
        public Task<ProviderResult> SendAsync(Notification n) => Task.FromResult(new ProviderResult(true));
    }

    private sealed class AwsSnsSmsProvider : INotificationProvider
    {
        public bool Enabled { get; set; } = false;
        public int Priority { get; set; } = 9;
        public bool CanHandle(ChannelType c) => c == ChannelType.Sms;
        public Task<ProviderResult> SendAsync(Notification n) => Task.FromResult(new ProviderResult(true));
    }

    private sealed class EmailProvider : INotificationProvider
    {
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; } = 3;
        public bool CanHandle(ChannelType c) => c == ChannelType.Email;
        public Task<ProviderResult> SendAsync(Notification n) => Task.FromResult(new ProviderResult(true));
    }

    [Fact]
    public void ApplyOptions_updates_enabled_and_priority_from_config()
    {
        var reg = new ProviderRegistry([new TwilioSmsProvider(), new AwsSnsSmsProvider(), new EmailProvider()]);

        var options = new MessagingOptions
        {
            Channels = new Dictionary<string, List<ProviderOption>>
            {
                ["Sms"] =
                [
                    new ProviderOption { Name = "TwilioSms", Enabled = false, Priority = 2 },
                    new ProviderOption { Name = "AwsSnsSms", Enabled = true, Priority = 1 }
                ],
                ["Email"] = [new ProviderOption { Name = "Email", Enabled = true, Priority = 1 }]
            }
        };

        reg.ApplyOptions(options);

        var all = reg.GetAll();

        all.Should().Contain(p => p.Name == "TwilioSms" && p.Enabled == false && p.Priority == 2);
        all.Should().Contain(p => p.Name == "AwsSnsSms" && p.Enabled == true  && p.Priority == 1);
        all.Should().Contain(p => p.Name == "Email"     && p.Enabled == true  && p.Priority == 1);
    }

    [Fact]
    public void TryUpdate_changes_state_at_runtime()
    {
        var reg = new ProviderRegistry([new TwilioSmsProvider()]);

        var ok = reg.TryUpdate("TwilioSms", enabled: false, priority: 7);
        ok.Should().BeTrue();

        var twilio = reg.GetAll().Single(p => p.Name == "TwilioSms");
        twilio.Enabled.Should().BeFalse();
        twilio.Priority.Should().Be(7);
    }
}