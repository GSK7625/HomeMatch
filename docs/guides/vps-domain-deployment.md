# Hướng Dẫn Triển Khai HomeMatch Lên VPS & Gắn Domain (Production Deployment)

Tài liệu này hướng dẫn chi tiết từng bước đưa hệ sinh thái **HomeMatch** (Next.js 15 Frontend, ASP.NET Core .NET 10 Modular Monolith, PostgreSQL 16 + PostGIS và Nginx Reverse Proxy) lên máy chủ thực tế (**VPS Linux**) và gắn tên miền riêng có bảo mật **SSL (HTTPS)**.

---

## 1. Hiện Trạng Cấu Hình Docker & Nginx Trong Dự Án

| Thành phần | Vị trí file trong dự án | Trạng thái hiện tại |
| :--- | :--- | :--- |
| **Backend Docker** | `backend/Dockerfile` | Đã cấu hình multi-stage build (.NET 10 SDK & ASP.NET Runtime) |
| **Frontend Docker** | `frontend/Dockerfile` | Đã cấu hình multi-stage build (Node 22 Alpine, Next.js Standalone) |
| **Docker Compose** | `compose.yaml` | Đã cấu hình 3 services chính: `db`, `backend`, `frontend` trên mạng nội bộ `homematch-network` |
| **Nginx Reverse Proxy** | `infrastructure/nginx/nginx.conf` | Đã cấu hình đầy đủ reverse proxy cho: Frontend (`/`), Backend API (`/api/`), SignalR WebSockets (`/hubs/`), Scalar Docs (`/scalar/`), Healthcheck (`/health`) |
| **Kịch bản Deploy** | `infrastructure/scripts/deploy.sh` | Kịch bản tự động pull code mới, rebuild và khởi động lại container |
| **Kịch bản Sao lưu DB** | `infrastructure/scripts/backup.sh` | Kịch bản tự động backup `homematch_db` nén `.sql.gz` theo ngày |

> [!NOTE]
> Trong môi trường local, `compose.yaml` đang mở trực tiếp các cổng `3000` (Frontend) và `5000` (Backend). Khi lên **VPS Production**, **Nginx** sẽ đóng vai trò là cổng đón duy nhất (cổng `80` và `443` HTTPS), định tuyến toàn bộ traffic vào các container nội bộ.

---

## 2. Chuẩn Bị Trước Khi Deploy

1. **Máy chủ VPS:**
   * Hệ điều hành khuyến nghị: **Ubuntu 22.04 LTS** hoặc **Ubuntu 24.04 LTS**.
   * Cấu hình tối thiểu: **2 vCPU, 4GB RAM, 30GB SSD** (đáp ứng build Docker và chạy PostGIS mượt mà).
2. **Tên miền (Domain):**
   * Đã sở hữu tên miền (ví dụ: `homematch.vn`).
   * Truy cập trang quản lý DNS của nhà cung cấp tên miền (Cloudflare, Namecheap, v.v.) và trỏ 2 bản ghi:
     * **Bản ghi A:** `@` (hoặc `homematch.vn`) trỏ về **Địa chỉ IPv4 của VPS**.
     * **Bản ghi A:** `www` trỏ về **Địa chỉ IPv4 của VPS**.

---

## 3. Các Bước Triển Khai Chi Tiết Trên VPS

### Bước 1: Kết nối VPS và cài đặt Docker Engine

Mở Terminal trên máy tính và SSH vào VPS:

```bash
ssh root@<IP_VPS_CỦA_BẠN>
```

Cập nhật hệ thống và cài đặt Docker + Docker Compose plugin chính thức:

```bash
# Cập nhật repository
sudo apt update && sudo apt upgrade -y

# Cài đặt các gói phụ thuộc
sudo apt install -y ca-certificates curl gnupg lsb-release git ufw

# Thêm GPG key chính thức của Docker
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

# Thêm Docker repository
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Cài đặt Docker Engine & Docker Compose
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Kiểm tra phiên bản
docker --version
docker compose version
```

---

### Bước 2: Thiết lập Tường lửa (Firewall UFW)

Chỉ mở các cổng cần thiết ra ngoài Internet để bảo vệ cơ sở dữ liệu:

```bash
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS
sudo ufw enable
```

> [!IMPORTANT]
> Tuyệt đối **không mở cổng 5432 hoặc 5433** trên tường lửa UFW. Database PostgreSQL sẽ chỉ giao tiếp an toàn bên trong Docker bridge network.

---

### Bước 3: Clone Mã Nguồn Dự Án

```bash
# Di chuyển tới thư mục chứa ứng dụng
cd /var/www

# Clone repository từ GitHub
git clone https://github.com/GSK7625/HomeMatch.git
cd HomeMatch
```

---

### Bước 4: Thiết Lập File Biến Môi Trường (.env) Cho Production

Tạo file `.env` trên VPS:

```bash
cp .env.example .env
nano .env
```

Điền các thông số Production bảo mật (nhấn `Ctrl + O` để lưu, `Ctrl + X` để thoát):

```dotenv
# ==========================================
# 1. Database Configuration
# ==========================================
POSTGRES_USER=homematch_user
POSTGRES_PASSWORD=MatKhauDatabaseThatManh2026!@#
POSTGRES_DB=homematch_db
POSTGRES_PORT=5432
POSTGRES_HOST=db

# Chuỗi kết nối nội bộ giữa backend và database trong Docker network
ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=homematch_db;Username=homematch_user;Password=MatKhauDatabaseThatManh2026!@#;

# ==========================================
# 2. Backend ASP.NET Core
# ==========================================
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
API_PORT=5000

# Khóa bảo mật JWT (Chuỗi Base64 ngẫu nhiên tối thiểu 32 bytes)
Jwt__Secret=qbM1MZVIe7VjQ7RLTVHJMpEFjrOCHCbrMBhFMQfaPXw=
Jwt__Issuer=HomeMatch
Jwt__Audience=HomeMatchApp
Jwt__ExpirationMinutes=15
Jwt__ClockSkewSeconds=30

# Khóa bảo mật mã OTP (Chuỗi Base64 ngẫu nhiên tối thiểu 32 bytes, KHÁC khóa JWT)
Identity__EmailVerification__Enabled=true
Identity__EmailVerification__KeyId=v1
Identity__EmailVerification__Keys__v1=POOsDcQ9k8i2RDt1QDMQCCu7VE5lCBzB4SmP3A2iFf0=

# Cấu hình SMTP gửi OTP qua Gmail
Email__Enabled=true
Email__Smtp__Host=smtp.gmail.com
Email__Smtp__Port=587
Email__Smtp__Security=StartTls
Email__Smtp__Username=bkav104104@gmail.com
Email__Smtp__FromAddress=bkav104104@gmail.com
Email__Smtp__AppPassword=vzbnnkpafezjuwrz
Email__Smtp__FromName=HomeMatch
Email__Smtp__TimeoutSeconds=15

# Cho phép CORS từ Domain thật của bạn
Cors__AllowedOrigins__0=https://homematch.vn
Cors__AllowedOrigins__1=https://www.homematch.vn

# ==========================================
# 3. Frontend Next.js Configuration
# ==========================================
FRONTEND_PORT=3000
NEXT_PUBLIC_API_URL=https://homematch.vn
API_INTERNAL_URL=http://backend:5000
```

---

### Bước 5: Cấu Hình Nginx & Cài Đặt Chứng Chỉ SSL (HTTPS Miễn Phí)

Cách ổn định và phổ biến nhất trên VPS là cài đặt Nginx trực tiếp trên máy chủ để quản lý SSL tự động bằng Certbot:

```bash
# Cài đặt Nginx & Certbot
sudo apt install -y nginx certbot python3-certbot-nginx
```

Tạo file cấu hình Nginx cho tên miền:

```bash
sudo nano /etc/nginx/sites-available/homematch
```

Dán nội dung cấu hình sau (thay `homematch.vn` bằng tên miền thật của bạn):

```nginx
upstream frontend_upstream {
    server 127.0.0.1:3000;
}

upstream backend_upstream {
    server 127.0.0.1:5000;
}

server {
    listen 80;
    server_name homematch.vn www.homematch.vn;

    client_max_body_size 50M;

    # Gzip nén dữ liệu tăng tốc web
    gzip on;
    gzip_types text/plain text/css application/json application/javascript text/xml;

    # 1. Định tuyến Backend API
    location /api/ {
        proxy_pass http://backend_upstream;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # 2. Định tuyến SignalR WebSockets (Chat realtime)
    location /hubs/ {
        proxy_pass http://backend_upstream;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
    }

    # 3. Định tuyến Healthcheck
    location /health {
        proxy_pass http://backend_upstream/health;
        access_log off;
    }

    # 4. Định tuyến toàn bộ Frontend Next.js
    location / {
        proxy_pass http://frontend_upstream;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Kích hoạt file cấu hình và reload Nginx:

```bash
sudo ln -s /etc/nginx/sites-available/homematch /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx
```

Cấp chứng chỉ SSL HTTPS miễn phí tự động bằng Certbot:

```bash
sudo certbot --nginx -d homematch.vn -d www.homematch.vn
```
*(Certbot sẽ tự động cấu hình HTTPS, chuyển hướng HTTP sang HTTPS và tự động gia hạn chứng chỉ trước khi hết hạn).*

---

### Bước 6: Khởi Chạy Dự Án Bằng Docker Compose

Tại thư mục `/var/www/HomeMatch`:

```bash
# Cấp quyền thực thi cho kịch bản triển khai
chmod +x infrastructure/scripts/deploy.sh
chmod +x infrastructure/scripts/backup.sh

# Chạy deploy
docker compose up -d --build
```

Kiểm tra trạng thái các container:

```bash
docker compose ps
```

*Kết quả mong đợi:* Cả 3 container `homematch-db` (healthy), `homematch-backend` (Up), `homematch-frontend` (Up) đều đang hoạt động.

---

### Bước 7: Cài Đặt Sao Lưu Định Kỳ Database (Cron Job)

Mở bảng lập lịch tự động của Linux:

```bash
sudo crontab -e
```

Thêm dòng sau vào cuối tệp để tự động sao lưu CSDL vào lúc **02:00 sáng mỗi ngày**:

```cron
0 2 * * * /var/www/HomeMatch/infrastructure/scripts/backup.sh >> /var/log/homematch_backup.log 2>&1
```

---

## 4. Quy Trình Cập Nhật Code Mới Sau Này (CI/CD Update)

Mỗi khi nhóm có tính năng mới đã merge vào nhánh `main` trên GitHub, bạn chỉ cần SSH vào VPS và chạy một lệnh duy nhất:

```bash
cd /var/www/HomeMatch
./infrastructure/scripts/deploy.sh
```

Kịch bản `deploy.sh` sẽ tự động:
1. Kéo code mới nhất từ GitHub (`git pull origin main`).
2. Build lại image mới mà không làm mất dữ liệu của PostgreSQL.
3. Khởi động lại container với thời gian gián đoạn (downtime) gần như bằng 0.

