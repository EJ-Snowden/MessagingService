using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Providers;

public sealed class EmailProvider : INotificationProvider
{
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 1;

    public bool CanHandle(ChannelType channel) => channel == ChannelType.Email;

    public Task<ProviderResult> SendAsync(Notification notification) => Task.FromResult(new ProviderResult(true));
}
