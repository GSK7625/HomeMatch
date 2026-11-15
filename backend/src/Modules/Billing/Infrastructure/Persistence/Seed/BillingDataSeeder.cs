using HomeMatch.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HomeMatch.Modules.Billing.Infrastructure.Persistence.Seed;

public static class BillingDataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        // Đảm bảo schema billing và toàn bộ bảng được tạo nếu chưa tồn tại
        await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS billing;");
        var creator = db.Database.GetService<IDatabaseCreator>() as IRelationalDatabaseCreator;
        if (creator != null)
        {
            try
            {
                await creator.CreateTablesAsync();
            }
            catch (PostgresException ex) when (ex.SqlState == "42P07")
            {
                // Bảng đã tồn tại, an toàn bỏ qua
            }
            catch (Exception)
            {
                // Dự phòng nếu bảng đã được tạo qua migration
            }
        }

        // Check if data already seeded
        if (await db.Rooms.AnyAsync())
        {
            return;
        }

        // 1. Seed Rooms
        var rooms = new List<BillingRoom>
        {
            new() { BuildingId = "bld-001", BuildingName = "HomeMatch Green - Cầu Giấy", RoomNumber = "P.101 - Studio Ban công", Status = "RENTED", BasePrice = 4800000m },
            new() { BuildingId = "bld-001", BuildingName = "HomeMatch Green - Cầu Giấy", RoomNumber = "P.102 - 1N1K Thoáng", Status = "RENTED", BasePrice = 5500000m },
            new() { BuildingId = "bld-001", BuildingName = "HomeMatch Green - Cầu Giấy", RoomNumber = "P.103 - Studio Full đồ", Status = "AVAILABLE", BasePrice = 4500000m },
            new() { BuildingId = "bld-001", BuildingName = "HomeMatch Green - Cầu Giấy", RoomNumber = "P.201 - Duplex Gác lửng", Status = "RENTED", BasePrice = 6200000m },
            new() { BuildingId = "bld-001", BuildingName = "HomeMatch Green - Cầu Giấy", RoomNumber = "P.202 - Studio Cửa sổ", Status = "AVAILABLE", BasePrice = 4300000m },

            new() { BuildingId = "bld-002", BuildingName = "Sunrise House - Đống Đa", RoomNumber = "P.301 - Căn hộ Dịch vụ", Status = "RENTED", BasePrice = 7500000m },
            new() { BuildingId = "bld-002", BuildingName = "Sunrise House - Đống Đa", RoomNumber = "P.302 - 1N1K Cao cấp", Status = "RENTED", BasePrice = 7000000m },
            new() { BuildingId = "bld-002", BuildingName = "Sunrise House - Đống Đa", RoomNumber = "P.303 - Studio Đống Đa", Status = "AVAILABLE", BasePrice = 5000000m },

            new() { BuildingId = "bld-003", BuildingName = "EcoLife Studio - Nam Từ Liêm", RoomNumber = "P.401 - Mini Nam Từ Liêm", Status = "RENTED", BasePrice = 3800000m },
            new() { BuildingId = "bld-003", BuildingName = "EcoLife Studio - Nam Từ Liêm", RoomNumber = "P.402 - Phòng khép kín", Status = "RENTED", BasePrice = 3900000m },
            new() { BuildingId = "bld-003", BuildingName = "EcoLife Studio - Nam Từ Liêm", RoomNumber = "P.403 - Căn gác xép", Status = "AVAILABLE", BasePrice = 3500000m },
            new() { BuildingId = "bld-003", BuildingName = "EcoLife Studio - Nam Từ Liêm", RoomNumber = "P.404 - Studio Thoáng", Status = "RENTED", BasePrice = 4100000m }
        };
        await db.Rooms.AddRangeAsync(rooms);
        await db.SaveChangesAsync();

        // 2. Seed Contracts
        var contracts = new List<BillingContract>
        {
            new() { ContractCode = "HD-2026-001", BuildingId = "bld-001", RoomName = "P.101 - Studio Ban công", TenantName = "Nguyễn Văn An", DepositAmount = 5000000m, StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2027, 1, 1), Status = "ACTIVE", DepositStatus = "HELD_IN_ESCROW" },
            new() { ContractCode = "HD-2026-002", BuildingId = "bld-001", RoomName = "P.102 - 1N1K Thoáng", TenantName = "Trần Thị Bình", DepositAmount = 6000000m, StartDate = new DateOnly(2026, 3, 1), EndDate = new DateOnly(2026, 11, 30), Status = "ACTIVE", DepositStatus = "HELD_IN_ESCROW" },
            new() { ContractCode = "HD-2026-003", BuildingId = "bld-001", RoomName = "P.201 - Duplex Gác lửng", TenantName = "Lê Hoàng Long", DepositAmount = 7000000m, StartDate = new DateOnly(2026, 2, 1), EndDate = new DateOnly(2027, 2, 1), Status = "ACTIVE", DepositStatus = "HELD_IN_ESCROW" },
            new() { ContractCode = "HD-2026-004", BuildingId = "bld-002", RoomName = "P.301 - Căn hộ Dịch vụ", TenantName = "Phạm Minh Đức", DepositAmount = 8000000m, StartDate = new DateOnly(2025, 12, 1), EndDate = new DateOnly(2026, 12, 1), Status = "ACTIVE", DepositStatus = "HELD_IN_ESCROW" },
            new() { ContractCode = "HD-2026-005", BuildingId = "bld-002", RoomName = "P.302 - 1N1K Cao cấp", TenantName = "Đỗ Quỳnh Chi", DepositAmount = 7500000m, StartDate = new DateOnly(2026, 5, 1), EndDate = new DateOnly(2027, 5, 1), Status = "ACTIVE", DepositStatus = "HELD_IN_ESCROW" },
            new() { ContractCode = "HD-2026-006", BuildingId = "bld-003", RoomName = "P.401 - Mini Nam Từ Liêm", TenantName = "Vũ Hải Nam", DepositAmount = 4000000m, StartDate = new DateOnly(2026, 4, 1), EndDate = new DateOnly(2026, 12, 15), Status = "ACTIVE", DepositStatus = "HELD_IN_ESCROW" },
            new() { ContractCode = "HD-2026-007", BuildingId = "bld-003", RoomName = "P.402 - Phòng khép kín", TenantName = "Hoàng Ngọc Ánh", DepositAmount = 4000000m, StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2027, 6, 1), Status = "ACTIVE", DepositStatus = "HELD_IN_ESCROW" },
            new() { ContractCode = "HD-2026-008", BuildingId = "bld-003", RoomName = "P.404 - Studio Thoáng", TenantName = "Đinh Tuấn Kiệt", DepositAmount = 4500000m, StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2027, 7, 1), Status = "ACTIVE", DepositStatus = "HELD_IN_ESCROW" }
        };
        await db.Contracts.AddRangeAsync(contracts);

        // 3. Seed Invoices
        var invoices = new List<Invoice>
        {
            new()
            {
                InvoiceCode = "INV-202611-001",
                BuildingId = "bld-001",
                RoomId = 1,
                RoomName = "P.101 - Studio Ban công",
                TenantName = "Nguyễn Văn An",
                TenantPhone = "0912345678",
                BillingMonth = 11,
                BillingYear = 2026,
                Period = "11/2026",
                RentAmount = 4800000m,
                ElectricityAmount = 520000m,
                WaterAmount = 150000m,
                ServiceAmount = 200000m,
                TotalAmount = 5670000m,
                PaidAmount = 5670000m,
                RemainingDebt = 0m,
                DueDate = new DateOnly(2026, 11, 10),
                Status = "PAID"
            },
            new()
            {
                InvoiceCode = "INV-202611-002",
                BuildingId = "bld-001",
                RoomId = 2,
                RoomName = "P.102 - 1N1K Thoáng",
                TenantName = "Trần Thị Bình",
                TenantPhone = "0987654321",
                BillingMonth = 11,
                BillingYear = 2026,
                Period = "11/2026",
                RentAmount = 5500000m,
                ElectricityAmount = 680000m,
                WaterAmount = 180000m,
                ServiceAmount = 200000m,
                TotalAmount = 6560000m,
                PaidAmount = 6560000m,
                RemainingDebt = 0m,
                DueDate = new DateOnly(2026, 11, 10),
                Status = "PAID"
            },
            new()
            {
                InvoiceCode = "INV-202611-003",
                BuildingId = "bld-001",
                RoomId = 4,
                RoomName = "P.201 - Duplex Gác lửng",
                TenantName = "Lê Hoàng Long",
                TenantPhone = "0933221100",
                BillingMonth = 11,
                BillingYear = 2026,
                Period = "11/2026",
                RentAmount = 6200000m,
                ElectricityAmount = 710000m,
                WaterAmount = 200000m,
                ServiceAmount = 250000m,
                TotalAmount = 7360000m,
                PaidAmount = 4000000m,
                RemainingDebt = 3360000m,
                DueDate = new DateOnly(2026, 11, 5),
                Status = "PARTIALLY_PAID"
            },
            new()
            {
                InvoiceCode = "INV-202611-004",
                BuildingId = "bld-002",
                RoomId = 6,
                RoomName = "P.301 - Căn hộ Dịch vụ",
                TenantName = "Phạm Minh Đức",
                TenantPhone = "0905123987",
                BillingMonth = 11,
                BillingYear = 2026,
                Period = "11/2026",
                RentAmount = 7500000m,
                ElectricityAmount = 890000m,
                WaterAmount = 250000m,
                ServiceAmount = 350000m,
                TotalAmount = 8990000m,
                PaidAmount = 0m,
                RemainingDebt = 8990000m,
                DueDate = new DateOnly(2026, 10, 25),
                Status = "OVERDUE"
            },
            new()
            {
                InvoiceCode = "INV-202611-005",
                BuildingId = "bld-002",
                RoomId = 7,
                RoomName = "P.302 - 1N1K Cao cấp",
                TenantName = "Đỗ Quỳnh Chi",
                TenantPhone = "0978112233",
                BillingMonth = 11,
                BillingYear = 2026,
                Period = "11/2026",
                RentAmount = 7000000m,
                ElectricityAmount = 760000m,
                WaterAmount = 220000m,
                ServiceAmount = 300000m,
                TotalAmount = 8280000m,
                PaidAmount = 8280000m,
                RemainingDebt = 0m,
                DueDate = new DateOnly(2026, 11, 12),
                Status = "PAID"
            },
            new()
            {
                InvoiceCode = "INV-202611-006",
                BuildingId = "bld-003",
                RoomId = 9,
                RoomName = "P.401 - Mini Nam Từ Liêm",
                TenantName = "Vũ Hải Nam",
                TenantPhone = "0966443322",
                BillingMonth = 11,
                BillingYear = 2026,
                Period = "11/2026",
                RentAmount = 3800000m,
                ElectricityAmount = 390000m,
                WaterAmount = 120000m,
                ServiceAmount = 150000m,
                TotalAmount = 4460000m,
                PaidAmount = 0m,
                RemainingDebt = 4460000m,
                DueDate = new DateOnly(2026, 9, 20),
                Status = "OVERDUE"
            },
            new()
            {
                InvoiceCode = "INV-202611-007",
                BuildingId = "bld-003",
                RoomId = 10,
                RoomName = "P.402 - Phòng khép kín",
                TenantName = "Hoàng Ngọc Ánh",
                TenantPhone = "0945678901",
                BillingMonth = 11,
                BillingYear = 2026,
                Period = "11/2026",
                RentAmount = 3900000m,
                ElectricityAmount = 420000m,
                WaterAmount = 130000m,
                ServiceAmount = 150000m,
                TotalAmount = 4600000m,
                PaidAmount = 4600000m,
                RemainingDebt = 0m,
                DueDate = new DateOnly(2026, 11, 10),
                Status = "PAID"
            },
            new()
            {
                InvoiceCode = "INV-202611-008",
                BuildingId = "bld-003",
                RoomId = 12,
                RoomName = "P.404 - Studio Thoáng",
                TenantName = "Đinh Tuấn Kiệt",
                TenantPhone = "0922889911",
                BillingMonth = 11,
                BillingYear = 2026,
                Period = "11/2026",
                RentAmount = 4100000m,
                ElectricityAmount = 450000m,
                WaterAmount = 140000m,
                ServiceAmount = 180000m,
                TotalAmount = 4870000m,
                PaidAmount = 2000000m,
                RemainingDebt = 2870000m,
                DueDate = new DateOnly(2026, 11, 15),
                Status = "PARTIALLY_PAID"
            }
        };
        await db.Invoices.AddRangeAsync(invoices);

        // 4. Seed Viewings
        var viewings = new List<BillingViewing>
        {
            new() { BuildingId = "bld-001", ScheduledTime = "09:30", RoomName = "P.103 - Studio Full đồ", TenantName = "Ngô Thanh Tùng", TenantPhone = "0911223344", ViewingType = "Trực tiếp", Status = "SCHEDULED" },
            new() { BuildingId = "bld-001", ScheduledTime = "14:00", RoomName = "P.202 - Studio Cửa sổ", TenantName = "Lý Thị Mai", TenantPhone = "0934567890", ViewingType = "Online (Google Meet)", Status = "SCHEDULED" },
            new() { BuildingId = "bld-002", ScheduledTime = "16:30", RoomName = "P.303 - Studio Đống Đa", TenantName = "Trần Đình Khang", TenantPhone = "0988776655", ViewingType = "Trực tiếp", Status = "CONFIRMED" },
            new() { BuildingId = "bld-003", ScheduledTime = "18:00", RoomName = "P.403 - Căn gác xép", TenantName = "Phan Gia Hân", TenantPhone = "0977665544", ViewingType = "Trực tiếp", Status = "SCHEDULED" }
        };
        await db.Viewings.AddRangeAsync(viewings);

        await db.SaveChangesAsync();
    }
}
