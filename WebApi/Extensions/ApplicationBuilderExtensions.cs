using Infrastructure.Providers;
using Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace MessagingService.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseProviderConfig(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<MessagingOptions>>().Value;
        var registry = app.ApplicationServices.GetRequiredService<IProviderRegistry>();
        registry.ApplyOptions(options);
        return app;
    }
}