namespace Application.Interfaces;

public interface IRetryQueue
{
    Task EnqueueAsync(Guid notificationId, DateTimeOffset nextAttemptAt);
    Task<IReadOnlyList<(Guid Id, DateTimeOffset Due)>> DequeueDueAsync(DateTimeOffset now, int max);
    Task<int> CountAsync();
}