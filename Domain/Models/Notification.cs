using Domain.Enums;

namespace Domain.Entities;

public sealed class Notification(ChannelType channel, string recipient, string message, string? subject)
{
    private Notification() : this(default, string.Empty, string.Empty, null) { }
    
    public Guid Id { get; private set; } = Guid.NewGuid();
    public ChannelType Channel { get; private set; } = channel;
    public string Recipient { get; private set; } = recipient;
    public string Message { get; private set; } = message;
    public string? Subject { get; private set; } = subject;
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;
    public int Attempts { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public string? LastError { get; private set; }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        Attempts += 1;
        LastError = null;
        NextAttemptAt = null;
    }

    public void MarkDelayed(string error, DateTimeOffset nextAttemptAt)
    {
        Status = NotificationStatus.Delayed;
        Attempts += 1;
        LastError = error;
        NextAttemptAt = nextAttemptAt;
    }

    public void MarkFailed(string error)
    {
        Status = NotificationStatus.Failed;
        Attempts += 1;
        LastError = error;
        NextAttemptAt = null;
    }
}