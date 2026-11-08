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

`CompleteListingsFeatures` bổ sung metadata media, cờ nổi bật, bảng yêu thích/lượt xem/so sánh và mở rộng slug. Migration mới gỡ hai khóa ngoại xuyên schema trên bảng Listings; migration lịch sử được giữ nguyên.

`Program.cs` tự chạy migrations theo thứ tự Identity → Properties → Listings trong Development. Production cần áp dụng migration qua quy trình triển khai. Database mới vẫn cần chạy migrations Identity và Properties trước vì migration Listings lịch sử có tham chiếu hai schema này.

Thêm media bằng URL vẫn chỉ bật trong Development theo giới hạn có sẵn, chờ tích hợp R2.

## Kiểm tra

- `backend/tests/UnitTests/Listings/ListingsHandlerTests.cs`: ranh giới kiến trúc, validation, quyền truy cập và xử lý kết quả persistence.
- `backend/tests/IntegrationTests/Listings/ListingsPersistenceTests.cs`: nâng cấp dữ liệu cũ và thao tác đồng thời trên PostgreSQL/PostGIS.
- `backend/tests/IntegrationTests/Properties/PropertyListingFlowTests.cs`: vòng đời tin đăng qua HTTP, bao gồm chống đọc tin nháp/đã ẩn qua yêu thích.
- `backend/tests/IntegrationTests/Listings/ListingsRouteTests.cs`: kiểm tra quyền Tenant của hai API yêu thích và xác nhận ba API so sánh không còn đăng ký route; URL cũ trả 404 hoặc 405 từ bộ định tuyến.
