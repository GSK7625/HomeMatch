using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using HomeMatch.Modules.Listings.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Listings.Infrastructure.Services;

public sealed class R2ImageStorage : IListingImageStorage, IDisposable
{
    private readonly ListingR2Settings _settings;
    private readonly IAmazonS3 _client;

    public R2ImageStorage(IOptions<ListingR2Settings> options)
    {
        _settings = options.Value;
        if (string.IsNullOrWhiteSpace(_settings.AccountId)
            || string.IsNullOrWhiteSpace(_settings.AccessKeyId)
            || string.IsNullOrWhiteSpace(_settings.SecretAccessKey)
            || string.IsNullOrWhiteSpace(_settings.BucketName)
            || !Uri.TryCreate(_settings.PublicCustomDomain, UriKind.Absolute, out var domain)
            || domain.Scheme != Uri.UriSchemeHttps || domain.AbsolutePath != "/"
            || !string.IsNullOrEmpty(domain.Query) || !string.IsNullOrEmpty(domain.Fragment)
            || !string.IsNullOrEmpty(domain.UserInfo))
            throw new InvalidOperationException("Chưa cấu hình R2 credentials, bucket và public domain cho ảnh.");

        _client = new AmazonS3Client(new BasicAWSCredentials(_settings.AccessKeyId, _settings.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = $"https://{_settings.AccountId}.r2.cloudflarestorage.com",
                AuthenticationRegion = "auto",
                ForcePathStyle = true
            });
    }

    public async Task<string> UploadAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken)
    {
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        }, cancellationToken);
        return $"{_settings.PublicCustomDomain.TrimEnd('/')}/{objectKey}";
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
        _client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _settings.BucketName, Key = objectKey }, cancellationToken);

    public void Dispose() => _client.Dispose();
}
