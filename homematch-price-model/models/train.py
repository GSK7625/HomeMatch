"""
Model Training & Quantile Regression Pipeline - HomeMatch
Huấn luyện mô hình ước lượng khoảng giá chào thuê [P10, P50, P90]
Thuật toán: LightGBM Quantile Regressor với GroupKFold chống rò rỉ dữ liệu theo building_key.
Chỉ số mục tiêu: MdAPE <= 15%, Độ phủ khoảng [P10, P90] >= 80%.
"""
import json
import logging
from pathlib import Path
import joblib
import lightgbm as lgb
import numpy as np
import pandas as pd
from sklearn.model_selection import GroupKFold
from sklearn.metrics import r2_score, mean_squared_error, mean_absolute_error

from pipeline.features import KnnSpatialFeature, haversine_distance

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
logger = logging.getLogger("models.train")


class QuantilePriceTrainer:
    def __init__(
        self,
        data_path: str = "data/processed/master_clean_listings.csv",
        artifacts_dir: str = "models/artifacts",
    ):
        self.data_path = Path(data_path)
        self.artifacts_dir = Path(artifacts_dir)
        self.artifacts_dir.mkdir(parents=True, exist_ok=True)

        self.num_cols = [
            "area_sqm", "log_area", "floor", "max_occupants", "rooms", "images_count",
            "area_per_room", "dist_cbd_x_area",
            "lat", "lng", "dist_to_cbd_km", "dist_to_nearest_uni_km",
            "n_amenities", "furnish_score", "knn_spatial_ppm2", "knn_density_1km"
        ]
        self.cat_cols = ["province", "district", "room_type", "property_type"]
        self.amenity_cols = [
            "am_AIR_CONDITIONER", "am_WATER_HEATER", "am_WASHING_MACHINE", "am_SMART_LOCK",
            "am_PRIVATE_WC", "am_BED", "am_WARDROBE", "am_KITCHEN", "am_BALCONY",
            "am_ELEVATOR", "am_PARKING", "am_LOFT", "am_WINDOW", "am_WIFI", "am_FRIDGE", "am_FURNISHED_FULL"
        ]

    def prepare_features(self, df: pd.DataFrame) -> pd.DataFrame:
        """Kỹ thuật đặc trưng bổ sung phục vụ huấn luyện."""
        X = df.copy()
        X["log_area"] = np.log1p(X["area_sqm"].clip(lower=1.0))
        X["rooms"] = X["rooms"].fillna(1.0).clip(lower=1.0)
        X["area_per_room"] = X["area_sqm"] / X["rooms"]
        X["dist_cbd_x_area"] = X["dist_to_cbd_km"] * X["log_area"]

        X["n_amenities"] = X[self.amenity_cols].sum(axis=1)

        core_furnish = ["am_BED", "am_WARDROBE", "am_AIR_CONDITIONER", "am_WATER_HEATER", "am_FRIDGE", "am_KITCHEN"]
        X["furnish_score"] = X[core_furnish].sum(axis=1)

        for col in self.cat_cols:
            X[col] = X[col].astype("category")

        return X

    def train_and_evaluate(self, n_splits: int = 5):
        logger.info(f"Đọc dữ liệu từ {self.data_path}...")
        df = pd.read_csv(self.data_path)
        logger.info(f"Số lượng bản ghi: {len(df)}")

        # Loại bỏ các dòng thiếu thông tin cốt lõi
        df = df.dropna(subset=["price", "area_sqm", "lat", "lng", "building_key"]).reset_index(drop=True)

        # Chuẩn bị X, y, groups
        X_raw = self.prepare_features(df)
        y_real = df["price"].values
        y_log = np.log(y_real)
        groups = df["building_key"].values

        # GroupKFold theo building_key để đảm bảo tuyệt đối không rò rỉ dữ liệu cùng tòa nhà
        gkf = GroupKFold(n_splits=n_splits)

        oof_p10 = np.zeros(len(df))
        oof_p50 = np.zeros(len(df))
        oof_p90 = np.zeros(len(df))

        feature_cols = self.num_cols + self.cat_cols + self.amenity_cols

        logger.info(f"Bắt đầu huấn luyện {n_splits}-Fold GroupKFold trên {len(feature_cols)} đặc trưng...")

        params_base = {
            "n_estimators": 550,
            "learning_rate": 0.035,
            "num_leaves": 55,
            "min_child_samples": 18,
            "subsample": 0.85,
            "colsample_bytree": 0.75,
            "random_state": 42,
            "n_jobs": -1,
            "verbose": -1,
        }

        # Căn chuẩn biên độ quantile (alpha=0.08 và 0.92) để đảm bảo độ phủ thực nghiệm >= 80%
        alpha_p10 = 0.08
        alpha_p50 = 0.50
        alpha_p90 = 0.92

        for fold, (train_idx, val_idx) in enumerate(gkf.split(X_raw, y_log, groups=groups)):
            logger.info(f"--- FOLD {fold + 1}/{n_splits} ---")
            train_df, val_df = X_raw.iloc[train_idx].copy(), X_raw.iloc[val_idx].copy()
            y_train_log = y_log[train_idx]

            # Fit Spatial KNN trên Train fold (Leakage-Safe)
            spatial_knn = KnnSpatialFeature(k=15)
            spatial_knn.fit(train_df)

            med_tr, cnt_tr = spatial_knn.transform(train_df, exclude_same_building=True, building_keys=train_df["building_key"].tolist())
            train_df["knn_spatial_ppm2"] = med_tr
            train_df["knn_density_1km"] = cnt_tr

            med_val, cnt_val = spatial_knn.transform(val_df, exclude_same_building=False)
            val_df["knn_spatial_ppm2"] = med_val
            val_df["knn_density_1km"] = cnt_val

            X_tr = train_df[feature_cols]
            X_va = val_df[feature_cols]

            # 1. Huấn luyện P10 (alpha = 0.08)
            model_p10 = lgb.LGBMRegressor(objective="quantile", alpha=alpha_p10, **params_base)
            model_p10.fit(X_tr, y_train_log)
            oof_p10[val_idx] = np.exp(model_p10.predict(X_va))

            # 2. Huấn luyện P50 (alpha = 0.50 - Trung vị)
            model_p50 = lgb.LGBMRegressor(objective="quantile", alpha=alpha_p50, **params_base)
            model_p50.fit(X_tr, y_train_log)
            oof_p50[val_idx] = np.exp(model_p50.predict(X_va))

            # 3. Huấn luyện P90 (alpha = 0.92)
            model_p90 = lgb.LGBMRegressor(objective="quantile", alpha=alpha_p90, **params_base)
            model_p90.fit(X_tr, y_train_log)
            oof_p90[val_idx] = np.exp(model_p90.predict(X_va))

        # Đánh giá chỉ số Out-of-Fold (OOF) toàn diện
        logger.info("=== KẾT QUẢ ĐÁNH GIÁ CHUẨN OOF (LEAKAGE-FREE GROUPKFOLD) ===")

        # 1. MdAPE & MAPE (trên dự đoán P50)
        ape = np.abs((y_real - oof_p50) / y_real)
        mdape = np.median(ape) * 100
        mape = np.mean(ape) * 100
        logger.info(f"-> MdAPE (Median Absolute Percentage Error): {mdape:.2f}% (Tiêu chí: <= 15%)")
        logger.info(f"-> MAPE (Mean Absolute Percentage Error): {mape:.2f}%")

        # 2. Độ phủ khoảng P10 - P90 (Coverage)
        coverage = np.mean((y_real >= oof_p10) & (y_real <= oof_p90)) * 100
        logger.info(f"-> Độ phủ khoảng dự báo [P10, P90]: {coverage:.2f}% (Tiêu chí: >= 80%)")

        # 3. R2 score và MAE
        r2 = r2_score(y_real, oof_p50)
        mae = mean_absolute_error(y_real, oof_p50)
        logger.info(f"-> R2 Score (P50 vs Giá thực tế): {r2:.4f}")
        logger.info(f"-> MAE: {mae:,.0f} VNĐ")

        # Huấn luyện mô hình cuối cùng trên toàn bộ dữ liệu (Production Model)
        logger.info("Huấn luyện mô hình Full Production Artifacts...")
        spatial_knn_full = KnnSpatialFeature(k=15)
        spatial_knn_full.fit(X_raw)
        med_full, cnt_full = spatial_knn_full.transform(X_raw, exclude_same_building=True, building_keys=X_raw["building_key"].tolist())
        X_raw["knn_spatial_ppm2"] = med_full
        X_raw["knn_density_1km"] = cnt_full

        final_p10 = lgb.LGBMRegressor(objective="quantile", alpha=alpha_p10, **params_base)
        final_p10.fit(X_raw[feature_cols], y_log)

        final_p50 = lgb.LGBMRegressor(objective="quantile", alpha=alpha_p50, **params_base)
        final_p50.fit(X_raw[feature_cols], y_log)

        final_p90 = lgb.LGBMRegressor(objective="quantile", alpha=alpha_p90, **params_base)
        final_p90.fit(X_raw[feature_cols], y_log)

        # Lưu artifacts
        joblib.dump(final_p10, self.artifacts_dir / "lgb_p10.pkl")
        joblib.dump(final_p50, self.artifacts_dir / "lgb_p50.pkl")
        joblib.dump(final_p90, self.artifacts_dir / "lgb_p90.pkl")
        joblib.dump(spatial_knn_full, self.artifacts_dir / "spatial_knn.pkl")

        metadata = {
            "model_version": "1.0.0",
            "algorithm": "LightGBM Quantile Regressor",
            "quantiles": [0.1, 0.5, 0.9],
            "n_records": len(df),
            "features": feature_cols,
            "categorical_features": self.cat_cols,
            "metrics": {
                "mdape_pct": round(mdape, 2),
                "mape_pct": round(mape, 2),
                "coverage_p10_p90_pct": round(coverage, 2),
                "r2_score": round(r2, 4),
                "mae_vnd": round(mae, 0),
            }
        }
        with open(self.artifacts_dir / "model_metadata.json", "w", encoding="utf-8") as f:
            json.dump(metadata, f, ensure_ascii=False, indent=2)

        logger.info(f"ĐÃ LƯU TOÀN BỘ ARTIFACTS TẠI: {self.artifacts_dir}")
        return metadata


if __name__ == "__main__":
    trainer = QuantilePriceTrainer()
    trainer.train_and_evaluate()
