using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints.Requests;

/// <summary>Request DTO để tạo mới tin đăng</summary>
public sealed class CreateListingRequest
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long RoomId { get; init; }

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required]
    public string Description { get; init; } = string.Empty;

    [Range(typeof(long), "1", "9223372036854775807")]
    public long Price { get; init; }

    [Range(typeof(long), "0", "9223372036854775807")]
    public long DepositAmount { get; init; }

    [Range(1, int.MaxValue)]
    public int MinLeaseMonths { get; init; }

    [StringLength(2000)]
    public string? HouseRules { get; init; }

}
