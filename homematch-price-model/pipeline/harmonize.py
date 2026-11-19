"""
Data Harmonization Pipeline - HomeMatch
Tích hợp và chuẩn hóa an toàn giữa:
1. data/processed/clean_listings.csv (13.679 tin sạch từ phongtro123 & thuephongtro)
2. master_features_dataset.json (17.051 tin đa nguồn từ chotot & phongtro123)

Tạo ra: data/processed/master_clean_listings.csv (~26.700+ tin siêu sạch chuẩn ML)
"""
import json
import logging
from pathlib import Path
import numpy as np
import pandas as pd
from tqdm import tqdm

from .clean import (
    extract_amenities,
    map_room_type,
    map_property_type,
    drop_outliers,
    remove_accents,
)
from .geocode import GeocodingService
from .features import haversine_distance

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
logger = logging.getLogger("pipeline.harmonize")


def run_harmonization(
    our_csv: str = "data/processed/clean_listings.csv",
    friend_json: str = "../master_features_dataset.json",
    ref_csv: str = "data/processed/reference_points.csv",
    output_master_csv: str = "data/processed/master_clean_listings.csv",
):
    # 1. Đọc bộ dữ liệu hiện tại của chúng ta
    our_path = Path(our_csv)
    if not our_path.exists():
        raise FileNotFoundError(f"Không tìm thấy file của chúng ta: {our_csv}")

    logger.info(f"Đọc dữ liệu hiện tại từ {our_csv}...")
    df_our = pd.read_csv(our_path)
    logger.info(f"Số lượng tin hiện tại: {len(df_our)}")

    # 2. Đọc các điểm mốc (reference points)
    ref_path = Path(ref_csv)
    ref_points = pd.read_csv(ref_path) if ref_path.exists() else None
    unis = ref_points[ref_points["type"] == "UNIVERSITY"] if ref_points is not None else None

    # Hàm tính khoảng cách đến trường ĐH gần nhất
    def get_nearest_uni(lat: float, lng: float, prov: str):
        if unis is None or pd.isna(lat) or pd.isna(lng):
            return 999.0, "Mặc định"
        prov_key = "Hà Nội" if "hà nội" in prov.lower() else "Hồ Chí Minh"
        city_unis = unis[unis["city"] == prov_key]
        if city_unis.empty:
            city_unis = unis

        min_d = 999.0
        best_name = "Mặc định"
        for _, u in city_unis.iterrows():
            d = haversine_distance(lat, lng, u["lat"], u["lng"])
            if d < min_d:
                min_d = d
                best_name = u["name"]
        return round(min_d, 2), best_name

    # Hàm tính khoảng cách đến CBD
    def get_dist_to_cbd(lat: float, lng: float, prov: str):
        if pd.isna(lat) or pd.isna(lng):
            return 999.0
        c_lat, c_lng = (21.0285, 105.8542) if "hà nội" in prov.lower() else (10.7721, 106.6983)
        return round(haversine_distance(lat, lng, c_lat, c_lng), 2)

    # Chuẩn hóa tỉnh thành dựa trên tọa độ thực tế (lat > 18: Hà Nội, lat < 14: Hồ Chí Minh)
    corrected_provs = []
    for _, row in df_our.iterrows():
        lat = row["lat"]
        if lat > 18.0:
            corrected_provs.append("Hà Nội")
        elif lat < 14.0:
            corrected_provs.append("Hồ Chí Minh")
        else:
            corrected_provs.append(str(row["province"]))
    df_our["province"] = corrected_provs

    # Bổ sung các đặc trưng khoảng cách cho bộ hiện tại của chúng ta nếu chưa có
    logger.info("Tính toán đặc trưng không gian cho bộ dữ liệu hiện tại...")
    cbd_dists = []
    uni_dists = []
    uni_names = []
    for _, row in df_our.iterrows():
        lat, lng, prov = row["lat"], row["lng"], row["province"]
        d_cbd = get_dist_to_cbd(lat, lng, prov)
        d_uni, u_name = get_nearest_uni(lat, lng, prov)
        cbd_dists.append(d_cbd)
        uni_dists.append(d_uni)
        uni_names.append(u_name)

    df_our["dist_to_cbd_km"] = cbd_dists
    df_our["dist_to_nearest_uni_km"] = uni_dists
    df_our["nearest_uni_name"] = uni_names
    if "images_count" not in df_our.columns:
        df_our["images_count"] = 4
    if "rooms" not in df_our.columns:
        df_our["rooms"] = 1.0

    # 3. Đọc và chuẩn hóa dữ liệu của người bạn
    friend_p = Path(friend_json)
    if not friend_p.exists():
        # Thử tìm ở thư mục cha
        friend_p = Path("../master_features_dataset.json")
    if not friend_p.exists():
        friend_p = Path("master_features_dataset.json")

    logger.info(f"Đọc dữ liệu của người bạn từ {friend_p}...")
    with open(friend_p, "r", encoding="utf-8") as f:
        friend_raw = json.load(f)

    logger.info(f"Tổng số bản ghi từ bạn: {len(friend_raw)}")

    geo_service = GeocodingService("config.yaml" if Path("config.yaml").exists() else "homematch-price-model/config.yaml")
    our_titles = set(df_our["title"].str.strip().str.lower())

    friend_records = []
    skipped_out_of_scope = 0
    skipped_bad_coords = 0
    skipped_duplicate = 0

    for d in tqdm(friend_raw, desc="Harmonizing friend data"):
        city_raw = str(d.get("city", "")).strip()
        # Chỉ nhận tin tại Hà Nội hoặc TP. Hồ Chí Minh
        if city_raw not in ["Hà Nội", "TP. Hồ Chí Minh"]:
            skipped_out_of_scope += 1
            continue

        prov = "Hà Nội" if city_raw == "Hà Nội" else "Hồ Chí Minh"
        title = str(d.get("title", "")).strip()

        # Kiểm tra trùng lặp
        if title.lower() in our_titles:
            skipped_duplicate += 1
            continue

        # Kiểm tra tọa độ hợp lệ
        lat = d.get("latitude")
        lng = d.get("longitude")
        if not lat or not lng or not geo_service.is_within_bbox(lat, lng, prov):
            skipped_bad_coords += 1
            continue

        price = float(d.get("price", 0.0))
        area = float(d.get("area_sqm", 0.0))
        if price <= 0 or area <= 0:
            continue

        dist_raw = str(d.get("district", "")).strip()
        ward_raw = str(d.get("ward", "")).strip()
        body_text = f"{title} {d.get('body', '')}"

        # Bóc tách 16 tiện ích chuẩn từ mô tả văn bản
        amenities = extract_amenities(body_text)

        # Kết hợp các cờ có sẵn từ người bạn (không làm mất tín hiệu)
        if d.get("has_air_conditioner") == 1:
            amenities["am_AIR_CONDITIONER"] = 1
        if d.get("has_washing_machine") == 1:
            amenities["am_WASHING_MACHINE"] = 1
        if d.get("has_refrigerator") == 1:
            amenities["am_FRIDGE"] = 1
        if d.get("has_elevator") == 1:
            amenities["am_ELEVATOR"] = 1
        if d.get("has_balcony") == 1:
            amenities["am_BALCONY"] = 1
        if d.get("has_private_bathroom") == 1:
            amenities["am_PRIVATE_WC"] = 1
        if d.get("has_mezzanine") == 1:
            amenities["am_LOFT"] = 1

        # Sinh building_key chuẩn hóa để chống rò rỉ dữ liệu
        bkey = GeocodingService.generate_building_key(title, dist_raw, prov)

        # Tính toán lại khoảng cách thực sự chuẩn xác (loại bỏ hoàn toàn số 999km giả định)
        d_cbd = get_dist_to_cbd(lat, lng, prov)
        d_uni, u_name = get_nearest_uni(lat, lng, prov)

        images = int(d.get("images_count", 4))
        rooms = float(d.get("rooms", 1.0) if d.get("rooms") is not None else 1.0)

        rec = {
            "source": d.get("source", "chotot"),
            "source_id": str(d.get("unified_id", "")),
            "building_key": bkey,
            "title": title,
            "price": int(price),
            "area_sqm": round(area, 1),
            "floor": 1,
            "max_occupants": 2 if area < 30 else (3 if area < 50 else 4),
            "province": prov,
            "district": dist_raw,
            "ward": ward_raw,
            "lat": round(lat, 6),
            "lng": round(lng, 6),
            "geo_precision": "street" if "chotot" in str(d.get("source", "")) else "district",
            "room_type": map_room_type(title, "", body_text),
            "property_type": map_property_type(title, ""),
            "posted_at": "",
            "images_count": images,
            "rooms": rooms,
            "dist_to_cbd_km": d_cbd,
            "dist_to_nearest_uni_km": d_uni,
            "nearest_uni_name": u_name,
        }
        rec.update(amenities)
        friend_records.append(rec)

    logger.info(f"Kết quả xử lý file bạn bạn:")
    logger.info(f" - Bỏ qua do ngoài phạm vi HN/HCM: {skipped_out_of_scope}")
    logger.info(f" - Bỏ qua do trùng lặp với bộ của chúng ta: {skipped_duplicate}")
    logger.info(f" - Bỏ qua do tọa độ sai lệch: {skipped_bad_coords}")
    logger.info(f" - Thu nhận thành công: {len(friend_records)} tin sạch chuẩn!")

    df_friend_clean = pd.DataFrame(friend_records)

    # 4. Gộp hai tập dữ liệu
    logger.info("Gộp hai tập dữ liệu...")
    cols_order = [
        "source", "source_id", "building_key", "title", "price", "area_sqm",
        "floor", "max_occupants", "province", "district", "ward",
        "lat", "lng", "geo_precision", "room_type", "property_type",
        "images_count", "rooms", "dist_to_cbd_km", "dist_to_nearest_uni_km", "nearest_uni_name",
        "am_AIR_CONDITIONER", "am_WATER_HEATER", "am_WASHING_MACHINE", "am_SMART_LOCK",
        "am_PRIVATE_WC", "am_BED", "am_WARDROBE", "am_KITCHEN", "am_BALCONY",
        "am_ELEVATOR", "am_PARKING", "am_LOFT", "am_WINDOW", "am_WIFI", "am_FRIDGE", "am_FURNISHED_FULL"
    ]

    # Đảm bảo đủ cột
    for c in cols_order:
        if c not in df_our.columns:
            df_our[c] = 0
        if c not in df_friend_clean.columns:
            df_friend_clean[c] = 0

    df_master = pd.concat([df_our[cols_order], df_friend_clean[cols_order]], ignore_index=True)

    # 5. Lọc ngoại lệ thống nhất
    df_master = drop_outliers(df_master)
    logger.info(f"Tổng số bản ghi sau khi lọc ngoại lệ: {len(df_master)}")

    # 6. Kiểm tra đảm bảo chất lượng (Data Integrity Assertions)
    assert df_master["price"].isna().sum() == 0, "Lỗi: còn giá trị null ở cột price"
    assert df_master["area_sqm"].isna().sum() == 0, "Lỗi: còn giá trị null ở cột area_sqm"
    assert df_master["lat"].isna().sum() == 0, "Lỗi: còn giá trị null ở cột lat"
    assert df_master["lng"].isna().sum() == 0, "Lỗi: còn giá trị null ở cột lng"
    assert df_master["dist_to_cbd_km"].max() < 100.0, "Lỗi: khoảng cách CBD bất thường (>100km)"
    assert df_master["dist_to_nearest_uni_km"].max() < 100.0, "Lỗi: khoảng cách Đại học bất thường (>100km)"

    # 7. Xuất ra file Siêu Dữ Liệu
    out_path = Path(output_master_csv)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    df_master.to_csv(out_path, index=False, encoding="utf-8-sig")
    logger.info(f"ĐÃ XUẤT THÀNH CÔNG SIÊU TẬP DỮ LIỆU: {out_path} ({len(df_master)} dòng)")

    return df_master


if __name__ == "__main__":
    run_harmonization()
