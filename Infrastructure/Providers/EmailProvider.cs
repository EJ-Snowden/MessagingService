using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Providers;

public sealed class EmailProvider : INotificationProvider
{
    public bool Enabled { get; init; } = true;
    public int Priority { get; init; } = 1;

    public bool CanHandle(ChannelType channel) => channel == ChannelType.Email;

    public Task<ProviderResult> SendAsync(Notification notification)
    {
        return Task.FromResult(new ProviderResult(true));
    }
}
