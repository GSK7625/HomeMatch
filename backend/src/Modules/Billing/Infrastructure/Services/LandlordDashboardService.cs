using System.Globalization;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Billing;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Leasing;
using HomeMatch.Shared.Contracts.Listings;
using HomeMatch.Shared.Contracts.Messaging;
using HomeMatch.Shared.Contracts.Properties;
using HomeMatch.Shared.Contracts.Viewings;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.Modules.Billing.Infrastructure.Services;

public class LandlordDashboardService : ILandlordDashboardService
{
    private static readonly TimeZoneInfo VietnamTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPropertyDashboardReader _propertyReader;
    private readonly ILeasingContractReader _contractReader;
    private readonly IListingDashboardReader _listingReader;
    private readonly IViewingDashboardReader _viewingReader;
    private readonly IMessagingDashboardReader _messagingReader;
    private readonly IIdentityUserReader _identityUserReader;
    private readonly Func<DateOnly>? _todayProvider;

    [ActivatorUtilitiesConstructor]
    public LandlordDashboardService(
        IInvoiceRepository invoiceRepository,
        IPropertyDashboardReader propertyReader,
        ILeasingContractReader contractReader,
        IListingDashboardReader listingReader,
        IViewingDashboardReader viewingReader,
        IMessagingDashboardReader messagingReader,
        IIdentityUserReader identityUserReader)
        : this(
            invoiceRepository,
            propertyReader,
            contractReader,
            listingReader,
            viewingReader,
            messagingReader,
            identityUserReader,
            todayProvider: null)
    {
    }

    public LandlordDashboardService(
        IInvoiceRepository invoiceRepository,
        IPropertyDashboardReader propertyReader,
        ILeasingContractReader contractReader,
        IListingDashboardReader listingReader,
        IViewingDashboardReader viewingReader,
        IMessagingDashboardReader messagingReader,
        IIdentityUserReader identityUserReader,
        Func<DateOnly>? todayProvider)
    {
        _invoiceRepository = invoiceRepository;
        _propertyReader = propertyReader;
        _contractReader = contractReader;
        _listingReader = listingReader;
        _viewingReader = viewingReader;
        _messagingReader = messagingReader;
        _identityUserReader = identityUserReader;
        _todayProvider = todayProvider;
    }

    public async Task<LandlordDashboardDto> GetDashboardAsync(
        long? landlordId,
        string? buildingId,
        string? period,
        CancellationToken cancellationToken = default)
    {
        var today = _todayProvider?.Invoke() ?? BusinessDate.Today();

        if (!TryParseBuildingId(buildingId, out var propertyId))
        {
            return new LandlordDashboardDto();
        }

        var (billingMonth, billingYear) = ParsePeriod(period, today);

        // 1. Rooms & Occupancy from Properties module
        var rooms = await _propertyReader.GetRoomsForDashboardAsync(landlordId, propertyId, cancellationToken);
        var roomMap = rooms.ToDictionary(r => r.RoomId);
        var roomIdSet = rooms.Select(r => r.RoomId).ToHashSet();

        var totalRooms = rooms.Count;
        var vacantRooms = rooms.Count(r => string.Equals(r.Status, "AVAILABLE", StringComparison.OrdinalIgnoreCase));
        var rentedRooms = rooms.Count(r => string.Equals(r.Status, "RENTED", StringComparison.OrdinalIgnoreCase));
        var occupancyRate = totalRooms > 0 ? Math.Round((double)rentedRooms * 100.0 / totalRooms, 1) : 0;

        // 2. Listings & Views from Listings module
        var listings = await _listingReader.GetListingsForDashboardAsync(
            landlordId,
            propertyId.HasValue ? roomIdSet : null,
            cancellationToken);

        var activeListings = listings.Count(l => string.Equals(l.Status, "PUBLISHED", StringComparison.OrdinalIgnoreCase));
        var totalViewsCount = (int)Math.Min(int.MaxValue, listings.Sum(l => l.ViewCount));
        var listingMap = listings.ToDictionary(l => l.ListingId);
        var listingIds = listings.Select(l => l.ListingId).ToList();

        // 3. Invoices & Financials from Billing module (billing.invoices)
        var invoices = await _invoiceRepository.GetForDashboardAsync(
            landlordId,
            propertyId,
            billingMonth,
            billingYear,
            cancellationToken);

        decimal totalRent = 0;
        decimal totalElectricity = 0;
        decimal totalWater = 0;
        decimal totalService = 0;
        decimal totalReceivables = 0;
        decimal totalCollected = 0;

        foreach (var inv in invoices)
        {
            totalRent += inv.RentAmount;
            totalElectricity += inv.ElectricityAmount;
            totalWater += inv.WaterAmount;
            totalService += inv.ServiceAmount;
            totalReceivables += inv.TotalAmount;
            totalCollected += inv.PaidAmount;
        }

        var remainingDebt = Math.Max(0m, totalReceivables - totalCollected);
        var collectionRate = totalReceivables > 0
            ? Math.Round((double)totalCollected * 100.0 / (double)totalReceivables, 1)
            : 0;

        // 4. Active Contracts & Deposits from Leasing module (leasing.contracts)
        var allActiveContracts = await _contractReader.GetActiveContractsAsync(landlordId, cancellationToken);
        var contracts = propertyId.HasValue
            ? allActiveContracts.Where(c => roomIdSet.Contains(c.RoomId)).ToList()
            : allActiveContracts.ToList();

        // 5. Viewings & Lead Funnel from Viewings & Messaging modules
        var allViewings = await _viewingReader.GetViewingsByListingIdsAsync(listingIds, cancellationToken);
        var leadStats = await _messagingReader.GetLeadFunnelByListingIdsAsync(listingIds, cancellationToken);

        var nonCancelledViewings = allViewings
            .Where(v => !string.Equals(v.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var viewingsTodaySource = nonCancelledViewings
            .Where(v => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(v.AppointmentTime, VietnamTimeZone).DateTime) == today)
            .OrderBy(v => v.AppointmentTime)
            .ToList();

        // 6. Batch-resolve unique tenant profiles via Identity module
        var unpaidInvoices = invoices
            .Where(inv => inv.TotalAmount - inv.PaidAmount > 0)
            .ToList();

        var tenantIds = unpaidInvoices.Select(i => i.TenantId)
            .Concat(contracts.Select(c => c.TenantId))
            .Concat(viewingsTodaySource.Select(v => v.TenantId))
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var tenantMap = new Dictionary<long, IdentityUserInfo>(tenantIds.Count);
        foreach (var tenantId in tenantIds)
        {
            var userInfo = await _identityUserReader.GetUserByIdAsync(tenantId, cancellationToken);
            if (userInfo is not null)
            {
                tenantMap[tenantId] = userInfo;
            }
        }

        // 7. Debt Aging Analysis from real billing.invoices
        decimal currentAmount = 0;
        decimal overdue1To30 = 0;
        decimal overdue31To60 = 0;
        decimal overdueOver60 = 0;
        decimal totalDebt = 0;
        decimal overdueDebt = 0;

        var debtItems = new List<LandlordDebtItemDto>(unpaidInvoices.Count);

        foreach (var inv in unpaidInvoices)
        {
            var invRemaining = (decimal)(inv.TotalAmount - inv.PaidAmount);
            totalDebt += invRemaining;

            var overdueDays = today.DayNumber - inv.DueDate.DayNumber;
            string bucket;
            if (overdueDays <= 0)
            {
                bucket = "Trong hạn";
                currentAmount += invRemaining;
            }
            else
            {
                overdueDebt += invRemaining;
                if (overdueDays <= 30)
                {
                    bucket = "1-30 ngày";
                    overdue1To30 += invRemaining;
                }
                else if (overdueDays <= 60)
                {
                    bucket = "31-60 ngày";
                    overdue31To60 += invRemaining;
                }
                else
                {
                    bucket = ">60 ngày";
                    overdueOver60 += invRemaining;
                }
            }

            tenantMap.TryGetValue(inv.TenantId, out var tenant);

            debtItems.Add(new LandlordDebtItemDto
            {
                InvoiceId = inv.InvoiceCode,
                RoomName = FormatRoomName(inv.RoomId, roomMap),
                TenantName = !string.IsNullOrWhiteSpace(tenant?.FullName) ? tenant.FullName : $"Khách thuê #{inv.TenantId}",
                TenantPhone = tenant?.PhoneNumber ?? string.Empty,
                Amount = invRemaining,
                DueDate = inv.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                OverdueDays = Math.Max(0, overdueDays),
                AgingBucket = bucket
            });
        }

        debtItems.Sort((a, b) => b.OverdueDays.CompareTo(a.OverdueDays));

        // 8. Deposits Report from real leasing.contracts
        decimal totalDeposits = 0;
        var depositItems = new List<LandlordDepositItemDto>(contracts.Count);

        foreach (var c in contracts)
        {
            totalDeposits += c.DepositAmount;
            var daysRemaining = c.EndDate.DayNumber - today.DayNumber;
            tenantMap.TryGetValue(c.TenantId, out var tenant);

            depositItems.Add(new LandlordDepositItemDto
            {
                ContractId = c.Id.ToString(CultureInfo.InvariantCulture),
                ContractCode = c.ContractCode,
                RoomName = FormatRoomName(c.RoomId, roomMap),
                TenantName = !string.IsNullOrWhiteSpace(tenant?.FullName) ? tenant.FullName : $"Khách thuê #{c.TenantId}",
                DepositAmount = c.DepositAmount,
                StartDate = c.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                EndDate = c.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DaysRemaining = Math.Max(0, daysRemaining),
                DepositStatus = daysRemaining <= 30 ? "Sắp đáo hạn" : "Đang giữ"
            });
        }

        depositItems.Sort((a, b) => a.DaysRemaining.CompareTo(b.DaysRemaining));

        // 9. Viewings Today from real viewings.viewing_appointments
        var viewingItems = viewingsTodaySource.Select(v =>
        {
            var localTime = TimeZoneInfo.ConvertTime(v.AppointmentTime, VietnamTimeZone);
            listingMap.TryGetValue(v.ListingId, out var listing);
            tenantMap.TryGetValue(v.TenantId, out var tenant);

            var roomName = listing is not null
                ? FormatRoomName(listing.RoomId, roomMap, listing.Title)
                : $"Tin đăng #{v.ListingId}";

            return new LandlordViewingItemDto
            {
                AppointmentId = v.AppointmentId.ToString(CultureInfo.InvariantCulture),
                ScheduledTime = localTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                RoomName = roomName,
                TenantName = !string.IsNullOrWhiteSpace(tenant?.FullName) ? tenant.FullName : $"Khách thuê #{v.TenantId}",
                TenantPhone = tenant?.PhoneNumber ?? string.Empty,
                ViewingType = v.IsVirtual ? "Online" : "Trực tiếp",
                Status = v.Status
            };
        }).ToList();

        // 10. Marketing & Leads Funnel (monotonic conversion pipeline from real modules)
        var contractSignedLeads = Math.Max(leadStats.ContractSignedLeads, contracts.Count);
        var viewingScheduledLeads = Math.Max(leadStats.ViewingScheduledLeads, nonCancelledViewings.Count);
        var contactedLeads = Math.Max(leadStats.ContactedLeads, Math.Max(viewingScheduledLeads, contractSignedLeads));
        var newLeads = Math.Max(leadStats.TotalLeads, contactedLeads);

        var funnel = new LandlordMarketingFunnelDto
        {
            ActiveListingsCount = activeListings,
            TotalViewsCount = totalViewsCount,
            NewLeads = newLeads,
            ContactedLeads = contactedLeads,
            ViewingScheduledLeads = viewingScheduledLeads,
            ContractSignedLeads = contractSignedLeads
        };

        return new LandlordDashboardDto
        {
            Overview = new LandlordOverviewDto
            {
                TotalRooms = totalRooms,
                VacantRooms = vacantRooms,
                RentedRooms = rentedRooms,
                ActiveListings = activeListings,
                OccupancyRate = occupancyRate
            },
            Financials = new LandlordFinancialsDto
            {
                TotalReceivables = totalReceivables,
                TotalCollected = totalCollected,
                RemainingDebt = remainingDebt,
                CollectionRate = collectionRate,
                Breakdown = new RevenueBreakdownDto
                {
                    RentAmount = totalRent,
                    ElectricityAmount = totalElectricity,
                    WaterAmount = totalWater,
                    ServiceAmount = totalService
                }
            },
            DebtAging = new LandlordDebtAgingDto
            {
                TotalDebt = totalDebt,
                OverdueDebt = overdueDebt,
                CurrentAmount = currentAmount,
                Overdue1To30Days = overdue1To30,
                Overdue31To60Days = overdue31To60,
                OverdueOver60Days = overdueOver60,
                Items = debtItems
            },
            Deposits = new LandlordDepositReportDto
            {
                TotalActiveDeposits = totalDeposits,
                ActiveContractsCount = contracts.Count,
                Items = depositItems
            },
            MarketingAndLeads = funnel,
            ViewingsToday = viewingItems
        };
    }

    private static bool TryParseBuildingId(string? buildingId, out long? propertyId)
    {
        if (string.IsNullOrWhiteSpace(buildingId) ||
            buildingId.Trim().Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            propertyId = null;
            return true;
        }

        if (long.TryParse(buildingId.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
        {
            propertyId = parsed;
            return true;
        }

        propertyId = null;
        return false;
    }

    private static (int? Month, int? Year) ParsePeriod(string? period, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(period))
        {
            return (today.Month, today.Year);
        }

        var trimmed = period.Trim();
        if (trimmed.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        // Format: MM/yyyy
        var slashParts = trimmed.Split('/');
        if (slashParts.Length == 2 &&
            int.TryParse(slashParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var slashMonth) &&
            int.TryParse(slashParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var slashYear) &&
            slashMonth is >= 1 and <= 12 &&
            slashYear >= 2020)
        {
            return (slashMonth, slashYear);
        }

        // Format: yyyy-MM
        var dashParts = trimmed.Split('-');
        if (dashParts.Length == 2 &&
            int.TryParse(dashParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dashYear) &&
            int.TryParse(dashParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dashMonth) &&
            dashMonth is >= 1 and <= 12 &&
            dashYear >= 2020)
        {
            return (dashMonth, dashYear);
        }

        return (today.Month, today.Year);
    }

    private static string FormatRoomName(
        long roomId,
        IReadOnlyDictionary<long, DashboardRoomInfo> roomMap,
        string? fallbackTitle = null)
    {
        if (roomMap.TryGetValue(roomId, out var room))
        {
            return string.IsNullOrWhiteSpace(room.PropertyName)
                ? room.RoomNumber
                : $"{room.RoomNumber} - {room.PropertyName}";
        }

        if (!string.IsNullOrWhiteSpace(fallbackTitle))
        {
            return fallbackTitle;
        }

        return $"Phòng #{roomId}";
    }
}
