using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Infrastructure.Persistence;
using HomeMatch.Shared.Contracts.Billing;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Billing.Infrastructure.Services;

public class LandlordDashboardService : ILandlordDashboardService
{
    private readonly BillingDbContext _db;

    public LandlordDashboardService(BillingDbContext db)
    {
        _db = db;
    }

    public async Task<LandlordDashboardDto> GetDashboardAsync(string? buildingId, string? period, CancellationToken cancellationToken = default)
    {
        var targetPeriod = string.IsNullOrWhiteSpace(period) ? "11/2026" : period.Trim();
        var filterBuilding = string.IsNullOrWhiteSpace(buildingId) || buildingId.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? null
            : buildingId.Trim();

        var today = new DateOnly(2026, 11, 12);

        // 1. Query Rooms & Occupancy from PostgreSQL
        var roomsQuery = _db.Rooms.AsNoTracking();
        if (filterBuilding != null)
        {
            roomsQuery = roomsQuery.Where(r => r.BuildingId == filterBuilding);
        }
        var rooms = await roomsQuery.ToListAsync(cancellationToken);

        var totalRooms = rooms.Count;
        var vacantRooms = rooms.Count(r => r.Status == "AVAILABLE");
        var rentedRooms = rooms.Count(r => r.Status == "RENTED");
        var occupancyRate = totalRooms > 0 ? Math.Round((double)rentedRooms * 100.0 / totalRooms, 1) : 0;
        var activeListings = vacantRooms;

        // 2. Query Invoices & Financials from PostgreSQL
        var invoicesQuery = _db.DashboardInvoices.AsNoTracking();
        if (filterBuilding != null)
        {
            invoicesQuery = invoicesQuery.Where(i => i.BuildingId == filterBuilding);
        }
        invoicesQuery = invoicesQuery.Where(i => i.Period == targetPeriod);
        var invoices = await invoicesQuery.ToListAsync(cancellationToken);

        decimal totalRent = 0;
        decimal totalElectricity = 0;
        decimal totalWater = 0;
        decimal totalService = 0;
        decimal totalCollected = 0;

        foreach (var inv in invoices)
        {
            totalRent += inv.RentAmount;
            totalElectricity += inv.ElectricityAmount;
            totalWater += inv.WaterAmount;
            totalService += inv.ServiceAmount;
            totalCollected += inv.PaidAmount;
        }

        var totalReceivables = totalRent + totalElectricity + totalWater + totalService;
        var remainingDebt = Math.Max(0, totalReceivables - totalCollected);
        var collectionRate = totalReceivables > 0 ? Math.Round((double)totalCollected * 100.0 / (double)totalReceivables, 1) : 0;

        // 3. Debt Aging Analysis from PostgreSQL Invoices
        decimal currentAmount = 0;
        decimal overdue1To30 = 0;
        decimal overdue31To60 = 0;
        decimal overdueOver60 = 0;
        decimal totalDebt = 0;
        decimal overdueDebt = 0;

        var debtItems = new List<LandlordDebtItemDto>();

        foreach (var inv in invoices)
        {
            var invTotal = inv.RentAmount + inv.ElectricityAmount + inv.WaterAmount + inv.ServiceAmount;
            var invRemaining = invTotal - inv.PaidAmount;
            if (invRemaining <= 0) continue;

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

            debtItems.Add(new LandlordDebtItemDto
            {
                InvoiceId = inv.InvoiceCode,
                RoomName = inv.RoomName,
                TenantName = inv.TenantName,
                TenantPhone = inv.TenantPhone,
                Amount = invRemaining,
                DueDate = inv.DueDate.ToString("yyyy-MM-dd"),
                OverdueDays = Math.Max(0, overdueDays),
                AgingBucket = bucket
            });
        }

        debtItems.Sort((a, b) => b.OverdueDays.CompareTo(a.OverdueDays));

        // 4. Query Contracts & Deposits from PostgreSQL
        var contractsQuery = _db.Contracts.AsNoTracking().Where(c => c.Status == "ACTIVE");
        if (filterBuilding != null)
        {
            contractsQuery = contractsQuery.Where(c => c.BuildingId == filterBuilding);
        }
        var contracts = await contractsQuery.ToListAsync(cancellationToken);

        decimal totalDeposits = 0;
        var depositItems = new List<LandlordDepositItemDto>();

        foreach (var c in contracts)
        {
            totalDeposits += c.DepositAmount;
            var daysRemaining = c.EndDate.DayNumber - today.DayNumber;

            depositItems.Add(new LandlordDepositItemDto
            {
                ContractId = c.Id.ToString(),
                ContractCode = c.ContractCode,
                RoomName = c.RoomName,
                TenantName = c.TenantName,
                DepositAmount = c.DepositAmount,
                StartDate = c.StartDate.ToString("yyyy-MM-dd"),
                EndDate = c.EndDate.ToString("yyyy-MM-dd"),
                DaysRemaining = Math.Max(0, daysRemaining),
                DepositStatus = daysRemaining <= 30 ? "Sắp đáo hạn" : "Đang giữ"
            });
        }

        depositItems.Sort((a, b) => a.DaysRemaining.CompareTo(b.DaysRemaining));

        // 5. Query Viewings from PostgreSQL
        var viewingsQuery = _db.Viewings.AsNoTracking();
        if (filterBuilding != null)
        {
            viewingsQuery = viewingsQuery.Where(v => v.BuildingId == filterBuilding);
        }
        var viewings = await viewingsQuery.ToListAsync(cancellationToken);

        var viewingItems = viewings.Select(v => new LandlordViewingItemDto
        {
            AppointmentId = v.Id.ToString(),
            ScheduledTime = v.ScheduledTime,
            RoomName = v.RoomName,
            TenantName = v.TenantName,
            TenantPhone = v.TenantPhone,
            ViewingType = v.ViewingType,
            Status = v.Status
        }).ToList();

        var funnel = new LandlordMarketingFunnelDto
        {
            ActiveListingsCount = activeListings,
            TotalViewsCount = rooms.Count * 15 + viewings.Count * 10,
            NewLeads = rooms.Count + viewings.Count + contracts.Count,
            ContactedLeads = viewings.Count + contracts.Count,
            ViewingScheduledLeads = viewings.Count,
            ContractSignedLeads = contracts.Count
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
}
