# Cấu hình SSL cho Cloudflare Full (Strict)

Thư mục này chứa chứng chỉ SSL của máy chủ gốc (Origin Server) được gắn vào container Nginx tại `/etc/nginx/ssl`.

## Các bước thiết lập trên VPS Linux Ubuntu

1. Truy cập **Cloudflare Dashboard** -> **SSL/TLS** -> **Overview** -> chọn chế độ **Full (strict)**.
2. Vào **SSL/TLS** -> **Origin Server** -> **Create Certificate** để tạo **Origin Certificate**.
3. Lưu 2 tệp vào thư mục này trên VPS (các tệp `.pem` và `.key` đã được `.gitignore` loại trừ khỏi Git):
   - `infrastructure/nginx/ssl/origin.pem` (Origin Certificate)
   - `infrastructure/nginx/ssl/origin.key` (Private Key)
4. Thiết lập quyền đọc an toàn trên Linux:
   ```bash
   chmod 644 infrastructure/nginx/ssl/origin.pem
   chmod 600 infrastructure/nginx/ssl/origin.key
   ```
5. Trong file `.env` trên VPS, chuyển cấu hình Nginx sang chế độ SSL:
   ```env
   NGINX_CONF=nginx.ssl.conf
   NGINX_PORT=80
   NGINX_SSL_PORT=443
   ```
6. Khởi động lại container Nginx:
   ```bash
   docker compose up -d --force-recreate nginx
   ```
