using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Application.Features.ListingImages.AddListingImage;
using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Shared.Common;
using Microsoft.Extensions.Logging;
using System.Buffers.Binary;

namespace HomeMatch.Modules.Listings.Application.Features.ListingImages.UploadListingImage;

public sealed class UploadListingImageHandler(
    IListingRepository listings, IListingImageStorage storage, AddListingImageHandler addImage,
    ILogger<UploadListingImageHandler> logger)
{
    public const int MaxFileBytes = 5 * 1024 * 1024;
    public const int MaxVideoBytes = 50 * 1024 * 1024;
    public const int MaxRequestBytes = MaxVideoBytes + 64 * 1024;

    public async Task<Result<ListingImageDto>> HandleAsync(UploadListingImageCommand command, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(command.FileName).ToLowerInvariant();
        var expectedType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            _ => null
        };
        var maxBytes = expectedType == "video/mp4" ? MaxVideoBytes : MaxFileBytes;
        if (command.Length <= 0 || command.Length > maxBytes || command.SortOrder < 0
            || expectedType is null || command.ContentType != expectedType)
            return Result.Failure<ListingImageDto>(ListingsErrors.InvalidImageFile);

        var listing = await listings.GetOwnedAsync(command.LandlordId, command.ListingId, cancellationToken);
        if (listing is null) return Result.Failure<ListingImageDto>(ListingsErrors.ListingNotFound);
        if (!ListingPublicationPolicy.CanEdit(listing.Status))
            return Result.Failure<ListingImageDto>(ListingsErrors.EditLocked);

        // Buffer to a temporary file so concurrent video uploads do not consume 50 MB of RAM each.
        await using var content = new FileStream(Path.Combine(Path.GetTempPath(), $"homematch-media-{Guid.NewGuid():N}"),
            FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
        var buffer = new byte[81920];
        int read;
        while ((read = await command.Content.ReadAsync(buffer.AsMemory(0,
                   (int)Math.Min(buffer.Length, maxBytes + 1L - content.Length)), cancellationToken)) > 0)
        {
            await content.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            if (content.Length > maxBytes)
                return Result.Failure<ListingImageDto>(ListingsErrors.InvalidImageFile);
        }
        content.Position = 0;
        var header = new byte[4096];
        var headerLength = await content.ReadAtLeastAsync(header, (int)Math.Min(header.Length, content.Length), false, cancellationToken);
        if (content.Length != command.Length || !HasSignature(header.AsSpan(0, headerLength), expectedType))
            return Result.Failure<ListingImageDto>(ListingsErrors.InvalidImageFile);
        content.Position = 0;

        var objectKey = $"listings/{command.ListingId}/{Guid.NewGuid():N}{(extension == ".jpeg" ? ".jpg" : extension)}";
        var imageUrl = await storage.UploadAsync(objectKey, content, expectedType, cancellationToken);
        Result<ListingImageDto> result;
        try
        {
            result = await addImage.HandleAsync(new(command.LandlordId, command.ListingId, imageUrl,
                command.IsPrimary, command.SortOrder, expectedType == "video/mp4" ? "VIDEO" : "IMAGE"), cancellationToken);
        }
        catch (Exception exception)
        {
            // A failed commit acknowledgement does not prove the transaction was rolled back.
            logger.LogWarning(exception, "Kiểm tra DB trước khi dọn ảnh R2 có kết quả lưu chưa xác định: {ObjectKey}", objectKey);
            throw;
        }
        if (result.IsFailure)
        {
            try
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await storage.DeleteAsync(objectKey, cleanup.Token);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Không thể dọn ảnh R2 chưa được lưu: {ObjectKey}", objectKey);
            }
        }
        return result;
    }

    private static bool HasSignature(ReadOnlySpan<byte> bytes, string contentType) => contentType switch
    {
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
        "image/png" => bytes.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/webp" => bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8)
            && bytes.Slice(8, 4).SequenceEqual("WEBP"u8),
        "video/mp4" => HasMp4Signature(bytes),
        _ => false
    };

    private static bool HasMp4Signature(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 16 || !bytes.Slice(4, 4).SequenceEqual("ftyp"u8)) return false;
        var boxSize = BinaryPrimitives.ReadUInt32BigEndian(bytes);
        if (boxSize < 16 || boxSize > bytes.Length || boxSize % 4 != 0) return false;
        // Check the major/compatible brands; QuickTime and image-only HEIF/AVIF are not MP4 video.
        for (var offset = 8; offset + 4 <= boxSize; offset += 4)
        {
            if (offset == 12) continue; // Minor version is a number, not a brand.
            var brand = bytes.Slice(offset, 4);
            if (brand.SequenceEqual("isom"u8) || brand.SequenceEqual("iso2"u8)
                || brand.SequenceEqual("mp41"u8) || brand.SequenceEqual("mp42"u8)
                || brand.SequenceEqual("avc1"u8)) return true;
        }
        return false;
    }
}
