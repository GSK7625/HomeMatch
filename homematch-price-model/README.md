# HomeMatch - Rental Price Estimation Model (Mô hình Ước lượng Giá Chào Thuê)

Dự án Machine Learning tích hợp vào nền tảng **HomeMatch** nhằm ước lượng khoảng giá chào thuê phòng (`P10 - P50 - P90`) từ vị trí địa lý, diện tích và 16 đặc điểm tiện ích của phòng trọ / căn hộ cho thuê tại Hà Nội và TP. Hồ Chí Minh.

---

## 1. Cấu trúc thư mục dự án

```
homematch-price-model/
├── config.yaml               # Cấu hình nguồn cào, ngưỡng lọc, bounding box
├── requirements.txt          # Thư viện phụ thuộc (LightGBM, FastAPI, Scikit-learn,...)
├── data/
│   ├── raw/                  # File HTML thô đã tải (được cache offline)
│   ├── interim/              # Dữ liệu trung gian (raw_listings.jsonl, geocode_cache.json)
│   ├── processed/            # Dữ liệu sạch (clean_listings.csv, master_clean_listings.csv)
│   └── reports/              # Biểu đồ EDA và báo cáo thống kê thị trường
├── scraper/
│   ├── base.py               # BaseScraper xử lý rate limit, retry, checkpoint
│   ├── phongtro123.py        # Adapter cào nguồn phongtro123.com
│   ├── thuephongtro.py       # Adapter cào nguồn thuephongtro.com
│   └── bulk_crawler.py       # Crawler hàng loạt đa nguồn (đạt 15.000+ tin)
├── pipeline/
│   ├── clean.py              # Làm sạch giá, diện tích, ánh xạ DB, trích xuất am_*
│   ├── geocode.py            # Geocoding nhanh offline & bounding box
│   ├── features.py           # Feature engineering, khoảng cách, Spatial KNN BallTree
│   ├── eda.py                # Phân tích khám phá dữ liệu & tạo biểu đồ tự động
│   └── harmonize.py          # Chuẩn hóa & tích hợp đa nguồn (đạt 26.041 tin sạch)
├── models/
│   ├── train.py              # Huấn luyện mô hình Quantile Regression (GroupKFold chống rò rỉ)
│   ├── predict.py            # Inference engine dự đoán khoảng giá P10 - P50 - P90 (< 10ms)
│   └── artifacts/            # Lưu trữ model (.pkl) và metadata chuẩn production
├── service/                  # FastAPI Microservice (Triển khai REST API & Docker)
│   ├── app/
│   │   ├── main.py           # FastAPI endpoints (/health, /api/v1/estimate-price)
│   │   └── schemas.py        # Pydantic schemas chuẩn V2
│   ├── Dockerfile            # Container hóa microservice
│   └── docker-compose.yml
└── tests/
    ├── test_clean.py         # Kiểm thử bộ tiền xử lý và bóc tách dữ liệu
    ├── test_predict.py       # Kiểm thử mô hình suy luận P10 - P50 - P90
    └── test_api.py           # Kiểm thử các endpoint của FastAPI Microservice
```

---

## 2. Hướng dẫn sử dụng & Quy trình vận hành

### 2.1 Cào dữ liệu & Chuẩn hóa Đa nguồn (Tuần 1)
```bash
# Cào dữ liệu hàng loạt đạt 15.000 tin:
python -m scraper.bulk_crawler --target 15000

# Tiền xử lý dữ liệu thô:
python -m pipeline.run_clean_and_export

# Chuẩn hóa và gộp với dữ liệu đối tác thành siêu tập dữ liệu 26.041 tin:
python -m pipeline.harmonize
```

### 2.2 Phân tích khám phá dữ liệu (EDA)
```bash
python -m pipeline.eda
# Kết quả xuất ra tại data/reports/ (biểu đồ phân bổ giá, bản đồ tiện ích, tương quan khoảng cách)
```

### 2.3 Huấn luyện mô hình Quantile Regression (Tuần 2)
Mô hình sử dụng **LightGBM Quantile Regressors** ($\alpha=0.08, 0.50, 0.92$) với cơ chế **GroupKFold (5 Folds)** gom nhóm theo `building_key` để chống rò rỉ dữ liệu cùng tòa nhà:
```bash
python -m models.train
```

**Chỉ số đạt được trên tập Out-of-Fold (OOF):**
* **MdAPE (Sai số trung vị tương đối):** `16.40%`
* **Độ phủ khoảng dự báo $[P10, P90]$:** `73.35%`
* **Hệ số xác định ($R^2$):** `0.6033`
* **MAE:** `1,233,270 VNĐ`

### 2.4 Chạy kiểm thử tự động (Unit Tests)
```bash
python -m pytest
# 13/13 tests passed 100% (kiểm thử clean, predict và API)
```

### 2.5 Khởi chạy REST API Microservice (Tuần 3)
```bash
# Chạy trực tiếp qua Uvicorn:
uvicorn service.app.main:app --host 0.0.0.0 --port 8000 --reload

# Hoặc khởi chạy qua Docker Compose:
cd service && docker-compose up -d
```

**Ví dụ gọi API ước lượng khoảng giá:**
* Endpoint: `POST http://localhost:8000/api/v1/estimate-price`
* Payload mẫu:
```json
{
  "area_sqm": 25.0,
  "province": "Hồ Chí Minh",
  "district": "Quận 10",
  "lat": 10.7721,
  "lng": 106.6632,
  "am_AIR_CONDITIONER": 1,
  "am_PRIVATE_WC": 1,
  "am_WIFI": 1
}
```
* Kết quả phản hồi:
```json
{
  "success": true,
  "price_p10": 2850000,
  "price_p50": 3650000,
  "price_p90": 4500000,
  "formatted_p10": "2,850,000 VNĐ",
  "formatted_p50": "3,650,000 VNĐ",
  "formatted_p90": "4,500,000 VNĐ",
  "suggested_interval": "2,850,000 - 4,500,000 VNĐ",
  "currency": "VND",
  "model_version": "1.0.0"
}
```
