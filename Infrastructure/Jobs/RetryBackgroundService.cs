using Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs;

public sealed class RetryBackgroundService(
    IRetryQueue queue,
    INotificationRepository repo,
    IEnumerable<INotificationProvider> providers,
    IClock clock,
    ILogger<RetryBackgroundService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stopToken)
    {
        while (!stopToken.IsCancellationRequested)
        {
            try
            {
                var dueItems = await queue.DequeueDueAsync(clock.UtcNow, max: 50);

                foreach (var (notificationId, _) in dueItems)
                {
                    try
                    {
                        var notification = await repo.GetByIdAsync(notificationId);
                        if (notification is null)
                        {
                            continue;
                        }

                        var candidates = providers
                            .Where(p => p.Enabled && p.CanHandle(notification.Channel))
                            .OrderBy(p => p.Priority)
                            .ToArray();

                        if (candidates.Length == 0)
                        {
                            continue;
                        }

                        foreach (var provider in candidates)
                        {
                            var result = await provider.SendAsync(notification);
                            if (!result.Success) continue;

                            notification.MarkSent();
                            await repo.UpdateAsync(notification);

                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Error while retrying notification {Id}", notificationId);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Retry loop failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stopToken);
        }
    }
}