# Hướng Dẫn Chạy HomeMatch Bằng Docker

Tài liệu tóm tắt các bước khởi chạy toàn bộ hệ sinh thái **HomeMatch** (Frontend Next.js 15, Backend .NET 10, PostgreSQL 16 + PostGIS và Nginx) bằng Docker Compose.

---

## 1. Cấu Trúc Dịch Vụ & Cổng Truy Cập

| Service | Container Name | Cổng Host | Vai Trò |
|---|---|---|---|
| `nginx` | `homematch-nginx` | **80** | Reverse Proxy (Cổng vào chính cho toàn hệ thống) |
| `frontend` | `homematch-frontend` | **3000** | Next.js 15 App Router |
| `backend` | `homematch-backend` | **5000** | ASP.NET Core .NET 10 Modular Monolith |
| `db` | `homematch-db` | **5432** | PostgreSQL 16 tích hợp PostGIS & 10 Schemas |

---

## 2. Các Bước Khởi Chạy Nhanh

### Bước 1: Chuẩn bị biến môi trường
Tạo file `.env` tại thư mục gốc từ file mẫu:
```bash
# Windows PowerShell:
Copy-Item .env.example .env

# Linux / macOS:
cp .env.example .env
```
*(Nếu cổng 5432 trên máy bạn đã bị chiếm, chỉnh `POSTGRES_PORT=5433` trong file `.env`)*.

### Bước 2: Khởi động toàn bộ hệ thống
```bash
docker compose up -d --build
```
> [!NOTE]
> Database sẽ tự động khởi tạo 10 Schemas và nạp toàn bộ 44 bảng cùng dữ liệu mẫu (seed data) trong lần chạy đầu tiên từ thư mục `infrastructure/database/init/`.

---

## 3. Đường Dẫn Kiểm Tra & Sử Dụng

- **Ứng dụng qua Nginx:** [http://localhost](http://localhost) (Khuyên dùng)
- **Frontend trực tiếp:** [http://localhost:3000](http://localhost:3000)
- **Tài liệu API Scalar:** [http://localhost:5000/scalar/v1](http://localhost:5000/scalar/v1)
- **Kiểm tra Health Check:** [http://localhost:5000/health](http://localhost:5000/health)

---

## 4. Các Lệnh Vận Hành Thường Dùng

```bash
# Xem trạng thái các container
docker compose ps

# Xem logs thời gian thực
docker compose logs -f

# Dừng hệ thống (Giữ nguyên dữ liệu DB)
docker compose down

# Dừng và XÓA SẠCH dữ liệu DB để khởi tạo lại từ đầu
docker compose down -v

# Build lại khi có cập nhật code không dùng cache
docker compose build --no-cache && docker compose up -d
```
