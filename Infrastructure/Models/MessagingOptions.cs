namespace Infrastructure.Repositories;

public class MessagingOptions
{
    public Dictionary<string, List<ProviderOption>> Channels { get; init; } = new();
}