using Domain.Enums;

namespace MessagingService.Models.Responses;

public sealed record NotificationStatusDto(
    Guid Id,
    ChannelType Channel,
    NotificationStatus Status,
    int Attempts,
    DateTimeOffset CreatedAt,
    DateTimeOffset? NextAttemptAt,
    string? LastError
);