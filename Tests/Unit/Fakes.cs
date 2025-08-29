using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Tests.Unit;

public sealed class FakeClock(DateTime now) : IClock
{
    public DateTime UtcNow { get; private set; } = now;

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}

public sealed class StubNotificationRepository : INotificationRepository
{
    private readonly Dictionary<Guid, Notification> _store = new();

    public Task AddAsync(Notification notification)
    {
        _store[notification.Id] = notification;
        return Task.CompletedTask;
    }

    public Task<Notification?> GetByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var notification);
        return Task.FromResult(notification);
    }

    public Task UpdateAsync(Notification notification)
    {
        _store[notification.Id] = notification;
        return Task.CompletedTask;
    }

    public Notification GetOrThrow(Guid id) => _store[id];
}

public sealed class StubRetryQueue : IRetryQueue
{
    private readonly List<(Guid Id, DateTime Due)> _items = new();

    public Task EnqueueAsync(Guid notificationId, DateTime nextAttemptAt)
    {
        _items.Add((notificationId, nextAttemptAt));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<(Guid Id, DateTime Due)>> DequeueDueAsync(DateTime now, int max)
    {
        var due = _items
            .Where(x => x.Due <= now)
            .Take(max)
            .ToList();

        foreach (var item in due)
            _items.Remove(item);

        return Task.FromResult<IReadOnlyList<(Guid, DateTime)>>(due);
    }

    public Task<int> CountAsync() => Task.FromResult(_items.Count);

    public (Guid Id, DateTime Due)? LastEnqueued => _items.Count == 0 ? null : _items[^1];
}

public sealed class FakeProviderSuccess(ChannelType channel, int priority = 1, bool enabled = true) : INotificationProvider
{
    public bool Enabled { get; set; } = enabled;
    public int Priority { get; set; } = priority;

    public bool CanHandle(ChannelType channel1) => channel1 == channel;

    public Task<ProviderResult> SendAsync(Notification notification) => Task.FromResult(new ProviderResult(true));
}

public sealed class FakeProviderFail(ChannelType channel, string error, bool isTransient, int priority = 1, bool enabled = true) : INotificationProvider
{
    public bool Enabled { get; set; } = enabled;
    public int Priority { get; set; } = priority;

    public bool CanHandle(ChannelType channel1) => channel1 == channel;

    public Task<ProviderResult> SendAsync(Notification notification) => Task.FromResult(new ProviderResult(false, error, isTransient));
}