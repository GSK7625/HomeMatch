# Hướng Dẫn Chạy Dự Án HomeMatch Bằng Docker

Tài liệu này hướng dẫn chi tiết cách thiết lập, khởi chạy và kiểm tra toàn bộ hệ sinh thái dự án **HomeMatch** (bao gồm Backend ASP.NET Core .NET 10 Modular Monolith, Frontend Next.js 15 và Cơ sở dữ liệu PostgreSQL 16 tích hợp PostGIS) thông qua Docker & Docker Compose.

---

## 1. Yêu Cầu Tiên Quyết (Prerequisites)

- **Hệ điều hành:** Windows 10/11 (hỗ trợ WSL2), macOS hoặc Linux.
- **Docker Desktop:** Phiên bản 24.0+ trở lên (kèm Docker Compose v2+).
  - Đảm bảo **Docker Desktop đang chạy** trước khi thực hiện các lệnh bên dưới.
- **Git:** Đã clone repository HomeMatch về máy cục bộ.

---

## 2. Cấu Trúc Container & Port Mapping

Hệ thống bao gồm 3 container hoạt động trên cùng một Docker bridge network (`homematch-network`):

| Container Name | Service | Image / Base | Cổng Container | Cổng Host (Mặc định) |
|---|---|---|---|---|
| `homematch-db` | `db` | `postgis/postgis:16-3.4` | `5432` | `5433` (hoặc `5432`) |
| `homematch-backend` | `backend` | .NET 10 SDK / ASP.NET Runtime | `5000` | `5000` |
| `homematch-frontend` | `frontend` | Node.js 22 Alpine (Next.js Standalone) | `3000` | `3000` |

> [!NOTE]
> Bên trong Docker network (`homematch-network`), các service giao tiếp với nhau bằng tên container và cổng nội bộ (ví dụ: Backend kết nối tới DB qua `Host=db;Port=5432`). Cổng Host (`POSTGRES_PORT`) chỉ phục vụ kết nối trực tiếp từ máy của bạn (DBeaver, pgAdmin, CLI bên ngoài container).

---

## 3. Cấu Hình Biến Môi Trường (.env)

Tạo file `.env` tại thư mục gốc của repository từ file mẫu `.env.example`:

```bash
# Trên Windows PowerShell:
Copy-Item .env.example .env

# Trên Linux / macOS / Git Bash:
cp .env.example .env
```

Nội dung cấu hình quan trọng trong `.env`:

```env
# ==========================================
# Database Configuration (PostgreSQL + PostGIS)
# ==========================================
POSTGRES_USER=homematch_user
POSTGRES_PASSWORD=homematch_password
POSTGRES_DB=homematch_db
# Dùng 5433 nếu cổng 5432 trên máy host đã bị chiếm bởi dịch vụ khác
POSTGRES_PORT=5433
POSTGRES_HOST=db

# Backend Connection String (Dành cho container backend)
ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=homematch_db;Username=homematch_user;Password=homematch_password;

# ==========================================
# Backend API Configuration
# ==========================================
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5000
API_PORT=5000

# ==========================================
# Frontend Configuration
# ==========================================
FRONTEND_PORT=3000
NEXT_PUBLIC_API_URL=http://localhost:5000
API_INTERNAL_URL=http://backend:5000
```

---

## 4. Khởi Chạy Hệ Thống

### 4.1. Khởi động toàn bộ dịch vụ (Khuyên dùng)

Tại thư mục gốc dự án:

```bash
docker compose up -d --build
```

- Flag `-d`: Chạy ngầm dưới dạng background daemon.
- Flag `--build`: Tự động biên dịch lại mã nguồn khi có thay đổi trong `backend/` hoặc `frontend/`.

### 4.2. Khởi động riêng lẻ từng dịch vụ (Tùy chọn)

Nếu bạn chỉ muốn chạy Database để phát triển Backend trực tiếp trên máy host:

```bash
docker compose up -d db
```

---

## 5. Kiểm Tra Hoạt Động & Kết Nối

### 5.1. Kiểm tra trạng thái containers

```bash
docker compose ps
```

Kết quả mong đợi: Cả 3 container đều có trạng thái `Up` và `homematch-db` ở trạng thái `(healthy)`.

### 5.2. Kiểm tra Logs

```bash
# Xem logs toàn bộ hệ thống
docker compose logs -f

# Xem riêng logs của từng service
docker logs -f homematch-db
docker logs -f homematch-backend
docker logs -f homematch-frontend
```

### 5.3. Kiểm tra Cơ sở dữ liệu & PostGIS

Xác nhận PostgreSQL đã tự động chạy script khởi tạo 8 schema nghiệp vụ và kích hoạt PostGIS extension:

```bash
# Liệt kê các schema trong database
docker exec homematch-db psql -U homematch_user -d homematch_db -c "\dn"
```

*Kết quả mong đợi:* Danh sách schema gồm `billing`, `identity`, `leasing`, `listings`, `messaging`, `notifications`, `properties`, `viewings`, `public`.

```bash
# Kiểm tra phiên bản PostGIS
docker exec homematch-db psql -U homematch_user -d homematch_db -c "SELECT PostGIS_Version();"
```

*Kết quả mong đợi:* Trả về phiên bản `3.4 USE_GEOS=1 USE_PROJ=1 USE_STATS=1`.

### 5.4. Kiểm tra kết nối mạng Backend → Database

Kiểm tra kết nối TCP từ container Backend tới cổng 5432 của container Database:

```bash
docker exec homematch-backend bash -c "</dev/tcp/db/5432 && echo 'Backend to DB: CONNECTED' || echo 'Failed'"
```

*Kết quả mong đợi:* `Backend to DB: CONNECTED`.

### 5.5. Kiểm tra Endpoints Web & API

Mở trình duyệt hoặc dùng công cụ gọi API:

- **Frontend Home Page:** [http://localhost:3000](http://localhost:3000)
- **Backend Health Check:** [http://localhost:5000/health](http://localhost:5000/health)
- **Backend System Status:** [http://localhost:5000/api/v1/system/status](http://localhost:5000/api/v1/system/status)
- **Scalar API Documentation:** [http://localhost:5000/scalar/v1](http://localhost:5000/scalar/v1)

---

## 6. Dừng & Dọn Dẹp Môi Trường

### 6.1. Dừng hệ thống (Giữ lại dữ liệu database)

```bash
docker compose down
```

Lệnh này dừng và gỡ bỏ containers, mạng nội bộ nhưng **vẫn giữ lại volume dữ liệu** `postgres_data`. Khi chạy lại bằng `docker compose up -d`, toàn bộ dữ liệu database vẫn nguyên vẹn.

### 6.2. Dừng hệ thống và xóa sạch dữ liệu (Reset sạch)

Khi bạn muốn xóa toàn bộ dữ liệu và để database chạy lại các script init từ đầu:

```bash
docker compose down -v
```

---

## 7. Xử Lý Các Sự Cố Thường Gặp (Troubleshooting)

### Sự cố 1: `failed to connect to the docker API at npipe... The system cannot find the file specified`
- **Nguyên nhân:** Dịch vụ Docker Desktop trên máy chưa được khởi động hoặc bị treo.
- **Cách khắc phục:** Mở ứng dụng Docker Desktop trên Windows và đợi biểu tượng cá voi chuyển sang màu xanh (Engine running).

### Sự cố 2: `Bind for 0.0.0.0:5432 failed: port is already allocated`
- **Nguyên nhân:** Cổng `5432` trên máy của bạn đang bị chiếm bởi một PostgreSQL service chạy ngầm (hoặc container khác như `weblibrary_db`).
- **Cách khắc phục:** Đổi biến `POSTGRES_PORT=5433` trong file `.env`. Backend trong Docker network vẫn kết nối thông suốt với DB qua port `5432` nội bộ.

### Sự cố 3: Thay đổi code nhưng container không cập nhật
- **Nguyên nhân:** Docker đang dùng cache build cũ.
- **Cách khắc phục:** Chạy build lại không dùng cache:
  ```bash
  docker compose build --no-cache
  docker compose up -d
  ```
