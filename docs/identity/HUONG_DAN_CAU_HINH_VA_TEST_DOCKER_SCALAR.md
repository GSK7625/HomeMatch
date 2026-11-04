# Hướng dẫn cấu hình & Kiểm thử Identity bằng Docker Compose + Scalar UI

Tài liệu này hướng dẫn quy trình khởi động hệ thống và kiểm thử toàn diện các API của phân hệ **Identity** bằng công cụ **Scalar API Reference** trên môi trường Docker.

---

## 1. Kiến trúc môi trường kiểm thử

Hệ thống được khởi chạy đồng bộ thông qua tệp `compose.yaml`:

| Dịch vụ | Tên container | Cổng Host → Container | Vai trò |
|---|---|---|---|
| **PostgreSQL 16 + PostGIS** | `homematch-db` | `5432:5432` | Lưu trữ dữ liệu 10 schema, tự động nạp `HomeMatch_Database.sql` |
| **Backend API (.NET 10)** | `homematch-backend` | `5000:5000` | Cung cấp RESTful API, tự động migrate EF Core trong Development |
| **Frontend (Next.js 15)** | `homematch-frontend` | `3000:3000` | Giao diện người dùng web |
| **Scalar UI** | Tích hợp trong Backend | `http://localhost:5000/scalar/v1` | Giao diện tương tác và kiểm thử API trực quan |

---

## 2. Các bước khởi động hệ thống

### Bước 2.1: Cấu hình tệp `.env`
Đảm bảo tại thư mục gốc repository đã có tệp `.env` chứa các thông số cấu hình:

```dotenv
# Cấu hình Database
POSTGRES_USER=homematch_user
POSTGRES_PASSWORD=homematch_password
POSTGRES_DB=homematch_db
POSTGRES_PORT=5432

# Cấu hình Backend & JWT
ASPNETCORE_ENVIRONMENT=Development
API_PORT=5000
Jwt__Secret=<chuỗi_base64_ngẫu_nhiên_tối_thiểu_32_bytes>
Jwt__Issuer=HomeMatch
Jwt__Audience=HomeMatchApp

# Cấu hình gửi OTP qua Gmail SMTP
Email__Enabled=true
Email__Smtp__Host=smtp.gmail.com
Email__Smtp__Port=587
Email__Smtp__Security=StartTls
Email__Smtp__Username=<gmail_cua_ban>
Email__Smtp__FromAddress=<gmail_cua_ban>
Email__Smtp__AppPassword=<mat_khau_ung_dung_16_ky_tu_cua_gmail>

# Khóa mã hóa OTP Challenge
Identity__EmailVerification__KeyId=v1
Identity__EmailVerification__Keys__v1=<chuỗi_base64_ngẫu_nhiên_tối_thiểu_32_bytes>
```

### Bước 2.2: Khởi chạy bằng Docker Compose
Mở PowerShell tại thư mục gốc của dự án (`D:\My Project\HomeMatch\HomeMatch`) và chạy:

```powershell
# Dọn dẹp volume cũ (chỉ cần chạy khi muốn làm mới hoàn toàn database)
docker compose down -v

# Khởi động toàn bộ các dịch vụ ngầm
docker compose up -d
```

### Bước 2.3: Kiểm tra trạng thái
Kiểm tra sức khỏe backend:
```powershell
curl.exe http://localhost:5000/health
```
*(Kết quả trả về `{"status":"Healthy",...}` chứng tỏ hệ thống đã kết nối database và sẵn sàng tiếp nhận request).*

---

## 3. Quy tắc thao tác trên Scalar UI

Truy cập giao diện tại: **`http://localhost:5000/scalar/v1`**

1. **Cấu trúc màn hình:**
   - Cột trái: Danh sách các endpoint theo module.
   - Khung giữa: Thanh URL + nút **Send**, các tab thiết lập **Headers** và **Request Body**.
2. **Quy tắc nhập Request Body (JSON):**
   - Chọn định dạng **JSON** và chế độ **Raw**.
   - Bắt buộc dùng dấu ngoặc kép `"`, không đặt dấu phẩy `,` ở dòng thuộc tính cuối cùng.
3. **Quy tắc Header CSRF (`X-CSRF-TOKEN`):**
   - Các API thay đổi trạng thái phiên (`login`, `refresh`, `logout`) bắt buộc phải gọi `GET /api/v1/identity/auth/csrf` trước để lấy chuỗi `requestToken` và truyền vào header `X-CSRF-TOKEN`.
4. **Quy tắc Header xác thực (`Authorization`):**
   - Các API yêu cầu đăng nhập (`/me`, `logout-all`) bắt buộc phải có từ khóa **`Bearer `** (kèm 1 khoảng trắng) trước access token:
     `Authorization: Bearer <chuỗi_token>`

---

## 4. Kịch bản kiểm thử luồng nghiệp vụ chi tiết

### Kịch bản 1: Đăng ký tài khoản mới ➔ Xác thực OTP

#### 1. Đăng ký tài khoản
- **Method & Route:** `POST /api/v1/identity/auth/register`
- **Headers:** `content-type: application/json`
- **Request Body:**
```json
{
  "email": "email_that_cua_ban@gmail.com",
  "password": "HomeMatch2026!",
  "fullName": "Nguyễn Văn Test",
  "role": "Tenant"
}
```
- **Kết quả:** HTTP `202 Accepted`, nhận `challengeId`. Mở hòm thư Gmail để lấy mã OTP 6 số.

#### 2. Xác thực OTP
- **Method & Route:** `POST /api/v1/identity/auth/registration/verify-otp`
- **Headers:** `content-type: application/json`
- **Request Body:**
```json
{
  "challengeId": "<challengeId_vừa_nhận_ở_bước_1>",
  "code": "123456"
}
```
- **Kết quả:** HTTP `200 OK`, `status: "Verified"`. Tài khoản chính thức kích hoạt.

---

### Kịch bản 2: Lấy CSRF Token ➔ Đăng nhập ➔ Xem Profile

#### 1. Lấy CSRF Token
- **Method & Route:** `GET /api/v1/identity/auth/csrf`
- **Headers:** Không yêu cầu
- **Request Body:** Để trống
- **Kết quả:** HTTP `200 OK`, copy chuỗi trong `data.requestToken`.

#### 2. Đăng nhập
- **Method & Route:** `POST /api/v1/identity/auth/login`
- **Headers:**
  - `content-type`: `application/json`
  - `X-CSRF-TOKEN`: `<requestToken_ở_bước_trên>`
- **Request Body:**
```json
{
  "email": "email_that_cua_ban@gmail.com",
  "password": "HomeMatch2026!"
}
```
- **Kết quả:** HTTP `200 OK`, nhận `accessToken` (chuỗi dài dạng `eyJhbG...`).

#### 3. Xem thông tin cá nhân (Profile)
- **Method & Route:** `GET /api/v1/identity/me`
- **Headers:**
  - `Authorization`: `Bearer <accessToken_vừa_nhận>`
- **Request Body:** Để trống
- **Kết quả:** HTTP `200 OK`, hiển thị chi tiết tài khoản:
```json
{
  "isSuccess": true,
  "data": {
    "userId": "...",
    "email": "email_that_cua_ban@gmail.com",
    "fullName": "Nguyễn Văn Test",
    "role": "Tenant",
    "emailVerified": true
  }
}
```

---

### Kịch bản 3: Quên mật khẩu ➔ OTP ➔ Đặt mật khẩu mới

#### 1. Yêu cầu cấp lại mật khẩu
- **Method & Route:** `POST /api/v1/identity/auth/forgot-password`
- **Headers:** `content-type: application/json`
- **Request Body:**
```json
{
  "email": "email_that_cua_ban@gmail.com"
}
```
- **Kết quả:** HTTP `200 OK`, nhận `challengeId` và nhận OTP qua Gmail.

#### 2. Xác thực OTP quên mật khẩu
- **Method & Route:** `POST /api/v1/identity/auth/forgot-password/verify-otp`
- **Headers:** `content-type: application/json`
- **Request Body:**
```json
{
  "challengeId": "<challengeId_ở_bước_trên>",
  "code": "123456"
}
```
- **Kết quả:** HTTP `200 OK`, nhận chuỗi `resetToken` (hạn dùng 5 phút).

#### 3. Đặt mật khẩu mới
- **Method & Route:** `POST /api/v1/identity/auth/reset-password`
- **Headers:** `content-type: application/json`
- **Request Body:**
```json
{
  "resetToken": "<resetToken_ở_bước_trên>",
  "newPassword": "HomeMatchMoi2026!"
}
```
- **Kết quả:** HTTP `200 OK`, `status: "Reset"`. Mật khẩu cũ bị vô hiệu hóa hoàn toàn.

---

### Kịch bản 4: Đăng xuất (Logout)

#### 1. Đăng xuất phiên hiện tại
- **Method & Route:** `POST /api/v1/identity/auth/logout`
- **Headers:**
  - `X-CSRF-TOKEN`: `<requestToken_lấy_từ_GET_/auth/csrf>`
  - `content-type`: `application/json`
- **Request Body:** `{}`
- **Kết quả:** HTTP `204 No Content` (Refresh cookie bị xóa).

#### 2. Đăng xuất toàn bộ các thiết bị
- **Method & Route:** `POST /api/v1/identity/auth/logout-all`
- **Headers:**
  - `Authorization`: `Bearer <accessToken>`
  - `content-type`: `application/json`
- **Request Body:** `{}`
- **Kết quả:** HTTP `204 No Content` (Tất cả phiên đăng nhập của tài khoản trên mọi máy đều bị thu hồi).

---

## 5. Bảng tra cứu mã lỗi thường gặp

| HTTP Code | Error Code | Nguyên nhân | Cách khắc phục |
|---|---|---|---|
| **401 Unauthorized** | `Identity.AuthenticationRequired` | Header `Authorization` thiếu tiền tố `Bearer ` hoặc token đã hết hạn | Thêm chữ `Bearer ` (có khoảng trắng) trước token: `Bearer eyJ...` |
| **403 Forbidden** | `Identity.OriginNotAllowed` | Gọi API từ tên miền/cổng không nằm trong CORS whitelist | Chỉ gửi request từ `http://localhost:5000` (Scalar) hoặc `http://localhost:3000` (Frontend) |
| **403 Forbidden** | `Identity.CsrfInvalid` | Thiếu hoặc sai header `X-CSRF-TOKEN` trong các mutation (login/logout/refresh) | Gọi `GET /auth/csrf` để lấy token mới và điền vào header `X-CSRF-TOKEN` |
| **400 Bad Request** | `Identity.ValidationFailed` | Cú pháp JSON sai, hoặc truyền thừa/thiếu field (ví dụ gửi `fullName` vào `/auth/login`) | Kiểm tra lại JSON Body, chỉ truyền đúng các trường yêu cầu |
| **400 Bad Request** | `Identity.EmailNotVerified` | Tài khoản chưa hoàn tất bước verify OTP | Gọi API `verify-otp` trước khi đăng nhập |
| **429 Too Many Requests** | `Identity.RateLimitExceeded` | Gửi request liên tục vượt ngưỡng rate limit | Kiểm tra giá trị giây trong header `Retry-After` và đợi trước khi gửi lại |
