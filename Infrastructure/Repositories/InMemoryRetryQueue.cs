using Application.Interfaces;

namespace Infrastructure.Repositories;

public sealed class InMemoryRetryQueue : IRetryQueue
{
    private readonly List<(Guid Id, DateTimeOffset Due)> _items = [];

    public Task EnqueueAsync(Guid notificationId, DateTimeOffset nextAttemptAt)
    {
        _items.Add((notificationId, nextAttemptAt));

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<(Guid Id, DateTimeOffset Due)>> DequeueDueAsync(DateTimeOffset now, int max)
    {
        var dueItems = _items
            .Where(item => item.Due <= now)
            .Take(max)
            .ToList();

        foreach (var item in dueItems)
        {
            _items.Remove(item);
        }

        return Task.FromResult<IReadOnlyList<(Guid, DateTimeOffset)>>(dueItems);
    }

    public Task<int> CountAsync()
    {
        return Task.FromResult(_items.Count);
    }
}