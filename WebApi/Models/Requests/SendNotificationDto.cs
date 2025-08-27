using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace MessagingService.Models.Requests;

public sealed class SendNotificationDto
{
    [Required]
    public ChannelType Channel { get; init; }

    [Required, MinLength(3)]
    public string Recipient { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string Message { get; init; } = string.Empty;

    public string? Subject { get; init; }
}