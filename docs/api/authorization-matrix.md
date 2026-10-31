# Ma Trận Phân Quyền API & Quyền Sở Hữu (Authorization Matrix)

Tài liệu này xác định các vai trò (Roles), ma trận quyền hạn trên từng module tài nguyên, và quy tắc bắt buộc về **Quyền sở hữu tài nguyên (Resource Ownership)** trong HomeMatch.

---

## 1. Định Nghĩa Vai Trò Người Dùng (Roles)

| Role | Định danh | Ý nghĩa & Phạm vi |
|---|---|---|
| **Guest** | Không có token / Anonymous | Khách vãng lai; chỉ xem các dữ liệu công khai (danh sách tin đăng, tìm kiếm, đăng ký/đăng nhập). |
| **Tenant** | `Tenant` | Khách thuê trọ; xem tin, lưu yêu thích, đặt lịch xem phòng, nhắn tin, quản lý hợp đồng thuê của mình, xem hóa đơn tiền nhà của mình. |
| **Landlord** | `Landlord` | Chủ nhà trọ / Người cho thuê; quản lý bất động sản, phòng trọ, tạo tin đăng, quản lý lịch hẹn xem phòng, tạo hợp đồng thuê, phát hành hóa đơn tiền phòng. |
| **Admin** | `Admin` | Quản trị viên hệ thống; kiểm duyệt tin đăng, giải quyết khiếu nại, giám sát toàn hệ thống. |

---

## 2. Ma Trận Quyền Hạn Theo Module

| Module / Tài nguyên | Thao tác | Guest | Tenant | Landlord | Admin | Ghi chú & Điều kiện |
|---|---|:---:|:---:|:---:|:---:|---|
| **Identity** | Đăng ký, Đăng nhập, Refresh | ✅ | ✅ | ✅ | ✅ | Public endpoints |
| | Cập nhật hồ sơ cá nhân | ❌ | ✅ | ✅ | ✅ | Chỉ cập nhật hồ sơ chính mình |
| | Quản lý người dùng, khóa tài khoản | ❌ | ❌ | ❌ | ✅ | Chỉ Admin |
| **Properties** | Xem thông tin nhà trọ / phòng | ❌ | ✅ | ✅ (Own) | ✅ | Chủ nhà chỉ xem/sửa tài sản do mình sở hữu |
| | Tạo, cập nhật, xóa nhà trọ / phòng | ❌ | ❌ | ✅ (Own) | ✅ | Bắt buộc kiểm tra ownership |
| **Listings** | Tìm kiếm, xem chi tiết tin đăng | ✅ | ✅ | ✅ | ✅ | Tin đăng trạng thái `Published` |
| | Tạo tin đăng, sửa tin, ẩn tin | ❌ | ❌ | ✅ (Own) | ✅ | Chủ nhà chỉ thao tác trên tin của mình |
| | Lưu tin yêu thích (Favorites) | ❌ | ✅ | ✅ | ✅ | Tenant/User lưu danh sách của chính mình |
| **Viewings** | Đặt lịch xem phòng | ❌ | ✅ | ❌ | ✅ | Tenant tạo yêu cầu đặt lịch |
| | Chấp nhận, từ chối, hủy lịch hẹn | ❌ | ✅ (Own) | ✅ (Own) | ✅ | Chỉ người tham gia lịch hẹn |
| **Messaging** | Gửi tin nhắn, xem cuộc trò chuyện | ❌ | ✅ (Own) | ✅ (Own) | ✅ | Chỉ thành viên trong cuộc hội thoại (`Participants`) |
| **Leasing** | Tạo hợp đồng thuê phòng | ❌ | ❌ | ✅ (Own) | ✅ | Landlord tạo cho phòng mình sở hữu |
| | Ký hợp đồng, xem chi tiết hợp đồng | ❌ | ✅ (Own) | ✅ (Own) | ✅ | Tenant hoặc Landlord của hợp đồng đó |
| **Billing** | Lập hóa đơn tiền phòng, dịch vụ | ❌ | ❌ | ✅ (Own) | ✅ | Landlord tạo cho hợp đồng thuê phòng mình quản lý |
| | Xem hóa đơn, thanh toán | ❌ | ✅ (Own) | ✅ (Own) | ✅ | Tenant hoặc Landlord liên quan |
| **Notifications** | Nhận, đọc thông báo | ❌ | ✅ (Own) | ✅ (Own) | ✅ | Chỉ nhận thông báo gửi tới `UserId` của mình |

---

## 3. Quy Tắc Bắt Buộc Về Quyền Sở Hữu Tài Nguyên (Resource Ownership)

> ⚠️ **NGUYÊN TẮC VÀNG:**
> Có vai trò hợp lệ (Role) **KHÔNG ĐỒNG NGHĨA** với việc có quyền can thiệp vào tài nguyên của người khác!

1. **Kiểm tra quyền sở hữu tại Use Case / Application Layer:**
   - Khi một `Landlord` gửi request cập nhật một `Property` hoặc `Listing`, hệ thống bắt buộc phải kiểm tra `OwnerId == currentUserId`.
   - Nếu `OwnerId != currentUserId` và người dùng không phải là `Admin`, hệ thống phải trả về mã lỗi **`403 Forbidden`** (hoặc `404 Not Found` nếu muốn che giấu sự tồn tại của tài nguyên vì lý do bảo mật).
2. **Kiểm tra quyền tham gia cuộc hội thoại / lịch hẹn / hợp đồng:**
   - Các tài nguyên có 2 bên tham gia (như cuộc trò chuyện, lịch xem phòng, hợp đồng thuê) phải xác thực `currentUserId` thuộc danh sách các bên liên quan (`TenantId` hoặc `LandlordId`).
3. **Admin Override:**
   - Chỉ người dùng mang role `Admin` mới được phép thao tác hoặc tra cứu xuyên suốt tài nguyên của tất cả người dùng, phục vụ công tác kiểm duyệt và hỗ trợ kỹ thuật.
