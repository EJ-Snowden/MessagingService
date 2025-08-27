using Domain.Enums;

namespace Application.UseCases;

public sealed record SendNotificationRequest(
    ChannelType Channel,
    string Recipient,
    string Message,
    string? Subject
);