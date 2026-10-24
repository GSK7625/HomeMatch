# Thiết Kế Database - HomeMatch (PostgreSQL + PostGIS)

## 1. Chiến lược Database: One Database, Multiple Schemas
Thay vì dùng nhiều cơ sở dữ liệu vật lý (tốn tài nguyên) hoặc gộp chung tất cả bảng vào schema `public` (dễ bị coupling), HomeMatch sử dụng **một Database duy nhất với nhiều Schemas**:

| Schema | Nhiệm vụ chính |
|---|---|
| `identity` | `Users`, `Roles`, `UserRoles`, `RefreshTokens` |
| `properties` | `Properties`, `Rooms`, `Amenities`, `PropertyImages` |
| `listings` | `Listings`, `ListingLocations` (chứa cột `geography(Point, 4326)`), `ListingTags` |
| `viewings` | `ViewingAppointments`, `ViewingTimeSlots` |
| `messaging` | `Conversations`, `Messages`, `Participants` |
| `leasing` | `RentalAgreements`, `Tenants`, `LeaseTerms` |
| `billing` | `Invoices`, `InvoiceItems`, `Payments`, `UtilityReadings` |
| `notifications` | `NotificationLogs`, `UserDeviceTokens` |

## 2. Tận dụng PostGIS trong Module Listings
- Tọa độ địa lý của bất động sản được lưu trữ dưới dạng chuẩn `geography(Point, 4326)`.
- Sử dụng hàm `ST_DWithin` và spatial index `GIST` để tìm kiếm các phòng trọ trong bán kính (radius) một cách cực kỳ tối ưu.

## 3. Lưu trữ Media & Assets
- Không lưu file nhị phân vào PostgreSQL.
- Ảnh bất động sản, giấy tờ tùy thân được upload trực tiếp lên **Cloudflare R2**.
- PostgreSQL chỉ lưu trữ URL và metadata của ảnh.
