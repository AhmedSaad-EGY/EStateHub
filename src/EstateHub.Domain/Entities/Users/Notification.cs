namespace EstateHub.Domain.Entities.Users;

public class Notification
{
    public Guid Id { get; set; }
    public Guid RecipientApplicationUserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
}
