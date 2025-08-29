using Application.Interfaces;

namespace Infrastructure.Repositories;

public sealed class InMemoryRetryQueue : IRetryQueue
{
    private readonly List<(Guid Id, DateTime Due)> _items = [];
    private readonly Lock _gate = new();

    public Task EnqueueAsync(Guid notificationId, DateTime nextAttemptAt)
    {
        lock (_gate)
        {
            _items.Add((notificationId, nextAttemptAt));
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<(Guid Id, DateTime Due)>> DequeueDueAsync(DateTime now, int max)
    {
        List<(Guid Id, DateTime Due)> due;
        lock (_gate)
        {
            due = _items.Where(x => x.Due <= now).Take(max).ToList();
            foreach (var item in due) _items.Remove(item);
        }
        return Task.FromResult<IReadOnlyList<(Guid, DateTime)>>(due);
    }

    public Task<int> CountAsync()
    {
        lock (_gate) return Task.FromResult(_items.Count);
    }
}