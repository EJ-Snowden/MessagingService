using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Repositories;

public sealed class InMemoryNotificationRepository : INotificationRepository
{
    private readonly Dictionary<Guid, Notification> _store = new();

    public Task AddAsync(Notification notification)
    {
        _store[notification.Id] = notification;
        return Task.CompletedTask;
    }

    public Task<Notification?> GetByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var n);
        return Task.FromResult(n);
    }

    public Task UpdateAsync(Notification notification)
    {
        _store[notification.Id] = notification;
        return Task.CompletedTask;
    }
}