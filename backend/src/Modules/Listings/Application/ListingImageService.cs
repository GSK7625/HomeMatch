using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Modules.Listings.Infrastructure.Persistence;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace HomeMatch.Modules.Listings.Application;

public sealed class ListingImageService(ListingsDbContext db, IHostEnvironment environment)
    : IListingImageService
{
    public async Task<ListingImageResponse> AddImageAsync(
        long landlordId, long listingId, AddListingImageRequest request,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            throw new ApiException(501, "Listing.ImageUploadUnavailable",
                "Cần tích hợp dịch vụ lưu ảnh trước khi thêm ảnh ở môi trường này.");
        var listing = await GetOwnedListingAsync(landlordId, listingId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.ImageUrl) || request.ImageUrl.Length > 500
            || !Uri.TryCreate(request.ImageUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || request.SortOrder < 0)
            throw ApiException.BadRequest("Listing.InvalidImage", "URL ảnh HTTPS hoặc thứ tự ảnh không hợp lệ.");

        var hasImage = await db.ListingImages.AnyAsync(item => item.ListingId == listingId,
            cancellationToken);
        var primary = request.IsPrimary || !hasImage;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (primary)
            await db.ListingImages.Where(item => item.ListingId == listingId && item.IsPrimary)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsPrimary, false),
                    cancellationToken);
        var image = new ListingImage
        {
            ListingId = listingId,
            ImageUrl = request.ImageUrl.Trim(),
            IsPrimary = primary,
            SortOrder = request.SortOrder,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ListingImages.Add(image);
        listing.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ListingResponseMapper.ToImageResponse(image);
    }

    public async Task<ListingImageResponse> UpdateImageAsync(
        long landlordId, long listingId, long imageId, UpdateListingImageRequest request,
        CancellationToken cancellationToken)
    {
        var listing = await GetOwnedListingAsync(landlordId, listingId, cancellationToken);
        var image = await db.ListingImages.FirstOrDefaultAsync(item => item.Id == imageId
            && item.ListingId == listingId, cancellationToken)
            ?? throw ApiException.NotFound("Listing.ImageNotFound", "Không tìm thấy ảnh của tin.");
        if (request.SortOrder is < 0 || request.IsPrimary == false && image.IsPrimary)
            throw ApiException.BadRequest("Listing.InvalidImage", "Không thể bỏ ảnh đại diện nếu chưa chọn ảnh khác.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (request.IsPrimary == true && !image.IsPrimary)
        {
            await db.ListingImages.Where(item => item.ListingId == listingId && item.IsPrimary)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsPrimary, false),
                    cancellationToken);
            image.IsPrimary = true;
        }
        image.SortOrder = request.SortOrder ?? image.SortOrder;
        listing.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ListingResponseMapper.ToImageResponse(image);
    }

    public async Task DeleteImageAsync(
        long landlordId, long listingId, long imageId, CancellationToken cancellationToken)
    {
        var listing = await GetOwnedListingAsync(landlordId, listingId, cancellationToken);
        var images = await db.ListingImages.Where(item => item.ListingId == listingId)
            .OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
        var image = images.FirstOrDefault(item => item.Id == imageId)
            ?? throw ApiException.NotFound("Listing.ImageNotFound", "Không tìm thấy ảnh của tin.");
        if (listing.Status == "PUBLISHED" && images.Count == 1)
            throw ApiException.Conflict("Listing.LastImage", "Ẩn tin trước khi xóa ảnh cuối cùng.");
        db.ListingImages.Remove(image);
        if (image.IsPrimary && images.Count > 1)
            images.First(item => item.Id != imageId).IsPrimary = true;
        listing.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Listing> GetOwnedListingAsync(
        long landlordId, long listingId, CancellationToken cancellationToken) =>
        await db.Listings.FirstOrDefaultAsync(item => item.Id == listingId
            && item.LandlordId == landlordId, cancellationToken)
        ?? throw ApiException.NotFound("Listing.NotFound", "Không tìm thấy tin thuộc tài khoản này.");
}
