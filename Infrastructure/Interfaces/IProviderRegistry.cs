using Infrastructure.Repositories;

namespace Infrastructure.Providers;

public interface IProviderRegistry
{
    IReadOnlyList<ProviderInfo> GetAll();
    bool TryUpdate(string name, bool? enabled, int? priority);
    void ApplyOptions(MessagingOptions options);
}