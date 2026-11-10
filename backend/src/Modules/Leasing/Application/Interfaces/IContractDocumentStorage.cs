namespace HomeMatch.Modules.Leasing.Application.Interfaces;

/// <summary>
/// Giao diện lưu trữ và phân phối tài liệu hợp đồng (Cloudflare R2 / S3 tương thích)
/// </summary>
public interface IContractDocumentStorage
{
    /// <summary>
    /// Tải tài liệu lên storage và trả về object key / url tham chiếu
    /// </summary>
    Task<string> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo Presigned URL có thời hạn để xem/tải tài liệu bảo mật
    /// </summary>
    Task<string> GetPresignedDownloadUrlAsync(
        string objectKey,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mở luồng đọc trực tiếp tài liệu từ storage
    /// </summary>
    Task<Stream?> OpenReadStreamAsync(
        string objectKey,
        CancellationToken cancellationToken = default);
}
