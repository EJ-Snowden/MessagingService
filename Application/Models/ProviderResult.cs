namespace Application.Models;

public sealed record ProviderResult(bool Success, string? Error = null, bool IsTransient = true);
