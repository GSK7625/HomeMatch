namespace HomeMatch.Modules.Billing.Domain.Entities;

public class Invoice
{
    public long Id { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public string BuildingId { get; set; } = string.Empty;
    public long RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
    public int BillingMonth { get; set; }
    public int BillingYear { get; set; }
    public string Period { get; set; } = string.Empty; // e.g. "11/2026"
    public decimal RentAmount { get; set; }
    public decimal ElectricityAmount { get; set; }
    public decimal WaterAmount { get; set; }
    public decimal ServiceAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingDebt { get; set; }
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = "UNPAID";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class BillingContract
{
    public long Id { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public string BuildingId { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public decimal DepositAmount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string DepositStatus { get; set; } = "HELD_IN_ESCROW";
}

public class BillingRoom
{
    public long Id { get; set; }
    public string BuildingId { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "AVAILABLE"; // "AVAILABLE", "RENTED"
    public decimal BasePrice { get; set; }
}

public class BillingViewing
{
    public long Id { get; set; }
    public string BuildingId { get; set; } = string.Empty;
    public string ScheduledTime { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
    public string ViewingType { get; set; } = "Trực tiếp";
    public string Status { get; set; } = "SCHEDULED";
}
