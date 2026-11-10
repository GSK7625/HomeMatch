using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints.Requests;

/// <summary>Request DTO để cập nhật một phần tin đăng (PATCH)</summary>
public sealed class UpdateListingRequest
{
    [StringLength(255, MinimumLength = 1)]
    public string? Title { get; init; }

    public string? Description { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long? Price { get; init; }

    [Range(typeof(long), "0", "9223372036854775807")]
    public long? DepositAmount { get; init; }

    [Range(1, int.MaxValue)]
    public int? MinLeaseMonths { get; init; }

    [StringLength(2000)]
    public string? HouseRules { get; init; }

}
