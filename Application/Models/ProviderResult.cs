namespace Application.Models;

public abstract record ProviderResult(bool Success, string? Error = null, bool IsTransient = true);
