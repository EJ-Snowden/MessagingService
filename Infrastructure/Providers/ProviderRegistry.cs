using System.Collections.Concurrent;
using Application.Interfaces;
using Domain.Enums;
using Infrastructure.Repositories;

namespace Infrastructure.Providers;

public sealed class ProviderRegistry : IProviderRegistry
{
    private readonly ConcurrentDictionary<string, INotificationProvider> _providers 
        = new(StringComparer.OrdinalIgnoreCase);

    public ProviderRegistry(IEnumerable<INotificationProvider> providers)
    {
        foreach (var provider in providers)
        {
            var name = GetName(provider);
            _providers[name] = provider;
        }
    }

    public IReadOnlyList<ProviderInfo> GetAll()
    {
        return _providers
            .Select(kv => new ProviderInfo(
                Name: kv.Key,
                Channel: DetectChannel(kv.Value),
                Enabled: kv.Value.Enabled,
                Priority: kv.Value.Priority))
            .OrderBy(p => p.Channel)
            .ThenBy(p => p.Priority)
            .ToList();
    }

    public bool TryUpdate(string name, bool? enabled, int? priority)
    {
        if (!_providers.TryGetValue(name, out var provider))
            return false;

        if (enabled.HasValue)  provider.Enabled  = enabled.Value;
        if (priority.HasValue) provider.Priority = priority.Value;

        return true;
    }

    public void ApplyOptions(MessagingOptions? options)
    {
        if (options?.Channels is null) return;

        foreach (var (channelKey, configList) in options.Channels)
        {
            var channel = ParseChannel(channelKey);

            foreach (var config in configList)
            {
                if (!_providers.TryGetValue(config.Name, out var provider))
                    continue;

                if (!provider.CanHandle(channel))
                    continue;

                provider.Enabled  = config.Enabled;
                provider.Priority = config.Priority;
            }
        }
    }


    private static string GetName(INotificationProvider provider)
    {
        var typeName = provider.GetType().Name;
        return typeName.EndsWith("Provider", StringComparison.Ordinal) ? typeName[..^"Provider".Length] : typeName;
    }

    private static ChannelType DetectChannel(INotificationProvider provider)
    {
        if (provider.CanHandle(ChannelType.Sms)) return ChannelType.Sms;
        if (provider.CanHandle(ChannelType.Email)) return ChannelType.Email;
        if (provider.CanHandle(ChannelType.Push)) return ChannelType.Push;
        return ChannelType.Sms;
    }

    private static ChannelType ParseChannel(string key) => Enum.TryParse<ChannelType>(key, ignoreCase: true, out var c) ? c : ChannelType.Sms;
}