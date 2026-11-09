# Listings

Module tổ chức theo pattern của Auth:

- `Presentation/Endpoints/Requests`: dữ liệu HTTP đầu vào và Data Annotations.
- `Presentation/Endpoints/Responses`: dữ liệu HTTP đầu ra và mapping từ kết quả Application.
- `Application/Features`: Command/Query, Handler, Validator theo use case.
- `Application/Interfaces`: ranh giới persistence; không đưa EF Core hoặc PostGIS vào Application.
- `Application/Models`: kết quả nghiệp vụ và tọa độ thuần .NET.
- `Infrastructure/Persistence/Repositories`: EF Core, PostGIS, transaction và khóa đồng thời.

HTTP giữ các API tạo, sửa, đăng, ẩn tin, media và yêu thích. Các lỗi dự kiến trả về `Result` rồi được map sang `ApiResponse` tại Presentation.

## Dữ liệu và quyền truy cập

- Chủ nhà gọi `POST /api/v1/listings/{id}/submit` để gửi duyệt, nhận `PENDING_APPROVAL`; chưa công khai tin. `/publish` là alias deprecated, dùng cùng handler.
- Admin dùng `GET /api/v1/admin/listings/pending?page=1&pageSize=20`, `GET /api/v1/admin/listings/{id}`, `POST /api/v1/admin/listings/{id}/approve` và `/reject`.
- Approve không cần body; reject nhận `{ "reason": "Lý do" }` (bắt buộc, tối đa 500 ký tự).
- Chỉ Admin được duyệt thành `PUBLISHED`. Endpoint dùng role policy và `RequestActor.AdminId`; người duyệt lấy từ JWT, không nhận ID người duyệt trong body.
- Chủ nhà xem trạng thái/lý do từ chối qua `/mine/{id}`. Metadata `review` chỉ có trong response quản lý; response công khai không chứa metadata này.
- Tin `PENDING_APPROVAL` hoặc `PUBLISHED` bị khóa sửa nội dung và ảnh (409 `Listing.EditLocked`). Chủ nhà dùng `/hide` để rút/ẩn trước, sửa và gửi lại.
- Duyệt lại kiểm tra phòng, tọa độ, ảnh đại diện, giá điện/nước và tin trùng. Revision bảo vệ các cập nhật đồng thời tại database, trả 409 `Listing.RevisionConflict` khi có ghi đè; client không phải gửi revision. Cơ chế này không kiểm tra phiên bản Admin từng mở trước đó.
- Chỉ người thuê có role `Tenant` được lưu/bỏ lưu tin và xem danh sách yêu thích; chưa đăng nhập trả 401, vai trò khác trả 403.
- Chỉ tin `PUBLISHED` có phòng `AVAILABLE` được đưa vào yêu thích và trả về từ danh sách đó.
- Chủ nhà chỉ sửa tin thuộc sở hữu của mình.
- Slug tối đa 300 ký tự, thống nhất giữa request, Application và database.
- Toggle yêu thích được tuần tự hóa theo cặp user/tin bằng PostgreSQL advisory lock trong transaction.
- Vi phạm unique slug hoặc phòng đã có tin công khai được chuyển thành lỗi nghiệp vụ.

## So sánh — hoãn triển khai

Các API GET/POST `/api/v1/listings/comparisons` và DELETE `/api/v1/listings/comparisons/{listingId}` đã được gỡ, cùng use case, DTO và repository phục vụ các API này. Chưa triển khai API so sánh thay thế.

Bảng `user_room_comparisons`, entity EF và migration hiện có được giữ nguyên để tránh thay đổi schema hoặc xóa dữ liệu trong task gỡ API. Phần so sánh sẽ được xử lý ở task riêng.

## Migration

`RequireAdminApproval` thêm trạng thái chờ duyệt/từ chối và thông tin xét duyệt. `RemoveLegacyPublicationFlag` bỏ cột `is_legacy_published` cùng các constraint phụ thuộc, giữ `Revision`, `ReviewedBy`, `ReviewedAt`, `RejectionReason`. Model, DTO và logic hiển thị không còn cờ legacy.

Tin công khai cũ giữ nguyên `PUBLISHED`, `published_at`, `expires_at` và dữ liệu liên quan; thông tin người/thời điểm duyệt có thể null. Tin mới và tin gửi lại phải qua Admin. Public/saved dựa vào `PUBLISHED` và phòng `AVAILABLE`; khi chủ nhà sửa phòng/địa chỉ, tin bị rút về `HIDDEN` để gửi duyệt lại.

Sao lưu DB trước khi triển khai và không chạy song song bản ứng dụng cũ tự publish. Migration rollback đưa `PENDING_APPROVAL`/`REJECTED` về `DRAFT`, không tự công khai; rollback schema làm mất metadata xét duyệt nên ưu tiên sửa tiến và sao lưu trước.

`CompleteListingsFeatures` bổ sung metadata media, cờ nổi bật, bảng yêu thích/lượt xem/so sánh và mở rộng slug. Migration mới gỡ hai khóa ngoại xuyên schema trên bảng Listings; migration lịch sử được giữ nguyên.

`Program.cs` tự chạy migrations theo thứ tự Identity → Properties → Listings trong Development. Production cần áp dụng migration qua quy trình triển khai. Database mới vẫn cần chạy migrations Identity và Properties trước vì migration Listings lịch sử có tham chiếu hai schema này.

Thêm media bằng URL vẫn chỉ bật trong Development theo giới hạn có sẵn, chờ tích hợp R2.

## Kiểm tra

- `backend/tests/UnitTests/Listings/ListingsHandlerTests.cs`: ranh giới kiến trúc, validation, quyền truy cập và xử lý kết quả persistence.
- `backend/tests/IntegrationTests/Listings/ListingsPersistenceTests.cs`: nâng cấp dữ liệu cũ và thao tác đồng thời trên PostgreSQL/PostGIS.
- `backend/tests/IntegrationTests/Properties/PropertyListingFlowTests.cs`: vòng đời tin đăng qua HTTP, bao gồm chống đọc tin nháp/đã ẩn qua yêu thích.
- `backend/tests/IntegrationTests/Listings/ListingsRouteTests.cs`: kiểm tra quyền Tenant của hai API yêu thích và xác nhận ba API so sánh không còn đăng ký route; URL cũ trả 404 hoặc 405 từ bộ định tuyến.
- `backend/tests/IntegrationTests/Listings/ListingsModerationTests.cs`: JWT thật cho gửi duyệt/duyệt/từ chối, alias, gửi lại và kiểm tra lại điều kiện; dữ liệu phòng giả qua shared contract.
- Persistence tests xác minh tin cũ và bốn trường xét duyệt giữ nguyên khi bỏ cờ, concurrency quyết định, duyệt hai tin cùng phòng và rollback ảnh từ editor đã cũ. HTTP flow Properties/Listings dùng dữ liệu phòng thật với geocoding giả.
