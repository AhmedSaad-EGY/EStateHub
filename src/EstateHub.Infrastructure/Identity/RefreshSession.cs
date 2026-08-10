namespace EstateHub.Infrastructure.Identity;

public class RefreshSession
{
    public Guid Id { get; set; }
    public Guid ApplicationUserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool RememberMe { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public ApplicationUser ApplicationUser { get; set; } = null!;
}
