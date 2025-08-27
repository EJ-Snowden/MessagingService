using Domain.Entities;

namespace Application.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<Notification?> GetByIdAsync(Guid id);
    Task UpdateAsync(Notification notification);
}