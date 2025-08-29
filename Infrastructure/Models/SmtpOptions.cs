namespace Infrastructure.Repositories;

public sealed class SmtpOptions
{
    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string From { get; init; } = "no-reply@example.com";
    public bool UseSsl { get; init; } = true;
}