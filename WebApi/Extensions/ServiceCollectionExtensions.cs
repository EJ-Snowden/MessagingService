using System.Text.Json.Serialization;
using Application.Interfaces;
using Application.UseCases.Handlers;
using Infrastructure;
using Infrastructure.Jobs;
using Infrastructure.Providers;
using Infrastructure.Repositories;
using Microsoft.OpenApi.Models;

namespace MessagingService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IMvcBuilder AddJsonEnumSupport(this IMvcBuilder mvc)
    {
        return mvc.AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    }

    public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Messaging Service",
                Version = "v1",
                Description = "Simple notification API with provider failover and retry"
            });
        });

        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<INotificationRepository, InMemoryNotificationRepository>();
        services.AddSingleton<IRetryQueue, InMemoryRetryQueue>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<INotificationProvider>(new TwilioSmsProvider { Enabled = true, Priority = 1 });
        services.AddSingleton<INotificationProvider>(new AwsSnsSmsProvider { Enabled = true, Priority = 2 });
        services.AddSingleton<INotificationProvider>(new EmailProvider     { Enabled = true, Priority = 1 });

        services.AddSingleton<SendNotificationHandler>();

        services.AddHostedService<RetryBackgroundService>();

        return services;
    }
}