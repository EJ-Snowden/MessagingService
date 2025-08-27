using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface INotificationProvider
{
    bool Enabled { get; }
    int Priority { get; }
    bool CanHandle(ChannelType channel);
    Task<ProviderResult> SendAsync(Notification notification);
}