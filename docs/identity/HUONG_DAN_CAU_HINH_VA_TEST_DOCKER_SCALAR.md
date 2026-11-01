# Hướng dẫn cấu hình và test Identity bằng Docker + Scalar

Cập nhật 31/10/2026. Tài liệu này dành cho môi trường test trên máy cá nhân: PostgreSQL/PostGIS trong container `homematch-auth-test-db`, backend trong container `homematch-auth-api`, Scalar tại `http://localhost:5000/scalar/v1`. Mọi lệnh PowerShell dưới đây dùng được với **Windows PowerShell 5.1**. Base path của các API là `/api/v1/identity`.

## 1. Hiểu cấu hình đang dùng

| Thành phần | Giá trị test | Ý nghĩa |
|---|---|---|
| Database Docker | `homematch-auth-test-db:5432` | Backend container dùng **tên container** để kết nối; cổng `55434` chỉ dành cho kết nối từ Windows |
| Backend Docker | `127.0.0.1:5000` | Truy cập API/Scalar từ trình duyệt trên máy |
| `.env` ở gốc repository | JWT, email/OTP và cấu hình test | `docker run --env-file .env` nạp biến vào container; file được `.gitignore` bỏ qua |
| `ASPNETCORE_ENVIRONMENT=Development` | Scalar/OpenAPI hoạt động | API chỉ bật Scalar trong Development |

Các tên biến email/OTP trong `.env`:

```dotenv
Email__Enabled=true
Email__Smtp__Username=<gmail_gui_otp>
Email__Smtp__FromAddress=<cung_gmail_gui_otp>
Email__Smtp__AppPassword=<gmail_app_password_16_ky_tu_khong_khoang_trang>
Identity__EmailVerification__KeyId=v1
Identity__EmailVerification__Keys__v1=<base64_key_ngau_nhien_toi_thieu_32_byte>
Jwt__Secret=<base64_key_ngau_nhien_toi_thieu_32_byte_khac_key_otp>
```

Không thay các placeholder trên bằng giá trị thật trong tài liệu này. Gmail gửi và FromAddress phải trùng nhau; App Password đúng 16 ký tự chữ/số sau khi bỏ dấu cách. Khóa OTP và JWT phải độc lập. Nếu thay khóa OTP cùng `KeyId` khi đang có mã chờ, mã cũ sẽ không còn xác thực được. Khi chạy `dotnet run` trên Windows, `.env` **không tự nạp**; cách Docker bên dưới có `--env-file .env` nên nạp trực tiếp.

`.env` hiện chứa thông tin nhạy cảm và đã được chia sẻ một App Password trong chat. Sau khi test, thu hồi App Password đó, tạo mã mới rồi cập nhật `.env`; không đưa nội dung `.env`, mật khẩu, OTP, reset token hoặc access token vào ảnh/log công khai. Chỉ `.env` không đủ cho `docker compose up`: Compose hiện chưa chuyển tiếp các biến email/OTP vào backend, và dịch vụ database của Compose gắn script khởi tạo 44 bảng. Hướng dẫn này dùng container test riêng đã áp migration Identity.

## 2. Khởi động backend Docker

Mở PowerShell tại `D:\My Project\HomeMatch\HomeMatch`. Nếu backend Docker đã chạy và `http://localhost:5000/health` trả `Healthy`, chuyển thẳng sang mục 3.

```powershell
Set-Location -LiteralPath 'D:\My Project\HomeMatch\HomeMatch'
docker start homematch-auth-test-db
docker exec homematch-auth-test-db pg_isready -U homematch_test -d homematch_auth_test
```

Lần đầu, tạo mạng riêng và nối database vào đó. Nếu Docker báo mạng hoặc kết nối **đã tồn tại**, bỏ qua bước tương ứng.

```powershell
docker network create homematch-auth-docker-net
docker network connect homematch-auth-docker-net homematch-auth-test-db
docker build -t homematch-auth-api:local ./backend
```

Nhập mật khẩu của database **test** để tạo connection string trong phiên PowerShell (không in mật khẩu ra màn hình). Đây là mật khẩu bạn dùng lúc tạo `homematch-auth-test-db`:

```powershell
$dbPassword = Read-Host 'Nhap mat khau database test' -AsSecureString
$env:ConnectionStrings__DefaultConnection = 'Host=homematch-auth-test-db;Port=5432;Database=homematch_auth_test;Username=homematch_test;Password=' + ([Net.NetworkCredential]::new('', $dbPassword).Password)
$dbPassword.Dispose()
```

Tạo container API lần đầu:

```powershell
docker run -d --name homematch-auth-api `
  --network homematch-auth-docker-net `
  -p 127.0.0.1:5000:5000 `
  --env-file .env `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e ASPNETCORE_URLS=http://+:5000 `
  -e ConnectionStrings__DefaultConnection `
  -e Cors__AllowedOrigins__0=http://localhost:5000 `
  homematch-auth-api:local
```

Những lần sau có thể `docker start homematch-auth-api`, miễn là cấu hình container không thay đổi. Nếu sửa `.env`, phải **tạo lại container API** để nạp giá trị mới: dừng, xóa riêng container `homematch-auth-api`, rồi chạy lại lệnh `docker run` ở trên. Đừng xóa volume/database test. Nếu sửa code backend, build lại image trước khi tạo lại container.

Kiểm tra:

```powershell
docker logs --tail 30 homematch-auth-api
curl.exe http://localhost:5000/health
```

Kết quả health cần có `"status":"Healthy"`. Health chỉ xác nhận host API đang chạy; để xác nhận database và SMTP, hãy làm các request thực tế bên dưới. Mở `http://localhost:5000/scalar/v1` và dùng đúng **cổng 5000** suốt quá trình test. Tài khoản đã tạo trên database test ở cổng 5207 vẫn có thể dùng vì hai backend trỏ cùng database; token JWT/CSRF cần lấy mới từ backend Docker.

Database test phải bắt đầu trống và dùng migration duy nhất `20261101031824_InitialCreate`. Chạy image backend một lần với `--initialize-identity` và cùng network/cấu hình kết nối trước khi khởi động API. Không áp migration này lên database đã nạp `HomeMatch_Database.sql`: script đó tạo schema Identity theo bản thiết kế khác với model BIGINT hiện tại.

## 3. Quy tắc thao tác trên Scalar

Trong trang Scalar, bấm vào dòng endpoint, nhập **Request Body → JSON (Raw)**, bấm **Send** và đọc **status + Body phản hồi**. Các tên trường JSON phân biệt chữ hoa/thường. Đối với login/refresh/logout, request cần cookie CSRF và header `X-CSRF-TOKEN`. Trong bảng Headers, **cột bên trái là tên**, cột bên phải là giá trị; đừng để tên là `Key`. Không đưa request token vào Body.

Nếu endpoint không hiện sẵn ô Body, mở **Open API Client** hoặc tab **Raw** của Request Body để nhập JSON. `Content-Type` cần là `application/json`. Scalar có thể không suy luận đầy đủ body vì backend đọc JSON thủ công.

## 4. Test đăng ký → OTP → đăng nhập

Dùng email test bạn thực sự nhận được thư và **chưa có tài khoản**. Mật khẩu ví dụ chỉ dành cho tài khoản test; có thể tự chọn mật khẩu 8–128 ký tự Unicode, có chữ và số.

### 4.1 Đăng ký

`POST /api/v1/identity/auth/register`

```json
{
  "email": "email-moi-cua-ban@example.com",
  "password": "HomeMatch2026!",
  "fullName": "Nguyễn Văn Test",
  "role": "Tenant"
}
```

Thay email placeholder bằng mailbox nhận OTP của bạn. `Tenant` là người tìm/thuê trọ; thay bằng `Landlord` để test chủ nhà. Không thể tự đăng ký `Admin`. Kết quả đúng: HTTP 202 Accepted, `isSuccess: true`, `data.status: "Submitted"` và `data.challengeId`. Đây là yêu cầu đăng ký đang chờ xác thực OTP; API giữ cùng mã phản hồi cho email đã tồn tại để không tiết lộ tài khoản. Ghi lại `challengeId`, lấy OTP 6 số trong Inbox/Spam. `Submitted` không bảo đảm thư đã vào Inbox. Account đã tạo nhưng **chưa thể đăng nhập** trước khi xác minh.

Nếu cần gửi lại cho account chưa xác minh, dùng `POST /api/v1/identity/auth/registration/send-otp`:

```json
{"email":"email-chua-xac-minh@example.com","password":"mat-khau-da-dang-ky"}
```

Dùng `challengeId` **mới** từ phản hồi. Mã cũ có thể đã bị vô hiệu. Gửi lại quá sớm có thể trả 429 và header `Retry-After`.

### 4.2 Xác thực OTP

`POST /api/v1/identity/auth/registration/verify-otp`

```json
{"challengeId":"CHALLENGE_ID_TU_BUOC_DANG_KY","code":"123456"}
```

Thay `123456` bằng mã trong email, giữ cả số `0` ở đầu nếu có. Mã có hạn 5 phút, tối đa 5 lần nhập sai. Kết quả đúng: HTTP 200, `data.status: "Verified"`.

### 4.3 Lấy CSRF rồi đăng nhập

Trên **cùng trang Scalar cổng 5000**, gọi `GET /api/v1/identity/auth/csrf` và bấm **Send**. Phản hồi HTTP 200 có `data.requestToken`; trình duyệt cũng phải nhận cookie CSRF. Sao chép **chỉ chuỗi requestToken**, không sao chép toàn bộ JSON.

Gọi `POST /api/v1/identity/auth/login`. Trong Headers, thêm một hàng:

| Tên header (cột trái) | Giá trị (cột phải) |
|---|---|
| `X-CSRF-TOKEN` | Dán `data.requestToken` vừa lấy |

Body:

```json
{"email":"email-da-xac-minh@example.com","password":"HomeMatch2026!"}
```

Kết quả đúng: HTTP 200, `isSuccess: true`, `data.accessToken`, `data.tokenType: "Bearer"`. Access token thường có hạn 900 giây. Nếu nhận `Identity.CsrfInvalid`, lấy CSRF mới cùng cổng 5000, kiểm tra tên header và cookie trong request. Nếu Scalar không giữ cookie, thực hiện cặp request bằng một browser session có `credentials: 'include'` hoặc một API client có cookie jar. Nếu nhận `Identity.EmailNotVerified`, kiểm tra đã verify OTP đúng account chưa.

### 4.4 Kiểm tra tài khoản

Gọi `GET /api/v1/identity/me`, thêm header `Authorization` với giá trị `Bearer <accessToken>`. Kết quả đúng: HTTP 200, email/role của tài khoản, `emailVerified: true`. Dùng token mới lấy từ backend Docker; token từ phiên chạy local trước đó có thể không hợp lệ nếu JWT key khác.

## 5. Test quên mật khẩu → OTP → đặt mật khẩu mới

Dùng account đã xác minh email ở mục 4.

1. `POST /api/v1/identity/auth/forgot-password` với `{"email":"email-da-xac-minh@example.com"}`. Lấy `data.challengeId`, đọc OTP mới trong email. Phản hồi chung không tiết lộ email có account hay không.
2. `POST /api/v1/identity/auth/forgot-password/verify-otp` với `{"challengeId":"CHALLENGE_ID_MOI","code":"OTP_6_SO"}`. Kết quả đúng có `data.status: "Verified"` và `data.resetToken`. Token reset chỉ có hạn 5 phút, dùng một lần.
3. `POST /api/v1/identity/auth/reset-password` với `{"resetToken":"RESET_TOKEN_TU_BUOC_2","newPassword":"MatKhauMoi2026!"}`. Kết quả đúng có `data.status: "Reset"`.
4. Lấy CSRF mới và đăng nhập bằng mật khẩu mới; mật khẩu cũ phải bị từ chối. Phiên và access token cũ bị thu hồi. Gửi lại cùng reset token phải thất bại.

OTP đăng ký không dùng thay cho OTP quên mật khẩu. Đừng dán OTP/reset token/access token vào chat hoặc ảnh chụp.

## 6. Mã lỗi thường gặp

| Kết quả | Cách kiểm tra |
|---|---|
| `Identity.CsrfInvalid` | Lấy `GET /auth/csrf` mới trên cổng 5000; gửi cùng cookie và header `X-CSRF-TOKEN` đúng tên |
| `Identity.EmailNotVerified` | Hoàn tất `registration/verify-otp` trước khi login |
| `Identity.OtpInvalid` | Kiểm tra challengeId, mã 6 số, đúng mục đích, hạn 5 phút và chưa dùng |
| HTTP 429 | Đọc `Retry-After`, chờ rồi mới yêu cầu/gửi lại |
| HTTP 503 khi gửi OTP | Kiểm tra `docker logs homematch-auth-api`, cấu hình Gmail trong `.env` và kết nối SMTP; `health` không xác nhận gửi email |
| API không khởi động | Xem `docker logs --tail 50 homematch-auth-api`; kiểm tra biến JWT, OTP key, Gmail và connection string trong container mà không in giá trị bí mật |

Khi test xong: `docker stop homematch-auth-api`. Database test được giữ để lần sau tiếp tục. Không dùng `docker compose down -v` hoặc xóa volume khi còn cần dữ liệu test.
