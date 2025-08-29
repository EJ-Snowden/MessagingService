using Application.Interfaces;
using Application.Models;
using Domain.Entities;

namespace Application.UseCases.Handlers;

public sealed class SendNotificationHandler(IEnumerable<INotificationProvider> providers, INotificationRepository repo, IRetryQueue queue, IClock clock)
{
    private readonly IEnumerable<INotificationProvider> _providers = providers ?? throw new ArgumentNullException(nameof(providers));
    private readonly INotificationRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    private readonly IRetryQueue _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public async Task<Guid> HandleAsync(SendNotificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var notification = new Notification(request.Channel, request.Recipient, request.Message, request.Subject);
        await _repo.AddAsync(notification);

        var candidates = _providers
            .Where(p => p.Enabled && p.CanHandle(notification.Channel))
            .OrderBy(p => p.Priority)
            .ToArray();

        foreach (var provider in candidates)
        {
            ProviderResult result = await provider.SendAsync(notification);
            if (!result.Success) continue;
            notification.MarkSent();
            await _repo.UpdateAsync(notification);
            return notification.Id;
        }

        var next = _clock.UtcNow.AddMinutes(1);
        notification.MarkDelayed("All providers failed", next);
        await _repo.UpdateAsync(notification);
        await _queue.EnqueueAsync(notification.Id, next);

        return notification.Id;
    }
}