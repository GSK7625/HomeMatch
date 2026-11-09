# HomeMatch - Nền Tảng Tìm Kiếm & Quản Lý Nhà Trọ Thông Minh

Dự án áp dụng mô hình **Monorepo** kết hợp kiến trúc **Modular Monolith** trên nền tảng **Next.js (Frontend)**, **ASP.NET Core .NET 10 (Backend)** và **PostgreSQL 16 + PostGIS (Database)**.

---

## 1. Cấu Trúc Thư Mục Tổng Quan

```
HomeMatch/
│
├── frontend/                       # Next.js 15 (App Router) + TypeScript + Tailwind CSS
│   ├── public/                     # Static assets
│   ├── src/
│   │   ├── app/                    # App Router: Pages, routes, layouts, globals.css
│   │   ├── components/             # UI components dùng chung (Button, Card, Badge...)
│   │   ├── features/               # 9 chức năng nghiệp vụ độc lập:
│   │   │   ├── auth/               # Đăng nhập, đăng ký, phân quyền
│   │   │   ├── properties/         # Quản lý nhà trọ, tòa nhà, phòng
│   │   │   ├── listings/           # Tin đăng, tìm kiếm tối ưu
│   │   │   ├── favorites/          # Danh sách phòng đã lưu
│   │   │   ├── viewings/           # Lịch hẹn xem phòng
│   │   │   ├── messaging/          # Nhắn tin trực tiếp
│   │   │   ├── leasing/            # Hợp đồng thuê & khách thuê
│   │   │   ├── billing/            # Hóa đơn & công nợ
│   │   │   └── notifications/      # Trung tâm thông báo
│   │   ├── hooks/                  # Custom React hooks
│   │   ├── lib/                    # API client, utility functions
│   │   ├── types/                  # TypeScript interfaces & types
│   │   └── styles/                 # Custom styles
│   ├── Dockerfile                  # Multi-stage Next.js standalone container
│   ├── package.json
│   ├── next.config.ts
│   └── tsconfig.json
│
├── backend/                        # ASP.NET Core (.NET 10) Modular Monolith
│   ├── src/
│   │   ├── HomeMatch.Api/          # Entry point host, Swagger, CORS, Middleware
│   │   │   ├── Program.cs
│   │   │   ├── Middleware/         # ExceptionHandlingMiddleware
│   │   │   ├── appsettings.json
│   │   │   └── HomeMatch.Api.csproj
│   │   │
│   │   ├── HomeMatch.Shared/       # Abstractions, Contracts, Common Results dùng chung
│   │   │   ├── Abstractions/       # IDomainEvent, IEntity
│   │   │   ├── Contracts/          # DTOs / Events giao tiếp liên module
│   │   │   ├── Common/             # Result<T>, Error, ApiResponse<T>
│   │   │   └── HomeMatch.Shared.csproj
│   │   │
│   │   └── Modules/                # 8 module nghiệp vụ (Class Library riêng biệt)
│   │       ├── Identity/           # Tài khoản, xác thực JWT, phân quyền
│   │       ├── Properties/         # Bất động sản, phòng trọ, tiện ích
│   │       ├── Listings/           # Tin đăng cho thuê, tìm kiếm PostGIS
│   │       ├── Viewings/           # Lịch hẹn xem phòng
│   │       ├── Messaging/          # Chat realtime (SignalR)
│   │       ├── Leasing/            # Hợp đồng, điều khoản, người thuê
│   │       ├── Billing/            # Hóa đơn định kỳ, tiền điện nước
│   │       └── Notifications/      # Gửi email, push notification
│   │
│   ├── tests/
│   │   ├── UnitTests/              # xUnit Unit Tests
│   │   └── IntegrationTests/       # xUnit Integration Tests
│   │
│   ├── Dockerfile                  # Multi-stage .NET 10 SDK & Runtime container
│   └── HomeMatch.sln               # Solution liên kết toàn bộ 11 projects
│
├── infrastructure/                 # Cấu hình hạ tầng & triển khai
│   ├── nginx/
│   │   └── nginx.conf              # Reverse proxy cho Frontend & Backend
│   ├── database/
│   │   └── init/
│   │       └── 01-init-schemas.sql # Kích hoạt PostGIS và khởi tạo 8 schema
│   └── scripts/
│       ├── deploy.sh               # Kịch bản triển khai tự động
│       └── backup.sh               # Kịch bản sao lưu định kỳ database
│
├── docs/                           # Tài liệu kỹ thuật
│   ├── architecture/
│   │   └── modular-monolith.md     # Quy ước kiến trúc module & giao tiếp
│   ├── database/
│   │   └── schema-design.md        # Thiết kế 1 DB đa schema & PostGIS
│   └── api/
│       └── api-guidelines.md       # Chuẩn RESTful, ApiResponse, Status code
│
├── .github/
│   └── workflows/
│       ├── ci.yml                  # GitHub Actions CI: Test & Build toàn bộ Monorepo
│       └── cd.yml                  # GitHub Actions CD: Build & deploy containers
│
├── compose.yaml                    # Docker Compose môi trường hoàn chỉnh
├── .env.example                    # Mẫu cấu hình môi trường chuẩn
├── .gitignore                      # Loại trừ build artifacts, node_modules, env...
└── README.md
```

---

## 2. Cấu Trúc Bên Trong Mỗi Backend Module

Mỗi module nghiệp vụ (ví dụ `Listings`) tuân thủ cấu trúc phân tầng Clean Architecture:

```
Listings/
├── Domain/                         # Thực thể cốt lõi, business rules
│   ├── Entities/
│   ├── Enums/
│   └── Events/
├── Application/                    # Use cases, Commands, Queries, DTOs
│   ├── Commands/
│   ├── Queries/
│   ├── DTOs/
│   └── Interfaces/
├── Infrastructure/                 # EF Core DbContext riêng schema listings
│   ├── Persistence/
│   │   └── Migrations/
│   ├── Repositories/
│   └── Services/
├── Presentation/                   # REST API Minimal Endpoints
│   └── Endpoints/
├── ListingsModuleExtensions.cs     # Đăng ký DI & Map endpoints vào HomeMatch.Api
└── HomeMatch.Modules.Listings.csproj
```

---

## 3. Hướng Dẫn Khởi Chạy

### Cách 1: Khởi chạy toàn bộ bằng Docker Compose (Khuyến nghị)

Chỉ cần một lệnh duy nhất để chạy toàn bộ: **PostgreSQL 16 + PostGIS**, **ASP.NET Core API**, và **Next.js Frontend**:

```bash
# 1. Tạo file .env từ mẫu
cp .env.example .env

# 2. Khởi chạy toàn bộ hệ thống
docker compose up -d --build
```

- **Frontend:** http://localhost:3000
- **Backend API & Scalar API Docs:** http://localhost:5000/scalar/v1
- **OpenAPI Document:** http://localhost:5000/openapi/v1.json
- **Health Check:** http://localhost:5000/health
- **PostgreSQL Database:** `localhost:5432` hoặc `localhost:5433` (User: `homematch_user`, DB: `homematch_db`)

> 📖 **Xem hướng dẫn chi tiết:** [Hướng dẫn chạy dự án bằng Docker](docs/guides/docker-setup.md) (bao gồm cấu hình port, kiểm tra kết nối PostGIS, và xử lý sự cố thường gặp).

---

### Cách 2: Khởi chạy Local phục vụ Development

#### Bước 1: Khởi động Database PostGIS
```bash
docker compose up -d db
```

#### Bước 2: Chạy Backend ASP.NET Core
```bash
dotnet run --project backend/src/HomeMatch.Api
```
API sẽ lắng nghe tại `http://localhost:5000`.

#### Bước 3: Chạy Frontend Next.js
```bash
cd frontend
npm install
npm run dev
```
Giao diện Next.js sẽ mở tại `http://localhost:3000`.

---

## 4. Kiểm Thử (Testing)

Chạy bộ kiểm thử tự động cho toàn bộ các module backend:

```bash
dotnet test backend/HomeMatch.sln
```

Kiểm tra định dạng và build frontend:

```bash
cd frontend && npm run build
```

---

## 5. Quy Trình Phối Hợp Nhóm & CI/CD (GitHub Actions Git Flow)

Dự án áp dụng mô hình phân nhánh với 2 nhánh trọng yếu: **`dev`** (Môi trường tích hợp/Staging) và **`main`** (Môi trường Production) được kiểm soát chất lượng tự động 100% bằng **GitHub Actions**:

1. **Nhánh `main` (Production):** 
   - Nhánh chính của sản phẩm, luôn ở trạng thái ổn định và được bảo vệ nghiêm ngặt (Protected Branch).
   - Khi có commit hoặc Pull Request được merge vào `main`, workflow CD tự động kích hoạt để đóng gói và đẩy Production Docker Images lên **GitHub Container Registry (`ghcr.io`)**.
2. **Nhánh `dev` (Development / Integration):** 
   - Nhánh tích hợp tập trung, nơi tập hợp các tính năng mới trước khi đưa lên Production.
   - Nhánh `dev` được cấu hình kiểm thử tự động liên tục qua GitHub Actions mỗi khi có commit hoặc Pull Request mới.
3. **Quy trình phát triển tính năng mới:**
   - Tạo nhánh tính năng từ `dev` theo cú pháp: `feature/<module-name>-<task-name>` (ví dụ: `feature/listings-search-filter`, `feature/identity-jwt-login`).
   - Khi hoàn thành, tạo Pull Request (PR) trỏ vào nhánh **`dev`** (thay vì trực tiếp vào `main`).
4. **Cơ chế GitHub Actions CI/CD Pipeline:**
   - **Continuous Integration (`.github/workflows/ci.yml`):** Chạy đồng thời 3 jobs độc lập trên mọi nhánh (`dev`, `main`, `feature/*`):
     - `backend-ci`: Restore, build và chạy toàn bộ unit & integration tests cho Backend (.NET 10).
     - `frontend-ci`: Cài đặt dependencies, type-check và build tối ưu hóa ứng dụng Frontend Next.js 15 (Node.js 22).
     - `docker-compose-validate`: Kiểm tra tính hợp lệ của cấu hình [compose.yaml](compose.yaml).
   - **Continuous Delivery (`.github/workflows/cd.yml`):** Khi merge vào `main`, tự động build và push 2 Docker Images (`backend` & `frontend`) lên GitHub Container Registry (`ghcr.io`).
5. **Quy tắc Merge & Branch Protection:**
   - Cả 2 nhánh `dev` và `main` đều được bật **Branch Protection Rules**: Bắt buộc toàn bộ các jobs CI phải báo xanh (Passed) và có ít nhất 1 thành viên review chấp thuận thì nút Merge trên GitHub mới được mở.

> 📖 **Xem hướng dẫn chi tiết:** [Hướng dẫn thiết lập CI/CD Với GitHub Actions & GHCR](docs/guides/github-actions-setup.md).
