# Kiến Trúc Modular Monolith - HomeMatch

## 1. Giới thiệu tổng quan
HomeMatch được thiết kế theo mô hình **Modular Monolith**:
- Một ứng dụng backend duy nhất (`HomeMatch.Api`) đóng vai trò máy chủ API runtime.
- Logic nghiệp vụ được phân rã thành các **Modules** độc lập:
  - `Identity`: Xác thực, phân quyền, người dùng (Landlord, Tenant).
  - `Properties`: Quản lý thông tin nhà trọ, tòa nhà, loại phòng, tiện ích.
  - `Listings`: Quản lý tin đăng, tìm kiếm tối ưu với PostGIS (khoảng cách, bán kính).
  - `Viewings`: Quản lý lịch xem phòng, đặt lịch hẹn giữa khách và chủ nhà.
  - `Messaging`: Trò chuyện trực tiếp, tin nhắn thời gian thực (SignalR).
  - `Leasing`: Hợp đồng thuê phòng, điều khoản, quản lý khách thuê.
  - `Billing`: Hóa đơn tiền phòng, điện nước dịch vụ, theo dõi công nợ.
  - `Notifications`: Hệ thống thông báo in-app, email, push notification.

## 2. Nguyên tắc tổ chức Module
Mỗi module là một project Class Library (.csproj) riêng biệt với cấu trúc Clean Architecture:
- `Domain/`: Các thực thể (Entities), Value Objects, Enums, Domain Events. Không phụ thuộc vào thư viện bên ngoài.
- `Application/`: Use cases, Commands, Queries (MediatR/CQRS), DTOs, Repository Interfaces.
- `Infrastructure/`: Entity Framework Core DbContext (riêng cho schema của module), Repositories, Migrations, dịch vụ bên ngoài.
- `Presentation/`: Các Endpoints (Minimal API hoặc Controller) đăng ký vào API chung.

## 3. Giao tiếp giữa các Module
- **Tuyệt đối không:** Một module không tham chiếu trực tiếp DbContext hoặc Entity nội bộ của module khác.
- **Truy vấn đồng bộ:** Giao tiếp qua Contracts / Interfaces được định nghĩa trong `HomeMatch.Shared/Contracts`.
- **Bất đồng bộ (Event-Driven):** Sử dụng In-Memory Event Bus (MediatR Notifications) hoặc Outbox Pattern cho các sự kiện liên module (ví dụ: khi hợp đồng kích hoạt -> gửi thông báo và sinh hóa đơn đầu tiên).
