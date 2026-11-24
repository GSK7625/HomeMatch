using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints.Requests;

public sealed class UploadListingImageRequest
{
    public IFormFile? File { get; init; }
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }
}
