using Application.Interfaces;
using Application.UseCases;
using Application.UseCases.Handlers;
using MessagingService.Models.Requests;
using MessagingService.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace MessagingService.Controllers;

[ApiController]
[Route("notifications")]
public sealed class NotificationsController(SendNotificationHandler handler, INotificationRepository repo) : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendNotificationDto body)
    {
        var id = await handler.HandleAsync(new SendNotificationRequest(body.Channel, body.Recipient, body.Message, body.Subject));
        return AcceptedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var notification = await repo.GetByIdAsync(id);
        if (notification is null) return NotFound();

        var dto = new NotificationStatusDto(notification.Id, notification.Channel, notification.Status, notification.Attempts, notification.CreatedAt, notification.NextAttemptAt, notification.LastError);
        return Ok(dto);
    }
}