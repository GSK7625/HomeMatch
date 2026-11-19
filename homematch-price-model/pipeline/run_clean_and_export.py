"""
Run Cleaning, Geocoding & Export Processed Dataset - HomeMatch
Chuyển đổi raw_listings.jsonl -> data/processed/clean_listings.csv
"""
import json
import logging
from pathlib import Path
import pandas as pd
from tqdm import tqdm

from .clean import (
    parse_price,
    parse_area,
    map_room_type,
    map_property_type,
    extract_amenities,
    parse_address_components,
    drop_outliers,
)
from .geocode import GeocodingService

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
logger = logging.getLogger("pipeline.export")


def process_and_export(
    input_jsonl: str = "data/interim/raw_listings.jsonl",
    output_csv: str = "data/processed/clean_listings.csv",
    geocode: bool = True
):
    in_path = Path(input_jsonl)
    if not in_path.exists():
        raise FileNotFoundError(f"Không tìm thấy file đầu vào: {input_jsonl}")

    logger.info(f"Đọc dữ liệu thô từ {input_jsonl}...")
    raw_data = []
    with open(in_path, "r", encoding="utf-8") as f:
        for line in f:
            if line.strip():
                raw_data.append(json.loads(line))

    logger.info(f"Tổng số bản ghi đọc được: {len(raw_data)}")
    geo_service = GeocodingService() if geocode else None

    records = []
    for d in tqdm(raw_data, desc="Cleaning & Parsing"):
        price = parse_price(d.get("price_raw"))
        area = parse_area(d.get("area_raw"))

        if not price or not area:
            continue

        address_raw = d.get("address_raw", "")
        district_raw = d.get("district_raw", "")
        text_fb = f"{d.get('title', '')} {d.get('description', '')}"
        prov, dist, ward = parse_address_components(address_raw, district_raw, text_fb)

        # Geocoding & Bounding box
        lat, lng, geo_precision = None, None, "unknown"
        if geo_service:
            geo_res = geo_service.geocode_address(address_raw, prov, dist, ward)
            lat = geo_res.get("lat")
            lng = geo_res.get("lng")
            geo_precision = geo_res.get("geo_precision", "unknown")

        # Building key để chia tập chống rò rỉ dữ liệu
        bkey = GeocodingService.generate_building_key(address_raw, dist, prov)

        rec = {
            "source": d.get("source", "phongtro123"),
            "source_id": d.get("source_id", ""),
            "building_key": bkey,
            "title": d.get("title", ""),
            "price": price,
            "area_sqm": area,
            "floor": 1,
            "max_occupants": 2 if area < 30 else 3,
            "province": prov,
            "district": dist,
            "ward": ward,
            "lat": lat,
            "lng": lng,
            "geo_precision": geo_precision,
            "room_type": map_room_type(d.get("title", ""), d.get("category", ""), d.get("description", "")),
            "property_type": map_property_type(d.get("title", ""), d.get("category", "")),
            "posted_at": d.get("posted_at", ""),
        }

        # Trích xuất 16 cột tiện ích am_*
        amenities = extract_amenities(d.get("description", ""))
        rec.update(amenities)

        records.append(rec)

    df = pd.DataFrame(records)
    logger.info(f"Số bản ghi sau bóc tách số hợp lệ: {len(df)}")

    # Lọc bỏ ngoại lệ
    clean_df = drop_outliers(df)
    logger.info(f"Số bản ghi sau khi loại bỏ ngoại lệ: {len(clean_df)}")

    out_file = Path(output_csv)
    out_file.parent.mkdir(parents=True, exist_ok=True)
    clean_df.to_csv(out_file, index=False, encoding="utf-8-sig")
    logger.info(f"Xuất file thành công: {out_file} ({len(clean_df)} dòng)")

    # Tạo bảng reference_points.csv
    create_reference_points(out_file.parent / "reference_points.csv")


def create_reference_points(out_path: Path):
    points = [
        {"name": "Hồ Gươm (Trung tâm HN)", "city": "Hà Nội", "type": "CENTER", "lat": 21.0285, "lng": 105.8542},
        {"name": "Chợ Bến Thành (Trung tâm HCM)", "city": "Hồ Chí Minh", "type": "CENTER", "lat": 10.7721, "lng": 106.6983},
        {"name": "Đại học Bách Khoa HN", "city": "Hà Nội", "type": "UNIVERSITY", "lat": 21.0048, "lng": 105.8436},
        {"name": "Đại học Quốc gia Hà Nội (Cầu Giấy)", "city": "Hà Nội", "type": "UNIVERSITY", "lat": 21.0371, "lng": 105.7826},
        {"name": "Đại học Kinh tế Quốc dân", "city": "Hà Nội", "type": "UNIVERSITY", "lat": 20.9961, "lng": 105.8431},
        {"name": "Đại học Bách Khoa TP.HCM (Q.10)", "city": "Hồ Chí Minh", "type": "UNIVERSITY", "lat": 10.7722, "lng": 106.6578},
        {"name": "Đại học Khoa học Tự nhiên TP.HCM (Q.5)", "city": "Hồ Chí Minh", "type": "UNIVERSITY", "lat": 10.7632, "lng": 106.6822},
        {"name": "Đại học Sư phạm Kỹ thuật (Thủ Đức)", "city": "Hồ Chí Minh", "type": "UNIVERSITY", "lat": 10.8509, "lng": 106.7719},
        {"name": "Ga Metro Cát Linh (Hà Nội)", "city": "Hà Nội", "type": "METRO", "lat": 21.0267, "lng": 105.8242},
        {"name": "Ga Metro Nhà hát Thành phố (HCM)", "city": "Hồ Chí Minh", "type": "METRO", "lat": 10.7766, "lng": 106.7029},
    ]
    pd.DataFrame(points).to_csv(out_path, index=False, encoding="utf-8-sig")
    logger.info(f"Tạo reference_points.csv tại: {out_path}")


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", default="data/interim/raw_listings.jsonl")
    parser.add_argument("--output", default="data/processed/clean_listings.csv")
    parser.add_argument("--skip-geo", action="store_true", help="Bỏ qua bước gọi mạng geocoding để chạy nhanh")

    args = parser.parse_args()
    process_and_export(args.input, args.output, geocode=not args.skip_geo)
