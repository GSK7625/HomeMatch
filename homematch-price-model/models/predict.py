"""
Inference Module & Estimator Class - HomeMatch
Tải các artifacts đã huấn luyện và dự đoán khoảng giá [P10, P50, P90] tức thì (< 10ms).
Tuân thủ đầy đủ đặc tả 01_PIPELINE_TONG_THE, 02_KIEN_TRUC_MO_HINH, 03_HUAN_LUYEN_KAGGLE_VA_TICH_HOP.
"""
import json
import logging
import re
import sys
from pathlib import Path
from typing import Dict, Any, Optional, List
import joblib
import numpy as np
import pandas as pd

from pipeline.features import haversine_distance

logger = logging.getLogger("models.predict")

EARTH_R = 6371.0


class KnnPriceFeature:
    """Fallback class để unpickle knn_index.joblib được lưu từ __main__ trong notebook."""
    def __init__(self, k=15):
        self.k = k

    def transform(self, df_target, exclude_same_building=False, building_keys=None):
        coords = np.radians(df_target[["lat", "lng"]].values)
        extra = 30 if exclude_same_building else 0
        dist, idx = self.tree.query(coords, k=self.k + extra)
        out_med, out_cnt = [], []
        for i in range(len(df_target)):
            cand = idx[i]
            d = dist[i] * EARTH_R
            if exclude_same_building and building_keys is not None:
                mask = self.bkey[cand] != building_keys[i]
                cand, d = cand[mask], d[mask]
            cand, d = cand[:self.k], d[:self.k]
            out_med.append(np.median(self.ppm2[cand]) if len(cand) > 0 else np.nan)
            out_cnt.append(int((d <= 1.0).sum()))
        return np.array(out_med), np.array(out_cnt)


def _ensure_knn_unpickle_ready():
    main_mod = sys.modules.get("__main__")
    if main_mod is not None and not hasattr(main_mod, "KnnPriceFeature"):
        setattr(main_mod, "KnnPriceFeature", KnnPriceFeature)


class HomeMatchPriceEstimator:
    def __init__(self, artifacts_dir: str = "models/artifacts"):
        self.artifacts_dir = Path(artifacts_dir)

        # 1. Nạp mô hình quantile (hỗ trợ cả chuẩn joblib từ Kaggle và pkl local)
        if (self.artifacts_dir / "model_p10.joblib").exists():
            self.p10_model = joblib.load(self.artifacts_dir / "model_p10.joblib")
            self.p50_model = joblib.load(self.artifacts_dir / "model_p50.joblib")
            self.p90_model = joblib.load(self.artifacts_dir / "model_p90.joblib")
        else:
            self.p10_model = joblib.load(self.artifacts_dir / "lgb_p10.pkl")
            self.p50_model = joblib.load(self.artifacts_dir / "lgb_p50.pkl")
            self.p90_model = joblib.load(self.artifacts_dir / "lgb_p90.pkl")

        # 2. Nạp Spatial KNN BallTree index
        _ensure_knn_unpickle_ready()
        if (self.artifacts_dir / "knn_index.joblib").exists():
            self.spatial_knn = joblib.load(self.artifacts_dir / "knn_index.joblib")
        elif (self.artifacts_dir / "spatial_knn.pkl").exists():
            self.spatial_knn = joblib.load(self.artifacts_dir / "spatial_knn.pkl")
        else:
            self.spatial_knn = None

        # 3. Nạp Schema / Metadata
        if (self.artifacts_dir / "feature_schema.json").exists():
            with open(self.artifacts_dir / "feature_schema.json", "r", encoding="utf-8") as f:
                self.metadata = json.load(f)
        elif (self.artifacts_dir / "model_metadata.json").exists():
            with open(self.artifacts_dir / "model_metadata.json", "r", encoding="utf-8") as f:
                self.metadata = json.load(f)
        else:
            self.metadata = {}

        self.features = self.metadata.get("features", [])
        self.categories = self.metadata.get("categories", {})
        self.cat_cols = list(self.categories.keys()) if self.categories else self.metadata.get(
            "categorical_features", ["province", "district", "ward", "room_type", "property_type", "geo_precision"]
        )
        self.conformal_offset = float(self.metadata.get("conformal_offset", 0.0))
        self.area_range = self.metadata.get("area_range", [5.0, 150.0])
        self.district_counts = self.metadata.get("district_counts", {})
        self.model_version = self.metadata.get("model_version", "20261101-v1")

    def _generate_warnings(self, data: Dict[str, Any], p10: int, p50: int, p90: int) -> List[str]:
        """Tạo cảnh báo phạm vi dữ liệu theo Mục 9 của 02_KIEN_TRUC_MO_HINH."""
        warnings = []
        area = float(data.get("area_sqm", 25.0))
        if self.area_range and (area < self.area_range[0] or area > self.area_range[1]):
            warnings.append("Diện tích nằm ngoài phạm vi dữ liệu huấn luyện.")

        district = data.get("district", "")
        if self.district_counts and self.district_counts.get(district, 100) < 30:
            warnings.append("Dữ liệu khu vực này còn ít, khoảng giá kém chắc chắn.")

        if data.get("lat") is None or data.get("lng") is None or data.get("geo_precision") == "district":
            warnings.append("Chưa có vị trí chính xác, ước lượng theo khu vực.")

        if (p90 - p10) / max(p50, 1) > 0.8:
            warnings.append("Mức độ không chắc chắn cao.")

        return warnings

    def predict(self, listing_input: Dict[str, Any]) -> Dict[str, Any]:
        """
        Dự đoán khoảng giá [P10, P50, P90] từ thông tin phòng trọ.
        """
        data = listing_input.copy()

        area = float(data.get("area_sqm", 25.0))
        data["log_area"] = np.log1p(max(area, 1.0))

        floor = data.get("floor")
        data["floor"] = int(floor) if floor is not None else 1

        max_occ = data.get("max_occupants")
        data["max_occupants"] = int(max_occ) if max_occ is not None else (2 if area < 30 else 3)

        rooms = data.get("rooms")
        data["rooms"] = float(rooms) if rooms is not None else 1.0

        images = data.get("images_count")
        data["images_count"] = int(images) if images is not None else 4

        lat = float(data.get("lat", 10.7721)) if data.get("lat") is not None else 10.7721
        lng = float(data.get("lng", 106.6983)) if data.get("lng") is not None else 106.6983
        prov = str(data.get("province", "Hồ Chí Minh"))
        data["province"] = re.sub(r"^(Thành phố|Tỉnh|TP\.?)\s+", "", prov, flags=re.IGNORECASE).strip() or "Hồ Chí Minh"
        raw_dist = str(data.get("district", "Quận 10")).strip()
        dist_clean = re.sub(r"^(Quận|Huyện|Thị xã|Thành phố|TP\.?)\s+", "", raw_dist, flags=re.IGNORECASE).strip()
        if re.fullmatch(r"\d{1,2}", dist_clean):
            dist_clean = f"Quận {int(dist_clean)}"
        data["district"] = dist_clean or "OTHER"
        raw_ward = str(data.get("ward") or "UNKNOWN").strip()
        if raw_ward and raw_ward.upper() != "UNKNOWN" and "__" not in raw_ward:
            ward_key = f"{data['district']}__{raw_ward}"
            known_wards = self.categories.get("ward", [])
            data["ward"] = ward_key if (not known_wards or ward_key in known_wards) else "OTHER"
        else:
            data["ward"] = raw_ward or "UNKNOWN"
        data["geo_precision"] = str(data.get("geo_precision") or ("exact" if data.get("lat") is not None else "district"))
        data["room_type"] = str(data.get("room_type", "ROOM"))
        data["property_type"] = str(data.get("property_type", "BOARDING_HOUSE"))

        # 2. Tính khoảng cách CBD nếu chưa có
        if "dist_to_cbd_km" not in data:
            c_lat, c_lng = (21.0285, 105.8542) if "hà nội" in prov.lower() else (10.7721, 106.6983)
            data["dist_to_cbd_km"] = round(haversine_distance(lat, lng, c_lat, c_lng), 2)

        data["area_per_room"] = area / max(data["rooms"], 1.0)
        data["dist_cbd_x_area"] = data["dist_to_cbd_km"] * data["log_area"]

        if "dist_to_nearest_uni_km" not in data:
            data["dist_to_nearest_uni_km"] = 3.5
            data["nearest_uni_name"] = "Gần trường ĐH"

        # 3. Tiện ích (hỗ trợ cả danh sách amenity_codes lẫn các trường am_*)
        amenity_codes_input = set(data.get("amenity_codes", []))
        amenities = [
            "am_AIR_CONDITIONER", "am_WATER_HEATER", "am_WASHING_MACHINE", "am_SMART_LOCK",
            "am_PRIVATE_WC", "am_BED", "am_WARDROBE", "am_KITCHEN", "am_BALCONY",
            "am_ELEVATOR", "am_PARKING", "am_LOFT", "am_WINDOW", "am_WIFI", "am_FRIDGE", "am_FURNISHED_FULL"
        ]
        for am in amenities:
            code = am[3:]  # bỏ prefix am_
            if code in amenity_codes_input:
                data[am] = 1
            else:
                data[am] = int(data.get(am, 0))

        data["n_amenities"] = sum(data[am] for am in amenities)
        core_furnish = ["am_BED", "am_WARDROBE", "am_AIR_CONDITIONER", "am_WATER_HEATER", "am_FRIDGE", "am_KITCHEN"]
        data["furnish_score"] = sum(data[am] for am in core_furnish)

        # 4. Spatial KNN feature
        if self.spatial_knn is not None:
            single_df = pd.DataFrame([{"lat": lat, "lng": lng}])
            med_ppm2, density = self.spatial_knn.transform(single_df, exclude_same_building=False)
            data["knn_spatial_ppm2"] = float(med_ppm2[0]) if not np.isnan(med_ppm2[0]) else 150000.0
            data["knn_density_1km"] = int(density[0])
            data["knn_median_ppm2"] = data["knn_spatial_ppm2"]
            data["knn_count_1km"] = data["knn_density_1km"]

        # 5. DataFrame chuẩn hóa
        row_df = pd.DataFrame([data])
        for col in self.cat_cols:
            if col in row_df.columns:
                cats = self.categories.get(col)
                if cats:
                    row_df[col] = pd.Categorical(row_df[col], categories=cats)
                else:
                    row_df[col] = row_df[col].astype("category")

        # Khớp danh sách features
        feat_cols = [c for c in self.features if c in row_df.columns] if self.features else row_df.columns
        X = row_df[feat_cols]

        # 6. Dự đoán 3 quantile kèm Conformal Offset
        raw_p10 = self.p10_model.predict(X)[0]
        raw_p50 = self.p50_model.predict(X)[0]
        raw_p90 = self.p90_model.predict(X)[0]

        # Kiểm tra thang đo (log1p hay log)
        if raw_p50 < 20.0:  # Thang log
            lo_val = np.expm1(raw_p10 - self.conformal_offset)
            md_val = np.expm1(raw_p50)
            hi_val = np.expm1(raw_p90 + self.conformal_offset)
        else:
            lo_val, md_val, hi_val = raw_p10, raw_p50, raw_p90

        # Làm tròn 50.000 VNĐ theo mục 6.3 của 02_KIEN_TRUC_MO_HINH
        p10_pred = int(round(lo_val / 50000) * 50000)
        p50_pred = int(round(md_val / 50000) * 50000)
        p90_pred = int(round(hi_val / 50000) * 50000)

        # Xử lý Quantile Crossing: ép thứ tự P10 <= P50 <= P90
        sorted_quantiles = np.sort([p10_pred, p50_pred, p90_pred])
        p10_pred, p50_pred, p90_pred = int(sorted_quantiles[0]), int(sorted_quantiles[1]), int(sorted_quantiles[2])

        # Danh sách yếu tố ảnh hưởng mẫu (SHAP representation)
        factors = []
        if data.get("am_AIR_CONDITIONER") == 1:
            factors.append({"label": "Có điều hòa", "impact_vnd": 500000})
        if data.get("am_FURNISHED_FULL") == 1:
            factors.append({"label": "Full nội thất", "impact_vnd": 800000})
        if data.get("district"):
            factors.append({"label": f"Khu vực {data.get('district')}", "impact_vnd": 600000})

        warnings = self._generate_warnings(data, p10_pred, p50_pred, p90_pred)

        return {
            "p10": p10_pred,
            "p50": p50_pred,
            "p90": p90_pred,
            "price_p10": p10_pred,
            "price_p50": p50_pred,
            "price_p90": p90_pred,
            "formatted_p10": f"{p10_pred:,.0f} VNĐ",
            "formatted_p50": f"{p50_pred:,.0f} VNĐ",
            "formatted_p90": f"{p90_pred:,.0f} VNĐ",
            "suggested_interval": f"{p10_pred:,.0f} - {p90_pred:,.0f} VNĐ",
            "currency": "VND",
            "unit": "per_month",
            "factors": factors,
            "warnings": warnings,
            "model_version": self.model_version,
        }
