namespace HomeMatch.Modules.Listings.Application.Interfaces;

public interface IListingImageStorage
{
    Task<string> UploadAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
