# Bộ Quy Tắc Backend HomeMatch v1.0

Tài liệu này xác lập quy chuẩn kỹ thuật baseline cho toàn bộ Backend của dự án HomeMatch. Mọi thành viên và AI Agent khi làm việc trên phần Backend đều phải tuân thủ nghiêm ngặt các quy định dưới đây.

---

## 1. Kiến trúc và Cấu trúc Module

| Nội dung | Quyết định |
|---|---|
| **Kiến trúc tổng thể** | Modular Monolith, một Backend duy nhất triển khai chung (`HomeMatch.Api`) |
| **Số lượng module** | 8 module nghiệp vụ: `Identity`, `Properties`, `Listings`, `Viewings`, `Messaging`, `Leasing`, `Billing`, `Notifications` |
| **Phân chia project** | Một project `.csproj` độc lập cho mỗi module |
| **Phân tầng** | Phân tầng bằng thư mục bên trong mỗi module: `Domain`, `Application`, `Infrastructure`, `Presentation` |
| **Đăng ký module** | Giữ quy ước `Add…Module(services, config)` và `Map…Endpoints(endpoints)` |
| **API Host (`HomeMatch.Api`)** | Đóng vai trò Host: chỉ cấu hình middleware, bảo mật và nối các module |
| **Kiểm soát kiến trúc** | Code review và Architecture tests (NetArchTest / unit tests) |
| **`IModule`** | Chưa sử dụng; không xây thêm cơ chế dynamic discovery |

### Chiều phụ thuộc bắt buộc:
1. **Domain:** Độc lập hoàn toàn, không phụ thuộc vào các tầng khác hoặc thư viện hạ tầng/ORM.
2. **Application:** Sử dụng Domain, chứa use cases, commands, queries, DTOs và định nghĩa interface.
3. **Infrastructure:** Triển khai các interface từ Application (EF Core DbContext, Repositories, dịch vụ bên thứ 3).
4. **Presentation:** Chứa Minimal API endpoints, tiếp nhận request và gọi use case ở Application.
5. **Module Entry Point (`…ModuleExtensions.cs`):** Nối và đăng ký các thành phần nội bộ vào DI container.

> 🚫 **Điều cấm kỵ:**
> - Tuyệt đối không reference trực tiếp project module khác.
> - Tuyệt đối không truy cập Entity, DbContext, bảng hoặc Repository nội bộ của module khác.

---

## 2. Cách Triển Khai Nghiệp Vụ

| Nội dung | Quyết định |
|---|---|
| **API** | Minimal API; endpoint mỏng, chỉ nhận request, gọi use case và map HTTP response |
| **Use case** | Phân tách class theo nghiệp vụ, phân biệt Commands và Queries khi phù hợp |
| **Persistence** | Application gọi qua interface; implementation EF Core nằm ở Infrastructure |
| **Repository** | Thiết kế theo nhu cầu nghiệp vụ cụ thể; không bắt buộc Generic Repository |
| **DTO** | Tách riêng Request/Response; tuyệt đối không trả entity trực tiếp ra client |
| **Mapping** | Viết tường minh (explicit mapping ban đầu), không lạm dụng reflection |
| **Validation input** | Kiểm tra cú pháp/định dạng tường minh tại Presentation |
| **Validation nghiệp vụ** | Xử lý tại Application và Domain |
| **Lỗi dự kiến (Expected errors)** | Sử dụng pattern `Result` / `Result<T>` |
| **Lỗi bất ngờ (Unexpected errors)** | Ném Exception, xử lý tập trung qua Exception Handling Middleware |
| **Thư viện bổ sung** | Chưa đưa MediatR, FluentValidation, AutoMapper vào mặc định |

*Lưu ý:* Không bắt buộc mỗi class đều phải có interface. Interface chỉ được tạo ở ranh giới persistence, dịch vụ bên ngoài và giao tiếp liên module qua Shared Contracts.

---

## 3. Quy Chuẩn C# Bắt Buộc

- **Quy tắc đặt tên:**
  - Class, Interface, Method, Property: `PascalCase`.
  - Biến cục bộ và parameter: `camelCase`.
  - Private field: `_camelCase`.
  - Interface bắt đầu bằng tiền tố `I`.
  - Method bất đồng bộ luôn có hậu tố `Async`.
- **Tập tin & Namespace:** Tên file trùng tên kiểu chính; Namespace phản ánh đúng cấu trúc thư mục/tầng của module.
- **Null Safety:** Luôn bật Nullable Reference Types (`<Nullable>enable</Nullable>`); không dùng toán tử `!` (null-forgiving) tùy tiện.
- **Dependency Injection:** Constructor Injection; `DbContext` và Repositories có vòng đời `Scoped`.
- **Bất đồng bộ (Async/Await):**
  - Luôn truyền `CancellationToken` qua các tác vụ I/O.
  - Tuyệt đối không dùng `.Result`, `.Wait()`, hoặc `async void` trong xử lý HTTP request.
- **Concurrency:** Không chạy nhiều thao tác song song bất đồng bộ trên cùng một instance `DbContext`.
- **Logging & Security:**
  - Dùng `ILogger<T>`, structured logging và trace ID.
  - Tuyệt đối không log mật khẩu, token, giấy tờ cá nhân hay secrets.
- **Configuration:** Quản lý cấu hình qua Options Pattern (`IOptions<T>`); không commit secret vào Git.

---

## 4. Quy Chuẩn API

### 4.1 URI Design
- Chuẩn RESTful: `/api/v1/<resources>`, dùng danh từ số nhiều; không đặt tên kiểu RPC (ví dụ: cấm `/getAllListings`).

### 4.2 HTTP Status Codes
| Trường hợp | HTTP Status |
|---|---:|
| Đọc/cập nhật có dữ liệu trả về | **200 OK** |
| Tạo mới thành công | **201 Created** (kèm header `Location` khi phù hợp) |
| Thành công không trả nội dung | **204 No Content** |
| Dữ liệu đầu vào hoặc validation sai | **400 Bad Request** |
| Chưa xác thực danh tính | **401 Unauthorized** |
| Không có quyền truy cập | **403 Forbidden** |
| Tài nguyên không tồn tại | **404 Not Found** |
| Xung đột dữ liệu hoặc trạng thái nghiệp vụ | **409 Conflict** |
| Lỗi hệ thống bất ngờ | **500 Internal Server Error** |

### 4.3 Response Envelope (`ApiResponse<T>`)
- Tất cả response trả về client được bọc trong envelope `ApiResponse<T>` với format JSON `camelCase`:
```json
{
  "isSuccess": true,
  "data": { ... },
  "error": null,
  "timestamp": "2026-10-31T09:00:00Z"
}
```
- Khi xảy ra lỗi:
```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "Listing.NotFound",
    "message": "Không tìm thấy tin đăng yêu cầu",
    "details": [
      { "field": "price", "message": "Giá thuê phải lớn hơn 0" }
    ]
  },
  "timestamp": "2026-10-31T09:00:00Z"
}
```
- Header phản hồi luôn có `X-Trace-Id` để đối chiếu log.
- `204 No Content` không có response body.
- Các endpoint hệ thống (Health check, Scalar/OpenAPI) được miễn envelope `ApiResponse<T>`.
- Tuyệt đối không trả `exception.Message` hoặc StackTrace thô ra client.

### 4.4 Phân Trang & Sắp Xếp
- Chuẩn query param: `page=1`, `pageSize=20` (tối đa 100).
- Response phân trang trả về danh sách `items`, cùng các trường `page`, `pageSize`, `totalCount`.
- Sắp xếp chỉ cho phép trên các trường trong danh sách whitelist an toàn và có khóa phụ ổn định.

### 4.5 Phân Quyền & Bảo Mật
- Xác thực qua JWT Bearer token.
- Endpoint yêu cầu quyền hạn phải kiểm tra Policy và **quyền sở hữu tài nguyên (Resource Ownership)** (ví dụ: Landlord chỉ được sửa tài sản/tin đăng của chính mình).

---

## 5. Database và Migrations

| Nội dung | Quyết định |
|---|---|
| **Hệ quản trị CSDL** | Một PostgreSQL database duy nhất |
| **Schema** | Tách riêng theo từng module (`identity`, `properties`, `listings`, v.v.) |
| **DbContext** | Mỗi module sở hữu DbContext độc lập trỏ vào schema tương ứng |
| **ORM** | EF Core + Npgsql |
| **Quy ước đặt tên** | Schema viết thường; bảng và cột theo quy tắc `snake_case` |
| **Khóa chính (ID)** | `UUID` (Guid) cho các thực thể nghiệp vụ độc lập |
| **Thời gian** | Luôn lưu giờ UTC, kiểu dữ liệu `timestamptz` |
| **Tiền tệ** | Kiểu `decimal`/`numeric`, quy định rõ độ chính xác và đơn vị (VND) |
| **Bảng lịch sử Migration** | Độc lập trong từng schema của module (`__EFMigrationsHistory`) |
| **Khóa ngoại liên module** | Chỉ lưu ID tham chiếu; không tạo Navigation Property, Foreign Key hay Cascade Delete xuyên module |
| **Media Storage** | Cloudflare R2; database chỉ lưu URL/object key và metadata liên quan |

### Quy tắc Migrations:
- Mỗi module tự quản lý migrations và snapshot trong project của mình.
- Không sửa file migration đã áp dụng trên các nhánh/môi trường dùng chung.
- Không tự động kích hoạt `Database.Migrate()` trong quá trình khởi động API production.
- **PostGIS:** Tọa độ gốc thuộc quyền quản lý của `Properties`. `Listings` giữ bản sao tọa độ định dạng `geography(Point, 4326)` có chỉ mục GiST để tối ưu truy vấn tìm kiếm theo khoảng cách.

---

## 6. Giao Tiếp Giữa Các Module

| Nhu cầu | Cách thức thực hiện |
|---|---|
| **Cần dữ liệu/kết quả ngay** | Gọi Interface được định nghĩa trong `HomeMatch.Shared/Contracts`, thực thi cùng process (In-Memory DI) |
| **Quy tắc nội bộ Domain** | Domain Event nội bộ bên trong module |
| **Thông báo sự kiện sang module khác** | Integration Event qua in-memory handler đơn giản trong cùng process |
| **Gọi HTTP giữa các module** | **Nghiêm cấm** (tất cả chạy chung một monolith in-process) |

### Nguyên tắc giao tiếp & Cơ chế phục hồi khi không dùng Outbox:
- **Shared Contracts:** Do module cung cấp (producer) làm chủ interface; tuyệt đối không đưa Entity, DbContext hoặc `IQueryable` vào Shared Contracts.
- **Cơ chế phục hồi liên module (Recovery & Idempotency):**
  - Khi không dùng Outbox, các thao tác liên module (ví dụ: Kích hoạt hợp đồng tại `Leasing` -> Lập hóa đơn tại `Billing`) được thực thi in-process qua Shared Contract kèm theo **Trạng thái chuyển tiếp (Pending State) và Khóa Idempotent**:
    1. Module khởi tạo (`Leasing`) lưu hợp đồng ở trạng thái `PendingActivation` (chưa chuyển sang `Active`).
    2. Gọi `IBillingModuleContract.CreateInitialInvoiceAsync(agreementId, idempotencyKey: agreementId)`.
    3. Phía `Billing` sử dụng `idempotencyKey` để đảm bảo không tạo hóa đơn trùng nếu có retry.
    4. Nếu gọi thành công: `Leasing` chuyển trạng thái sang `Active`.
    5. Nếu gọi thất bại: Trả mã lỗi `500` hoặc `409` cho client và hợp đồng chuyển `ActivationFailed`; một tác vụ định kỳ (Reconciliation Job) sẽ quét các bản ghi pending để thử lại an toàn mà không sinh trùng hóa đơn.
- **Favorites:** Thuộc phạm vi của module `Listings` trong giai đoạn MVP (không tách module riêng). Module `Listings` chỉ lưu `UserId` tham chiếu từ `Identity`.

---

## 7. Quy Chuẩn Git & Pull Request

- **Quy tắc nhánh (Branching):**
  - Tuyệt đối không commit/push trực tiếp vào `main`.
  - Tên branch: `feature/<module-name>-<task-name>`.
- **Commit Message (Bắt buộc theo AGENTS.md):**
  - Viết bằng **tiếng Việt không dấu** (không dấu thanh, không ký tự đặc biệt).
  - Định dạng **một dòng duy nhất (Single-line)**, không viết body/description.
  - Ví dụ: `git commit -m "them api tao hop dong thue phong"`.
- **Pull Request:**
  - Giữ PR nhỏ gọn, tập trung vào một phạm vi công việc.
  - Mô tả rõ thay đổi về API hoặc Database migration nếu có.
  - Cần ít nhất 1 thành viên review và approve trước khi merge.
- **Cấm kỵ:** Không force push (`--force`) lên nhánh chung; không tự động merge nếu có conflict hoặc test thất bại.

---

## 8. Kiểm Thử và CI Pipeline

**Tiêu chuẩn bắt buộc trước khi merge PR:**
1. Build Backend (`dotnet build backend/HomeMatch.sln`) và Frontend (`cd frontend && npm run build`) thành công 100%.
2. Unit tests vượt qua toàn bộ cho các logic nghiệp vụ mới.
3. Integration tests chạy thành công trên cơ sở dữ liệu PostgreSQL + PostGIS riêng biệt cho test.
4. Kiểm tra Architecture rules và Code format không có vi phạm.
5. Kiểm tra Migration khởi tạo và rollback an toàn.
6. Docker build và smoke test khởi động API thành công.

---

## 9. Thứ Tự Triển Khai Tiếp Theo

| Thứ tự | Công việc | Điều kiện hoàn thành |
|---|---|---|
| **1** | Hoàn thiện tài liệu quy tắc và các tài liệu kiến trúc liên quan | Nhất quán với `AGENTS.md` và kiến trúc Modular Monolith |
| **2** | Chốt danh sách package, version và ma trận phân quyền API | Không còn lựa chọn kỹ thuật hay thư viện phụ thuộc bỏ ngỏ |
| **3** | Chuẩn hóa phần hạ tầng chung tối thiểu | Xong `ApiResponse<T>`, Error Details, JWT Auth, Base Options, Test Fixtures |
| **4** | Triển khai song song các module theo kiến trúc chuẩn | Tuân thủ tuyệt đối ranh giới 4 tầng, EF Core schema riêng và Minimal API |
| **5** | Tích hợp và kiểm thử các luồng liên module | Hoàn thành Contract Tests và Integration Tests in-process |
