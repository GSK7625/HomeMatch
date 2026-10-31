# OTP đăng ký và quên mật khẩu — 01/11/2026

Bản này thay quy tắc cũ cho đăng ký/login: đăng ký tạo tài khoản chưa xác minh và gửi OTP; login trả 403 `Identity.EmailNotVerified` nếu chưa xác minh email. Tài khoản cũ chưa xác minh cũng phải xác minh qua API mới. Phạm vi thay đổi là backend, frontend hiện chưa có màn hình xác thực.

Base path `/api/v1/identity`. Response giữ envelope `ApiResponse`; dữ liệu bên dưới nằm trong `data`.

| Bước | API POST | Body |
|---|---|---|
| Đăng ký và gửi OTP | `/auth/register` | `{email,password,fullName,role}` như trước |
| Gửi lại mã đăng ký | `/auth/registration/send-otp` | `{email,password}` |
| Xác thực đăng ký | `/auth/registration/verify-otp` | `{challengeId,code}` |
| Yêu cầu khôi phục | `/auth/forgot-password` | `{email}` |
| Xác thực mã khôi phục | `/auth/forgot-password/verify-otp` | `{challengeId,code}` |
| Đặt mật khẩu mới | `/auth/reset-password` | `{resetToken,newPassword}` |

Đăng ký trả HTTP 202 Accepted; gửi lại mã, verify và reset thành công trả HTTP 200.

Gửi mã trả `status=Submitted`, `challengeId`, `expiresAt`, `retryAfterSeconds`. Đây là phản hồi chung nhằm không công khai email có tài khoản hay không; không bảo đảm SMTP đã gửi thành công. Đăng ký trùng không ghi đè mật khẩu/hồ sơ. Gửi lại mã đăng ký yêu cầu đúng mật khẩu tài khoản chưa xác minh. Khôi phục chỉ gửi tới tài khoản Active đã xác minh email.

OTP là string đúng 6 chữ số (giữ số 0 đầu), hạn 5 phút, khóa sau 5 lần sai. Gửi lại tối thiểu 60 giây; reservation chưa được xác nhận gửi phải chờ tối thiểu 120 giây. Quota tài khoản: 5 lần/15 phút, 20 lần/24 giờ; sender: 100/ngày UTC dùng chung với cơ chế gửi email cũ. Verify: 20 lần/15 phút/tài khoản. API có thêm quota IP trong process. `429` kèm `Retry-After`.

Verify đăng ký thành công trả `status=Verified`; sau đó dùng login bình thường (CSRF vẫn bắt buộc). Verify khôi phục thành công trả `status=Verified`, `resetToken`, `expiresAt`; giữ token trong bộ nhớ đến bước đặt mật khẩu mới. Token này hết hạn sau 5 phút, chỉ dùng một lần, không phải access token. OTP không được dùng chéo mục đích. Reset thành công trả `status=Reset`, thu hồi các phiên/refresh credential cũ và tăng SecurityVersion. Người dùng đăng nhập lại bằng mật khẩu mới.

Mã/token sai, hết hạn hoặc đã dùng: `400 Identity.OtpInvalid`; mật khẩu mới sai policy: `400 Identity.ValidationFailed`; tính năng email bị tắt: `503 Server.DependencyUnavailable`. Mật khẩu mới theo policy hiện tại: 8–128 Unicode scalar, có chữ và số.

Cần áp đầy đủ migration tới `ConvertIdentityToBigInt` qua thao tác initialize database tường minh. Xem [thiết kế dữ liệu](../database/DATABASE_DOCUMENTATION.md); cách chuyển đổi/rollback nằm trong `Infrastructure/Persistence/Migrations/Sql/ConvertIdentityToBigInt.Up.sql` và `.Down.sql`. Không tự động áp migration vào DB đang dùng. Bảng mới `identity.public_otp_challenges` chỉ lưu MAC OTP và hash reset token; reset grant còn có bản ghi `identity.user_tokens` loại RESET_PASSWORD; dùng lại bảng quota email. Cấu hình `Email:Enabled=true`, SMTP và `Identity:EmailVerification:Keys` theo hướng dẫn email hiện có; mặc định email vẫn tắt. Chưa thử SMTP thật.

Nếu đăng ký tạo account thành công nhưng gửi thư bị lỗi/tính năng tắt, account vẫn tồn tại chưa xác minh; cấu hình email rồi gửi lại bằng endpoint registration/send-otp. Không tự coi tạo account là đã xác minh.

Kiểm thử mới: `dotnet test backend/tests/IntegrationTests/HomeMatch.IntegrationTests.csproj -m:1 --filter FullyQualifiedName~PublicOtpFlowTests`. Các kiểm thử cũ đã cập nhật: xác minh OTP qua API hoặc seed tài khoản đã xác minh theo mục tiêu kiểm thử. Xem [đặc tả chức năng và API hiện hành](SO_DO_VA_DAC_TA_CHUC_NANG_XAC_THUC.md).

Lỗi database được nhận diện trong `PostgresPublicOtpStore` (Infrastructure), trả `PublicOtpErrors.Unavailable` qua Result; Presentation ánh xạ HTTP 503 và mã `Server.DependencyUnavailable`. Endpoint không phụ thuộc kiểu lỗi PostgreSQL. Các trường hợp lỗi bất ngờ vẫn do middleware xử lý.
