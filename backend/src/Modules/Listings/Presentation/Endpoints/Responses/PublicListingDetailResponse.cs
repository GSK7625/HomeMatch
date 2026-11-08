namespace HomeMatch.Modules.Listings.Presentation.Endpoints.Responses;

public sealed class PublicListingDetailResponse(
    ListingResponse listing,
    PublicListingRoomResponse room,
    PublicListingPropertyResponse property,
    IReadOnlyList<PublicListingTariffResponse> tariffs)
{
    public long Id { get; } = listing.Id;
    public long RoomId { get; } = listing.RoomId;
    public long LandlordId { get; } = listing.LandlordId;
    public string Title { get; } = listing.Title;
    public string Slug { get; } = listing.Slug;
    public string Description { get; } = listing.Description;
    public long Price { get; } = listing.Price;
    public long DepositAmount { get; } = listing.DepositAmount;
    public int MinLeaseMonths { get; } = listing.MinLeaseMonths;
    public bool AllowPets { get; } = listing.AllowPets;
    public bool HasCurfew { get; } = listing.HasCurfew;
    public TimeOnly? CurfewTime { get; } = listing.CurfewTime;
    public string Status { get; } = listing.Status;
    public bool IsFeatured { get; } = listing.IsFeatured;
    public long ViewCount { get; } = listing.ViewCount;
    public double? Longitude { get; } = listing.Longitude;
    public double? Latitude { get; } = listing.Latitude;
    public IReadOnlyList<ListingImageResponse> Images { get; } = listing.Images;
    public PublicListingRoomResponse Room { get; } = room;
    public PublicListingPropertyResponse Property { get; } = property;
    public IReadOnlyList<PublicListingTariffResponse> Tariffs { get; } = tariffs;
}

public sealed record PublicListingRoomResponse(
    long Id,
    string RoomNumber,
    int Floor,
    decimal AreaSqm,
    string RoomType,
    int MaxOccupants,
    string Status,
    DateOnly AvailableFrom,
    IReadOnlyList<PublicListingAmenityResponse> Amenities);

public sealed record PublicListingPropertyResponse(
    long Id,
    string Name,
    string PropertyType,
    string ProvinceCode,
    string ProvinceName,
    string WardCode,
    string WardName,
    string? DistrictName,
    string AddressDetail);

public sealed record PublicListingAmenityResponse(long Id, string Code, string Name);

public sealed record PublicListingTariffResponse(long Id, string TariffType, string CalcMethod, long UnitPrice);
