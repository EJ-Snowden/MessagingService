using System.Collections.Concurrent;
using System.Diagnostics;
using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Providers;
using Infrastructure.Repositories;

namespace Tests.Unit.Infrastructure;

public sealed class ProviderRegistryConcurrencyTests
{
    private sealed class TwilioSmsProvider : INotificationProvider
    {
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; } = 1;
        public bool CanHandle(ChannelType c) => c == ChannelType.Sms;
        public Task<ProviderResult> SendAsync(Notification n) => Task.FromResult(new ProviderResult(true));
    }

    private sealed class AwsSnsSmsProvider : INotificationProvider
    {
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; } = 2;
        public bool CanHandle(ChannelType c) => c == ChannelType.Sms;
        public Task<ProviderResult> SendAsync(Notification n) => Task.FromResult(new ProviderResult(true));
    }

    private sealed class EmailProvider : INotificationProvider
    {
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; } = 1;
        public bool CanHandle(ChannelType c) => c == ChannelType.Email;
        public Task<ProviderResult> SendAsync(Notification n) => Task.FromResult(new ProviderResult(true));
    }

    [Fact]
    public void TryUpdate_is_safe_under_parallel_calls()
    {
        var registry = new ProviderRegistry([
            new TwilioSmsProvider(),
            new AwsSnsSmsProvider(),
            new EmailProvider()
        ]);

        var errors = new ConcurrentQueue<Exception>();

        Parallel.For(0, 300, i =>
        {
            try
            {
                var enabled = (i % 2) == 0;
                var priority = 1 + (i % 10);

                registry.TryUpdate("TwilioSms", enabled: enabled, priority: priority);
            }
            catch (Exception ex)
            {
                errors.Enqueue(ex);
            }
        });

        errors.Should().BeEmpty("registry updates should be thread-safe");

        var all = registry.GetAll();
        all.Count(p => p.Name == "TwilioSms").Should().Be(1);
        var twilio = all.Single(p => p.Name == "TwilioSms");
        twilio.Priority.Should().BeInRange(1, 10);
    }

    [Fact]
    public void ApplyOptions_and_TryUpdate_can_run_together_without_breaking_registry()
    {
        var registry = new ProviderRegistry([
            new TwilioSmsProvider(),
            new AwsSnsSmsProvider(),
            new EmailProvider()
        ]);

        var errors = new ConcurrentQueue<Exception>();
        var sw = Stopwatch.StartNew();
        var duration = TimeSpan.FromMilliseconds(250);

        Parallel.Invoke(
            () =>
            {
                var rnd = new Random(123);
                while (sw.Elapsed < duration)
                {
                    try
                    {
                        var smsP1 = 1 + rnd.Next(0, 5);
                        var smsP2 = 1 + rnd.Next(0, 5);
                        var emailP = 1 + rnd.Next(0, 5);

                        var options = new MessagingOptions
                        {
                            Channels = new Dictionary<string, List<ProviderOption>>
                            {
                                ["Sms"] =
                                [
                                    new ProviderOption { Name = "TwilioSms", Enabled = true, Priority = smsP1 },
                                    new ProviderOption { Name = "AwsSnsSms", Enabled = true, Priority = smsP2 }
                                ],
                                ["Email"] = [new ProviderOption { Name = "Email", Enabled = true, Priority = emailP }]
                            }
                        };

                        registry.ApplyOptions(options);
                    }
                    catch (Exception ex)
                    {
                        errors.Enqueue(ex);
                    }
                }
            },
            () =>
            {
                var toggle = false;
                var priority = 1;

                while (sw.Elapsed < duration)
                {
                    try
                    {
                        toggle = !toggle;
                        priority = priority == 5 ? 1 : priority + 1;
                        registry.TryUpdate("TwilioSms", enabled: toggle, priority: priority);
                    }
                    catch (Exception ex)
                    {
                        errors.Enqueue(ex);
                    }
                }
            }
        );

        errors.Should().BeEmpty("registry must remain consistent under concurrent reads and writes");

        var snapshot = registry.GetAll();
        snapshot.Count(p => p.Name == "TwilioSms").Should().Be(1);
        snapshot.Count(p => p.Name == "AwsSnsSms").Should().Be(1);
        snapshot.Count(p => p.Name == "Email").Should().Be(1);

        snapshot.Single(p => p.Name == "TwilioSms").Channel.Should().Be(ChannelType.Sms);
        snapshot.Single(p => p.Name == "AwsSnsSms").Channel.Should().Be(ChannelType.Sms);
        snapshot.Single(p => p.Name == "Email").Channel.Should().Be(ChannelType.Email);
    }
}