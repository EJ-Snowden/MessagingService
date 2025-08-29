using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Jobs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tests.Integration.Support;

public abstract class TestAppFactoryBase : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            Remove(services, d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(RetryBackgroundService));

            Remove(services, d => d.ServiceType == typeof(INotificationProvider));

            ConfigureProviders(services);
        });
    }

    protected abstract void ConfigureProviders(IServiceCollection services);

    private static void Remove(IServiceCollection services, Func<ServiceDescriptor, bool> predicate)
    {
        var matches = services.Where(predicate).ToList();
        foreach (var d in matches) services.Remove(d);
    }

    protected sealed class TestProvider(ChannelType channel, bool succeed, string error = "down", bool isTransient = true) : INotificationProvider
    {
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; } = 1;

        public bool CanHandle(ChannelType channel1) => channel1 == channel;

        public Task<ProviderResult> SendAsync(Notification n) => Task.FromResult(succeed ? new ProviderResult(true) : new ProviderResult(false, error, isTransient));
    }
}

public sealed class SuccessAppFactory : TestAppFactoryBase
{
    protected override void ConfigureProviders(IServiceCollection services)
    {
        services.AddSingleton<INotificationProvider>(new TestProvider(ChannelType.Email, succeed: true) { Priority = 1 });
    }
}

public sealed class AllFailAppFactory : TestAppFactoryBase
{
    protected override void ConfigureProviders(IServiceCollection services)
    {
        services.AddSingleton<INotificationProvider>(new TestProvider(ChannelType.Email, succeed: false, error: "smtp down", isTransient: true) { Priority = 1 });
    }
}