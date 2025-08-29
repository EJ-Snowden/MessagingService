namespace Application.Interfaces;

public interface IRetryQueue
{
    Task EnqueueAsync(Guid notificationId, DateTime nextAttemptAt);
    Task<IReadOnlyList<(Guid Id, DateTime Due)>> DequeueDueAsync(DateTime now, int max);
    Task<int> CountAsync();
}