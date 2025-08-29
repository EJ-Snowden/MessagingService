using Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs;

public sealed class RetryBackgroundService(IRetryQueue queue, INotificationRepository repo, IEnumerable<INotificationProvider> providers, IClock clock, ILogger<RetryBackgroundService> logger) : BackgroundService
{
    private const int MaxAttempts = 15;

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
                        if (notification is null) continue;

                        var candidates = providers
                            .Where(p => p.Enabled && p.CanHandle(notification.Channel))
                            .OrderBy(p => p.Priority)
                            .ToArray();

                        if (candidates.Length == 0) continue;

                        var sent = false;

                        foreach (var provider in candidates)
                        {
                            var result = await provider.SendAsync(notification);
                            if (!result.Success) continue;

                            notification.MarkSent();
                            await repo.UpdateAsync(notification);
                            sent = true;
                            break;
                        }

                        if (sent) continue;

                        if (notification.Attempts >= MaxAttempts - 1)
                        {
                            notification.MarkFailed("max attempts reached");
                            await repo.UpdateAsync(notification);
                            continue;
                        }

                        var next = clock.UtcNow.AddMinutes(5);
                        notification.MarkDelayed("retry failed", next);
                        await repo.UpdateAsync(notification);
                        await queue.EnqueueAsync(notification.Id, next);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Retry failed for {Id}", notificationId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Retry loop crashed");
            }

            await Task.Delay(TimeSpan.FromSeconds(300), stopToken);
        }
    }
}