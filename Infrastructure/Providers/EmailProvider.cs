using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Providers;

public sealed class EmailProvider(IOptions<SmtpOptions> options) : INotificationProvider
{
    private readonly SmtpOptions _options = options.Value;
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 1;

    public bool CanHandle(ChannelType channel) => channel == ChannelType.Email;

    public async Task<ProviderResult> SendAsync(Notification n)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(n.Recipient))
                return new ProviderResult(false, "empty recipient", IsTransient: false);

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_options.From));
            message.To.Add(MailboxAddress.Parse(n.Recipient));
            message.Subject = n.Subject ?? "(no subject)";
            message.Body = new TextPart("plain") { Text = n.Message };

            using var client = new SmtpClient();

            await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls);
            if (!string.IsNullOrWhiteSpace(_options.Username))
                await client.AuthenticateAsync(_options.Username, _options.Password);


            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return new ProviderResult(true);
        }
        catch (FormatException fe)
        {
            return new ProviderResult(false, fe.Message, IsTransient: false);
        }
        catch (Exception ex)
        {
            return new ProviderResult(false, ex.Message, IsTransient: true);
        }
    }
}
