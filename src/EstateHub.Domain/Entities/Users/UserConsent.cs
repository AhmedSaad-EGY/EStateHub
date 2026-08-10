namespace EstateHub.Domain.Entities.Users;

public class UserConsent
{
    public Guid Id { get; set; }
    public Guid ApplicationUserId { get; set; }
    public string PolicyType { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = string.Empty;
    public DateTimeOffset AcceptedAt { get; set; }
    public string ConsentSource { get; set; } = string.Empty;
}
