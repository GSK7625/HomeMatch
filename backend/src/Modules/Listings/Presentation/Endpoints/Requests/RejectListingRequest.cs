using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints.Requests;

public sealed class RejectListingRequest
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Reason { get; init; } = string.Empty;
}
