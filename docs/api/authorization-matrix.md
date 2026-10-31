# Ma Trận Phân Quyền API & Quyền Sở Hữu (Authorization Matrix)

Tài liệu này xác định các vai trò (Roles), ma trận quyền hạn chi tiết trên từng thao tác tài nguyên, và quy tắc bắt buộc về **Quyền sở hữu tài nguyên (Resource Ownership)** trong HomeMatch.

---

## 1. Định Nghĩa Vai Trò Người Dùng (Roles)

| Role | Định danh | Ý nghĩa & Phạm vi |
|---|---|---|
| **Guest** | Không có token / Anonymous | Khách vãng lai; chỉ xem các dữ liệu công khai (danh sách tin đăng, tìm kiếm, đăng ký/đăng nhập). |
| **Tenant** | `Tenant` | Khách thuê trọ; xem tin, lưu yêu thích, đặt lịch xem phòng, nhắn tin, quản lý hợp đồng thuê của mình, xem hóa đơn tiền nhà của mình. |
| **Landlord** | `Landlord` | Chủ nhà trọ / Người cho thuê; quản lý bất động sản, phòng trọ, tạo tin đăng, tiếp nhận và quản lý lịch hẹn xem phòng, tạo hợp đồng thuê, phát hành hóa đơn tiền phòng. |
| **Admin** | `Admin` | Quản trị viên hệ thống; kiểm duyệt tin đăng, khóa tài khoản vi phạm, giải quyết khiếu nại báo cáo, giám sát sức khỏe hệ thống. |

> 📌 **Trọng tâm Sprint tuần này:** Tập trung hoàn thiện xác thực tài khoản (`Identity`), quản lý nhà trọ/phòng trọ (`Properties` cho Landlord), và đăng tin/tìm kiếm tin thuê (`Listings` cho Landlord & Tenant). Các module giao tiếp, hợp đồng, hóa đơn sẽ triển khai ở sprint tiếp theo.

---

## 2. Ma Trận Quyền Hạn Chi Tiết

| Module / Tài nguyên | Thao tác cụ thể | Guest | Tenant | Landlord | Admin | Ghi chú & Ràng buộc bảo mật |
|---|---|:---:|:---:|:---:|:---:|---|
| **Identity** | Đăng ký, Đăng nhập, Refresh | ✅ | ✅ | ✅ | ✅ | Public endpoints |
| | Cập nhật hồ sơ cá nhân | ❌ | ✅ (Own) | ✅ (Own) | ✅ (Own) | Chỉ cập nhật hồ sơ chính mình |
| | Quản lý người dùng, khóa tài khoản | ❌ | ❌ | ❌ | ✅ | Quyền quản trị hệ thống |
| **Properties** | Xem danh sách/chi tiết nhà trọ & phòng | ❌ | ❌ | ✅ (Own) | ✅ | Chủ nhà chỉ xem tài sản do mình sở hữu |
| | Tạo, cập nhật, xóa nhà trọ & phòng | ❌ | ❌ | ✅ (Own) | ✅ | Bắt buộc kiểm tra `OwnerId == currentUserId` |
| **Listings** | Tìm kiếm, xem chi tiết tin đăng | ✅ | ✅ | ✅ | ✅ | Public (chỉ tin trạng thái `Published`) |
| | Đăng tin, cập nhật tin, ẩn tin | ❌ | ❌ | ✅ (Own) | ✅ | Chủ nhà chỉ quản lý tin đăng của mình |
| | Lưu tin yêu thích (Favorites) | ❌ | ✅ | ✅ | ❌ | Tenant/Landlord lưu danh sách riêng của mình |
| **Viewings** | Gửi yêu cầu đặt lịch xem phòng | ❌ | ✅ | ❌ | ❌ | Khách thuê khởi tạo yêu cầu |
| | Chấp nhận / Từ chối yêu cầu xem | ❌ | ❌ | ✅ (Own) | ❌ | Chỉ chủ nhà sở hữu phòng liên quan |
| | Hủy lịch hẹn đã đặt | ❌ | ✅ (Own) | ✅ (Own) | ✅ | Các bên tham gia lịch hẹn |
| | Xác nhận hoàn thành buổi xem | ❌ | ❌ | ✅ (Own) | ❌ | Chủ nhà xác nhận sau khi khách tới |
| **Messaging** | Gửi và nhận tin nhắn trò chuyện | ❌ | ✅ (Own) | ✅ (Own) | ❌ | **Bảo mật riêng tư:** Admin KHÔNG có quyền đọc tin nhắn riêng tư |
| | Báo cáo hội thoại vi phạm (Report) | ❌ | ✅ (Own) | ✅ (Own) | ❌ | Người tham gia báo cáo tin nhắn xấu |
| | Xử lý vi phạm tin nhắn bị báo cáo | ❌ | ❌ | ❌ | ✅ | Admin chỉ xem được tin nhắn khi có ticket Report |
| **Leasing** | Tạo dự thảo hợp đồng thuê | ❌ | ❌ | ✅ (Own) | ❌ | Chủ nhà tạo cho phòng mình quản lý |
| | Ký duyệt / Xác nhận hợp đồng | ❌ | ✅ (Own) | ✅ (Own) | ❌ | Các bên tham gia hợp đồng |
| | Chấm dứt hợp đồng | ❌ | ✅ (Own) | ✅ (Own) | ✅ | Theo điều khoản hoặc can thiệp tranh chấp |
| **Billing** | Lập hóa đơn tiền phòng & dịch vụ | ❌ | ❌ | ✅ (Own) | ❌ | Chủ nhà lập cho hợp đồng đang hiệu lực |
| | Xem chi tiết và thanh toán hóa đơn | ❌ | ✅ (Own) | ✅ (Own) | ❌ | Các bên liên quan tới hợp đồng |
| **Notifications** | Xem và đánh dấu đã đọc thông báo | ❌ | ✅ (Own) | ✅ (Own) | ❌ | Chỉ người nhận thông báo (`UserId`) |

---

## 3. Quy Tắc Bắt Buộc Về Quyền Sở Hữu Tài Nguyên (Resource Ownership)

1. **Kiểm tra quyền sở hữu tại Use Case / Application Layer:**
   - Khi một `Landlord` gửi request cập nhật một `Property` hoặc `Listing`, hệ thống bắt buộc phải kiểm tra `OwnerId == currentUserId`.
   - Nếu `OwnerId != currentUserId` và người dùng không phải là `Admin` có thẩm quyền, hệ thống trả về mã lỗi **`403 Forbidden`** (hoặc `404 Not Found`).
2. **Quyền riêng tư cá nhân (Privacy by Design):**
   - Vai trò `Admin` không đồng nghĩa với quyền xem toàn bộ dữ liệu nhạy cảm (như nội dung tin nhắn chat cá nhân hoặc giấy tờ riêng tư). Quyền can thiệp của Admin chỉ phát sinh khi có khiếu nại (Report/Dispute ticket) hoặc vi phạm chính sách được báo cáo.
