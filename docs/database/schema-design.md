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

## 4. Quy chuẩn Thiết kế Bảng & Dữ liệu
- **Quy ước đặt tên:** Schema viết thường (`identity`, `properties`, v.v.); Tên bảng và tên cột dùng `snake_case`.
- **Khóa chính (ID):** Sử dụng `UUID` (Guid) cho các thực thể nghiệp vụ độc lập.
- **Thời gian:** Mọi mốc thời gian lưu theo chuẩn UTC, kiểu dữ liệu `timestamptz`.
- **Tiền tệ:** Sử dụng kiểu `decimal`/`numeric` với precision và đơn vị xác định rõ ràng (VND).
- **Ranh giới quan hệ liên module:** Giữa các module chỉ lưu khóa ngoại logic dưới dạng ID (`UUID`); nghiêm cấm định nghĩa Foreign Key, Navigation Property hoặc Cascade Delete xuyên schema.

## 5. Quy tắc Migrations
- Mỗi module tự quản lý migrations và snapshot trong project của mình.
- Lịch sử migration được lưu trữ độc lập trong từng schema qua bảng `__EFMigrationsHistory`.
- Không sửa đổi migration đã được áp dụng trên môi trường dùng chung.
- Không tự động chạy migration production trong quá trình khởi động API (`Program.cs`).

