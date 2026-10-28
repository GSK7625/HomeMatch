using System.Text.RegularExpressions;
using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Modules.Listings.Infrastructure.Persistence;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace HomeMatch.Modules.Listings.Application;

public sealed class ListingWriteService(ListingsDbContext db, IRoomListingReader rooms)
    : IListingWriteService
{
    public async Task<ListingResponse> CreateAsync(
        long landlordId, CreateListingRequest request, CancellationToken cancellationToken)
    {
        var room = await rooms.GetRoomAsync(request.RoomId, cancellationToken);
        if (room is null || room.LandlordId != landlordId)
            throw ApiException.NotFound("Room.NotFound", "Không tìm thấy phòng thuộc tài khoản này.");
        ValidateListing(request.Title, request.Slug, request.Description, request.Price,
            request.DepositAmount, request.MinLeaseMonths, request.HasCurfew, request.CurfewTime);
        ValidateCoordinatePair(request.Longitude, request.Latitude);
        await EnsureSlugAvailableAsync(request.Slug, null, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var listing = new Listing
        {
            RoomId = request.RoomId,
            LandlordId = landlordId,
            Title = request.Title.Trim(),
            Slug = request.Slug.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            DepositAmount = request.DepositAmount,
            MinLeaseMonths = request.MinLeaseMonths,
            AllowPets = request.AllowPets,
            HasCurfew = request.HasCurfew,
            CurfewTime = request.HasCurfew ? request.CurfewTime : null,
            Status = "DRAFT",
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Listings.Add(listing);
        if (request.Longitude is not null && request.Latitude is not null)
            db.ListingLocations.Add(new ListingLocation
            {
                Listing = listing,
                Coordinates = MakePoint(request.Longitude.Value, request.Latitude.Value)
            });
        await db.SaveChangesAsync(cancellationToken);
        return await ListingResponseMapper.ToResponseAsync(db, listing, cancellationToken);
    }

    public async Task<ListingResponse> UpdateAsync(
        long landlordId, long listingId, UpdateListingRequest request,
        CancellationToken cancellationToken)
    {
        var listing = await GetOwnedListingAsync(landlordId, listingId, cancellationToken);
        var title = request.Title ?? listing.Title;
        var slug = request.Slug ?? listing.Slug;
        var description = request.Description ?? listing.Description;
        var price = request.Price ?? listing.Price;
        var deposit = request.DepositAmount ?? listing.DepositAmount;
        var leaseMonths = request.MinLeaseMonths ?? listing.MinLeaseMonths;
        var hasCurfew = request.HasCurfew ?? listing.HasCurfew;
        var curfewTime = request.CurfewTime ?? listing.CurfewTime;
        ValidateListing(title, slug, description, price, deposit, leaseMonths, hasCurfew, curfewTime);
        ValidateCoordinatePair(request.Longitude, request.Latitude);
        if (slug != listing.Slug)
            await EnsureSlugAvailableAsync(slug, listingId, cancellationToken);
        listing.Title = title.Trim();
        listing.Slug = slug.Trim();
        listing.Description = description.Trim();
        listing.Price = price;
        listing.DepositAmount = deposit;
        listing.MinLeaseMonths = leaseMonths;
        listing.AllowPets = request.AllowPets ?? listing.AllowPets;
        listing.HasCurfew = hasCurfew;
        listing.CurfewTime = hasCurfew ? curfewTime : null;
        listing.UpdatedAt = DateTimeOffset.UtcNow;

        if (request.Longitude is not null && request.Latitude is not null)
        {
            var location = await db.ListingLocations.FindAsync([listingId], cancellationToken);
            if (location is null)
                db.ListingLocations.Add(new ListingLocation
                {
                    ListingId = listingId,
                    Coordinates = MakePoint(request.Longitude.Value, request.Latitude.Value)
                });
            else
                location.Coordinates = MakePoint(request.Longitude.Value, request.Latitude.Value);
        }
        await db.SaveChangesAsync(cancellationToken);
        return await ListingResponseMapper.ToResponseAsync(db, listing, cancellationToken);
    }

    public async Task<ListingResponse> PublishAsync(
        long landlordId, long listingId, CancellationToken cancellationToken)
    {
        var listing = await GetOwnedListingAsync(landlordId, listingId, cancellationToken);
        var room = await rooms.GetRoomAsync(listing.RoomId, cancellationToken);
        var location = await db.ListingLocations.AsNoTracking()
            .FirstOrDefaultAsync(item => item.ListingId == listingId, cancellationToken);
        var images = await db.ListingImages.AsNoTracking()
            .Where(item => item.ListingId == listingId).ToListAsync(cancellationToken);
        var hasRates = await rooms.HasElectricityAndWaterRatesAsync(
            listing.RoomId, BusinessDate.Today(), cancellationToken);
        var anotherPublished = await db.Listings.AnyAsync(item => item.RoomId == listing.RoomId
            && item.Id != listingId && item.Status == "PUBLISHED", cancellationToken);
        ListingPublicationPolicy.EnsureCanPublish(listing.Status, room, landlordId,
            location?.Coordinates is not null, images.Select(item => item.IsPrimary).ToArray(),
            hasRates, anotherPublished);

        listing.Status = "PUBLISHED";
        listing.PublishedAt = DateTimeOffset.UtcNow;
        listing.UpdatedAt = listing.PublishedAt.Value;
        await db.SaveChangesAsync(cancellationToken);
        return await ListingResponseMapper.ToResponseAsync(db, listing, cancellationToken);
    }

    public async Task<ListingResponse> HideAsync(
        long landlordId, long listingId, CancellationToken cancellationToken)
    {
        var listing = await GetOwnedListingAsync(landlordId, listingId, cancellationToken);
        if (listing.Status == "PUBLISHED")
        {
            listing.Status = "HIDDEN";
            listing.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (listing.Status != "HIDDEN")
            throw ApiException.Conflict("Listing.InvalidStatus", "Tin này chưa được đăng.");
        return await ListingResponseMapper.ToResponseAsync(db, listing, cancellationToken);
    }

    private async Task<Listing> GetOwnedListingAsync(
        long landlordId, long listingId, CancellationToken cancellationToken) =>
        await db.Listings.Include(item => item.Images).FirstOrDefaultAsync(item =>
            item.Id == listingId && item.LandlordId == landlordId, cancellationToken)
        ?? throw ApiException.NotFound("Listing.NotFound", "Không tìm thấy tin thuộc tài khoản này.");

    private async Task EnsureSlugAvailableAsync(
        string slug, long? exceptId, CancellationToken cancellationToken)
    {
        if (await db.Listings.AnyAsync(item => item.Slug == slug.Trim()
            && item.Id != exceptId, cancellationToken))
            throw ApiException.Conflict("Listing.DuplicateSlug", "Đường dẫn tin đã tồn tại.");
    }

    private static void ValidateListing(
        string title, string slug, string description, long price, long deposit,
        int leaseMonths, bool hasCurfew, TimeOnly? curfewTime)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 255
            || string.IsNullOrWhiteSpace(slug) || slug.Length > 255
            || !Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)
            || string.IsNullOrWhiteSpace(description) || price <= 0
            || deposit < 0 || leaseMonths <= 0 || hasCurfew && curfewTime is null)
            throw ApiException.BadRequest("Listing.InvalidInput", "Thông tin tin đăng không hợp lệ.");
    }

    private static void ValidateCoordinatePair(double? longitude, double? latitude)
    {
        if (longitude.HasValue != latitude.HasValue
            || longitude is < -180 or > 180 || latitude is < -90 or > 90
            || longitude.HasValue && !double.IsFinite(longitude.Value)
            || latitude.HasValue && !double.IsFinite(latitude.Value))
            throw ApiException.BadRequest("Listing.InvalidCoordinates", "Tọa độ phải có cả kinh độ và vĩ độ hợp lệ.");
    }

    private static Point MakePoint(double longitude, double latitude) =>
        new(longitude, latitude) { SRID = 4326 };
}
