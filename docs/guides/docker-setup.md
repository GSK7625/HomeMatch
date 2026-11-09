# Hướng Dẫn Chạy Dự Án HomeMatch Bằng Docker

Tài liệu này hướng dẫn chi tiết cách thiết lập, khởi chạy và kiểm tra toàn bộ hệ sinh thái dự án **HomeMatch** (bao gồm Backend ASP.NET Core .NET 10 Modular Monolith, Frontend Next.js 15, Cơ sở dữ liệu PostgreSQL 16 tích hợp PostGIS và Nginx Reverse Proxy) thông qua Docker & Docker Compose.

---

## 1. Yêu Cầu Tiên Quyết (Prerequisites)

- **Hệ điều hành:** Windows 10/11 (hỗ trợ WSL2), macOS hoặc Linux.
- **Docker Desktop:** Phiên bản 24.0+ trở lên (kèm Docker Compose v2+).
  - Đảm bảo **Docker Desktop đang chạy** trước khi thực hiện các lệnh bên dưới.
- **Git:** Đã clone repository HomeMatch về máy cục bộ.

---

## 2. Cấu Trúc Container & Port Mapping

Hệ thống bao gồm **4 container** hoạt động trên cùng một Docker bridge network (`homematch-network`):

| Container Name | Service | Image / Base | Cổng Nội Bộ | Cổng Máy Host (Mặc định) | Vai trò |
|---|---|---|---|---|---|
| `homematch-db` | `db` | `postgis/postgis:16-3.4` | `5432` | `5433` (hoặc `5432`) | PostgreSQL 16 + PostGIS (10 Schemas) |
| `homematch-backend` | `backend` | .NET 10 SDK / ASP.NET Runtime | `5000` | `5000` | Modular Monolith REST API + SignalR Hub |
| `homematch-frontend` | `frontend` | Node.js 22 Alpine (Next.js Standalone) | `3000` | `3000` | Giao diện Next.js 15 App Router |
| `homematch-nginx` | `nginx` | `nginx:alpine` | `80` | `80` | Reverse Proxy (Định tuyến Web, API & WebSocket) |

> [!NOTE]
> Bên trong mạng nội bộ `homematch-network`, các service giao tiếp với nhau bằng tên service và cổng nội bộ (ví dụ: Backend kết nối tới DB qua `Host=db;Port=5432`, Frontend gọi Backend qua `http://backend:5000`). Cổng Host (`POSTGRES_PORT=5433`) chỉ phục vụ khi bạn kết nối trực tiếp từ máy ngoài (DBeaver, pgAdmin, DataGrip).

---

## 3. Cấu Hình Biến Môi Trường (.env)

Tạo file `.env` tại thư mục gốc của repository từ file mẫu `.env.example`:

```bash
# Trên Windows PowerShell:
Copy-Item .env.example .env

# Trên Linux / macOS / Git Bash:
cp .env.example .env
```

Nội dung cấu hình chuẩn trong `.env` (đã tối ưu sẵn để chạy ngay trên máy local):

```env
# ==========================================
# Database Configuration (PostgreSQL + PostGIS)
# ==========================================
POSTGRES_USER=homematch_user
POSTGRES_PASSWORD=homematch_password
POSTGRES_DB=homematch_db
# Mặc định dùng 5433 trên máy host để tránh đụng độ với PostgreSQL cài sẵn ở cổng 5432
POSTGRES_PORT=5433
POSTGRES_HOST=db

# Backend Connection String (Dành cho container backend)
ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=homematch_db;Username=homematch_user;Password=homematch_password;

# ==========================================
# Backend API Configuration (ASP.NET Core .NET 10)
# ==========================================
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5000
API_PORT=5000

# ==========================================
# Frontend Configuration (Next.js 15)
# ==========================================
FRONTEND_PORT=3000
NEXT_PUBLIC_API_URL=
API_INTERNAL_URL=http://backend:5000
```

---

## 4. Khởi Chạy Hệ Thống & Cơ Chế Tự Động Khởi Tạo

### 4.1. Khởi động toàn bộ dịch vụ (Khuyên dùng)

Tại thư mục gốc dự án (nơi có file `compose.yaml`):

```bash
docker compose up -d --build
```

- Flag `-d`: Chạy ngầm dưới dạng background daemon.
- Flag `--build`: Tự động biên dịch lại mã nguồn mới nhất trong `backend/` và `frontend/`.

**Quy trình tự động diễn ra khi khởi động:**
1. **`homematch-db`:** Tự động nạp script `infrastructure/database/init/01-init-schemas.sql` để kích hoạt `uuid-ossp`, `postgis` và tạo sẵn **10 schemas** phân hệ (`identity`, `properties`, `listings`, `viewings`, `messaging`, `leasing`, `billing`, `notifications`, `admin`, `monetization`).
2. **`homematch-backend`:** Chờ `db` báo `healthy`, sau đó trong `Program.cs` tự động thực thi:
   - `MigrateAsync()` cho các DbContext (`IdentityDbContext`, `ViewingsDbContext`, `MessagingDbContext`, `NotificationsDbContext`).
   - `IdentityDataSeeder`: Tự động khởi tạo 3 vai trò (`ADMIN`, `LANDLORD`, `TENANT`) và 3 tài khoản mẫu sẵn sàng để đăng nhập kiểm thử.
3. **`homematch-frontend` & `homematch-nginx`:** Khởi chạy giao diện Next.js Standalone và cổng Reverse Proxy.

### 4.2. Danh sách Tài Khoản Mẫu (Tự động Seed khi chạy Docker)

Ngay sau khi container `homematch-backend` khởi chạy thành công, bạn có thể dùng ngay các tài khoản sau để đăng nhập trên giao diện web:

| Vai trò | Email đăng nhập | Mật khẩu | Họ tên hiển thị |
|---|---|---|---|
| **Người thuê (Tenant)** | `tenant@homematch.vn` | `Password123@` | Trần Bình An (Người Thuê) |
| **Chủ nhà (Landlord)** | `landlord@homematch.vn` | `Password123@` | Nguyễn Văn Hoàng (Chủ Trọ) |
| **Quản trị viên (Admin)** | `admin@homematch.vn` | `Password123@` | Quản Trị Viên Hệ Thống |

### 4.3. Khởi động riêng Database (Dành cho Dev chạy code ngoài máy Host)

Nếu bạn muốn chạy lệnh `dotnet run` và `npm run dev` trực tiếp trên máy để debug nhanh mà chỉ dùng Database trong Docker:

```bash
docker compose up -d db
```
*(Lúc này chuỗi kết nối trong `backend/src/HomeMatch.Api/appsettings.Development.json` trỏ về `Host=localhost;Port=5433`).*

---

## 5. Kiểm Tra Hoạt Động & Kết Nối

### 5.1. Kiểm tra trạng thái containers

```bash
docker compose ps
```

Kết quả mong đợi: Cả 4 container (`homematch-db`, `homematch-backend`, `homematch-frontend`, `homematch-nginx`) đều ở trạng thái `Up` và `homematch-db` có trạng thái `(healthy)`.

### 5.2. Kiểm tra Logs

```bash
# Xem logs toàn bộ hệ thống
docker compose logs -f

# Xem riêng logs của từng service
docker logs -f homematch-db
docker logs -f homematch-backend
docker logs -f homematch-frontend
docker logs -f homematch-nginx
```

### 5.3. Kiểm tra Cơ sở dữ liệu & Schemas

```bash
# Liệt kê các schema trong database
docker exec homematch-db psql -U homematch_user -d homematch_db -c "\dn"

# Kiểm tra các bảng đã được EF Core Migration tạo tự động
docker exec homematch-db psql -U homematch_user -d homematch_db -c "\dt *.*"
```

### 5.4. Kiểm tra các Đường Dẫn (Endpoints) Web & API

Mở trình duyệt và truy cập:

- **Giao diện Web (Trực tiếp cổng 3000):** [http://localhost:3000](http://localhost:3000)
- **Giao diện Web (Qua Nginx cổng 80):** [http://localhost](http://localhost)
- **Swagger UI (Tài liệu & Test API trực quan):** [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **OpenAPI v1 JSON Spec:** [http://localhost:5000/openapi/v1.json](http://localhost:5000/openapi/v1.json)
- **Backend Health Check:** [http://localhost:5000/health](http://localhost:5000/health)
- **Backend System Status:** [http://localhost:5000/api/v1/system/status](http://localhost:5000/api/v1/system/status)

---

## 6. Dừng & Dọn Dẹp Môi Trường

### 6.1. Dừng hệ thống (Giữ lại dữ liệu database)

```bash
docker compose down
```

Lệnh này dừng và gỡ bỏ containers nhưng **vẫn giữ lại volume dữ liệu** `postgres_data`. Khi bật lại bằng `docker compose up -d`, mọi tài khoản, lịch hẹn và tin nhắn vẫn còn nguyên.

### 6.2. Dừng hệ thống và xóa sạch dữ liệu (Reset Database về ban đầu)

Khi bạn muốn xóa toàn bộ dữ liệu cũ để hệ thống tạo lại bảng và seed lại từ đầu:

```bash
docker compose down -v
docker compose up -d --build
```

---

## 7. Xử Lý Các Sự Cố Thường Gặp (Troubleshooting)

### Sự cố 1: `failed to connect to the docker API at npipe... The system cannot find the file specified`

- **Nguyên nhân:** Ứng dụng Docker Desktop trên máy chưa được bật.
- **Cách khắc phục:** Mở **Docker Desktop** và đợi biểu tượng góc dưới chuyển sang trạng thái *Engine running*, sau đó chạy lại lệnh.

### Sự cố 2: `Bind for 127.0.0.1:5432 failed: port is already allocated` (hoặc cổng `80`, `3000`, `5000`)
- **Nguyên nhân:** Cổng tương ứng trên máy host đang bị một tiến trình khác chiếm dụng (ví dụ bạn đang bật sẵn `dotnet run` ở cổng `5000` hoặc `npm run dev` ở cổng `3000` bên ngoài).
- **Cách khắc phục:**
  - Tắt các terminal đang chạy `dotnet run` / `npm run dev` ở ngoài máy host trước khi chạy `docker compose up -d`, hoặc đổi cổng trong file `.env` (`POSTGRES_PORT=5433`, `NGINX_PORT=8080`).

### Sự cố 3: Vừa kéo code mới từ Git về nhưng giao diện/API trong Docker chưa thay đổi
- **Nguyên nhân:** Docker đang chạy image cũ đã build từ trước.
- **Cách khắc phục:** Chạy lệnh build lại kèm khởi tạo lại container:
  ```bash
  docker compose up -d --build --force-recreate
  ```
