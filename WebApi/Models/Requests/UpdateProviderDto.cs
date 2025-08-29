namespace MessagingService.Models.Requests;

public sealed class UpdateProviderDto
{
    public bool? Enabled { get; init; }
    public int? Priority { get; init; }
}