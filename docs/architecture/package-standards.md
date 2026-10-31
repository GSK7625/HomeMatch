# Tiêu Chuẩn Thư Viện & Package Backend HomeMatch

Tài liệu này xác định danh mục các NuGet packages được chuẩn hóa và chấp thuận sử dụng trong toàn bộ giải pháp Backend HomeMatch (.NET 10). Mọi việc bổ sung package mới đều phải qua thảo luận và thống nhất theo quy định tại `AGENTS.md`.

---

## 1. Nền Tảng Cốt Lõi (.NET Runtime)
- **Target Framework:** `net10.0`
- **Ngôn ngữ:** C# 14 / .NET 10 Native Minimal API
- **Nullability:** `<Nullable>enable</Nullable>`
- **Implicit Usings:** `<ImplicitUsings>enable</ImplicitUsings>`

---

## 2. Danh Mục Package Được Phê Duyệt

### 2.1 ORM & PostgreSQL Database
Dùng trong tầng `Infrastructure` của các module:
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.Relational`
- `Microsoft.EntityFrameworkCore.Design` (chỉ tại Host hoặc tooling)

### 2.2 Dữ Liệu Không Gian & Địa Lý (GIS)
Dành riêng cho module `Listings` và `Properties`:
- `NetTopologySuite`
- `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite`

### 2.3 Xác Thực & Phân Quyền (Security & Auth)
Dành cho `Identity` và `HomeMatch.Api`:
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `System.IdentityModel.Tokens.Jwt`

### 2.4 Tài Liệu & API Reference
Dành cho `HomeMatch.Api`:
- `Microsoft.AspNetCore.OpenApi` (Native .NET 10 OpenAPI engine)
- `Scalar.AspNetCore` (Giao diện tài liệu API interactive hiện đại)

### 2.5 Kiểm Thử (Testing)
Dành cho `tests/UnitTests` và `tests/IntegrationTests`:
- `xunit`
- `xunit.runner.visualstudio`
- `Microsoft.NET.Test.Sdk`
- `coverlet.collector`
- `Testcontainers.PostgreSql` (Dành cho Integration test môi trường Docker)

---

## 3. Các Thư Viện Chưa Đưa Vào Mặc Định (Cần Phê Duyệt Trước Khi Dùng)
Để tránh over-engineering, code phình to và khó debug:
1. **MediatR / MassTransit:** Chưa sử dụng; ưu tiên viết class use case trực tiếp và gọi interface in-process.
2. **AutoMapper / Mapster:** Chưa sử dụng; viết code ánh xạ (explicit mapping) trực quan, có type safety tốt hơn và không runtime overhead.
3. **FluentValidation:** Chưa sử dụng; kiểm tra validation input trực tiếp tại presentation / DTO methods để code rõ ràng và ít dependency.
4. **Third-party Result Libraries:** Đã xây dựng sẵn `Result` và `Result<T>` chuẩn hóa nhẹ trong `HomeMatch.Shared/Common`.
