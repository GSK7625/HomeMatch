namespace HomeMatch.Shared.Contracts.Billing;

public class LandlordDashboardDto
{
    public LandlordOverviewDto Overview { get; set; } = new();
    public LandlordFinancialsDto Financials { get; set; } = new();
    public LandlordDebtAgingDto DebtAging { get; set; } = new();
    public LandlordDepositReportDto Deposits { get; set; } = new();
    public LandlordMarketingFunnelDto MarketingAndLeads { get; set; } = new();
    public List<LandlordViewingItemDto> ViewingsToday { get; set; } = [];
}

public class LandlordOverviewDto
{
    public int TotalRooms { get; set; }
    public int VacantRooms { get; set; }
    public int RentedRooms { get; set; }
    public int ActiveListings { get; set; }
    public double OccupancyRate { get; set; }
}

public class LandlordFinancialsDto
{
    public decimal TotalReceivables { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal RemainingDebt { get; set; }
    public double CollectionRate { get; set; }
    public RevenueBreakdownDto Breakdown { get; set; } = new();
}

public class RevenueBreakdownDto
{
    public decimal RentAmount { get; set; }
    public decimal ElectricityAmount { get; set; }
    public decimal WaterAmount { get; set; }
    public decimal ServiceAmount { get; set; }
}

public class LandlordDebtAgingDto
{
    public decimal TotalDebt { get; set; }
    public decimal OverdueDebt { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal Overdue1To30Days { get; set; }
    public decimal Overdue31To60Days { get; set; }
    public decimal OverdueOver60Days { get; set; }
    public List<LandlordDebtItemDto> Items { get; set; } = [];
}

public class LandlordDebtItemDto
{
    public string InvoiceId { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string DueDate { get; set; } = string.Empty;
    public int OverdueDays { get; set; }
    public string AgingBucket { get; set; } = string.Empty; // "Trong hạn", "1-30 ngày", "31-60 ngày", ">60 ngày"
}

public class LandlordDepositReportDto
{
    public decimal TotalActiveDeposits { get; set; }
    public int ActiveContractsCount { get; set; }
    public List<LandlordDepositItemDto> Items { get; set; } = [];
}

public class LandlordDepositItemDto
{
    public string ContractId { get; set; } = string.Empty;
    public string ContractCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public decimal DepositAmount { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int DaysRemaining { get; set; }
    public string DepositStatus { get; set; } = "HELD_IN_ESCROW";
}

public class LandlordMarketingFunnelDto
{
    public int ActiveListingsCount { get; set; }
    public int TotalViewsCount { get; set; }
    public int NewLeads { get; set; }
    public int ContactedLeads { get; set; }
    public int ViewingScheduledLeads { get; set; }
    public int ContractSignedLeads { get; set; }
}

public class LandlordViewingItemDto
{
    public string AppointmentId { get; set; } = string.Empty;
    public string ScheduledTime { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
    public string ViewingType { get; set; } = "Trực tiếp"; // "Trực tiếp" hoặc "Online"
    public string Status { get; set; } = "SCHEDULED";
}
