namespace Infrastructure.Repositories;

public sealed class ProviderOption
{
    public string Name { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public int Priority { get; init; } = 1;
}