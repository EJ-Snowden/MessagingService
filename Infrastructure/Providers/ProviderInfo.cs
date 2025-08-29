using Domain.Enums;

namespace Infrastructure.Repositories;

public sealed record ProviderInfo(string Name, ChannelType Channel, bool Enabled, int Priority);
