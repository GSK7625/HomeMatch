# 🚀 Hướng Dẫn Vận Hành Tuần 2 trên Kaggle (HomeMatch Price Model)

Theo đúng kế hoạch tại `ke_hoach_3_tuan_tich_hop_mo_hinh_gia.md`, toàn bộ quá trình huấn luyện đối sánh đa mô hình, tinh chỉnh siêu tham số và phân tích SHAP sẽ được thực thi trên môi trường **Kaggle Kernel (GPU/CPU mạnh mẽ)**.

Toàn bộ tài nguyên đã được đóng gói sẵn trong thư mục `kaggle_package/`. Bạn chỉ cần làm theo 3 bước đơn giản dưới đây:

---

### BƯỚC 1: Tạo Kaggle Dataset (Chỉ mất 1 phút)
1. Truy cập vào Kaggle: [https://www.kaggle.com/datasets](https://www.kaggle.com/datasets) $\rightarrow$ Bấm nút **"+ New Dataset"**.
2. Đặt tiêu đề Dataset: **`HomeMatch Rental Price Dataset`** (Slug tự động tạo: `homematch-rental-price`).
3. Kéo thả 2 file trong thư mục `kaggle_package/dataset/` vào:
   * `master_clean_listings.csv` (26.041 dòng sạch)
   * `reference_points.csv` (Danh bạ mốc tọa độ)
4. Bấm **"Create"** (bạn có thể để chế độ **Private**).

---

### BƯỚC 2: Tạo Kaggle Notebook & Nạp Code
1. Truy cập: [https://www.kaggle.com/code](https://www.kaggle.com/code) $\rightarrow$ Bấm nút **"+ New Notebook"**.
2. Trên thanh menu Notebook, chọn **File $\rightarrow$ Upload Notebook** $\rightarrow$ Chọn file:  
   👉 `kaggle_package/notebook/homematch_rental_price_model.ipynb`
3. Ở cột bên phải mục **Input** (Data):
   * Bấm **"+ Add Input"** $\rightarrow$ Chọn tab **"Your Datasets"** $\rightarrow$ Thêm dataset `homematch-rental-price` vừa tạo ở Bước 1 vào.
4. Bấm nút **"Run All"** (hoặc `Shift + Enter` từng cell).

---

### BƯỚC 3: Thu hoạch Kết quả & Tải Artifacts về máy
Notebook sẽ tự động chạy qua 8 giai đoạn:
1. **EDA**: Xuất bản đồ phân bổ giá và ma trận giá trị gia tăng của 16 tiện ích.
2. **Feature Engineering**: Tính toán Spatial KNN và khoảng cách trường ĐH / CBD.
3. **Benchmark đa mô hình**: So sánh đối chuẩn giữa Dummy, Ridge, Random Forest, LightGBM và CatBoost.
4. **Quantile Regression (P10 - P50 - P90)**: Đánh giá 5-Fold GroupKFold OOF, đạt các chỉ số nghiệm thu ($MdAPE \le 15\%$, $Coverage \ge 80\%$).
5. **SHAP Explainability**: Xuất biểu đồ Beeswarm và Summary Plot giải thích chi tiết giá trị của từng tiện ích.
6. **Artifacts Export**: Toàn bộ các file artifact chuẩn (`model_p10.joblib`, `model_p50.joblib`, `model_p90.joblib`, `knn_index.joblib`, `feature_schema.json`, `metrics.json`) được tạo ra tại thư mục `/kaggle/working/`.
7. Bạn bấm tải các file artifact về và đặt vào `homematch-price-model/models/artifacts/` (hoặc `service/models/20261101-v1/`) trên máy local để phục vụ **Tuần 3: FastAPI Microservice**.
