using System.Text.RegularExpressions;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application;

public static class ListingsValidation
{
    private static readonly Regex SlugRegex = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static Error? ValidateListing(
        string title, string description, long price, long deposit,
        int leaseMonths, string? houseRules = null)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 255
            || string.IsNullOrWhiteSpace(description) || price <= 0
            || deposit < 0 || leaseMonths <= 0 || (houseRules is not null && houseRules.Length > 2000))
        {
            return ListingsErrors.InvalidInput;
        }

        return null;
    }

    public static Error? ValidateCoordinates(double? longitude, double? latitude)
    {
        if (longitude.HasValue != latitude.HasValue)
            return ListingsErrors.InvalidCoordinates;

        if (longitude.HasValue)
        {
            if (longitude.Value is < -180 or > 180 || latitude!.Value is < -90 or > 90
                || !double.IsFinite(longitude.Value) || !double.IsFinite(latitude.Value))
            {
                return ListingsErrors.InvalidCoordinates;
            }
        }

        return null;
    }

    public static Error? ValidateMedia(string? mediaType, string? thumbnailUrl, int? durationSeconds, int? sortOrder)
    {
        if (mediaType is not null && mediaType is not ("IMAGE" or "VIDEO" or "VIRTUAL_TOUR_360")
            || durationSeconds is <= 0 || sortOrder is < 0
            || thumbnailUrl is not null && (thumbnailUrl.Length > 500
                || !Uri.TryCreate(thumbnailUrl, UriKind.Absolute, out var thumbnail)
                || thumbnail.Scheme is not ("https" or "http")))
            return ListingsErrors.InvalidImage;
        return null;
    }
}

