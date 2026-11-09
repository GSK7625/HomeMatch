# Tài Liệu Giải Thích Chi Tiết Thiết Kế & Mã Nguồn Mục 3.3 và 3.4 (Phân Hệ Người Thuê)

Tài liệu này mô tả chi tiết sự thay đổi **Trước / Sau** khi triển khai **Mục 3.3 (Lịch xem phòng và liên hệ chủ nhà)** và **Mục 3.4 (Tổng quan và tài khoản người thuê)**, kèm theo **Sơ đồ kiến trúc trực quan (dạng khung hình văn bản xem được trên mọi trình đọc)** và **Giải thích mã nguồn chuyên sâu từng dòng, từng luồng chạy** theo đúng quy chuẩn [backend-rules.md](backend-rules.md), [modular-monolith.md](modular-monolith.md) và [package-standards.md](package-standards.md).

---

## 1. Tổng Quan Phạm Vi Chức Năng & Ánh Xạ Module

| Mục | Tên chức năng | Vai trò | Module đảm nhận | Schema CSDL | Trạng thái |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **3.3.1** | Đặt và quản lý lịch xem phòng | Người thuê, Chủ nhà | `HomeMatch.Modules.Viewings` | `viewings` | Hoàn thành (Code-First + API) |
| **3.3.2** | Nhắn tin với chủ nhà (kèm Realtime) | Người thuê, Chủ nhà | `HomeMatch.Modules.Messaging` | `messaging` | Hoàn thành (Code-First + API + SignalR) |
| **3.4.1** | Xem dashboard tìm phòng | Người thuê | `HomeMatch.Modules.Identity` | Tổng hợp liên module | Hoàn thành (In-Process Aggregation API) |
| **3.4.2** | Xem danh sách phòng đã xem | Người thuê | `HomeMatch.Modules.Identity` + Contract `Listings` | Đọc qua Contract | Hoàn thành (Shared Contract + API) |
| **3.4.3** | Xem thông báo tài khoản | Người thuê | `HomeMatch.Modules.Notifications` | `notifications` | Hoàn thành (Code-First + API + Publisher) |

---

## 2. Lộ Trình Triển Khai 7 Bước Nhỏ (Incremental Roadmap)

Để đảm bảo kiểm soát chất lượng, dễ kiểm thử từng phần, tuân thủ tuyệt đối kiến trúc **Code-First EF Core** trên **Docker PostgreSQL cổng `5433`** và hoàn toàn không gây xung đột Git với lập trình viên đang làm **Mục 3.2 (Module `Listings`)**, toàn bộ công việc được chia thành **7 bước nhỏ tuần tự**:

```text
+---------------------------------------------------------------------------------------------------+
|                        LỘ TRÌNH 7 BƯỚC TRIỂN KHAI MỤC 3.3 & MỤC 3.4                               |
+---------------------------------------------------------------------------------------------------+
|                                                                                                   |
|  [Bước 1] Module Viewings (Mục 3.3.1 - Phần 1): Domain + ViewingsDbContext + Code-First Migration |
|     |     (Tạo bảng `viewings.viewing_appointments` & `viewings.virtual_viewings` trên cổng 5433) |
|     v                                                                                             |
|  [Bước 2] Module Viewings (Mục 3.3.1 - Phần 2): Shared Contracts + UseCases + REST Endpoints      |
|     |     (Đặt lịch, Duyệt lịch, Hủy lịch, Hoàn thành, Xem danh sách/chi tiết + Fallback Listing) |
|     v                                                                                             |
|  [Bước 3] Module Messaging (Mục 3.3.2 - Phần 1): Domain + MessagingDbContext + Migration          |
|     |     (Tạo bảng `conversations`, `messages`, `message_drafts`, `leads` trên cổng 5433)        |
|     v                                                                                             |
|  [Bước 4] Module Messaging (Mục 3.3.2 - Phần 2): UseCases + REST Endpoints + SignalR ChatHub      |
|     |     (Mở hội thoại, Gửi tin nhắn, Đánh dấu đã đọc, Bản nháp + WebSocket Realtime)            |
|     v                                                                                             |
|  [Bước 5] Module Notifications (Mục 3.4.3): Domain + DbContext + Migration + Publisher + APIs     |
|     |     (Tạo bảng `notifications`, tự động bắn thông báo từ Viewings/Messaging + API xem/đọc)   |
|     v                                                                                             |
|  [Bước 6] Tổng quan & Tài khoản Người thuê (Mục 3.4.1 & 3.4.2): Dashboard & Phòng đã xem APIs     |
|     |     (Tổng hợp dữ liệu liên module In-Process qua Shared Contracts + Kiểm thử End-to-End)    |
|     v                                                                                             |
|  [Bước 7] Giao diện Frontend Next.js cho Mục 3.3 & 3.4 (Bước tiếp theo)                           |
|           (Trang Lịch xem phòng, Trang Chat Realtime SignalR, Trang Dashboard, Phòng đã xem,      |
|            và Chuông thông báo cho Người thuê)                                                    |
+---------------------------------------------------------------------------------------------------+
```

### Chi tiết từng bước trong lộ trình 7 bước:

| Bước | Mục tiêu & Nội dung công việc | Các file tiêu biểu được tạo / cập nhật | Trạng thái |
| :--- | :--- | :--- | :--- |
| **Bước 1** | **Mục 3.3.1 (Phần 1) — Domain & Code-First Database cho `Viewings`:**<br>- Xây dựng Rich Domain Entities (`ViewingAppointment`, `VirtualViewing`), Enums (`ViewingAppointmentStatus`, `ViewingCancelledBy`, `VirtualViewingStatus`), mã lỗi `ViewingErrors`.<br>- Cấu hình Fluent API (`ViewingAppointmentConfiguration`, `VirtualViewingConfiguration`), `ViewingsDbContext` (schema `viewings`).<br>- Tạo và áp dụng EF Core Migration `20261109040000_InitialViewingsSchema` vào Docker DB cổng `5433`. | - `ViewingAppointment.cs`<br>- `VirtualViewing.cs`<br>- `ViewingsDbContext.cs`<br>- `20261109040000_InitialViewingsSchema.cs` | **Đã hoàn thành** |
| **Bước 2** | **Mục 3.3.1 (Phần 2) — Shared Contracts, Use Cases & Endpoints cho `Viewings`:**<br>- Tạo `IListingContractReader` (kèm `FallbackListingContractReader` dùng `TryAddScoped`) và `IViewingStatsReader` trong `HomeMatch.Shared`.<br>- Đăng ký `IIdentityUserReader` vào DI của `IdentityModuleExtensions`.<br>- Viết `ViewingAppointmentRepository` (có kiểm tra trùng lịch ±1h) và 6 Use Case Handlers (Book, Confirm, Cancel, Complete, GetList, GetById).<br>- Mở cụm API `/api/v1/viewings/appointments`. | - `IListingContractReader.cs`<br>- `ViewingAppointmentRepository.cs`<br>- `BookViewingAppointmentHandler.cs`<br>- `ViewingAppointmentEndpoints.cs` | **Đã hoàn thành** |
| **Bước 3** | **Mục 3.3.2 (Phần 1) — Domain & Code-First Database cho `Messaging`:**<br>- Đồng bộ kết nối `appsettings.json` sang cổng `5433` và dọn sạch các bảng nạp sẵn từ SQL cũ.<br>- Xây dựng Domain Entities (`Conversation`, `Message`, `MessageDraft`, `Lead`), Enums (`ConversationType`, `MessageType`, `MessageDraftSource`, `LeadStatus`, `LeadSource`).<br>- Cấu hình Fluent API với cột tính toán `LEAST`/`GREATEST` chống trùng lặp hội thoại, tạo `MessagingDbContext` (schema `messaging`) và Migration `20261109043000_InitialMessagingSchema`. | - `Conversation.cs`, `Message.cs`<br>- `ConversationConfiguration.cs`<br>- `MessagingDbContext.cs`<br>- `20261109043000_InitialMessagingSchema.cs` | **Đã hoàn thành** |
| **Bước 4** | **Mục 3.3.2 (Phần 2) — Use Cases, REST APIs & SignalR Realtime cho `Messaging`:**<br>- Tạo `IMessagingStatsReader` trong `HomeMatch.Shared`.<br>- Viết `ConversationRepository` và 6 Use Case Handlers (StartOrGetConversation, SendMessage, MarkConversationRead, SaveMessageDraft, GetConversations, GetConversationMessages).<br>- Xây dựng **SignalR `ChatHub`** (`/api/v1/messaging/hub` & `/hubs/chat`), `SignalRChatRealtimeNotifier`, và cấu hình đọc JWT `?access_token=` cho WebSocket.<br>- Mở cụm API `/api/v1/messaging/conversations`. | - `SendMessageHandler.cs`<br>- `ChatHub.cs`<br>- `SignalRChatRealtimeNotifier.cs`<br>- `MessagingEndpoints.cs` | **Đã hoàn thành** |
| **Bước 5** | **Mục 3.4.3 — Module `Notifications` (Code-First, Publisher & APIs):**<br>- Tạo `INotificationPublisher` trong `HomeMatch.Shared`.<br>- Xây dựng `Notification` entity, `NotificationType`, `NotificationsDbContext` (schema `notifications`) và Migration `20261109050000_InitialNotificationsSchema`.<br>- Viết `NotificationRepository`, `NotificationPublisher` và tích hợp phát thông báo tự động vào các Handler của `Viewings` và `Messaging`.<br>- Mở cụm API `/api/v1/notifications`. | - `INotificationPublisher.cs`<br>- `Notification.cs`<br>- `NotificationPublisher.cs`<br>- `NotificationEndpoints.cs` | **Đã hoàn thành** |
| **Bước 6** | **Mục 3.4.1 & 3.4.2 — Tổng quan Dashboard & Danh sách phòng đã xem (`Identity`) + Đồng bộ Docker/Nginx:**<br>- Tạo `ITenantListingActivityReader` (kèm `FallbackTenantListingActivityReader` dùng `TryAddScoped`) trong `HomeMatch.Shared`.<br>- Viết `GetTenantDashboardHandler` (tổng hợp dữ liệu liên module từ `Identity`, `Listings`, `Viewings`, `Messaging`) và `GetViewedListingsHandler`.<br>- Mở 2 API `GET /api/v1/identity/tenant/dashboard` và `GET /api/v1/identity/tenant/viewed-listings`.<br>- Đồng bộ `Program.cs` (auto-migrate 4 DbContext) và `nginx.conf` (WebSocket timeout). | - `ITenantListingActivityReader.cs`<br>- `GetTenantDashboardHandler.cs`<br>- `GetViewedListingsHandler.cs`<br>- `TenantOverviewEndpoints.cs` | **Đã hoàn thành** |
| **Bước 7** | **Giao diện Frontend (Next.js App Router) cho Mục 3.3 & 3.4:**<br>- Xây dựng giao diện Đặt & Quản lý lịch xem phòng (Mục 3.3.1).<br>- Xây dựng giao diện Nhắn tin với chủ nhà tích hợp SignalR Realtime (Mục 3.3.2).<br>- Xây dựng giao diện Dashboard tìm phòng (Mục 3.4.1), Danh sách phòng đã xem (Mục 3.4.2) và Trung tâm thông báo (Mục 3.4.3). | - `frontend/src/lib/api-client.ts`<br>- `frontend/src/features/messaging/signalr-client.ts`<br>- `frontend/src/app/tenant/viewings/page.tsx`<br>- `frontend/src/app/messages/page.tsx`<br>- `frontend/src/app/tenant/dashboard/page.tsx`<br>- `frontend/src/app/tenant/viewed-listings/page.tsx`<br>- `frontend/src/app/tenant/notifications/page.tsx`<br>- `frontend/src/components/Navbar.tsx` | **Đã hoàn thành** |

---

## 3. So Sánh Chi Tiết Trước và Sau Khi Thực Hiện (Before vs. After)

| Thành phần | TRƯỚC khi làm Mục 3.3 & 3.4 | SAU khi làm Mục 3.3 & 3.4 |
| :--- | :--- | :--- |
| **Cơ sở dữ liệu & Kết nối** | `appsettings.json` trỏ vào cổng `5432` (PostgreSQL trên máy host). Các bảng nghiệp vụ ngoài `identity` trước đó được nạp thủ công từ file SQL tổng `HomeMatch_Database.sql`. | Chuyển sang kết nối thống nhất **Docker Database cổng `5433`** (`homematch-db`). Dọn sạch hoàn toàn các bảng nạp sẵn bằng SQL; **100% bảng được sinh tự động bằng EF Core Code-First Migrations** theo từng module (`identity`, `viewings`, `messaging`, `notifications`). |
| **Module `Viewings`** | Chỉ là khung xương rỗng (`.gitkeep`), chỉ có duy nhất endpoint `/api/v1/viewings/ping`, chưa có Entity, DbContext, Migration hay logic nghiệp vụ. | Có đầy đủ **Code-First Domain, `ViewingsDbContext`, Migration (`20261109040000_InitialViewingsSchema`)**, Repository, 6 Use Cases (Đặt lịch, Xác nhận, Hủy, Hoàn thành, Danh sách, Chi tiết) và 6 RESTful Endpoints. |
| **Module `Messaging`** | Chỉ là khung xương rỗng (`.gitkeep`), chỉ có `/api/v1/messaging/ping`, chưa có Entity, DbContext, Migration, API hay SignalR. | Có đầy đủ **Code-First Domain, `MessagingDbContext`, Migration (`20261109043000_InitialMessagingSchema`)**, Repository, 6 Use Cases (Tạo/mở hội thoại, Gửi tin nhắn, Lịch sử chat, Danh sách hội thoại, Đánh dấu đã đọc, Lưu bản nháp) và **SignalR `ChatHub` Realtime**. |
| **Module `Notifications`** | Chỉ là khung xương rỗng (`.gitkeep`), chỉ có `/api/v1/notifications/ping`. | Có đầy đủ **Code-First Domain, `NotificationsDbContext`, Migration (`20261109050000_InitialNotificationsSchema`)**, Shared Contract `INotificationPublisher` (tự động phát thông báo khi đặt/duyệt/hủy lịch hoặc có tin nhắn mới) và 3 API Endpoints. |
| **Module `Identity` & `HomeMatch.Shared`** | Mới chỉ có `IIdentityUserReader` (nhưng chưa đăng ký vào DI trong `IdentityModuleExtensions`) và API `/api/v1/identity/me`. | Đã đăng ký `IIdentityUserReader` vào DI; bổ sung 5 Shared Contracts mới (`IListingContractReader`, `ITenantListingActivityReader`, `IViewingStatsReader`, `IMessagingStatsReader`, `INotificationPublisher`) và 2 API cho Mục 3.4 (`/tenant/dashboard`, `/tenant/viewed-listings`). |
| **Tính độc lập với Mục 3.2 (`Listings`)** | Chưa có cơ chế giao tiếp hay tách biệt với module `Listings` (do dev khác đang làm song song). | **Hoàn toàn không chạm vào bất kỳ file nào trong `Modules/Listings`** (tránh 100% xung đột Git). Sử dụng `TryAddScoped` với `FallbackListingContractReader` và `FallbackTenantListingActivityReader` để chạy độc lập ngay lập tức và tự động nhường quyền khi Dev 3.2 đăng ký implementation thật. |
| **Docker Compose & Nginx** | `Program.cs` chỉ mới chạy `MigrateAsync()` cho `IdentityDbContext`. Nginx chưa có cấu hình timeout dài cho WebSocket. | `Program.cs` tự động chạy `MigrateAsync()` cho cả 4 DbContext (`Identity`, `Viewings`, `Messaging`, `Notifications`). `nginx.conf` và `ChatHub` đã đồng bộ đường dẫn `/hubs/chat` và `/api/v1/messaging/hub` kèm timeout 3600s cho kết nối WebSocket. |

---

## 4. Sơ Đồ Kiến Trúc Trực Quan (Xem Trực Tiếp Không Cần Plugin)

### 4.1. Sơ đồ tổng thể kiến trúc Modular Monolith (Mục 3.3 & 3.4)

```text
+---------------------------------------------------------------------------------------------------+
|                             TRÌNH DUYỆT (FRONTEND NEXT.JS - PORT 3000)                            |
|   [Trang Lịch Xem Phòng]       [Trang Nhắn Tin Realtime]       [Trang Dashboard & Thông Báo]      |
+-------------+------------------------------+-------------------+----------------------------------+
              | REST HTTP/JSON               | WebSocket (SignalR ?access_token=JWT)
              v                              v
+---------------------------------------------------------------------------------------------------+
|                                   NGINX / HOMEMATCH.API (PORT 5000)                               |
|   /api/v1/viewings/*          /api/v1/messaging/* + /hubs/chat    /api/v1/identity/* + /notif/*   |
+-------------+------------------------------+-----------------------------------+------------------+
              |                              |                                   |
              v                              v                                   v
+---------------------------+  +---------------------------+  +-------------------------------------+
|     MODULE VIEWINGS       |  |     MODULE MESSAGING      |  |    MODULE IDENTITY & NOTIFICATIONS  |
|     (Mục 3.3.1)           |  |     (Mục 3.3.2)           |  |    (Mục 3.4.1, 3.4.2, 3.4.3)        |
|                           |  |                           |  |                                     |
| - ViewingEndpoints        |  | - MessagingEndpoints      |  | - TenantOverviewEndpoints           |
| - Book/Confirm/Cancel     |  | - ChatHub (SignalR)       |  | - GetTenantDashboardHandler         |
| - ViewingRepository       |  | - SendMessageHandler      |  | - GetViewedListingsHandler          |
| - ViewingsDbContext       |  | - MessagingDbContext      |  | - NotificationsDbContext            |
+-------------+-------------+  +-------------+-------------+  +------------------+------------------+
              |                              |                                   |
              +------------------------------+-----------------------------------+
                                             |
                        Giao tiếp nội bộ qua | Interface (In-Process DI)
                                             v
+---------------------------------------------------------------------------------------------------+
|                         TẦNG GIAO TIẾP CHUNG: HOMEMATCH.SHARED (CONTRACTS)                        |
|                                                                                                   |
|  1. IIdentityUserReader          ---> Lấy thông tin Họ tên, SĐT, Avatar của User                  |
|  2. IListingContractReader       ---> Lấy thông tin Phòng trọ & Chủ nhà (TryAddScoped Fallback)   |
|  3. ITenantListingActivityReader ---> Lấy lịch sử xem phòng & số liệu (TryAddScoped Fallback)     |
|  4. IViewingStatsReader          ---> Lấy số lượng lịch hẹn (Sắp tới, Chờ duyệt, Đã duyệt)        |
|  5. IMessagingStatsReader        ---> Lấy tổng số tin nhắn chưa đọc của người dùng                |
|  6. INotificationPublisher       ---> Phát thông báo tự động khi Đặt/Duyệt/Hủy lịch & Nhắn tin    |
+-------------+------------------------------+-----------------------------------+------------------+
              |                              |                                   |
              v                              v                                   v
+---------------------------------------------------------------------------------------------------+
|                       DOCKER POSTGRESQL 16 + POSTGIS (LOCALHOST:5433)                             |
|                                                                                                   |
|  +-------------------+   +-------------------+   +-------------------+   +---------------------+  |
|  | Schema: identity  |   | Schema: viewings  |   | Schema: messaging |   | Schema:             |  |
|  | - users           |   | - viewing_        |   | - conversations   |   |   notifications     |  |
|  | - roles           |   |   appointments    |   | - messages        |   | - notifications     |  |
|  | - user_roles      |   | - virtual_        |   | - message_drafts  |   | - __EFMigrations... |  |
|  | - refresh_tokens  |   |   viewings        |   | - leads           |   +---------------------+  |
|  | - __EFMigrations..|   | - __EFMigrations..|   | - __EFMigrations..|                            |
|  +-------------------+   +-------------------+   +-------------------+                            |
+---------------------------------------------------------------------------------------------------+
```

---

### 4.2. Sơ đồ luồng xử lý bên trong 1 Module (Clean Architecture 4 Tầng)

Mọi chức năng trong Mục 3.3 và 3.4 đều tuân thủ nghiêm ngặt 4 tầng từ ngoài vào trong như sau:

```text
[1. PRESENTATION LAYER] (Endpoints / SignalR Hub)
   |  - Nhận HTTP Request hoặc WebSocket frame từ người dùng
   |  - Đọc UserId từ JWT Token (ClaimsPrincipal)
   |  - Gọi trực tiếp UseCase Handler (Không dùng MediatR)
   v
[2. APPLICATION LAYER] (UseCases: Command/Query + Handler + DTO + Mapper)
   |  - Điều phối luồng nghiệp vụ (Orchestration)
   |  - Gọi Shared Contracts (ví dụ: kiểm tra phòng có tồn tại không)
   |  - Gọi phương thức của Domain Entity để kiểm tra luật nghiệp vụ
   |  - Lưu xuống DB thông qua Repository Interface
   |  - Chuyển đổi Entity sang DTO bằng Static Mapper (Không dùng AutoMapper)
   v
[3. DOMAIN LAYER] (Entities + Enums + Constants/Errors)
   |  - Chứa trái tim nghiệp vụ (Rich Domain Model)
   |  - Mọi thuộc tính đều là `private set` (không ai bên ngoài sửa tùy tiện được)
   |  - Chứa các hàm nghiệp vụ: Create(), Confirm(), Cancel(), Complete(), AddMessage()
   |  - Trả về `Result<T>` hoặc `Result` (Không ném Exception cho lỗi nghiệp vụ)
   v
[4. INFRASTRUCTURE LAYER] (DbContext + EF Configurations + Repositories + Readers)
      - Ánh xạ C# Entity thành bảng PostgreSQL (Fluent API Configuration)
      - Thực thi câu lệnh SQL qua EF Core (AsNoTracking cho đọc, SaveChangesAsync cho ghi)
      - Triển khai các Shared Contracts (ViewingStatsReader, MessagingStatsReader, SignalRNotifier)
```

---

## 5. Giải Thích Mã Nguồn Chuyên Sâu (Code Walkthrough Từng Thành Phần)

### 5.1. Cơ chế `Result<T>` và `ApiResponse<T>` — Tại sao không dùng `throw new Exception()`?

Trong toàn bộ dự án, chúng ta sử dụng [`Result.cs`](../../backend/src/HomeMatch.Shared/Results/Result.cs) để biểu diễn kết quả thành công hoặc thất bại của một hành động nghiệp vụ:

```csharp
// Khi thành công:
return Result.Success(dto);

// Khi vi phạm quy tắc nghiệp vụ (ví dụ: đặt lịch vào thời điểm trong quá khứ):
return Result.Failure<ViewingAppointmentDto>(
    new Error(ViewingErrors.InvalidScheduledTime, "Thời gian hẹn xem phòng phải nằm trong tương lai."));
```

**Ý nghĩa:**
1. `throw Exception` trong .NET rất tốn tài nguyên CPU (do phải thu thập StackTrace) và làm luồng code nhảy bất thường.
2. Với `Result<T>`, mọi lỗi nghiệp vụ đều tường minh. Khi trả về tới tầng Presentation (`Endpoints`), hàm `ResultExtensions.ToProblemResponse()` sẽ tự động đọc mã lỗi:
   - Nếu mã lỗi có chứa `.NOT_FOUND` -> trả về HTTP `404 Not Found`.
   - Nếu mã lỗi có chứa `.FORBIDDEN` -> trả về HTTP `403 Forbidden`.
   - Nếu mã lỗi có chứa `.CONFLICT` -> trả về HTTP `409 Conflict`.
   - Các lỗi kiểm tra đầu vào khác -> trả về HTTP `400 Bad Request`.
   Tất cả đều được gói trong khung JSON chuẩn `{ "success": false, "error": { "code": "...", "message": "..." } }`.

---

### 5.2. Giải thích kỹ thuật `TryAddScoped` trong `HomeMatch.Shared` — Cách làm việc song song không đụng chạm Mục 3.2 (`Listings`)

Khi Người thuê đặt lịch xem phòng (`ListingId = 101`) hoặc nhắn tin hỏi thuê phòng (`ListingId = 101`), Module `Viewings` và `Messaging` cần biết:
- Phòng `101` có tồn tại và đang mở cho thuê (`IsAvailable`) không?
- Chủ nhà của phòng `101` là ai (`LandlordId`)?
- Tiêu đề, địa chỉ, giá thuê của phòng `101` là bao nhiêu?

Vì Module `Listings` (Mục 3.2) đang do một lập trình viên khác phát triển trên nhánh riêng, nếu chúng ta sửa code trong `Modules/Listings`, lúc gộp code (Git Merge) chắc chắn sẽ xảy ra xung đột.

**Cách giải quyết trong code:**
1. Tạo Interface [`IListingContractReader.cs`](../../backend/src/HomeMatch.Shared/Contracts/Listings/IListingContractReader.cs) trong `HomeMatch.Shared`:
   ```csharp
   public interface IListingContractReader
   {
       Task<ListingContractInfo?> GetByIdAsync(long listingId, CancellationToken cancellationToken = default);
       Task<IReadOnlyDictionary<long, ListingContractInfo>> GetByIdsAsync(
           IEnumerable<long> listingIds,
           CancellationToken cancellationToken = default);
   }
   ```
2. Tạo lớp dự phòng [`FallbackListingContractReader.cs`](../../backend/src/Modules/Viewings/HomeMatch.Modules.Viewings/Infrastructure/Services/FallbackListingContractReader.cs) trả về thông tin phòng mẫu hợp lệ gắn với tài khoản Chủ trọ (`LandlordId = 2`).
3. Trong [`ViewingsModuleExtensions.cs`](../../backend/src/Modules/Viewings/HomeMatch.Modules.Viewings/ViewingsModuleExtensions.cs), đăng ký bằng **`TryAddScoped`**:
   ```csharp
   // TryAddScoped có nghĩa là: "Chỉ đăng ký FallbackListingContractReader NẾU chưa có ai đăng ký IListingContractReader"
   services.TryAddScoped<IListingContractReader, FallbackListingContractReader>();
   ```
   - **Hiện tại:** Chưa có `ListingsContractReader` thật -> Hệ thống dùng `FallbackListingContractReader`, giúp toàn bộ API và UI của Mục 3.3 & 3.4 chạy trơn tru 100%.
   - **Khi ghép với Mục 3.2:** Dev 3.2 chỉ cần đăng ký `services.AddScoped<IListingContractReader, ListingContractReader>()` trong `ListingsModuleExtensions`. Lập tức `TryAddScoped` sẽ tự động nhường quyền cho bản thật mà không phải xóa hay sửa dòng code nào của chúng ta!

---

### 5.3. Giải thích chi tiết Module `Viewings` (Mục 3.3.1: Đặt và quản lý lịch xem phòng)

#### A. Thực thể Domain [`ViewingAppointment.cs`](../../backend/src/Modules/Viewings/HomeMatch.Modules.Viewings/Domain/Entities/ViewingAppointment.cs)

Đây là lớp trung tâm kiểm soát toàn bộ vòng đời của một lịch hẹn xem phòng:

```text
                  Người thuê đặt lịch (Create)
                               |
                               v
                         +-----------+
                         |  PENDING  | (Chờ chủ nhà xác nhận)
                         +-----+-----+
                               |
          +--------------------+--------------------+
          | Chủ nhà duyệt                           | Người thuê hoặc Chủ nhà hủy
          | (Confirm)                               | (Cancel)
          v                                         v
    +-----------+                             +-----------+
    | CONFIRMED |                             | CANCELLED | (Trạng thái kết thúc)
    +-----+-----+                             +-----------+
          |                                         ^
          | Chủ nhà xác nhận đã xem xong            | Người thuê hoặc Chủ nhà hủy
          | (Complete)                              | trước khi buổi xem kết thúc
          v                                         |
    +-----------+                                   |
    | COMPLETED | (Trạng thái kết thúc)             +
    +-----------+
```

Hãy xem từng hàm trong [`ViewingAppointment.cs`](../../backend/src/Modules/Viewings/HomeMatch.Modules.Viewings/Domain/Entities/ViewingAppointment.cs):

1. **Hàm khởi tạo `ViewingAppointment.Create(...)` (Dòng 31 - 82):**
   ```csharp
   public static Result<ViewingAppointment> Create(
       long listingId,
       long tenantId,
       long landlordId,
       DateTimeOffset scheduledAt,
       int durationMins,
       bool isVirtual,
       string? tenantNotes,
       string? meetingUrl = null)
   {
       // Quy tắc 1: Người thuê không được đặt lịch xem phòng do chính mình làm chủ nhà
       if (tenantId == landlordId)
       {
           return Result.Failure<ViewingAppointment>(
               new Error(ViewingErrors.SelfBookingNotAllowed, "Bạn không thể tự đặt lịch xem phòng của chính mình."));
       }

       // Quy tắc 2: Thời gian hẹn phải lớn hơn thời điểm hiện tại ít nhất 5 phút
       if (scheduledAt <= DateTimeOffset.UtcNow.AddMinutes(5))
       {
           return Result.Failure<ViewingAppointment>(
               new Error(ViewingErrors.InvalidScheduledTime, "Thời gian hẹn xem phòng phải nằm trong tương lai (ít nhất 5 phút kể từ hiện tại)."));
       }

       // Quy tắc 3: Thời lượng buổi xem phải từ 15 đến 180 phút
       if (durationMins is < 15 or > 180)
       {
           return Result.Failure<ViewingAppointment>(
               new Error(ViewingErrors.InvalidDuration, "Thời lượng xem phòng phải từ 15 đến 180 phút."));
       }
       ...
   ```
   - Nếu `isVirtual == true` (xem phòng qua Video Call), hàm `Create` tự động tạo luôn đối tượng con [`VirtualViewing`](../../backend/src/Modules/Viewings/HomeMatch.Modules.Viewings/Domain/Entities/VirtualViewing.cs) với đường dẫn phòng họp (nếu người dùng không truyền link thì hệ thống tự sinh link `https://meet.homematch.vn/room-...`).

2. **Hàm duyệt lịch `Confirm(long actorUserId, string? landlordNotes)` (Dòng 84 - 103):**
   ```csharp
   public Result Confirm(long actorUserId, string? landlordNotes)
   {
       // Kiểm tra phân quyền: Chỉ có Chủ nhà của lịch hẹn này mới được phép duyệt
       if (actorUserId != LandlordId)
       {
           return Result.Failure(new Error(ViewingErrors.ForbiddenAccess, "Chỉ chủ nhà mới có quyền xác nhận lịch xem phòng."));
       }

       // Kiểm tra trạng thái: Chỉ duyệt được khi lịch đang ở trạng thái PENDING
       if (Status != ViewingAppointmentStatus.PENDING)
       {
           return Result.Failure(new Error(ViewingErrors.InvalidStatusTransition, $"Không thể xác nhận lịch hẹn đang ở trạng thái {Status}."));
       }

       Status = ViewingAppointmentStatus.CONFIRMED;
       ...
   ```

3. **Hàm hủy lịch `Cancel(long actorUserId, string? cancelReason)` (Dòng 105 - 131):**
   - Kiểm tra `actorUserId` có phải là `TenantId` hoặc `LandlordId` của lịch hẹn không (người lạ không thể hủy lịch của người khác).
   - Không cho phép hủy lịch đã `COMPLETED` hoặc đã `CANCELLED`.
   - Tự động gán `CancelledBy = actorUserId == TenantId ? ViewingCancelledBy.TENANT : ViewingCancelledBy.LANDLORD` để biết rõ ai là người hủy.

#### B. Kiểm tra trùng lịch trong [`ViewingAppointmentRepository.cs`](../../backend/src/Modules/Viewings/HomeMatch.Modules.Viewings/Infrastructure/Repositories/ViewingAppointmentRepository.cs)

Trong hàm `HasActiveAppointmentAsync` (Dòng 33 - 51):
```csharp
public async Task<bool> HasActiveAppointmentAsync(
    long listingId,
    long tenantId,
    DateTimeOffset scheduledAt,
    CancellationToken cancellationToken = default)
{
    DateTimeOffset windowStart = scheduledAt.AddHours(-1);
    DateTimeOffset windowEnd = scheduledAt.AddHours(1);

    return await dbContext.ViewingAppointments
        .AsNoTracking()
        .AnyAsync(
            x => x.ListingId == listingId
                && x.TenantId == tenantId
                && (x.Status == ViewingAppointmentStatus.PENDING || x.Status == ViewingAppointmentStatus.CONFIRMED)
                && x.ScheduledAt >= windowStart
                && x.ScheduledAt <= windowEnd,
            cancellationToken);
}
```
- **Giải thích:** Khi người thuê bấm đặt lịch cho phòng `X` vào lúc `10:00`, câu truy vấn này kiểm tra trong bảng `viewings.viewing_appointments` xem người thuê đó đã có lịch nào đang chờ duyệt (`PENDING`) hoặc đã duyệt (`CONFIRMED`) cho phòng `X` trong khoảng từ `09:00` đến `11:00` chưa. Nếu có, hệ thống chặn lại và báo lỗi `409 Conflict` để tránh spam đặt trùng lịch.

#### C. Luồng điều phối trong [`BookViewingAppointmentHandler.cs`](../../backend/src/Modules/Viewings/HomeMatch.Modules.Viewings/Application/UseCases/BookViewingAppointment/BookViewingAppointmentHandler.cs)

Khi người thuê gửi request `POST /api/v1/viewings/appointments`, Handler thực hiện tuần tự 6 bước:
1. **Bước 1:** Gọi `listingReader.GetByIdAsync(command.ListingId)` kiểm tra phòng có tồn tại và đang mở cho thuê không.
2. **Bước 2:** Gọi `appointmentRepository.HasActiveAppointmentAsync(...)` kiểm tra trùng lịch hẹn trong vòng ±1 giờ.
3. **Bước 3:** Gọi `ViewingAppointment.Create(...)` để kiểm tra luật Domain và khởi tạo thực thể.
4. **Bước 4:** Gọi `appointmentRepository.AddAsync(...)` và `SaveChangesAsync()` để lưu vào bảng `viewings.viewing_appointments` (và `viewings.virtual_viewings` nếu xem online).
5. **Bước 5:** Gọi `identityUserReader.GetByIdsAsync([TenantId, LandlordId])` để lấy họ tên, số điện thoại của 2 bên.
6. **Bước 6:** Gọi `notificationPublisher.PublishAsync(...)` để tạo ngay 1 thông báo mới cho Chủ nhà: *"Bạn có yêu cầu xem phòng mới cho '...' vào lúc ..."*, sau đó trả kết quả DTO về cho trình duyệt.

---

### 5.4. Giải thích chi tiết Module `Messaging` & SignalR Realtime (Mục 3.3.2: Nhắn tin với chủ nhà)

#### A. Cách chống tạo trùng cuộc hội thoại bằng `UserMinId` & `UserMaxId`

Giả sử Người thuê (`UserId = 3`) và Chủ nhà (`UserId = 2`) nhắn tin trao đổi về Phòng trọ (`ListingId = 101`).
- Nếu Người thuê bấm nhắn trước: `UserAId = 3`, `UserBId = 2`.
- Nếu Chủ nhà bấm nhắn trước: `UserAId = 2`, `UserBId = 3`.
Nếu chỉ đặt Unique Index trên `(ListingId, UserAId, UserBId)`, cơ sở dữ liệu sẽ coi `(101, 3, 2)` và `(101, 2, 3)` là 2 dòng khác nhau -> sinh ra 2 hộp chat trùng lặp giữa cùng 2 người!

**Cách giải quyết trong [`Conversation.cs`](../../backend/src/Modules/Messaging/HomeMatch.Modules.Messaging/Domain/Entities/Conversation.cs) và [`ConversationConfiguration.cs`](../../backend/src/Modules/Messaging/HomeMatch.Modules.Messaging/Infrastructure/Persistence/Configurations/ConversationConfiguration.cs):**
- Trong C# (`Conversation.Create`):
  ```csharp
  UserMinId = Math.Min(userAId, userBId), // Luôn bằng 2
  UserMaxId = Math.Max(userAId, userBId), // Luôn bằng 3
  ```
- Trong PostgreSQL (`ConversationConfiguration.cs` dòng 24 - 42):
  ```csharp
  builder.Property(x => x.UserMinId)
      .HasColumnName("user_min_id")
      .HasComputedColumnSql("LEAST(user_a_id, user_b_id)", stored: true);

  builder.Property(x => x.UserMaxId)
      .HasColumnName("user_max_id")
      .HasComputedColumnSql("GREATEST(user_a_id, user_b_id)", stored: true);

  builder.HasIndex(x => new { x.ConvType, x.ListingId, x.UserMinId, x.UserMaxId })
      .IsUnique()
      .HasFilter("listing_id IS NOT NULL")
      .HasDatabaseName("uq_conv_listing");
  ```
- **Kết quả:** Dù ai là người bắt đầu cuộc trò chuyện trước, cặp `(UserMinId, UserMaxId)` luôn luôn là `(2, 3)`. Cả trong code C# (`GetByParticipantsAsync`) lẫn tầng ổ cứng PostgreSQL (`uq_conv_listing`) đều đảm bảo **tuyệt đối chỉ có 1 cuộc hội thoại duy nhất** giữa 2 người cho phòng đó.

#### B. Sơ đồ luồng gửi tin nhắn & đẩy Realtime qua SignalR

```text
[Người Thuê (User 3)]                        [Backend HomeMatch.Api]                       [Chủ Nhà (User 2)]
         |                                              |                                           |
         | 1. POST /api/v1/messaging/conversations/1/messages                                       |
         |    Body: { "content": "Phòng còn không ạ?" } |                                           |
         +--------------------------------------------->|                                           |
         |                                              | 2. SendMessageHandler:                    |
         |                                              |    - Kiểm tra User 3 thuộc hội thoại #1   |
         |                                              |    - Lưu tin nhắn vào bảng `messages`     |
         |                                              |    - Cập nhật `last_message_at`           |
         |                                              |                                           |
         |                                              | 3. SignalRChatRealtimeNotifier:           |
         |                                              |    Gửi sự kiện WebSocket "ReceiveMessage" |
         |                                              +------------------------------------------>|
         |                                              |    tới Group "user:2" và "conversation:1" | (Hiển thị tin nhắn
         |                                              |                                           |  ngay lập tức!)
         |                                              | 4. INotificationPublisher:                |
         |                                              |    Lưu thông báo vào bảng `notifications` |
         | 5. Trả về HTTP 201 Created + MessageDto      |                                           |
         |<---------------------------------------------+                                           |
```

#### C. Giải thích code SignalR (`ChatHub.cs`, `SignalRChatRealtimeNotifier.cs` và JWT WebSocket Auth)

1. **Xác thực JWT cho kết nối WebSocket trong [`IdentityModuleExtensions.cs`](../../backend/src/Modules/Identity/HomeMatch.Modules.Identity/IdentityModuleExtensions.cs#L58-L73):**
   - Với REST API thông thường, trình duyệt gửi JWT trong header `Authorization: Bearer <token>`.
   - Tuy nhiên, chuẩn WebSocket trên trình duyệt **không hỗ trợ gắn custom HTTP Header** khi bắt tay kết nối (`handshake`), mà SignalR Client sẽ truyền token qua URL query string: `ws://localhost:5000/api/v1/messaging/hub?access_token=<token>`.
   - Do đó, đoạn code sau trong `IdentityModuleExtensions.cs` giúp trích xuất token từ `access_token` cho các đường dẫn Hub:
     ```csharp
     options.Events = new JwtBearerEvents
     {
         OnMessageReceived = context =>
         {
             StringValues accessToken = context.Request.Query["access_token"];
             PathString path = context.HttpContext.Request.Path;
             if (!string.IsNullOrEmpty(accessToken) &&
                 (path.StartsWithSegments("/api/v1/messaging/hub") || path.StartsWithSegments("/hubs/chat")))
             {
                 context.Token = accessToken;
             }
             return Task.CompletedTask;
         }
     };
     ```

2. **Quản lý phòng chat trong [`ChatHub.cs`](../../backend/src/Modules/Messaging/HomeMatch.Modules.Messaging/Presentation/Hubs/ChatHub.cs):**
   - `OnConnectedAsync()`: Ngay khi người dùng mở ứng dụng, lấy `userId` từ JWT Token và đưa kết nối vào nhóm riêng `user:{userId}`. Nhờ vậy, dù người dùng đang ở bất kỳ trang nào trên web, họ vẫn nhận được thông báo có tin nhắn mới.
   - `JoinConversation(long conversationId)`: Khi người dùng bấm mở một cuộc hội thoại cụ thể, kết nối được đưa thêm vào nhóm `conversation:{conversationId}` để nhận trạng thái *"Đang nhập..."* (`UserTyping`) và trạng thái *"Đã xem"* (`ConversationRead`).

3. **Đẩy tin nhắn tức thời trong [`SignalRChatRealtimeNotifier.cs`](../../backend/src/Modules/Messaging/HomeMatch.Modules.Messaging/Infrastructure/Realtime/SignalRChatRealtimeNotifier.cs):**
   ```csharp
   public async Task NotifyNewMessageAsync(
       long conversationId,
       long senderUserId,
       long recipientUserId,
       MessageDto message,
       CancellationToken cancellationToken = default)
   {
       await Task.WhenAll(
           // Đẩy tới tất cả các tab trình duyệt của người nhận
           hubContext.Clients.Group(ChatHub.GetUserGroup(recipientUserId))
               .SendAsync("ReceiveMessage", message, cancellationToken),
           // Đẩy tới các tab trình duyệt khác của chính người gửi để đồng bộ giao diện
           hubContext.Clients.Group(ChatHub.GetUserGroup(senderUserId))
               .SendAsync("ReceiveMessage", message, cancellationToken));
   }
   ```

---

### 5.5. Giải thích chi tiết Module `Notifications` (Mục 3.4.3: Xem thông báo)

1. **Thực thể [`Notification.cs`](../../backend/src/Modules/Notifications/HomeMatch.Modules.Notifications/Domain/Entities/Notification.cs):**
   - Lưu trữ `UserId` (người nhận), `Type` (`VIEWING`, `MESSAGE`, `LISTING`, `AI_INSIGHT`, `SYSTEM`), `Title`, `Body`, `DataJson` (chuỗi JSON lưu thông tin ngữ cảnh như `{"appointmentId": 1, "listingId": 101}` để khi người dùng bấm vào thông báo trên giao diện, Frontend biết cần điều hướng tới trang nào), cùng trạng thái `IsRead` và thời điểm đọc `ReadAt`.
   - Hàm `MarkAsRead()` kiểm tra nếu thông báo chưa đọc (`!IsRead`) thì mới cập nhật `IsRead = true` và `ReadAt = DateTimeOffset.UtcNow`.

2. **Cầu nối phát thông báo [`NotificationPublisher.cs`](../../backend/src/Modules/Notifications/HomeMatch.Modules.Notifications/Infrastructure/Services/NotificationPublisher.cs):**
   - Triển khai interface `INotificationPublisher` từ `HomeMatch.Shared`.
   - Khi `BookViewingAppointmentHandler`, `ConfirmViewingAppointmentHandler`, `CancelViewingAppointmentHandler` hoặc `SendMessageHandler` chạy xong, chúng gọi `notificationPublisher.PublishAsync(...)` để tự động ghi thông báo vào bảng `notifications.notifications`.

3. **Truy vấn tối ưu trong [`NotificationRepository.cs`](../../backend/src/Modules/Notifications/HomeMatch.Modules.Notifications/Infrastructure/Repositories/NotificationRepository.cs):**
   - Hàm `MarkAllAsReadAsync(long userId)` sử dụng câu lệnh cập nhật hàng loạt `ExecuteUpdateAsync` của EF Core:
     ```csharp
     return await dbContext.Notifications
         .Where(x => x.UserId == userId && !x.IsRead)
         .ExecuteUpdateAsync(
             setters => setters
                 .SetProperty(x => x.IsRead, true)
                 .SetProperty(x => x.ReadAt, now),
             cancellationToken);
     ```
   - **Ưu điểm:** Thay vì phải tải hàng trăm dòng thông báo vào RAM rồi sửa từng dòng, EF Core dịch thẳng thành 1 câu SQL `UPDATE notifications.notifications SET is_read = TRUE, read_at = NOW() WHERE user_id = @p0 AND NOT is_read` cực kỳ nhanh.

---

### 5.6. Giải thích chi tiết Tổng quan & Tài khoản Người thuê (Mục 3.4.1 & 3.4.2)

#### A. Bộ tổng hợp Dashboard [`GetTenantDashboardHandler.cs`](../../backend/src/Modules/Identity/HomeMatch.Modules.Identity/Application/UseCases/GetTenantDashboard/GetTenantDashboardHandler.cs)

Trên giao diện Dashboard của Người thuê (Mục 3.4.1), người dùng cần nhìn thấy bức tranh toàn cảnh chỉ trong **1 lần tải trang**:
- Thông tin cá nhân (Họ tên, Email, SĐT, Ảnh đại diện).
- Số lượng phòng đã xem (`ViewedListingsCount`), số phòng đã lưu yêu thích (`SavedListingsCount`), số lượt tìm kiếm (`RecentSearchesCount`).
- Số lịch hẹn xem phòng sắp tới (`UpcomingViewingsCount`), số lịch đang chờ duyệt (`PendingViewingsCount`), đã xác nhận (`ConfirmedViewingsCount`), đã hoàn thành (`CompletedViewingsCount`).
- Tổng số tin nhắn chưa đọc (`UnreadMessagesCount`).
- Danh sách 5 phòng vừa xem gần đây nhất (`RecentlyViewedListings`).

Hãy xem cách [`GetTenantDashboardHandler.cs`](../../backend/src/Modules/Identity/HomeMatch.Modules.Identity/Application/UseCases/GetTenantDashboard/GetTenantDashboardHandler.cs) thu thập toàn bộ dữ liệu này mà **không hề vi phạm ranh giới Module**:

```csharp
public sealed class GetTenantDashboardHandler(
    IUserRepository userRepository,                         // Từ Module Identity
    ITenantListingActivityReader listingActivityReader,     // Contract từ Module Listings
    IViewingStatsReader viewingStatsReader,                 // Contract từ Module Viewings
    IMessagingStatsReader messagingStatsReader)             // Contract từ Module Messaging
{
    public async Task<Result<TenantDashboardResponse>> HandleAsync(
        GetTenantDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        // 1. Lấy thông tin hồ sơ người thuê từ bảng identity.users
        User? user = await userRepository.GetByIdWithRolesAsync(query.TenantId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<TenantDashboardResponse>(
                new Error(IdentityErrors.AuthenticationRequired, "Không tìm thấy thông tin tài khoản người thuê."));
        }

        // 2. Lấy số liệu hoạt động tìm phòng & 5 phòng vừa xem gần nhất qua Contract
        TenantListingActivitySummary listingSummary = await listingActivityReader.GetSummaryAsync(
            query.TenantId, cancellationToken);
        IReadOnlyList<ViewedListingContractItem> recentViewed = await listingActivityReader.GetRecentlyViewedListingsAsync(
            query.TenantId, limit: 5, cancellationToken);

        // 3. Lấy thống kê lịch hẹn xem phòng qua Contract của Module Viewings
        TenantViewingStats viewingStats = await viewingStatsReader.GetTenantStatsAsync(
            query.TenantId, cancellationToken);

        // 4. Lấy tổng số tin nhắn chưa đọc qua Contract của Module Messaging
        int unreadMessages = await messagingStatsReader.GetUnreadMessagesCountAsync(
            query.TenantId, cancellationToken);

        // 5. Đóng gói tất cả thành 1 gói JSON duy nhất trả về cho Frontend
        return Result.Success(new TenantDashboardResponse(...));
    }
}
```

#### B. Xem danh sách phòng đã xem [`GetViewedListingsHandler.cs`](../../backend/src/Modules/Identity/HomeMatch.Modules.Identity/Application/UseCases/GetViewedListings/GetViewedListingsHandler.cs)

- Xử lý endpoint `GET /api/v1/identity/tenant/viewed-listings?limit=20`.
- Kiểm tra và giới hạn tham số `limit` an toàn trong khoảng `[1..100]` (`int safeLimit = Math.Clamp(query.Limit, 1, 100)`).
- Gọi `listingActivityReader.GetRecentlyViewedListingsAsync(query.TenantId, safeLimit)` để trả về danh sách các phòng người thuê đã xem kèm số lần xem (`ViewCount`) và thời điểm xem gần nhất (`LastViewedAt`).

---

## 6. Danh Mục Toàn Bộ API & SignalR Hub Đã Hoàn Thiện Cho Mục 3.3 & 3.4

### 6.1. Mục 3.3.1 — Đặt và quản lý lịch xem phòng (`Viewings`)
| Phương thức | Endpoint | Mô tả nghiệp vụ | Quyền truy cập |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/viewings/appointments` | Lấy danh sách lịch xem phòng của tôi (hỗ trợ lọc `?status=PENDING`, `?listingId=...`) | Người thuê / Chủ nhà |
| `GET` | `/api/v1/viewings/appointments/{id}` | Xem chi tiết 1 lịch hẹn (kèm thông tin phòng, SĐT chủ nhà/người thuê, link họp online) | Người thuê / Chủ nhà liên quan |
| `POST` | `/api/v1/viewings/appointments` | Đặt lịch xem phòng mới (kiểm tra trùng lịch ±1h, tự động gửi thông báo cho chủ nhà) | Người thuê |
| `PATCH` | `/api/v1/viewings/appointments/{id}/confirm` | Chủ nhà xác nhận lịch xem phòng (tự động gửi thông báo cho người thuê) | Chủ nhà của lịch hẹn |
| `PATCH` | `/api/v1/viewings/appointments/{id}/cancel` | Hủy lịch xem phòng (ghi nhận bên hủy `TENANT`/`LANDLORD` và gửi thông báo cho đối phương) | Người thuê / Chủ nhà liên quan |
| `PATCH` | `/api/v1/viewings/appointments/{id}/complete` | Đánh dấu buổi xem phòng đã hoàn thành | Chủ nhà của lịch hẹn |

### 6.2. Mục 3.3.2 — Nhắn tin với chủ nhà & Realtime (`Messaging`)
| Phương thức | Endpoint / Hub | Mô tả nghiệp vụ | Quyền truy cập |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/messaging/conversations` | Lấy danh sách cuộc hội thoại của tôi (kèm tin nhắn cuối, số tin chưa đọc, tên đối phương) | Người thuê / Chủ nhà |
| `POST` | `/api/v1/messaging/conversations` | Bắt đầu hoặc mở lại cuộc hội thoại với chủ nhà về 1 phòng trọ (có thể gửi kèm tin nhắn đầu tiên) | Người thuê / Chủ nhà |
| `GET` | `/api/v1/messaging/conversations/{id}/messages` | Lấy toàn bộ lịch sử tin nhắn và bản nháp (`MessageDraft`) của cuộc hội thoại | Thành viên hội thoại |
| `POST` | `/api/v1/messaging/conversations/{id}/messages` | Gửi tin nhắn mới (lưu DB + đẩy SignalR Realtime + tạo thông báo cho người nhận) | Thành viên hội thoại |
| `PATCH` | `/api/v1/messaging/conversations/{id}/read` | Đánh dấu toàn bộ tin nhắn trong cuộc hội thoại là đã đọc (kèm đẩy sự kiện SignalR `ConversationRead`) | Thành viên hội thoại |
| `PUT` | `/api/v1/messaging/conversations/{id}/draft` | Lưu bản nháp tin nhắn đang gõ dở (hoặc bản nháp gợi ý từ AI) | Thành viên hội thoại |
| `WS` | `/api/v1/messaging/hub` (và `/hubs/chat`) | **SignalR Realtime Hub**: Nhận sự kiện `ReceiveMessage`, `ConversationRead`, `UserTyping` tức thời | JWT (`?access_token=...`) |

### 6.3. Mục 3.4.1, 3.4.2 & 3.4.3 — Tổng quan, Phòng đã xem & Thông báo (`Identity` & `Notifications`)
| Phương thức | Endpoint | Mô tả nghiệp vụ | Quyền truy cập |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/identity/tenant/dashboard` | **Mục 3.4.1:** Lấy dữ liệu tổng hợp Dashboard người thuê (Hồ sơ, Thống kê phòng đã xem/đã lưu, Thống kê lịch hẹn, Tin nhắn chưa đọc, 5 phòng vừa xem gần nhất) | Người thuê |
| `GET` | `/api/v1/identity/tenant/viewed-listings` | **Mục 3.4.2:** Xem danh sách chi tiết lịch sử các phòng trọ đã xem (`?limit=20`) | Người thuê |
| `GET` | `/api/v1/notifications` | **Mục 3.4.3:** Lấy danh sách thông báo của tài khoản kèm tổng số thông báo chưa đọc (`?unreadOnly=true`) | Người dùng đã đăng nhập |
| `PATCH` | `/api/v1/notifications/{id}/read` | **Mục 3.4.3:** Đánh dấu 1 thông báo là đã đọc | Chủ sở hữu thông báo |
| `PATCH` | `/api/v1/notifications/read-all` | **Mục 3.4.3:** Đánh dấu tất cả thông báo là đã đọc | Người dùng đã đăng nhập |
