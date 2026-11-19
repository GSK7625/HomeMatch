"""
Feature Engineering Module - HomeMatch
Tính toán đặc trưng không gian (Haversine distance, KNN BallTree an toàn rò rỉ), diện tích và tiện ích.
Module này được dùng chung giữa huấn luyện Kaggle và phục vụ API thực tế.
"""
import numpy as np
import pandas as pd
from sklearn.neighbors import BallTree
from typing import List, Tuple, Optional

EARTH_RADIUS_KM = 6371.0


def haversine_distance(lat1: float, lng1: float, lat2: float, lng2: float) -> float:
    """Tính khoảng cách đường chim bay giữa 2 tọa độ (km)."""
    phi1, phi2 = np.radians(lat1), np.radians(lat2)
    delta_phi = np.radians(lat2 - lat1)
    delta_lambda = np.radians(lng2 - lng1)

    a = (np.sin(delta_phi / 2.0) ** 2 +
         np.cos(phi1) * np.cos(phi2) * (np.sin(delta_lambda / 2.0) ** 2))
    c = 2.0 * np.arctan2(np.sqrt(a), np.sqrt(1.0 - a))
    return EARTH_RADIUS_KM * c


class KnnSpatialFeature:
    """
    Tính trung vị giá/m2 của k láng giềng gần nhất một cách an toàn (Leakage-Safe).
    - Fit chỉ trên tập Train.
    - Với Train: loại trừ chính nó và tất cả các phòng cùng building_key.
    - Với Val/Test/Inference: dùng toàn bộ kho Train.
    """
    def __init__(self, k: int = 15):
        self.k = k
        self.tree: Optional[BallTree] = None
        self.ppm2: Optional[np.ndarray] = None
        self.bkeys: Optional[np.ndarray] = None

    def fit(self, df_train: pd.DataFrame):
        # Yêu cầu df_train có: lat, lng, price, area_sqm, building_key
        valid_mask = df_train["lat"].notna() & df_train["lng"].notna() & (df_train["area_sqm"] > 0)
        df = df_train[valid_mask].reset_index(drop=True)

        self.ppm2 = (df["price"] / df["area_sqm"]).values
        self.bkeys = df["building_key"].values
        coords = np.radians(df[["lat", "lng"]].values)
        self.tree = BallTree(coords, metric="haversine")
        return self

    def transform(self, df: pd.DataFrame, exclude_same_building: bool = False, building_keys: Optional[List[str]] = None) -> Tuple[np.ndarray, np.ndarray]:
        if self.tree is None:
            raise ValueError("KnnSpatialFeature chưa được fit!")

        coords = np.radians(df[["lat", "lng"]].fillna(0.0).values)
        extra = 30 if exclude_same_building else 0
        dist, idx = self.tree.query(coords, k=min(self.k + extra, len(self.ppm2)))

        out_med = []
        out_cnt = []

        for i in range(len(df)):
            if pd.isna(df["lat"].iloc[i]) or pd.isna(df["lng"].iloc[i]):
                out_med.append(np.nan)
                out_cnt.append(0)
                continue

            cand = idx[i]
            d = dist[i] * EARTH_RADIUS_KM

            if exclude_same_building and building_keys is not None:
                mask = self.bkeys[cand] != building_keys[i]
                cand = cand[mask]
                d = d[mask]

            cand = cand[:self.k]
            d = d[:self.k]

            out_med.append(np.median(self.ppm2[cand]) if len(cand) > 0 else np.nan)
            out_cnt.append(int((d <= 1.0).sum()))

        return np.array(out_med), np.array(out_cnt)


class FeatureBuilder:
    def __init__(self, ref_points_df: Optional[pd.DataFrame] = None):
        self.ref_points = ref_points_df

    def transform(self, df: pd.DataFrame) -> pd.DataFrame:
        X = df.copy()

        # 1. log_area
        X["log_area"] = np.log1p(X["area_sqm"].clip(lower=1.0))

        # 2. Tổng số tiện ích
        am_cols = [c for c in X.columns if c.startswith("am_")]
        if am_cols:
            X["n_amenities"] = X[am_cols].sum(axis=1)
        else:
            X["n_amenities"] = 0

        # 3. Điểm nội thất cơ bản (furnish_score)
        core_furnish = ["am_BED", "am_WARDROBE", "am_AIR_CONDITIONER", "am_WATER_HEATER", "am_FRIDGE", "am_KITCHEN"]
        existing_core = [c for c in core_furnish if c in X.columns]
        X["furnish_score"] = X[existing_core].sum(axis=1) if existing_core else 0

        # 4. Khoảng cách tới các tâm thành phố
        # Trung tâm HN: 21.0285, 105.8542 (Hồ Gươm)
        # Trung tâm HCM: 10.7721, 106.6983 (Chợ Bến Thành)
        if "lat" in X.columns and "lng" in X.columns:
            dist_center = []
            for _, row in X.iterrows():
                lat, lng = row.get("lat"), row.get("lng")
                if pd.isna(lat) or pd.isna(lng):
                    dist_center.append(np.nan)
                else:
                    prov = str(row.get("province", "")).lower()
                    c_lat, c_lng = (21.0285, 105.8542) if "hà nội" in prov else (10.7721, 106.6983)
                    dist_center.append(haversine_distance(lat, lng, c_lat, c_lng))
            X["dist_center_km"] = dist_center

        return X
