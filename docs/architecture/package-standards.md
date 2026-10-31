# Tiêu Chuẩn Thư Viện & Package Backend HomeMatch

Tài liệu này xác định danh mục các NuGet packages được sử dụng trong toàn bộ giải pháp Backend HomeMatch (.NET 10). Mọi việc bổ sung package mới đều phải qua thảo luận và thống nhất theo quy định tại `AGENTS.md`.

---

## 1. Nền Tảng Cốt Lõi (.NET Runtime)
- **Target Framework:** `net10.0`
- **Ngôn ngữ:** C# 14 / .NET 10 Native Minimal API
- **Nullability:** `<Nullable>enable</Nullable>`
- **Implicit Usings:** `<ImplicitUsings>enable</ImplicitUsings>`

---

## 2. Phân Nhóm Thư Viện & Packages

### 2.1 Packages Đang Cài Đặt Thực Tế (Installed)
Các package hiện diện trực tiếp trong các file `.csproj` của repository:

| Package | Phiên bản | Project sử dụng | Mục đích |
|---|---|---|---|
| `Microsoft.AspNetCore.OpenApi` | `10.0.13` | `HomeMatch.Api` | OpenAPI engine cho .NET 10 |
| `Scalar.AspNetCore` | `2.18.3` | `HomeMatch.Api` | Giao diện tài liệu API tương tác |
| `Microsoft.NET.Test.Sdk` | `18.11.0` | `UnitTests`, `IntegrationTests` | Test runner SDK |
| `xunit` | `2.9.3` | `UnitTests`, `IntegrationTests` | XUnit testing framework |
| `xunit.runner.visualstudio` | `4.0.1` | `UnitTests`, `IntegrationTests` | Visual Studio test runner adapter |
| `coverlet.collector` | `10.1.0` | `UnitTests`, `IntegrationTests` | Thu thập code coverage |

### 2.2 Packages Đã Được Phê Duyệt Sẵn Sàng Sử Dụng (Pre-Approved)
Các package đã được kiến trúc phê duyệt và gán phiên bản cụ thể, sẵn sàng cài đặt khi triển khai các module `Identity`, `Properties`, `Listings`:

| Package | Phiên bản khuyến nghị | Module mục tiêu | Mục đích |
|---|---|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.0.0-preview.*` hoặc `9.0.x` | `Identity`, `Properties`, `Listings` | PostgreSQL Provider cho EF Core |
| `Microsoft.EntityFrameworkCore` | `10.0.x` | Tầng `Infrastructure` của module | Core EF Core engine |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.x` | `HomeMatch.Api` (PrivateAssets=all) | EF Core Migrations tooling |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.x` | `Identity`, `HomeMatch.Api` | Xác thực JWT Bearer tokens |
| `System.IdentityModel.Tokens.Jwt` | `8.x` | `Identity` | Tạo và giải mã JWT token |
| `NetTopologySuite` | `2.5.x` | `Listings` | Thao tác kiểu dữ liệu tọa độ địa lý |
| `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite` | Tương thích Npgsql | `Listings` | Hỗ trợ PostGIS spatial query trong EF Core |

### 2.3 Packages Đang Trong Diện Đề Xuất (Under Proposal)
Các package hữu ích nhưng cần xem xét điều kiện môi trường trước khi áp dụng:

| Package | Mục đích đề xuất | Điều kiện tiên quyết & Cân nhắc |
|---|---|---|
| `Testcontainers.PostgreSql` | Tự động dựng container PostgreSQL khi chạy Integration Test | **Yêu cầu Docker daemon** phải đang chạy trên máy local dev và Jenkins agent. Nếu máy phát triển không bật Docker, test suite sẽ bị lỗi. Đang chờ thống nhất môi trường CI/CD trước khi cài. |

---

## 3. Các Thư Viện Chưa Đưa Vào Mặc Định (Cần Phê Duyệt Trước Khi Dùng)
Để tránh over-engineering, code phình to và khó debug:
1. **MediatR / MassTransit:** Chưa sử dụng; ưu tiên viết class use case trực tiếp và gọi interface in-process.
2. **AutoMapper / Mapster:** Chưa sử dụng; viết code ánh xạ (explicit mapping) trực quan, có type safety tốt hơn và không có runtime overhead.
3. **FluentValidation:** Chưa sử dụng; kiểm tra validation input trực tiếp tại presentation / DTO methods để code rõ ràng và ít dependency.
4. **Third-party Result Libraries:** Đã xây dựng sẵn `Result` và `Result<T>` chuẩn hóa nhẹ trong `HomeMatch.Shared/Common`.
