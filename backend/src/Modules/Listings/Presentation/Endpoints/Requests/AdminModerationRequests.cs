using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints.Requests;

public sealed record AdminHideListingRequest
{
    [StringLength(500)]
    public string? Reason { get; init; }
}

public sealed record CreateViolationReportRequest
{
    [Required]
    [RegularExpression("^(LISTING|LANDLORD)$")]
    public string TargetType { get; init; } = "LISTING";

    [Range(1, long.MaxValue)]
    public long TargetId { get; init; }

    public long? ListingId { get; init; }

    [Required]
    [StringLength(255, MinimumLength = 3)]
    public string Reason { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    [StringLength(500)]
    public string? EvidenceUrl { get; init; }
}

public sealed record ResolveViolationReportRequest
{
    [Required]
    [RegularExpression("^(RESOLVED|DISMISSED)$")]
    public string Resolution { get; init; } = "RESOLVED";

    [StringLength(1000)]
    public string? AdminNote { get; init; }

    public bool HideListing { get; init; }
}
