using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Leasing.Infrastructure.Services;

/// <summary>
/// Dịch vụ lưu trữ tài liệu trực tiếp trên Cloudflare R2 (tương thích giao thức Amazon S3)
/// </summary>
public sealed class R2DocumentStorageService : IContractDocumentStorage
{
    private readonly R2Settings _settings;
    private readonly ILogger<R2DocumentStorageService> _logger;
    private readonly IAmazonS3? _s3Client;

    public R2DocumentStorageService(
        IOptions<R2Settings> options,
        ILogger<R2DocumentStorageService> logger)
    {
        _settings = options.Value;
        _logger = logger;

        if (_settings.IsConfigured)
        {
            var credentials = new BasicAWSCredentials(_settings.AccessKeyId, _settings.SecretAccessKey);
            var s3Config = new AmazonS3Config
            {
                ServiceURL = $"https://{_settings.AccountId}.r2.cloudflarestorage.com",
                AuthenticationRegion = "auto"
            };
            _s3Client = new AmazonS3Client(credentials, s3Config);
            _logger.LogInformation("Khởi tạo Cloudflare R2 Storage client thành công cho Bucket: {Bucket}", _settings.BucketName);
        }
        else
        {
            _logger.LogWarning("Chưa cấu hình Cloudflare R2 credentials (AccountId, BucketName, AccessKeyId, SecretAccessKey) trong appsettings.");
        }
    }

    public async Task<string> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        EnsureClientConfigured();

        var putRequest = new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        await _s3Client!.PutObjectAsync(putRequest, cancellationToken);
        _logger.LogInformation("Đã upload tài liệu lên Cloudflare R2: bucket={Bucket}, key={Key}", _settings.BucketName, objectKey);
        return objectKey;
    }

    public Task<string> GetPresignedDownloadUrlAsync(
        string objectKey,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        EnsureClientConfigured();

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET
        };

        var presignedUrl = _s3Client!.GetPreSignedURL(request);
        return Task.FromResult(presignedUrl);
    }

    public async Task<Stream?> OpenReadStreamAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        EnsureClientConfigured();

        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = objectKey
            };

            var response = await _s3Client!.GetObjectAsync(request, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Không tìm thấy file trên Cloudflare R2: bucket={Bucket}, key={Key}", _settings.BucketName, objectKey);
            return null;
        }
    }

    private void EnsureClientConfigured()
    {
        if (_s3Client is null || !_settings.IsConfigured)
        {
            throw new InvalidOperationException("Chưa cấu hình Cloudflare R2 credentials (AccountId, BucketName, AccessKeyId, SecretAccessKey) trong appsettings.json.");
        }
    }
}
