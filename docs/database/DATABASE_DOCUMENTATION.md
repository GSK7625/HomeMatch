# TÀI LIỆU ĐẶC TẢ CƠ SỞ DỮ LIỆU HOMEMATCH (44 BẢNG - 10 PHÂN HỆ)
**Hệ thống Hỗ trợ Tìm kiếm Thuê nhà và Quản lý Cho thuê (HomeMatch)**  
*Tài liệu tra cứu: Phân hệ, Ý nghĩa từng bảng và Chi tiết tất cả các trường dữ liệu*  
*Hệ quản trị CSDL: PostgreSQL 16/18 & Tiện ích mở rộng PostGIS*

---

## 1. TỔNG QUAN QUY MÔ & DANH MỤC PHÂN HỆ
- **Tổng số bảng nghiệp vụ**: **44 bảng** (Chuẩn hóa 3NF, không dư thừa, tối ưu hóa hiệu năng và mở rộng).
- **Tổng số phân hệ nghiệp vụ**: **10 phân hệ** bao phủ trọn vẹn vòng đời Tìm kiếm, Vận hành thuê phòng và Quản trị kinh doanh sàn.

### Bảng Tổng hợp 10 Phân hệ Nghiệp vụ
| STT | Phân hệ Nghiệp vụ | Số lượng bảng | Danh mục các bảng trực thuộc |
| :---: | :--- | :---: | :--- |
| **1** | **Địa giới Hành chính** | 2 bảng | [`AdministrativeProvinces`](#administrativeprovinces), [`AdministrativeWards`](#administrativewards) |
| **2** | **Tài khoản, Xác thực & Định danh CCCD (KYC)** | 6 bảng | [`Users`](#users), [`UserProfiles`](#userprofiles), [`UserTokens`](#usertokens), [`TenantIdentityProfiles`](#tenantidentityprofiles), [`TenantIdentityShares`](#tenantidentityshares), [`Notifications`](#notifications) |
| **3** | **Bất động sản, Căn phòng & Tiện ích** | 5 bảng | [`Properties`](#properties), [`Rooms`](#rooms), [`Amenities`](#amenities), [`RoomAmenities`](#roomamenities), [`UtilityTariffs`](#utilitytariffs) |
| **4** | **Tin đăng, Hình ảnh, Tiếp thị & So sánh** | 7 bảng | [`Listings`](#listings), [`ListingImages`](#listingimages), [`SavedListings`](#savedlistings), [`ListingViews`](#listingviews), [`UserRoomComparisons`](#userroomcomparisons), [`MarketingCampaigns`](#marketingcampaigns), [`MarketingClicks`](#marketingclicks) |
| **5** | **Lịch hẹn Xem phòng & Buổi xem Online** | 2 bảng | [`ViewingAppointments`](#viewingappointments), [`VirtualViewings`](#virtualviewings) |
| **6** | **Khách tiềm năng, Hội thoại & Ở ghép** | 6 bảng | [`Leads`](#leads), [`RoommatePosts`](#roommateposts), [`AIChatSessions`](#aichatsessions), [`Conversations`](#conversations), [`MessageDrafts`](#messagedrafts), [`Messages`](#messages) |
| **7** | **Hợp đồng, Chỉ số điện nước, Hóa đơn & Thanh toán** | 4 bảng | [`Contracts`](#contracts), [`UtilityReadings`](#utilityreadings), [`Invoices`](#invoices), [`Payments`](#payments) |
| **8** | **Quản lý Bảo trì & Sự cố** | 2 bảng | [`MaintenanceRequests`](#maintenancerequests), [`MaintenanceImages`](#maintenanceimages) |
| **9** | **Trợ lý AI & Đề xuất Thông minh** | 3 bảng | [`AIChatMessages`](#aichatmessages), [`AIMessageSuggestions`](#aimessagesuggestions), [`AITaskSuggestions`](#aitasksuggestions) |
| **10** | **Quản trị Vận hành Sàn & Động cơ Doanh thu** | 7 bảng | [`PlatformConfigs`](#platformconfigs), [`AdminAuditLogs`](#adminauditlogs), [`ContentReports`](#contentreports), [`PlatformServicePlans`](#platformserviceplans), [`LandlordSubscriptions`](#landlordsubscriptions), [`ListingPromotions`](#listingpromotions), [`PlatformTransactions`](#platformtransactions) |

---

## 2. CHI TIẾT 44 BẢNG THEO TỪNG PHÂN HỆ VÀ DANH MỤC CÁC TRƯỜNG

### PHÂN HỆ 1: ĐỊA GIỚI HÀNH CHÍNH (2 BẢNG)
> **Ý nghĩa phân hệ**: Quản lý danh mục Tỉnh/Thành phố và Xã/Phường phục vụ định vị và tìm kiếm bất động sản.

<a id="administrativeprovinces"></a>
#### 1. Bảng `AdministrativeProvinces`
* **Ý nghĩa nghiệp vụ**: Danh mục 63 Tỉnh / Thành phố trực thuộc trung ương tại Việt Nam.
* **Số lượng trường**: 4 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `code` | `VARCHAR(20)` | PK | Mã định danh duy nhất |
| 2 | `name` | `VARCHAR(100)` | Not Null | Tên gọi hiển thị |
| 3 | `name_en` | `VARCHAR(100)` | Null | Tên tiếng Anh không dấu |
| 4 | `full_name` | `VARCHAR(150)` | Not Null | Tên đầy đủ có tiền tố phân loại |


<a id="administrativewards"></a>
#### 2. Bảng `AdministrativeWards`
* **Ý nghĩa nghiệp vụ**: Danh mục Xã / Phường / Thị trấn trực thuộc Tỉnh/Thành phố.
* **Số lượng trường**: 6 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `code` | `VARCHAR(20)` | PK | Mã định danh duy nhất |
| 2 | `name` | `VARCHAR(100)` | Not Null | Tên gọi hiển thị |
| 3 | `name_en` | `VARCHAR(100)` | Null | Tên tiếng Anh không dấu |
| 4 | `full_name` | `VARCHAR(150)` | Not Null | Tên đầy đủ có tiền tố phân loại |
| 5 | `district_name` | `VARCHAR(100)` | Null | Tên Quận/Huyện/Thị xã hỗ trợ nhận diện ngữ cảnh |
| 6 | `province_code` | `VARCHAR(20)` | FK -> AdministrativeProvinces(code), Not Null | Mã Tỉnh/Thành phố trực thuộc |


---

### PHÂN HỆ 2: TÀI KHOẢN, XÁC THỰC & ĐỊNH DANH CCCD (KYC) (6 BẢNG)
> **Ý nghĩa phân hệ**: Quản lý thông tin đăng nhập, hồ sơ cá nhân, phiên làm việc, thông báo và bảo mật dữ liệu CCCD gắn chip theo Nghị định 13/2023/NĐ-CP.

<a id="users"></a>
#### 3. Bảng `Users`
* **Ý nghĩa nghiệp vụ**: Tài khoản người dùng đăng nhập hệ thống (Người thuê, Chủ nhà, Quản trị viên).
* **Số lượng trường**: 8 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `email` | `VARCHAR(255)` | Not Null, Unique | Email đăng nhập duy nhất của tài khoản |
| 3 | `phone_number` | `VARCHAR(20)` | Null | Số điện thoại liên hệ (cho phép để trống) |
| 4 | `password_hash` | `VARCHAR(255)` | Not Null | Mật khẩu đã được mã hóa bảo mật BCrypt/Argon2 |
| 5 | `role` | `VARCHAR(20)` | Not Null, Default: 'TENANT' | Vai trò tài khoản: TENANT (Người thuê), LANDLORD (Chủ nhà), ADMIN (Quản trị) |
| 6 | `status` | `VARCHAR(20)` | Not Null, Default: 'ACTIVE' | Trạng thái hoạt động của đối tượng |
| 7 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |
| 8 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="userprofiles"></a>
#### 4. Bảng `UserProfiles`
* **Ý nghĩa nghiệp vụ**: Hồ sơ thông tin cá nhân, ảnh đại diện, ngày sinh, giới tính và cài đặt thông báo.
* **Số lượng trường**: 9 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `user_id` | `BIGINT` | PK, FK -> Users(id) | Mã người dùng sở hữu |
| 2 | `full_name` | `VARCHAR(100)` | Not Null | Tên đầy đủ có tiền tố phân loại |
| 3 | `avatar_url` | `VARCHAR(500)` | Null | Đường dẫn ảnh đại diện cá nhân |
| 4 | `date_of_birth` | `DATE` | Null | Ngày tháng năm sinh |
| 5 | `gender` | `VARCHAR(10)` | Null | Giới tính: MALE (Nam), FEMALE (Nữ), OTHER (Khác) |
| 6 | `notify_email` | `BOOLEAN` | Not Null, Default: TRUE | Cho phép nhận thông báo qua email |
| 7 | `notify_sms` | `BOOLEAN` | Not Null, Default: FALSE | Cho phép nhận tin nhắn SMS |
| 8 | `notify_push` | `BOOLEAN` | Not Null, Default: TRUE | Cho phép nhận thông báo đẩy trên ứng dụng |
| 9 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="usertokens"></a>
#### 5. Bảng `UserTokens`
* **Ý nghĩa nghiệp vụ**: Quản lý Refresh Token, mã đặt lại mật khẩu và phiên đăng nhập trên thiết bị.
* **Số lượng trường**: 8 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `user_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người dùng sở hữu |
| 3 | `token_hash` | `CHAR(64)` | Not Null | Mã băm token xác thực phiên làm việc |
| 4 | `token_type` | `VARCHAR(30)` | Not Null | Loại token: REFRESH, RESET_PASSWORD, REMEMBER_ME |
| 5 | `device_info` | `VARCHAR(255)` | Null | Thông tin thiết bị và trình duyệt đăng nhập |
| 6 | `ip_address` | `VARCHAR(45)` | Null | Địa chỉ IP thực hiện yêu cầu |
| 7 | `expires_at` | `TIMESTAMPTZ` | Not Null | Thời điểm hết hạn hiệu lực |
| 8 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


<a id="tenantidentityprofiles"></a>
#### 6. Bảng `TenantIdentityProfiles`
* **Ý nghĩa nghiệp vụ**: Hồ sơ CCCD gốc của người thuê, mã hóa dữ liệu nhạy cảm và băm HMAC-SHA256 chống dò số.
* **Số lượng trường**: 13 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `tenant_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người thuê nhà sở hữu |
| 3 | `id_card_number_encrypted` | `VARCHAR(255)` | Not Null | Chuỗi mã hóa toàn văn số thẻ CCCD (AES-256) |
| 4 | `id_card_last4` | `CHAR(4)` | Not Null | 4 chữ số cuối của thẻ CCCD dùng để hiển thị an toàn |
| 5 | `id_card_hmac_hash` | `CHAR(64)` | Not Null | Mã băm HMAC-SHA256 số CCCD kèm khóa bí mật chống dò ngược |
| 6 | `masked_id_card` | `Computed` | Computed (CONCAT(N'******', id_card_last4) | Cột tính toán hiển thị thẻ CCCD dạng ******1234 |
| 7 | `full_name` | `VARCHAR(150)` | Not Null | Tên đầy đủ có tiền tố phân loại |
| 8 | `date_of_birth` | `DATE` | Not Null | Ngày tháng năm sinh |
| 9 | `front_card_url` | `VARCHAR(500)` | Not Null | Đường dẫn ảnh chụp mặt trước thẻ CCCD |
| 10 | `back_card_url` | `VARCHAR(500)` | Not Null | Đường dẫn ảnh chụp mặt sau thẻ CCCD |
| 11 | `is_system_verified` | `BOOLEAN` | Not Null, Default: FALSE | Cờ xác minh danh tính cấp nền tảng bởi OCR / Admin |
| 12 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |
| 13 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="tenantidentityshares"></a>
#### 7. Bảng `TenantIdentityShares`
* **Ý nghĩa nghiệp vụ**: Cấp quyền chia sẻ hồ sơ CCCD cho từng chủ nhà cụ thể để duyệt hồ sơ thuê.
* **Số lượng trường**: 7 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `tenant_profile_id` | `BIGINT` | FK -> TenantIdentityProfiles(id), Not Null | Mã hồ sơ CCCD gốc được chia sẻ |
| 3 | `target_landlord_id` | `BIGINT` | FK -> Users(id), Not Null | Mã chủ nhà được nhận quyền xem hồ sơ |
| 4 | `contract_id` | `BIGINT` | Null | Mã hợp đồng thuê liên quan đến đợt chia sẻ |
| 5 | `status` | `VARCHAR(20)` | Not Null, Default: 'PENDING' | Trạng thái hoạt động của đối tượng |
| 6 | `reviewed_at` | `TIMESTAMPTZ` | Null | Thời điểm chủ nhà xem xét và duyệt hồ sơ CCCD |
| 7 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


<a id="notifications"></a>
#### 8. Bảng `Notifications`
* **Ý nghĩa nghiệp vụ**: Hộp thư thông báo trong ứng dụng gửi tới từng người dùng.
* **Số lượng trường**: 9 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `user_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người dùng sở hữu |
| 3 | `title` | `VARCHAR(200)` | Not Null | Tiêu đề nội dung |
| 4 | `content` | `TEXT` | Not Null | Nội dung chi tiết |
| 5 | `type` | `VARCHAR(50)` | Not Null | Phân loại thông báo / đối tượng |
| 6 | `reference_type` | `VARCHAR(50)` | Null | Tên thực thể dữ liệu liên kết |
| 7 | `reference_id` | `BIGINT` | Null | Mã bản ghi trong bảng nghiệp vụ liên kết |
| 8 | `is_read` | `BOOLEAN` | Not Null, Default: FALSE | Cờ đánh dấu người dùng đã đọc thông báo |
| 9 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


---

### PHÂN HỆ 3: BẤT ĐỘNG SẢN, CĂN PHÒNG & TIỆN ÍCH (5 BẢNG)
> **Ý nghĩa phân hệ**: Quản lý thông tin tòa nhà/khu trọ vật lý, từng căn phòng cho thuê, danh mục tiện ích dùng chung và biểu giá điện nước.

<a id="properties"></a>
#### 9. Bảng `Properties`
* **Ý nghĩa nghiệp vụ**: Tòa nhà / Khu nhà trọ cho thuê thuộc sở hữu của chủ nhà kèm tọa độ địa lý và chỉ mục không gian PostGIS.
* **Số lượng trường**: 12 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `landlord_id` | `BIGINT` | FK -> Users(id), Not Null | Mã chủ nhà sở hữu |
| 3 | `name` | `VARCHAR(200)` | Not Null | Tên gọi hiển thị |
| 4 | `province_code` | `VARCHAR(20)` | FK -> AdministrativeProvinces(code), Not Null | Mã Tỉnh/Thành phố trực thuộc |
| 5 | `ward_code` | `VARCHAR(20)` | FK -> AdministrativeWards(code), Not Null | Mã Xã/Phường trực thuộc |
| 6 | `address_detail` | `VARCHAR(255)` | Not Null | Địa chỉ cụ thể (số nhà, ngõ/ngách, tên đường) |
| 7 | `property_type` | `VARCHAR(50)` | Not Null | Loại hình tòa nhà: APARTMENT, BOARDING_HOUSE, SERVICED_APARTMENT, HOMESTAY |
| 8 | `latitude` | `DECIMAL(9,6)` | Null | Tọa độ vĩ độ (Latitude WGS-84) hỗ trợ bản đồ OpenStreetMap/Leaflet |
| 9 | `longitude` | `DECIMAL(9,6)` | Null | Tọa độ kinh độ (Longitude WGS-84) hỗ trợ bản đồ OpenStreetMap/Leaflet |
| 10 | `geom` | `GEOMETRY(Point, 4326)` | Null, GIST Index | Tọa độ không gian địa lý PostGIS (tự động tạo từ lat/lng) phục vụ tìm kiếm bán kính siêu tốc |
| 11 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |
| 12 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="rooms"></a>
#### 10. Bảng `Rooms`
* **Ý nghĩa nghiệp vụ**: Phòng cho thuê vật lý bên trong tòa nhà (diện tích, giá gốc, trạng thái trống/đã thuê).
* **Số lượng trường**: 12 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `property_id` | `BIGINT` | FK -> Properties(id), Not Null | Mã tòa nhà / khu trọ chứa căn phòng |
| 3 | `room_number` | `VARCHAR(50)` | Not Null | Mã số hoặc tên phòng (ví dụ: P.201, Căn 3A) |
| 4 | `floor` | `INT` | Not Null, Default: 1 | Vị trí tầng của phòng |
| 5 | `area_sqm` | `DECIMAL(6,2)` | Not Null | Diện tích căn phòng (m²) |
| 6 | `room_type` | `VARCHAR(50)` | Not Null | Phân loại phòng: SINGLE, STUDIO, 1B1B, 2B1B, DORMITORY |
| 7 | `max_occupants` | `INT` | Not Null, Default: 2 | Số lượng người ở tối đa cho phép |
| 8 | `base_price` | `BIGINT` | Not Null | Giá thuê gốc niêm yết của phòng (VNĐ/tháng) |
| 9 | `status` | `VARCHAR(20)` | Not Null, Default: 'AVAILABLE' | Trạng thái hoạt động của đối tượng |
| 10 | `available_from` | `DATE` | Not Null, Default: CAST(CURRENT_TIMESTAMP | Thuộc tính thông tin bản ghi |
| 11 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |
| 12 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="amenities"></a>
#### 11. Bảng `Amenities`
* **Ý nghĩa nghiệp vụ**: Danh mục tiện ích chuẩn hóa dùng chung toàn hệ thống (Wifi, Máy lạnh, Máy giặt...).
* **Số lượng trường**: 5 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `name` | `VARCHAR(100)` | Not Null, Unique | Tên gọi hiển thị |
| 3 | `code` | `VARCHAR(50)` | Not Null, Unique | Mã định danh duy nhất |
| 4 | `icon_url` | `VARCHAR(500)` | Null | Đường dẫn biểu tượng hiển thị tiện ích |
| 5 | `category` | `VARCHAR(50)` | Not Null | Nhóm phân loại tiện ích / dịch vụ |


<a id="roomamenities"></a>
#### 12. Bảng `RoomAmenities`
* **Ý nghĩa nghiệp vụ**: Bảng trung gian liên kết quan hệ nhiều-nhiều (N-N) giữa Căn phòng và Tiện ích.
* **Số lượng trường**: 2 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `room_id` | `BIGINT` | FK -> Rooms(id), Not Null | Mã phòng cho thuê liên quan |
| 2 | `amenity_id` | `BIGINT` | FK -> Amenities(id), Not Null | Mã tiện ích được trang bị |


<a id="utilitytariffs"></a>
#### 13. Bảng `UtilityTariffs`
* **Ý nghĩa nghiệp vụ**: Cấu hình đơn giá điện, nước, dịch vụ theo tòa nhà hoặc đặc thù theo từng phòng.
* **Số lượng trường**: 7 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `property_id` | `BIGINT` | FK -> Properties(id), Not Null | Mã tòa nhà / khu trọ chứa căn phòng |
| 3 | `room_id` | `BIGINT` | FK -> Rooms(id), Null | Mã phòng cho thuê liên quan |
| 4 | `tariff_type` | `VARCHAR(50)` | Not Null | Loại dịch vụ tính tiền: ELECTRICITY, WATER, INTERNET, SERVICE |
| 5 | `calc_method` | `VARCHAR(20)` | Not Null | Cách tính: PER_KWH, PER_M3, PER_PERSON, FIXED_PER_MONTH |
| 6 | `unit_price` | `BIGINT` | Not Null | Đơn giá dịch vụ tính trên đơn vị đo (VNĐ) |
| 7 | `effective_from` | `DATE` | Not Null, Default: CAST(CURRENT_TIMESTAMP | Thuộc tính thông tin bản ghi |


---

### PHÂN HỆ 4: TIN ĐĂNG, HÌNH ẢNH, TIẾP THỊ & SO SÁNH (7 BẢNG)
> **Ý nghĩa phân hệ**: Quản lý tin đăng tiếp thị phòng, album hình ảnh, tin yêu thích, lịch sử xem tin, khay so sánh 3 phòng và chiến dịch tiếp thị UTM.

<a id="listings"></a>
#### 14. Bảng `Listings`
* **Ý nghĩa nghiệp vụ**: Tin đăng tiếp thị cho thuê phòng (tiêu đề, giá niêm yết, chính sách thuê, ngày hết hạn).
* **Số lượng trường**: 18 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `room_id` | `BIGINT` | FK -> Rooms(id), Not Null | Mã phòng cho thuê liên quan |
| 3 | `landlord_id` | `BIGINT` | FK -> Users(id), Not Null | Mã chủ nhà sở hữu |
| 4 | `title` | `VARCHAR(255)` | Not Null | Tiêu đề nội dung |
| 5 | `slug` | `VARCHAR(255)` | Not Null, Unique | Chuỗi ký tự định danh URL thân thiện cho SEO |
| 6 | `description` | `TEXT` | Not Null | Mô tả giới thiệu chi tiết |
| 7 | `price` | `BIGINT` | Not Null | Giá bán gói dịch vụ niêm yết (VNĐ) |
| 8 | `deposit_amount` | `BIGINT` | Not Null | Số tiền đặt cọc giữ chỗ và bảo đảm hợp đồng (VNĐ) |
| 9 | `min_lease_months` | `INT` | Not Null, Default: 6 | Thời hạn hợp đồng tối thiểu yêu cầu (tháng) |
| 10 | `allow_pets` | `BOOLEAN` | Not Null, Default: FALSE | Cho phép nuôi thú cưng: 1 = Cho phép, 0 = Không |
| 11 | `has_curfew` | `BOOLEAN` | Not Null, Default: FALSE | Thuộc tính thông tin bản ghi |
| 12 | `curfew_time` | `VARCHAR(20)` | Null | Thuộc tính thông tin bản ghi |
| 13 | `status` | `VARCHAR(20)` | Not Null, Default: 'PUBLISHED' | Trạng thái hoạt động của đối tượng |
| 14 | `view_count` | `BIGINT` | Not Null, Default: 0 | Tổng lượt xem tin (đồng bộ tự động qua trigger) |
| 15 | `published_at` | `TIMESTAMPTZ` | Null | Thời điểm tin đăng được duyệt và xuất bản công khai |
| 16 | `expires_at` | `TIMESTAMPTZ` | Null | Thời điểm hết hạn hiệu lực |
| 17 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |
| 18 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="listingimages"></a>
#### 15. Bảng `ListingImages`
* **Ý nghĩa nghiệp vụ**: Album đa phương tiện (Hình ảnh, Video quay phòng, Tour 360°) của căn phòng trong tin đăng (chuẩn 1NF, đánh dấu ảnh chính).
* **Số lượng trường**: 9 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `listing_id` | `BIGINT` | FK -> Listings(id), Not Null | Mã tin đăng phòng được tham chiếu |
| 3 | `image_url` | `VARCHAR(500)` | Not Null | Đường dẫn lưu trữ hình ảnh hoặc video trên CDN |
| 4 | `is_primary` | `BOOLEAN` | Not Null, Default: FALSE | Cờ đánh dấu ảnh đại diện chính của tin (TRUE = Ảnh chính) |
| 5 | `sort_order` | `INT` | Not Null, Default: 0 | Thứ tự sắp xếp hiển thị trong slider đa phương tiện |
| 6 | `media_type` | `VARCHAR(20)` | Not Null, Default: 'IMAGE' | Loại đa phương tiện: 'IMAGE', 'VIDEO', 'VIRTUAL_TOUR_360' |
| 7 | `thumbnail_url` | `VARCHAR(500)` | Null | Ảnh bìa đại diện cho video trước khi bấm phát (Play) |
| 8 | `duration_seconds` | `INT` | Null, Check (> 0) | Thời lượng video tính bằng giây (phục vụ hiển thị badge và tối ưu băng thông) |
| 9 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


<a id="savedlistings"></a>
#### 16. Bảng `SavedListings`
* **Ý nghĩa nghiệp vụ**: Danh sách các tin đăng phòng được người thuê lưu lại để theo dõi (Wishlist).
* **Số lượng trường**: 4 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `user_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người dùng sở hữu |
| 3 | `listing_id` | `BIGINT` | FK -> Listings(id), Not Null | Mã tin đăng phòng được tham chiếu |
| 4 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


<a id="listingviews"></a>
#### 17. Bảng `ListingViews`
* **Ý nghĩa nghiệp vụ**: Nhật ký lượt xem tin đăng phục vụ thống kê phân tích và gợi ý phòng của AI.
* **Số lượng trường**: 5 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `listing_id` | `BIGINT` | FK -> Listings(id), Not Null | Mã tin đăng phòng được tham chiếu |
| 3 | `user_id` | `BIGINT` | FK -> Users(id), Null | Mã người dùng sở hữu |
| 4 | `ip_address` | `VARCHAR(45)` | Null | Địa chỉ IP thực hiện yêu cầu |
| 5 | `viewed_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm người dùng xem tin phòng |


<a id="userroomcomparisons"></a>
#### 18. Bảng `UserRoomComparisons`
* **Ý nghĩa nghiệp vụ**: Khay so sánh tối đa 3 phòng theo `listing_id` dành cho người thuê (slot 1..3).
* **Số lượng trường**: 5 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `user_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người dùng sở hữu |
| 3 | `listing_id` | `BIGINT` | FK -> Listings(id), Not Null | Mã tin đăng phòng được tham chiếu |
| 4 | `slot` | `INT` | Not Null | Vị trí so sánh trong khay (giới hạn từ 1 đến 3) |
| 5 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


<a id="marketingcampaigns"></a>
#### 19. Bảng `MarketingCampaigns`
* **Ý nghĩa nghiệp vụ**: Chiến dịch tiếp thị liên kết chia sẻ tin phòng qua mạng xã hội (UTM Tracking).
* **Số lượng trường**: 8 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `listing_id` | `BIGINT` | FK -> Listings(id), Not Null | Mã tin đăng phòng được tham chiếu |
| 3 | `utm_source` | `VARCHAR(50)` | Not Null | Nguồn tiếp thị truy cập (Facebook, Google, TikTok, Zalo) |
| 4 | `utm_medium` | `VARCHAR(50)` | Not Null | Hình thức tiếp thị (cpc, social, referral, banner) |
| 5 | `utm_campaign` | `VARCHAR(100)` | Not Null | Tên chiến dịch tiếp thị |
| 6 | `short_link` | `VARCHAR(255)` | Not Null, Unique | Thuộc tính thông tin bản ghi |
| 7 | `total_clicks` | `BIGINT` | Not Null, Default: 0 | Tổng số lượt nhấp chuột vào link chiến dịch |
| 8 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


<a id="marketingclicks"></a>
#### 20. Bảng `MarketingClicks`
* **Ý nghĩa nghiệp vụ**: Nhật ký ghi nhận từng lượt click vào link chiến dịch tiếp thị để tính chuyển đổi.
* **Số lượng trường**: 5 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `campaign_id` | `BIGINT` | FK -> MarketingCampaigns(id), Not Null | Mã chiến dịch tiếp thị được click |
| 3 | `ip_address` | `VARCHAR(45)` | Null | Địa chỉ IP thực hiện yêu cầu |
| 4 | `referrer` | `VARCHAR(500)` | Null | Trang web nguồn giới thiệu lượt click |
| 5 | `clicked_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm người dùng click vào link |


---

### PHÂN HỆ 5: LỊCH HẸN XEM PHÒNG & BUỔI XEM ONLINE (2 BẢNG)
> **Ý nghĩa phân hệ**: Quản lý lịch hẹn xem phòng trực tiếp tại chỗ và phòng họp trực tuyến (Virtual Viewing) có kiểm soát chống trùng lịch.

<a id="viewingappointments"></a>
#### 21. Bảng `ViewingAppointments`
* **Ý nghĩa nghiệp vụ**: Lịch hẹn đến xem phòng trực tiếp giữa khách thuê và chủ nhà (có chống trùng lịch).
* **Số lượng trường**: 13 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `tenant_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người thuê nhà sở hữu |
| 3 | `listing_id` | `BIGINT` | FK -> Listings(id), Not Null | Mã tin đăng phòng được tham chiếu |
| 4 | `appointment_time` | `TIMESTAMPTZ` | Not Null | Thời điểm hẹn xem phòng dự kiến |
| 5 | `duration_minutes` | `INT` | Not Null, Default: 30 | Thời lượng buổi xem phòng dự kiến (phút, mặc định 30) |
| 6 | `status` | `VARCHAR(20)` | Not Null, Default: 'PENDING' | Trạng thái hoạt động của đối tượng |
| 7 | `note` | `VARCHAR(500)` | Null | Thuộc tính thông tin bản ghi |
| 8 | `cancelled_by` | `VARCHAR(20)` | Null | Chủ thể hủy cuộc hẹn: TENANT, LANDLORD, SYSTEM |
| 9 | `cancel_reason` | `VARCHAR(255)` | Null | Thuộc tính thông tin bản ghi |
| 10 | `remind_at` | `TIMESTAMPTZ` | Null | Thời điểm tự động gửi thông báo nhắc lịch |
| 11 | `is_reminder_sent` | `BOOLEAN` | Not Null, Default: FALSE | Thuộc tính thông tin bản ghi |
| 12 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |
| 13 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="virtualviewings"></a>
#### 22. Bảng `VirtualViewings`
* **Ý nghĩa nghiệp vụ**: Buổi xem phòng trực tuyến qua video call (Google Meet, Zoom) gắn với lịch hẹn.
* **Số lượng trường**: 7 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `appointment_id` | `BIGINT` | FK -> ViewingAppointments(id), Not Null | Mã lịch hẹn xem phòng trực tiếp gốc |
| 3 | `room_url` | `VARCHAR(500)` | Not Null | Đường link phòng họp trực tuyến (Google Meet, Zoom) |
| 4 | `status` | `VARCHAR(20)` | Not Null, Default: 'SCHEDULED' | Trạng thái hoạt động của đối tượng |
| 5 | `started_at` | `TIMESTAMPTZ` | Null | Thời điểm bắt đầu buổi xem online thực tế |
| 6 | `ended_at` | `TIMESTAMPTZ` | Null | Thời điểm kết thúc buổi xem online |
| 7 | `recording_url` | `VARCHAR(500)` | Null | Thuộc tính thông tin bản ghi |


---

### PHÂN HỆ 6: KHÁCH TIỀM NĂNG, HỘI THOẠI & Ở GHÉP (6 BẢNG)
> **Ý nghĩa phân hệ**: Quản lý phễu khách hàng quan tâm (Leads), bài đăng tìm bạn ở ghép, cuộc trò chuyện chat 2 chiều và bản nháp tin nhắn.

<a id="leads"></a>
#### 23. Bảng `Leads`
* **Ý nghĩa nghiệp vụ**: Phễu quản lý khách hàng tiềm năng đang quan tâm đến phòng cho chủ nhà.
* **Số lượng trường**: 7 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `tenant_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người thuê nhà sở hữu |
| 3 | `listing_id` | `BIGINT` | FK -> Listings(id), Not Null | Mã tin đăng phòng được tham chiếu |
| 4 | `status` | `VARCHAR(20)` | Not Null, Default: 'NEW' | Trạng thái hoạt động của đối tượng |
| 5 | `notes` | `TEXT` | Null | Ghi chú thêm về yêu cầu hẹn xem phòng |
| 6 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |
| 7 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="roommateposts"></a>
#### 24. Bảng `RoommatePosts`
* **Ý nghĩa nghiệp vụ**: Bài đăng tìm người ở ghép của khách thuê (ngân sách, khu vực mong muốn).
* **Số lượng trường**: 13 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `tenant_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người thuê nhà sở hữu |
| 3 | `current_room_id` | `BIGINT` | FK -> Rooms(id), Null | Mã phòng đang thuê cần tìm thêm bạn cùng phòng |
| 4 | `target_province_code` | `VARCHAR(20)` | FK -> AdministrativeProvinces(code), Not Null | Mã Tỉnh/Thành muốn tìm bạn ở ghép |
| 5 | `target_ward_code` | `VARCHAR(20)` | FK -> AdministrativeWards(code), Not Null | Mã Xã/Phường muốn tìm bạn ở ghép |
| 6 | `title` | `VARCHAR(255)` | Not Null | Tiêu đề nội dung |
| 7 | `description` | `TEXT` | Not Null | Mô tả giới thiệu chi tiết |
| 8 | `budget` | `BIGINT` | Not Null | Thuộc tính thông tin bản ghi |
| 9 | `preferred_gender` | `VARCHAR(10)` | Not Null | Yêu cầu giới tính bạn ở ghép |
| 10 | `lifestyle_tags` | `VARCHAR(255)` | Null | Thuộc tính thông tin bản ghi |
| 11 | `status` | `VARCHAR(20)` | Not Null, Default: 'OPEN' | Trạng thái hoạt động của đối tượng |
| 12 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |
| 13 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="aichatsessions"></a>
#### 25. Bảng `AIChatSessions`
* **Ý nghĩa nghiệp vụ**: Phiên trò chuyện giữa người dùng và Trợ lý ảo AI tìm phòng.
* **Số lượng trường**: 5 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `user_id` | `BIGINT` | FK -> Users(id), Null | Mã người dùng sở hữu |
| 3 | `session_title` | `VARCHAR(255)` | Null | Tiêu đề tóm tắt phiên trò chuyện với AI |
| 4 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |
| 5 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="conversations"></a>
#### 26. Bảng `Conversations`
* **Ý nghĩa nghiệp vụ**: Hội thoại chat 2 chiều giữa Người thuê và Chủ nhà (chuẩn hóa chống trùng lặp luồng).
* **Số lượng trường**: 12 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `initiator_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người khởi xướng mở cuộc hội thoại |
| 3 | `recipient_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người tiếp nhận cuộc hội thoại |
| 4 | `listing_id` | `BIGINT` | FK -> Listings(id), Null | Mã tin đăng phòng được tham chiếu |
| 5 | `roommate_post_id` | `BIGINT` | FK -> RoommatePosts(id), Null | Mã bài đăng ở ghép liên quan cuộc chat |
| 6 | `ai_session_id` | `BIGINT` | FK -> AIChatSessions(id), Null | Mã phiên chat AI được chuyển tiếp sang chủ nhà |
| 7 | `conversation_type` | `VARCHAR(30)` | Not Null, Default: 'LISTING' | Phân loại hội thoại: LISTING, ROOMMATE, AI_ESCALATED |
| 8 | `forwarded_context` | `TEXT` | Null | Thuộc tính thông tin bản ghi |
| 9 | `last_message_preview` | `VARCHAR(255)` | Null | Thuộc tính thông tin bản ghi |
| 10 | `last_message_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm gửi tin nhắn cuối cùng trong hội thoại |
| 11 | `user_min_id` | `Computed` | Computed (CASE WHEN initiator_id < recipient_id THEN initiator_id ELSE recipient_id END) PERSISTED | Cột tính toán lưu ID nhỏ hơn để chuẩn hóa thứ tự chat |
| 12 | `user_max_id` | `Computed` | Computed (CASE WHEN initiator_id < recipient_id THEN recipient_id ELSE initiator_id END) PERSISTED | Cột tính toán lưu ID lớn hơn để chuẩn hóa thứ tự chat |


<a id="messagedrafts"></a>
#### 27. Bảng `MessageDrafts`
* **Ý nghĩa nghiệp vụ**: Bản nháp tin nhắn của từng người dùng trong hội thoại (nguồn gõ tay hoặc AI gợi ý).
* **Số lượng trường**: 7 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `conversation_id` | `BIGINT` | FK -> Conversations(id), Not Null | Mã cuộc hội thoại chứa tin nhắn / bản nháp |
| 3 | `author_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người soạn thảo bản nháp tin nhắn |
| 4 | `source` | `VARCHAR(20)` | Not Null, Default: 'MANUAL' | Nguồn phát sinh khách: DIRECT, SEARCH_FILTER, COMPARISON, AI_CHAT |
| 5 | `content` | `TEXT` | Not Null | Nội dung chi tiết |
| 6 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |
| 7 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="messages"></a>
#### 28. Bảng `Messages`
* **Ý nghĩa nghiệp vụ**: Chi tiết từng tin nhắn văn bản, hình ảnh được gửi trong cuộc hội thoại.
* **Số lượng trường**: 7 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `conversation_id` | `BIGINT` | FK -> Conversations(id), Not Null | Mã cuộc hội thoại chứa tin nhắn / bản nháp |
| 3 | `sender_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người gửi tin nhắn thực tế |
| 4 | `message_type` | `VARCHAR(20)` | Not Null, Default: 'TEXT' | Định dạng tin nhắn: TEXT, IMAGE, SYSTEM, AI_SUGGESTION |
| 5 | `content` | `TEXT` | Not Null | Nội dung chi tiết |
| 6 | `is_read` | `BOOLEAN` | Not Null, Default: FALSE | Cờ đánh dấu người dùng đã đọc thông báo |
| 7 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


---

### PHÂN HỆ 7: HỢP ĐỒNG, CHỈ SỐ ĐIỆN NƯỚC, HÓA ĐƠN & THANH TOÁN (4 BẢNG)
> **Ý nghĩa phân hệ**: Quản lý vòng đời hợp đồng thuê pháp lý, chốt chỉ số điện nước định kỳ, xuất hóa đơn tự động và duyệt thanh toán.

<a id="contracts"></a>
#### 29. Bảng `Contracts`
* **Ý nghĩa nghiệp vụ**: Hợp đồng thuê phòng pháp lý giữa Bên Cho Thuê và Bên Thuê (thời hạn, tiền cọc, tiền thuê).
* **Số lượng trường**: 15 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `contract_code` | `VARCHAR(50)` | Not Null, Unique | Số hiệu hợp đồng pháp lý (ví dụ: HD-2026-001) |
| 3 | `room_id` | `BIGINT` | FK -> Rooms(id), Not Null | Mã phòng cho thuê liên quan |
| 4 | `tenant_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người thuê nhà sở hữu |
| 5 | `landlord_id` | `BIGINT` | FK -> Users(id), Not Null | Mã chủ nhà sở hữu |
| 6 | `start_date` | `DATE` | Not Null | Ngày bắt đầu có hiệu lực hợp đồng |
| 7 | `end_date` | `DATE` | Not Null | Ngày kết thúc hợp đồng thuê |
| 8 | `monthly_rent` | `BIGINT` | Not Null | Giá thuê nhà cố định hàng tháng (VNĐ) |
| 9 | `deposit_amount` | `BIGINT` | Not Null | Số tiền đặt cọc giữ chỗ và bảo đảm hợp đồng (VNĐ) |
| 10 | `deposit_status` | `VARCHAR(20)` | Not Null, Default: 'PENDING' | Trạng thái cọc: PENDING, HELD_IN_ESCROW, PAID_TO_LANDLORD, REFUNDED, FORFEITED |
| 11 | `billing_due_day` | `INT` | Not Null, Default: 5 | Ngày thu tiền phòng định kỳ hàng tháng (ngày 1 - 28) |
| 12 | `contract_file_url` | `VARCHAR(500)` | Null | Thuộc tính thông tin bản ghi |
| 13 | `status` | `VARCHAR(20)` | Not Null, Default: 'DRAFT' | Trạng thái hoạt động của đối tượng |
| 14 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |
| 15 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="utilityreadings"></a>
#### 30. Bảng `UtilityReadings`
* **Ý nghĩa nghiệp vụ**: Phiếu ghi nhận chỉ số công tơ điện, nước hàng tháng theo từng phòng.
* **Số lượng trường**: 17 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `room_id` | `BIGINT` | FK -> Rooms(id), Not Null | Mã phòng cho thuê liên quan |
| 3 | `contract_id` | `BIGINT` | FK -> Contracts(id), Null | Mã hợp đồng thuê liên quan đến đợt chia sẻ |
| 4 | `billing_month` | `INT` | Not Null | Kỳ tính tiền tháng (1 đến 12) |
| 5 | `billing_year` | `INT` | Not Null | Kỳ tính tiền năm |
| 6 | `reading_date` | `DATE` | Not Null | Ngày chốt chỉ số công tơ thực tế |
| 7 | `old_electricity_index` | `INT` | Not Null | Thuộc tính thông tin bản ghi |
| 8 | `new_electricity_index` | `INT` | Not Null | Thuộc tính thông tin bản ghi |
| 9 | `electricity_usage` | `Computed` | Computed (new_electricity_index - old_electricity_index) PERSISTED | Cột tính toán số kWh điện tiêu thụ trong kỳ |
| 10 | `electricity_cost` | `BIGINT` | Not Null | Tiền điện phát sinh trong kỳ (VNĐ) |
| 11 | `old_water_index` | `INT` | Not Null | Thuộc tính thông tin bản ghi |
| 12 | `new_water_index` | `INT` | Not Null | Thuộc tính thông tin bản ghi |
| 13 | `water_usage` | `Computed` | Computed (new_water_index - old_water_index) PERSISTED | Cột tính toán số m³ nước tiêu thụ trong kỳ |
| 14 | `water_cost` | `BIGINT` | Not Null | Tiền nước phát sinh trong kỳ (VNĐ) |
| 15 | `meter_photo_url` | `VARCHAR(500)` | Null | Thuộc tính thông tin bản ghi |
| 16 | `recorded_by` | `BIGINT` | FK -> Users(id), Not Null | Mã người ghi nhận chỉ số (Chủ nhà hoặc Quản lý) |
| 17 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


<a id="invoices"></a>
#### 31. Bảng `Invoices`
* **Ý nghĩa nghiệp vụ**: Hóa đơn tiền phòng và dịch vụ định kỳ hàng tháng (tiền phòng + điện + nước - giảm giá).
* **Số lượng trường**: 17 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `invoice_code` | `VARCHAR(50)` | Not Null, Unique | Số hiệu hóa đơn thanh toán (ví dụ: INV-202610-001) |
| 3 | `contract_id` | `BIGINT` | FK -> Contracts(id), Not Null | Mã hợp đồng thuê liên quan đến đợt chia sẻ |
| 4 | `utility_reading_id` | `BIGINT` | FK -> UtilityReadings(id), Null | Mã phiếu chỉ số điện nước kèm theo hóa đơn |
| 5 | `billing_month` | `INT` | Not Null | Kỳ tính tiền tháng (1 đến 12) |
| 6 | `billing_year` | `INT` | Not Null | Kỳ tính tiền năm |
| 7 | `rent_amount` | `BIGINT` | Not Null | Tiền thuê phòng trong kỳ (VNĐ) |
| 8 | `electricity_amount` | `BIGINT` | Not Null, Default: 0 | Tiền điện trong kỳ theo chỉ số (VNĐ) |
| 9 | `water_amount` | `BIGINT` | Not Null, Default: 0 | Tiền nước trong kỳ theo chỉ số (VNĐ) |
| 10 | `service_amount` | `BIGINT` | Not Null, Default: 0 | Tổng chi phí dịch vụ khác (rác, vệ sinh, thang máy...) |
| 11 | `discount_amount` | `BIGINT` | Not Null, Default: 0 | Số tiền được giảm giá / khuyến mãi (VNĐ) |
| 12 | `total_amount` | `Computed` | Computed (rent_amount + electricity_amount + water_amount + service_amount - discount_amount) PERSISTED | Cột tính toán tổng tiền phải thanh toán của hóa đơn (VNĐ) |
| 13 | `paid_amount` | `BIGINT` | Not Null, Default: 0 | Số tiền thực tế đã được thanh toán và xác thực (VNĐ) |
| 14 | `due_date` | `DATE` | Not Null | Hạn chót thanh toán hóa đơn trước khi bị tính nợ quá hạn |
| 15 | `status` | `VARCHAR(20)` | Not Null, Default: 'UNPAID' | Trạng thái hoạt động của đối tượng |
| 16 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |
| 17 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="payments"></a>
#### 32. Bảng `Payments`
* **Ý nghĩa nghiệp vụ**: Lịch sử giao dịch thanh toán hóa đơn tiền phòng giữa Khách và Chủ nhà.
* **Số lượng trường**: 10 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `invoice_id` | `BIGINT` | FK -> Invoices(id), Not Null | Mã hóa đơn được thanh toán |
| 3 | `amount` | `BIGINT` | Not Null | Số tiền giao dịch thực tế (VNĐ) |
| 4 | `payment_method` | `VARCHAR(20)` | Not Null | Phương thức thanh toán: CASH, BANK_TRANSFER, VNPAY, MOMO |
| 5 | `transaction_ref` | `VARCHAR(100)` | Null | Mã tham chiếu giao dịch ngân hàng / cổng thanh toán |
| 6 | `proof_image_url` | `VARCHAR(500)` | Null | Đường dẫn ảnh chụp biên lai chuyển khoản ngân hàng |
| 7 | `status` | `VARCHAR(20)` | Not Null, Default: 'PENDING' | Trạng thái hoạt động của đối tượng |
| 8 | `verified_by` | `BIGINT` | FK -> Users(id), Null | Mã người duyệt thanh toán (Chủ nhà hoặc Admin) |
| 9 | `verified_at` | `TIMESTAMPTZ` | Null | Thời điểm duyệt xác nhận khoản thanh toán thành công |
| 10 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


---

### PHÂN HỆ 8: QUẢN LÝ BẢO TRÌ & SỰ CỐ (2 BẢNG)
> **Ý nghĩa phân hệ**: Tiếp nhận và theo dõi tiến độ sửa chữa các sự cố hư hỏng phòng kèm hình ảnh minh chứng thực tế (chuẩn 1NF).

<a id="maintenancerequests"></a>
#### 33. Bảng `MaintenanceRequests`
* **Ý nghĩa nghiệp vụ**: Phiếu báo cáo sự cố hư hỏng thiết bị phòng cần chủ nhà cử thợ bảo trì.
* **Số lượng trường**: 13 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `request_code` | `VARCHAR(50)` | Not Null, Unique | Mã số yêu cầu bảo trì (ví dụ: MNT-2026-001) |
| 3 | `room_id` | `BIGINT` | FK -> Rooms(id), Not Null | Mã phòng cho thuê liên quan |
| 4 | `contract_id` | `BIGINT` | FK -> Contracts(id), Null | Mã hợp đồng thuê liên quan đến đợt chia sẻ |
| 5 | `tenant_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người thuê nhà sở hữu |
| 6 | `title` | `VARCHAR(200)` | Not Null | Tiêu đề nội dung |
| 7 | `description` | `TEXT` | Not Null | Mô tả giới thiệu chi tiết |
| 8 | `category` | `VARCHAR(50)` | Not Null | Nhóm phân loại tiện ích / dịch vụ |
| 9 | `priority` | `VARCHAR(20)` | Not Null, Default: 'MEDIUM' | Mức độ khẩn cấp: LOW, MEDIUM, HIGH, EMERGENCY |
| 10 | `status` | `VARCHAR(20)` | Not Null, Default: 'OPEN' | Trạng thái hoạt động của đối tượng |
| 11 | `resolved_at` | `TIMESTAMPTZ` | Null | Thời điểm thợ hoàn tất xử lý sự cố |
| 12 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |
| 13 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="maintenanceimages"></a>
#### 34. Bảng `MaintenanceImages`
* **Ý nghĩa nghiệp vụ**: Album hình ảnh chụp thực tế vị trí sự cố hư hỏng đính kèm phiếu bảo trì (1NF).
* **Số lượng trường**: 4 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `request_id` | `BIGINT` | FK -> MaintenanceRequests(id), Not Null | Mã yêu cầu bảo trì được gắn ảnh |
| 3 | `image_url` | `VARCHAR(500)` | Not Null | Đường dẫn lưu trữ hình ảnh trên CDN |
| 4 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


---

### PHÂN HỆ 9: TRỢ LÝ AI & ĐỀ XUẤT THÔNG MINH (3 BẢNG)
> **Ý nghĩa phân hệ**: Hạ tầng dữ liệu cho Trợ lý AI: lưu trữ tin nhắn hội thoại AI, danh sách phòng AI đề xuất và việc cần làm cho chủ nhà.

<a id="aichatmessages"></a>
#### 35. Bảng `AIChatMessages`
* **Ý nghĩa nghiệp vụ**: Chi tiết từng câu hỏi của người dùng và câu trả lời của Trợ lý AI.
* **Số lượng trường**: 5 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `session_id` | `BIGINT` | FK -> AIChatSessions(id), Not Null | Mã phiên làm việc với AI |
| 3 | `sender_type` | `VARCHAR(20)` | Not Null | Bên phát ngôn trong chat AI: USER, ASSISTANT |
| 4 | `content` | `TEXT` | Not Null | Nội dung chi tiết |
| 5 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


<a id="aimessagesuggestions"></a>
#### 36. Bảng `AIMessageSuggestions`
* **Ý nghĩa nghiệp vụ**: Danh sách các tin đăng phòng được AI bóc tách gợi ý đính kèm tin nhắn (1NF).
* **Số lượng trường**: 5 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `message_id` | `BIGINT` | FK -> AIChatMessages(id), Not Null | Mã tin nhắn AI chứa gợi ý phòng |
| 3 | `listing_id` | `BIGINT` | FK -> Listings(id), Not Null | Mã tin đăng phòng được tham chiếu |
| 4 | `relevance_score` | `DECIMAL(5,2)` | Null | Điểm phần trăm phù hợp với nhu cầu người dùng (0 - 100%) |
| 5 | `suggested_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm AI đưa ra gợi ý phòng |


<a id="aitasksuggestions"></a>
#### 37. Bảng `AITaskSuggestions`
* **Ý nghĩa nghiệp vụ**: Danh sách các công việc quản trị thông minh do AI đề xuất cho chủ nhà xử lý.
* **Số lượng trường**: 10 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `landlord_id` | `BIGINT` | FK -> Users(id), Not Null | Mã chủ nhà sở hữu |
| 3 | `task_type` | `VARCHAR(50)` | Not Null | Loại việc AI đề xuất: VACANT_ROOM_PRICE_CUT, OVERDUE_DEBT_ALERT... |
| 4 | `reference_type` | `VARCHAR(50)` | Null | Tên thực thể dữ liệu liên kết |
| 5 | `reference_id` | `BIGINT` | Null | Mã bản ghi trong bảng nghiệp vụ liên kết |
| 6 | `title` | `VARCHAR(200)` | Not Null | Tiêu đề nội dung |
| 7 | `description` | `TEXT` | Not Null | Mô tả giới thiệu chi tiết |
| 8 | `suggested_action` | `TEXT` | Null | Hành động cụ thể được AI khuyến nghị chủ nhà bấm làm ngay |
| 9 | `is_dismissed` | `BOOLEAN` | Not Null, Default: FALSE | Cờ đánh dấu chủ nhà đã bỏ qua gợi ý của AI |
| 10 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


---

### PHÂN HỆ 10: QUẢN TRỊ VẬN HÀNH SÀN & ĐỘNG CƠ DOANH THU (7 BẢNG)
> **Ý nghĩa phân hệ**: Quản lý cấu hình sàn, nhật ký kiểm toán Admin, khiếu nại vi phạm, gói phần mềm SaaS, gói đẩy tin VIP và doanh thu sàn.

<a id="platformconfigs"></a>
#### 38. Bảng `PlatformConfigs`
* **Ý nghĩa nghiệp vụ**: Bảng tham số cấu hình hệ thống động do Admin quản lý (hạn mức, số ngày hết hạn...).
* **Số lượng trường**: 7 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `config_key` | `VARCHAR(100)` | PK | Khóa định danh tham số cấu hình hệ thống (PK) |
| 2 | `config_value` | `TEXT` | Not Null | Giá trị tham số cấu hình |
| 3 | `data_type` | `VARCHAR(20)` | Not Null | Kiểu dữ liệu: STRING, INT, DECIMAL, BOOLEAN, JSON |
| 4 | `category` | `VARCHAR(50)` | Not Null | Nhóm phân loại tiện ích / dịch vụ |
| 5 | `description` | `VARCHAR(500)` | Null | Mô tả giới thiệu chi tiết |
| 6 | `updated_by` | `BIGINT` | FK -> Users(id), Not Null | Mã Quản trị viên thực hiện cập nhật cấu hình |
| 7 | `updated_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm cập nhật bản ghi gần nhất |


<a id="adminauditlogs"></a>
#### 39. Bảng `AdminAuditLogs`
* **Ý nghĩa nghiệp vụ**: Nhật ký kiểm toán ghi vết mọi hành vi can thiệp nhạy cảm của Quản trị viên hệ thống.
* **Số lượng trường**: 10 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `admin_id` | `BIGINT` | FK -> Users(id), Not Null | Mã Quản trị viên thực hiện hành động kiểm toán |
| 3 | `action_type` | `VARCHAR(50)` | Not Null | Hành vi quản trị: LOCK_USER, HIDE_LISTING, APPROVE_KYC... |
| 4 | `target_table` | `VARCHAR(50)` | Not Null | Tên bảng dữ liệu chịu tác động của hành động |
| 5 | `target_id` | `BIGINT` | Not Null | Mã bản ghi trong bảng dữ liệu chịu tác động |
| 6 | `old_values` | `TEXT` | Null | Chuỗi JSON ghi nhận giá trị dữ liệu trước khi thay đổi |
| 7 | `new_values` | `TEXT` | Null | Chuỗi JSON ghi nhận giá trị dữ liệu sau khi can thiệp |
| 8 | `reason` | `VARCHAR(500)` | Not Null | Lý do căn cứ thực hiện hành động can thiệp |
| 9 | `ip_address` | `VARCHAR(45)` | Null | Địa chỉ IP thực hiện yêu cầu |
| 10 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


<a id="contentreports"></a>
#### 40. Bảng `ContentReports`
* **Ý nghĩa nghiệp vụ**: Phiếu khiếu nại, tố cáo vi phạm từ người dùng (tin ảo, lừa cọc, giá sai thực tế...).
* **Số lượng trường**: 12 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `reporter_id` | `BIGINT` | FK -> Users(id), Not Null | Mã người dùng gửi phiếu tố cáo / khiếu nại |
| 3 | `target_type` | `VARCHAR(20)` | Not Null | Đối tượng bị tố cáo: LISTING, USER, ROOMMATE_POST, CONTRACT |
| 4 | `target_id` | `BIGINT` | Not Null | Mã bản ghi trong bảng dữ liệu chịu tác động |
| 5 | `report_reason` | `VARCHAR(50)` | Not Null | Lý do vi phạm: FAKE_LISTING, SCAM_DEPOSIT, INCORRECT_PRICING... |
| 6 | `description` | `TEXT` | Not Null | Mô tả giới thiệu chi tiết |
| 7 | `evidence_urls` | `TEXT` | Null | Đường dẫn các hình ảnh/video bằng chứng vi phạm |
| 8 | `status` | `VARCHAR(20)` | Not Null, Default: 'PENDING' | Trạng thái hoạt động của đối tượng |
| 9 | `resolution_notes` | `TEXT` | Null | Kết luận và biện pháp giải quyết của Quản trị viên |
| 10 | `resolved_by` | `BIGINT` | FK -> Users(id), Null | Mã Quản trị viên trực tiếp thụ lý giải quyết |
| 11 | `resolved_at` | `TIMESTAMPTZ` | Null | Thời điểm thợ hoàn tất xử lý sự cố |
| 12 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


<a id="platformserviceplans"></a>
#### 41. Bảng `PlatformServicePlans`
* **Ý nghĩa nghiệp vụ**: Danh mục các gói sản phẩm, dịch vụ Sàn cung cấp (SaaS PMS, Đẩy tin VIP, AI Copilot).
* **Số lượng trường**: 10 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `INT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `name` | `VARCHAR(100)` | Not Null | Tên gọi hiển thị |
| 3 | `code` | `VARCHAR(50)` | Not Null, Unique | Mã định danh duy nhất |
| 4 | `plan_type` | `VARCHAR(30)` | Not Null | Phân loại gói dịch vụ: SAAS_PMS, LISTING_BOOST, AI_ADDON |
| 5 | `price` | `BIGINT` | Not Null | Giá bán gói dịch vụ niêm yết (VNĐ) |
| 6 | `duration_days` | `INT` | Not Null | Thời hạn sử dụng của gói tính theo ngày |
| 7 | `max_rooms_allowed` | `INT` | Null | Hạn mức số phòng tối đa được quản lý (gói SaaS) |
| 8 | `vip_boost_quota` | `INT` | Not Null, Default: 0 | Số lượt đẩy tin VIP được tặng kèm trong gói |
| 9 | `ai_query_quota` | `INT` | Not Null, Default: 0 | Số lượt hỏi đáp AI nâng cao được sử dụng trong gói |
| 10 | `is_active` | `BOOLEAN` | Not Null, Default: TRUE | Trạng thái gói sản phẩm: 1 = Đang mở bán, 0 = Tạm ngừng |


<a id="landlordsubscriptions"></a>
#### 42. Bảng `LandlordSubscriptions`
* **Ý nghĩa nghiệp vụ**: Hợp đồng thuê bao gói phần mềm quản lý trọ SaaS định kỳ của từng chủ nhà.
* **Số lượng trường**: 8 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `landlord_id` | `BIGINT` | FK -> Users(id), Not Null | Mã chủ nhà sở hữu |
| 3 | `plan_id` | `INT` | FK -> PlatformServicePlans(id), Not Null | Mã gói dịch vụ phần mềm được đăng ký |
| 4 | `start_date` | `DATE` | Not Null | Ngày bắt đầu có hiệu lực hợp đồng |
| 5 | `end_date` | `DATE` | Not Null | Ngày kết thúc hợp đồng thuê |
| 6 | `status` | `VARCHAR(20)` | Not Null, Default: 'ACTIVE' | Trạng thái hoạt động của đối tượng |
| 7 | `auto_renew` | `BOOLEAN` | Not Null, Default: FALSE | Cờ tự động gia hạn khi gói hết hạn (1 = Tự động gia hạn) |
| 8 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


<a id="listingpromotions"></a>
#### 43. Bảng `ListingPromotions`
* **Ý nghĩa nghiệp vụ**: Lịch sử và cấu hình mua gói đẩy tin VIP nổi bật cho tin đăng phòng (điểm ưu tiên 1..10).
* **Số lượng trường**: 9 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `listing_id` | `BIGINT` | FK -> Listings(id), Not Null | Mã tin đăng phòng được tham chiếu |
| 3 | `promotion_type` | `VARCHAR(30)` | Not Null | Loại hình đẩy tin VIP: TOP_RECOMMENDED, BADGE_VERIFIED, HIGHLIGHT_BORDER |
| 4 | `priority_score` | `INT` | Not Null, Default: 1 | Điểm ưu tiên hiển thị trên kết quả tìm kiếm (1 đến 10) |
| 5 | `start_time` | `TIMESTAMPTZ` | Not Null | Thời điểm bắt đầu kích hoạt gói đẩy tin VIP |
| 6 | `end_time` | `TIMESTAMPTZ` | Not Null | Thời điểm hết hạn gói đẩy tin VIP |
| 7 | `amount_paid` | `BIGINT` | Not Null | Số tiền thực trả cho gói đẩy tin VIP (VNĐ) |
| 8 | `status` | `VARCHAR(20)` | Not Null, Default: 'ACTIVE' | Trạng thái hoạt động của đối tượng |
| 9 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


<a id="platformtransactions"></a>
#### 44. Bảng `PlatformTransactions`
* **Ý nghĩa nghiệp vụ**: Toàn bộ dòng tiền doanh thu thực tế nộp về tài khoản Công ty Sàn HomeMatch.
* **Số lượng trường**: 12 cột

| STT | Tên trường (Column) | Kiểu dữ liệu | Khóa & Ràng buộc | Ý nghĩa dữ liệu |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `id` | `BIGINT` | PK, Identity | Khóa chính tự tăng định danh bản ghi |
| 2 | `transaction_code` | `VARCHAR(50)` | Not Null, Unique | Mã số giao dịch thanh toán duy nhất của Sàn HomeMatch |
| 3 | `payer_user_id` | `BIGINT` | FK -> Users(id), Not Null | Mã tài khoản khách hàng thanh toán tiền về Sàn |
| 4 | `revenue_stream` | `VARCHAR(50)` | Not Null | Nguồn doanh thu: SAAS_SUBSCRIPTION, LISTING_VIP_BOOST, BROKERAGE_COMMISSION... |
| 5 | `reference_table` | `VARCHAR(50)` | Null | Tên bảng nghiệp vụ liên kết doanh thu |
| 6 | `reference_id` | `BIGINT` | Null | Mã bản ghi trong bảng nghiệp vụ liên kết |
| 7 | `amount` | `BIGINT` | Not Null | Số tiền giao dịch thực tế (VNĐ) |
| 8 | `payment_method` | `VARCHAR(30)` | Not Null | Phương thức thanh toán: CASH, BANK_TRANSFER, VNPAY, MOMO |
| 9 | `gateway_transaction_id` | `VARCHAR(100)` | Null | Mã giao dịch đối soát từ cổng VNPAY/MoMo |
| 10 | `status` | `VARCHAR(20)` | Not Null, Default: 'PENDING' | Trạng thái hoạt động của đối tượng |
| 11 | `completed_at` | `TIMESTAMPTZ` | Null | Thời điểm giao dịch được cổng thanh toán ghi nhận thành công |
| 12 | `created_at` | `TIMESTAMPTZ` | Not Null, Default: CURRENT_TIMESTAMP | Thời điểm tạo bản ghi trong hệ thống |


---

## 3. TÓM TẮT CÁC ĐỐI TƯỢNG VẬN HÀNH BỔ TRỢ
- **16 Triggers**: Kiểm soát toàn vẹn đa bảng (khớp chủ tòa nhà với tin/hợp đồng), chống mạo danh ghi log Admin, chống trùng lịch xem phòng và đồng bộ tiền thanh toán.
- **11 Views**: View tìm kiếm công khai SLA < 3s (`vw_public_listings`), Dashboard sàn Admin (`vw_admin_platform_summary`), Hàng đợi khiếu nại vi phạm (`vw_admin_pending_reports`), Dashboard chủ nhà (`vw_landlord_dashboard_summary`) và Báo cáo công nợ quá hạn (`vw_landlord_overdue_invoices`).
- **2 Stored Procedures**: `sp_book_viewing_appointment` (Đặt lịch an toàn với khóa giao dịch `pg_advisory_xact_lock` loại trừ xung đột đồng thời) và `sp_process_daily_contract_expiration_and_reminders` (Quét tự động hết hạn hợp đồng, tin VIP, gói SaaS).
- **2 Stored Functions**: `fn_get_landlord_financial_report` (Báo cáo doanh thu dồn tích theo `due_date` vs dòng tiền thực thu theo `paid_at`) và `fn_search_properties_within_radius` (Tìm kiếm bất động sản theo bán kính tọa độ không gian PostGIS `ST_DWithin`).