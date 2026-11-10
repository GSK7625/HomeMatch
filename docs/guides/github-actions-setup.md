# Hướng Dẫn CI/CD Với GitHub Actions & GitHub Container Registry (GHCR)

Tài liệu này hướng dẫn chi tiết quy trình tự động hóa **Continuous Integration (CI)** và **Continuous Delivery (CD)** cho dự án **HomeMatch** sử dụng **GitHub Actions** kết hợp **GitHub Container Registry (`ghcr.io`)**, thay thế hoàn toàn hệ thống Jenkins cục bộ.

---

## 1. Kiến Trúc CI/CD (Architecture)

```mermaid
flowchart TD
    subgraph GitRepository [GitHub Repository: HomeMatch]
        Feature["feature/* (Nhánh tính năng)"] -->|Pull Request| Dev["dev (Nhánh tích hợp / Staging)"]
        Dev -->|Pull Request| Main["main (Nhánh Production)"]
    end

    subgraph CI [GitHub Actions CI (.github/workflows/ci.yml)]
        TriggerCI["Trigger on push & PR to dev, main"]
        JobBackend["Job: Backend (.NET 10)\nRestore, Build, Unit & Integration Tests"]
        JobFrontend["Job: Frontend (Next.js 15)\nInstall deps, Lint & Type-check, Next Build"]
        JobCompose["Job: Compose Validation\ndocker compose config"]
        TriggerCI --> JobBackend
        TriggerCI --> JobFrontend
        TriggerCI --> JobCompose
    end

    subgraph BranchProtection [GitHub Branch Protection Rules]
        GateCheck{"Tất cả 3 jobs CI Pass?"}
        JobBackend --> GateCheck
        JobFrontend --> GateCheck
        JobCompose --> GateCheck
        GateCheck -->|❌ FAIL| BlockMerge["Khóa nút Merge PR (Chặn code lỗi)"]
        GateCheck -->|✅ PASS| AllowMerge["Cho phép Merge PR"]
    end

    subgraph CD [GitHub Actions CD (.github/workflows/cd.yml)]
        TriggerCD["Trigger on push/merge to main"] --> BuildImages["Build Backend & Frontend Docker Images"]
        BuildImages --> PushGHCR["Push Images lên GitHub Container Registry\nghcr.io/<owner>/homematch/backend:latest\nghcr.io/<owner>/homematch/frontend:latest"]
    end

    subgraph ProductionDeployment [Môi Trường Triển Khai (VPS / Server)]
        PushGHCR --> PullDeploy["docker compose pull && docker compose up -d"]
    end

    Main --> TriggerCD
```

---

## 2. Chi Tiết Các Workflow Đã Thiết Lập

### 2.1. Continuous Integration Workflow (`.github/workflows/ci.yml`)
- **Kích hoạt (Triggers):**
  - Mọi `push` lên các nhánh: `main`, `dev`, `feature/**`.
  - Mọi `pull_request` mở vào: `main`, `dev`.
- **Concurrency:** Tự động hủy (cancel) các lượt build cũ nếu có commit mới được đẩy lên cùng một nhánh/PR để tiết kiệm tài nguyên.
- **Các Jobs thực thi đồng thời (Parallel Jobs):**
  1. **`backend-ci`:**
     - Setup .NET 10 SDK (tự động fallback sang Docker image `mcr.microsoft.com/dotnet/sdk:10.0` nếu runner chưa cập nhật).
     - Biên dịch toàn bộ Solution: `dotnet build backend/HomeMatch.sln --configuration Release`.
     - Chạy bộ kiểm thử tự động: `dotnet test backend/HomeMatch.sln` cho cả `HomeMatch.UnitTests` và `HomeMatch.IntegrationTests`.
  2. **`frontend-ci`:**
     - Setup môi trường Node.js 22 với bộ nhớ đệm (npm cache).
     - Cài đặt dependencies: `npm ci`.
     - Build tối ưu hóa production Next.js 15: `npm run build` (kiểm tra toàn bộ Typescript types và ESLint).
  3. **`docker-compose-validate`:**
     - Kiểm tra cú pháp và cấu trúc file [compose.yaml](file:///c:/Users/dotru/Documents/HomeMatch/compose.yaml) bằng lệnh `docker compose config`.

---

### 2.2. Continuous Delivery Workflow (`.github/workflows/cd.yml`)
- **Kích hoạt (Triggers):**
  - Khi có commit mới hoặc Pull Request được merge vào nhánh **`main`**.
  - Hoặc kích hoạt thủ công qua giao diện GitHub (nút **Run workflow**).
- **Cơ chế đóng gói & lưu trữ Container:**
  - Sử dụng **GitHub Container Registry (GHCR - `ghcr.io`)**:
    - Tích hợp 100% trong hệ sinh thái GitHub.
    - Miễn phí và không giới hạn lưu trữ cho public repository.
    - Không cần cài đặt tài khoản hay secret bên ngoài vì sử dụng trực tiếp token bảo mật tạm thời `secrets.GITHUB_TOKEN`.
  - Đóng gói 2 images:
    - `ghcr.io/<tên-tài-khoản>/homematch/backend:latest`
    - `ghcr.io/<tên-tài-khoản>/homematch/frontend:latest`

---

## 3. So Sánh Các Lựa Chọn Container Registry

Theo yêu cầu môn học/dự án về Container Registry:

| Tiêu chí | GitHub Container Registry (`ghcr.io`) | Docker Hub | Tự dựng Private Registry trên VPS |
| :--- | :--- | :--- | :--- |
| **Chi phí** | **Miễn phí hoàn toàn** (nếu repo Public). | Bị giới hạn chỉ **1 private repo** miễn phí. | Tốn tài nguyên CPU/RAM và dung lượng ổ cứng của VPS. |
| **Tích hợp GitHub Actions** | **Tự động 100%** qua `GITHUB_TOKEN`, không cần cấu hình thêm. | Phải tạo tài khoản, tạo Personal Access Token và gắn vào GitHub Secrets. | Phải cấu hình chứng chỉ SSL, domain và SSH key để push từ runner vào VPS. |
| **Tốc độ & Ổn định** | Hạ tầng mạng CDN toàn cầu của GitHub/Microsoft. | Tốc độ cao nhưng bị giới hạn lượt pull (Rate Limit) với tài khoản miễn phí. | Phụ thuộc vào băng thông và cấu hình của VPS. |
| **Đánh giá** | ⭐️ **Khuyên dùng tối ưu nhất cho dự án.** | Phù hợp nếu đã có tài khoản Pro trả phí. | Phù hợp cho doanh nghiệp muốn tự bảo mật on-premises hoàn toàn. |

---

## 4. Thiết Lập Branch Protection Rules (Bắt Buộc CI Xanh Mới Được Merge)

Để biến CI thành "chốt chặn bắt buộc 100%" (ngăn chặn mọi người merge code lỗi vào nhánh chính):

### Bước 1: Mở cài đặt Branches trên GitHub
1. Mở trang repository GitHub của dự án: `https://github.com/<username>/HomeMatch`.
2. Chọn tab **Settings** -> Mục bên trái chọn **Branches**.
3. Tại phần **Branch protection rules**, nhấn **Add branch protection rule** (hoặc **Add rule**).

### Bước 2: Cấu hình quy tắc cho nhánh `main`
- **Branch name pattern:** Nhập `main`.
- Đánh dấu các mục sau:
  - ✅ **Require a pull request before merging:**
    - Tích chọn *Require approvals* (đặt số lượng: 1).
  - ✅ **Require status checks to pass before merging:**
    - Tích chọn *Require branches to be up to date before merging*.
    - Tại ô tìm kiếm status checks, tìm và tích chọn cả 3 checks:
      - `Backend (.NET 10) Build & Tests`
      - `Frontend (Next.js 15) Build & Lint`
      - `Validate Docker Compose`
  - ✅ **Do not allow bypassing the above settings:** Áp dụng luật này cho toàn bộ thành viên (kể cả Admin / Repo Owner).
- Nhấn **Create** (hoặc **Save changes**).

### Bước 3: Cấu hình tương tự cho nhánh `dev`
- Lặp lại Bước 2 với **Branch name pattern** là `dev`.

> 🎯 **Kết quả:** Bất cứ ai tạo Pull Request vào `dev` hoặc `main`, nếu 1 trong các bài test hoặc build Next.js bị lỗi, nút **Merge pull request** sẽ lập tức bị khóa màu xám.

---

## 5. Hướng Dẫn Kéo & Triển Khai Container Trên VPS Production

Sau khi workflow CD hoàn tất việc build và push images lên `ghcr.io`, bạn có thể triển khai lên VPS cực kỳ đơn giản:

### Bước 1: Đăng nhập vào GHCR trên VPS (Một lần duy nhất)
Tạo một GitHub Personal Access Token (PAT) có quyền `read:packages` và chạy lệnh trên VPS:
```bash
echo "<YOUR_GITHUB_PAT>" | docker login ghcr.io -u <YOUR_GITHUB_USERNAME> --password-stdin
```

### Bước 2: Kéo và khởi động phiên bản mới nhất
Tại thư mục chứa `compose.yaml` trên VPS:
```bash
# Kéo phiên bản mới nhất từ Registry
docker compose pull

# Khởi động lại dịch vụ không làm gián đoạn
docker compose up -d --remove-orphans
```
