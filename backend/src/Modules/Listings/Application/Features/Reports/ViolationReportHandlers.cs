using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;

namespace HomeMatch.Modules.Listings.Application.Features.Reports;

public sealed record ViolationReportResult(
    long Id,
    string TargetType,
    long TargetId,
    long? ListingId,
    string TargetTitle,
    long ReporterUserId,
    string ReporterName,
    string ReporterEmail,
    string Reason,
    string? Description,
    string? EvidenceUrl,
    string Status,
    string? AdminNote,
    long? ResolvedBy,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset CreatedAt);

public sealed record PagedViolationReportsResult(
    IReadOnlyList<ViolationReportResult> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record CreateViolationReportCommand(
    long ReporterUserId,
    string TargetType,
    long TargetId,
    long? ListingId,
    string Reason,
    string? Description,
    string? EvidenceUrl);

public sealed class CreateViolationReportHandler(
    IViolationReportRepository reports,
    IListingRepository listings,
    IIdentityUserReader identityUsers)
{
    public async Task<Result<ViolationReportResult>> HandleAsync(
        CreateViolationReportCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ReporterUserId <= 0 || command.TargetId <= 0)
            return Result.Failure<ViolationReportResult>(ListingsErrors.InvalidReport);

        var targetType = (command.TargetType ?? string.Empty).Trim().ToUpperInvariant();
        if (targetType is not ("LISTING" or "LANDLORD"))
            return Result.Failure<ViolationReportResult>(ListingsErrors.InvalidReport);

        var reason = (command.Reason ?? string.Empty).Trim();
        if (reason.Length is < 3 or > 255)
            return Result.Failure<ViolationReportResult>(ListingsErrors.InvalidReport);

        var description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
        var evidenceUrl = string.IsNullOrWhiteSpace(command.EvidenceUrl) ? null : command.EvidenceUrl.Trim();
        if (evidenceUrl is not null && (evidenceUrl.Length > 500
            || !Uri.TryCreate(evidenceUrl, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("https" or "http")))
        {
            return Result.Failure<ViolationReportResult>(ListingsErrors.InvalidReport);
        }

        string targetTitle;
        long? resolvedListingId = command.ListingId;

        if (targetType == "LISTING")
        {
            var listing = await listings.GetByIdAsync(command.TargetId, cancellationToken);
            if (listing is null)
                return Result.Failure<ViolationReportResult>(ListingsErrors.ListingNotFound);
            targetTitle = listing.Title;
            resolvedListingId = listing.Id;
        }
        else
        {
            var landlord = await identityUsers.GetUserByIdAsync(command.TargetId, cancellationToken);
            if (landlord is null)
                return Result.Failure<ViolationReportResult>(ListingsErrors.InvalidReport);
            targetTitle = string.IsNullOrWhiteSpace(landlord.FullName)
                ? landlord.Email
                : $"{landlord.FullName} ({landlord.Email})";
        }

        var reporter = await identityUsers.GetUserByIdAsync(command.ReporterUserId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var entity = new ViolationReport
        {
            TargetType = targetType,
            TargetId = command.TargetId,
            ListingId = resolvedListingId,
            ReporterUserId = command.ReporterUserId,
            Reason = reason,
            Description = description,
            EvidenceUrl = evidenceUrl,
            Status = "PENDING",
            CreatedAt = now,
            UpdatedAt = now
        };

        reports.Add(entity);
        await reports.SaveChangesAsync(cancellationToken);

        return Result.Success(new ViolationReportResult(
            Id: entity.Id,
            TargetType: entity.TargetType,
            TargetId: entity.TargetId,
            ListingId: entity.ListingId,
            TargetTitle: targetTitle,
            ReporterUserId: entity.ReporterUserId,
            ReporterName: reporter?.FullName ?? $"User #{entity.ReporterUserId}",
            ReporterEmail: reporter?.Email ?? string.Empty,
            Reason: entity.Reason,
            Description: entity.Description,
            EvidenceUrl: entity.EvidenceUrl,
            Status: entity.Status,
            AdminNote: entity.AdminNote,
            ResolvedBy: entity.ResolvedBy,
            ResolvedAt: entity.ResolvedAt,
            CreatedAt: entity.CreatedAt));
    }
}

public sealed record GetViolationReportsQuery(
    string? Status = null,
    string? TargetType = null,
    int Page = 1,
    int PageSize = 20);

public sealed class GetViolationReportsHandler(
    IViolationReportRepository reports,
    IListingRepository listings,
    IIdentityUserReader identityUsers)
{
    public async Task<Result<PagedViolationReportsResult>> HandleAsync(
        GetViolationReportsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100 || (long)(query.Page - 1) * query.PageSize > int.MaxValue)
            return Result.Failure<PagedViolationReportsResult>(ListingsErrors.InvalidPage);

        var (items, total) = await reports.GetPagedAsync(
            query.Status,
            query.TargetType,
            query.Page,
            query.PageSize,
            cancellationToken);

        var listingTitleCache = new Dictionary<long, string>();
        var userCache = new Dictionary<long, IdentityUserInfo?>();

        async Task<IdentityUserInfo?> GetUserCachedAsync(long userId)
        {
            if (!userCache.TryGetValue(userId, out var info))
            {
                info = await identityUsers.GetUserByIdAsync(userId, cancellationToken);
                userCache[userId] = info;
            }
            return info;
        }

        async Task<string> GetListingTitleCachedAsync(long listingId)
        {
            if (!listingTitleCache.TryGetValue(listingId, out var title))
            {
                var l = await listings.GetByIdAsync(listingId, cancellationToken);
                title = l?.Title ?? $"Tin đăng #{listingId}";
                listingTitleCache[listingId] = title;
            }
            return title;
        }

        var resultItems = new List<ViolationReportResult>(items.Count);
        foreach (var r in items)
        {
            var reporter = await GetUserCachedAsync(r.ReporterUserId);
            string targetTitle;
            if (r.TargetType == "LISTING")
            {
                targetTitle = await GetListingTitleCachedAsync(r.TargetId);
            }
            else
            {
                var landlord = await GetUserCachedAsync(r.TargetId);
                targetTitle = landlord is null
                    ? $"Chủ nhà #{r.TargetId}"
                    : (!string.IsNullOrWhiteSpace(landlord.FullName) ? $"{landlord.FullName} ({landlord.Email})" : landlord.Email);
            }

            resultItems.Add(new ViolationReportResult(
                Id: r.Id,
                TargetType: r.TargetType,
                TargetId: r.TargetId,
                ListingId: r.ListingId,
                TargetTitle: targetTitle,
                ReporterUserId: r.ReporterUserId,
                ReporterName: reporter?.FullName ?? $"User #{r.ReporterUserId}",
                ReporterEmail: reporter?.Email ?? string.Empty,
                Reason: r.Reason,
                Description: r.Description,
                EvidenceUrl: r.EvidenceUrl,
                Status: r.Status,
                AdminNote: r.AdminNote,
                ResolvedBy: r.ResolvedBy,
                ResolvedAt: r.ResolvedAt,
                CreatedAt: r.CreatedAt));
        }

        return Result.Success(new PagedViolationReportsResult(resultItems, query.Page, query.PageSize, total));
    }
}

public sealed record ResolveViolationReportCommand(
    long AdminId,
    long ReportId,
    string Resolution,
    string? AdminNote,
    bool HideListing = false);

public sealed class ResolveViolationReportHandler(
    IViolationReportRepository reports,
    IListingRepository listings,
    IIdentityUserReader identityUsers)
{
    public async Task<Result<ViolationReportResult>> HandleAsync(
        ResolveViolationReportCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AdminId <= 0 || command.ReportId <= 0)
            return Result.Failure<ViolationReportResult>(ListingsErrors.InvalidReport);

        var resolution = (command.Resolution ?? string.Empty).Trim().ToUpperInvariant();
        if (resolution is not ("RESOLVED" or "DISMISSED"))
            return Result.Failure<ViolationReportResult>(ListingsErrors.InvalidReport);

        if (command.AdminNote is not null && command.AdminNote.Trim().Length > 1000)
            return Result.Failure<ViolationReportResult>(ListingsErrors.InvalidReport);

        var report = await reports.GetByIdAsync(command.ReportId, cancellationToken);
        if (report is null)
            return Result.Failure<ViolationReportResult>(ListingsErrors.ReportNotFound);

        if (report.Status != "PENDING")
            return Result.Failure<ViolationReportResult>(ListingsErrors.ReportAlreadyResolved);

        var now = DateTimeOffset.UtcNow;
        report.Status = resolution;
        report.AdminNote = string.IsNullOrWhiteSpace(command.AdminNote) ? null : command.AdminNote.Trim();
        report.ResolvedBy = command.AdminId;
        report.ResolvedAt = now;
        report.UpdatedAt = now;

        await reports.SaveChangesAsync(cancellationToken);

        var listingIdToHide = report.ListingId ?? (report.TargetType == "LISTING" ? report.TargetId : (long?)null);
        if (resolution == "RESOLVED" && command.HideListing && listingIdToHide.HasValue)
        {
            var listing = await listings.GetByIdAsync(listingIdToHide.Value, cancellationToken);
            if (listing is not null && listing.Status is "PUBLISHED" or "PENDING_APPROVAL")
            {
                listing.Status = "HIDDEN";
                listing.ReviewedBy = command.AdminId;
                listing.ReviewedAt = now;
                listing.RejectionReason = report.AdminNote ?? $"Ẩn do vi phạm: {report.Reason}";
                listing.UpdatedAt = now;
                await listings.SaveChangesAsync(cancellationToken);
            }
        }

        var reporter = await identityUsers.GetUserByIdAsync(report.ReporterUserId, cancellationToken);
        string targetTitle;
        if (report.TargetType == "LISTING")
        {
            var l = await listings.GetByIdAsync(report.TargetId, cancellationToken);
            targetTitle = l?.Title ?? $"Tin đăng #{report.TargetId}";
        }
        else
        {
            var landlord = await identityUsers.GetUserByIdAsync(report.TargetId, cancellationToken);
            targetTitle = landlord?.FullName ?? $"Chủ nhà #{report.TargetId}";
        }

        return Result.Success(new ViolationReportResult(
            Id: report.Id,
            TargetType: report.TargetType,
            TargetId: report.TargetId,
            ListingId: report.ListingId,
            TargetTitle: targetTitle,
            ReporterUserId: report.ReporterUserId,
            ReporterName: reporter?.FullName ?? $"User #{report.ReporterUserId}",
            ReporterEmail: reporter?.Email ?? string.Empty,
            Reason: report.Reason,
            Description: report.Description,
            EvidenceUrl: report.EvidenceUrl,
            Status: report.Status,
            AdminNote: report.AdminNote,
            ResolvedBy: report.ResolvedBy,
            ResolvedAt: report.ResolvedAt,
            CreatedAt: report.CreatedAt));
    }
}
