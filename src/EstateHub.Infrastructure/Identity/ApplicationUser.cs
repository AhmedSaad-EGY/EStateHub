using EstateHub.Infrastructure.Identity.Enums;
using Microsoft.AspNetCore.Identity;

namespace EstateHub.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUserAccountStatus AccountStatus { get; set; }
    public string PreferredLanguage { get; set; } = string.Empty;
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
