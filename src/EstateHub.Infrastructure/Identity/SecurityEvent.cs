namespace EstateHub.Infrastructure.Identity;

public class SecurityEvent
{
    public Guid Id { get; set; }
    public Guid? ApplicationUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public string? SourceIpHash { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }
}
