using Microsoft.AspNetCore.Identity;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Domain.Enums;

namespace HomeMatch.Modules.Identity.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<long>
{
    public UserStatus Status { get; set; } = UserStatus.Active;
    public int SecurityVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual UserProfile? Profile { get; set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
