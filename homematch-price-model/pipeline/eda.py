"""
Exploratory Data Analysis (EDA) & Market Insight Generator - HomeMatch
Tự động phân tích thống kê và trực quan hóa phân bổ giá thuê trên siêu tập dữ liệu 26.041 tin.
"""
import logging
from pathlib import Path
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
logger = logging.getLogger("pipeline.eda")


def run_eda(
    input_csv: str = "data/processed/master_clean_listings.csv",
    output_dir: str = "data/reports",
):
    in_path = Path(input_csv)
    if not in_path.exists():
        raise FileNotFoundError(f"Không tìm thấy file dữ liệu: {input_csv}")

    out_dir = Path(output_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    logger.info(f"Đọc dữ liệu từ {input_csv}...")
    df = pd.read_csv(in_path)
    logger.info(f"Tổng số bản ghi: {len(df)}")

    # Cấu hình font và style cho biểu đồ
    plt.style.use("seaborn-v0_8-whitegrid" if "seaborn-v0_8-whitegrid" in plt.style.available else "default")
    plt.rcParams["font.sans-serif"] = ["DejaVu Sans", "Arial"]
    plt.rcParams["axes.unicode_minus"] = False

    # 1. Phân bổ giá thuê (Price Distribution) & Log-Price
    fig, axes = plt.subplots(1, 2, figsize=(14, 5))
    
    # Giá thực tế (triệu VNĐ)
    price_mil = df["price"] / 1e6
    sns.histplot(price_mil, bins=50, kde=True, ax=axes[0], color="#2563eb")
    axes[0].set_title("Phan bo Gia thue Thuc te (Trieu VND)", fontsize=13, fontweight="bold")
    axes[0].set_xlabel("Gia thue (Trieu VND / thang)")
    axes[0].set_ylabel("So luong tin")
    axes[0].set_xlim(0, 20)

    # Log giá
    log_price = np.log(df["price"])
    sns.histplot(log_price, bins=50, kde=True, ax=axes[1], color="#10b981")
    axes[1].set_title("Phan bo Log-Price (Chuan hoa muc tieu ML)", fontsize=13, fontweight="bold")
    axes[1].set_xlabel("log(price)")
    axes[1].set_ylabel("So luong tin")

    plt.tight_layout()
    fig.savefig(out_dir / "01_price_distribution.png", dpi=200)
    plt.close()
    logger.info("Đã lưu biểu đồ phân bổ giá: 01_price_distribution.png")

    # 2. Giá thuê theo Quận / Huyện (Top 10 quận có lượng tin nhiều nhất HN & HCM)
    for prov in ["Hồ Chí Minh", "Hà Nội"]:
        sub_df = df[df["province"] == prov]
        top_districts = sub_df["district"].value_counts().head(10).index
        sub_top = sub_df[sub_df["district"].isin(top_districts)]

        plt.figure(figsize=(12, 6))
        order = sub_top.groupby("district")["price"].median().sort_values(ascending=False).index
        sns.boxplot(x="district", y=sub_top["price"] / 1e6, data=sub_top, order=order, palette="Blues_r")
        plt.title(f"Phan bo Gia thue tai Top 10 Quan dong tin nhat - {prov}", fontsize=13, fontweight="bold")
        plt.xlabel("Quan / Huyen")
        plt.ylabel("Gia thue (Trieu VND / thang)")
        plt.ylim(0, 18)
        plt.xticks(rotation=25, ha="right")
        plt.tight_layout()
        safe_name = "hcm" if "chí minh" in prov.lower() else "hn"
        plt.savefig(out_dir / f"02_district_price_{safe_name}.png", dpi=200)
        plt.close()
    logger.info("Đã lưu biểu đồ giá theo quận huyện: 02_district_price_*.png")

    # 3. Ma trận tương quan tiện ích & Giá trị gia tăng (Amenity Premium)
    am_cols = [c for c in df.columns if c.startswith("am_")]
    am_premiums = {}
    med_all = df["price"].median()

    for col in am_cols:
        has_am = df[df[col] == 1]["price"].median()
        no_am = df[df[col] == 0]["price"].median()
        premium_pct = ((has_am - no_am) / no_am) * 100 if no_am > 0 else 0
        clean_label = col.replace("am_", "")
        am_premiums[clean_label] = premium_pct

    s_prem = pd.Series(am_premiums).sort_values(ascending=True)

    plt.figure(figsize=(10, 7))
    colors = ["#10b981" if v > 0 else "#ef4444" for v in s_prem.values]
    s_prem.plot(kind="barh", color=colors)
    plt.title("Gia tri Gia tang (%) cua tung Tien ich len Gia thue Trung vi", fontsize=13, fontweight="bold")
    plt.xlabel("Chenh lech gia thue (%) khi co tien ich so voi khong co")
    plt.ylabel("Tien ich")
    plt.tight_layout()
    plt.savefig(out_dir / "03_amenity_premium.png", dpi=200)
    plt.close()
    logger.info("Đã lưu biểu đồ giá trị tiện ích: 03_amenity_premium.png")

    # 4. Mối quan hệ giữa Khoảng cách tới Trung tâm (CBD) và Giá thuê
    plt.figure(figsize=(10, 5))
    sample_df = df.sample(n=min(5000, len(df)), random_state=42)
    sns.scatterplot(
        x="dist_to_cbd_km",
        y=sample_df["price"] / 1e6,
        hue="province",
        data=sample_df,
        alpha=0.4,
        palette=["#2563eb", "#ea580c"],
    )
    plt.title("Tuong quan Khoang cach toi Trung tam (CBD) va Gia thue", fontsize=13, fontweight="bold")
    plt.xlabel("Khoang cach toi CBD (km)")
    plt.ylabel("Gia thue (Trieu VND / thang)")
    plt.ylim(0, 18)
    plt.xlim(0, 25)
    plt.tight_layout()
    plt.savefig(out_dir / "04_distance_to_cbd_vs_price.png", dpi=200)
    plt.close()
    logger.info("Đã lưu biểu đồ khoảng cách CBD: 04_distance_to_cbd_vs_price.png")

    # 5. Xuất bảng thống kê tổng hợp dạng Markdown
    summary_report = f"""# Báo cáo Thống kê & Phân tích Khám phá Dữ liệu (EDA) - HomeMatch

## 1. Tổng quan Bộ dữ liệu
* **Tổng số tin đăng sạch:** {len(df):,} tin
* **Số trường đặc trưng:** {df.shape[1]} cột
* **Nguồn dữ liệu:**
{df['source'].value_counts().to_markdown()}

## 2. Thống kê Giá & Diện tích theo Tỉnh / Thành
{df.groupby('province')[['price', 'area_sqm']].agg(['count', 'median', 'mean']).to_markdown()}

## 3. Top Tiện ích phổ biến nhất thị trường
{df[am_cols].mean().sort_values(ascending=False).apply(lambda x: f'{x*100:.1f}%').to_markdown()}

## 4. Tương quan Khoảng cách Không gian
* **Khoảng cách trung bình tới trung tâm:** {df['dist_to_cbd_km'].mean():.2f} km
* **Khoảng cách trung bình tới trường ĐH gần nhất:** {df['dist_to_nearest_uni_km'].mean():.2f} km
"""
    with open(out_dir / "eda_summary.md", "w", encoding="utf-8") as f:
        f.write(summary_report)

    logger.info(f"Đã xuất báo cáo EDA hoàn chỉnh tại: {out_dir / 'eda_summary.md'}")
    return summary_report


if __name__ == "__main__":
    run_eda()
