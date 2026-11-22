namespace HomeMatch.Modules.Listings.Domain.Entities;

/// <summary>
/// Thực thể Báo cáo vi phạm (ViolationReport) do người dùng gửi đối với tin đăng hoặc chủ nhà.
/// </summary>
public sealed class ViolationReport
{
    public long Id { get; set; }
    public string TargetType { get; set; } = "LISTING";
    public long TargetId { get; set; }
    public long? ListingId { get; set; }
    public long ReporterUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? EvidenceUrl { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? AdminNote { get; set; }
    public long? ResolvedBy { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
