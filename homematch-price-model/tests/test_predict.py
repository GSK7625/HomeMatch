"""
Unit Tests for Model Inference & Price Estimation - HomeMatch
Kiểm tra tính hợp lệ của mô hình dự đoán khoảng giá P10 - P50 - P90.
"""
import pytest
from pathlib import Path


def test_model_artifacts_exist():
    artifacts = Path("models/artifacts")
    assert (artifacts / "lgb_p10.pkl").exists()
    assert (artifacts / "lgb_p50.pkl").exists()
    assert (artifacts / "lgb_p90.pkl").exists()
    assert (artifacts / "spatial_knn.pkl").exists()
    assert (artifacts / "model_metadata.json").exists()


def test_estimator_prediction():
    from models.predict import HomeMatchPriceEstimator

    estimator = HomeMatchPriceEstimator()
    sample = {
        "area_sqm": 25.0,
        "province": "Hồ Chí Minh",
        "district": "Quận 10",
        "lat": 10.7721,
        "lng": 106.6632,
        "room_type": "ROOM",
        "property_type": "BOARDING_HOUSE",
        "am_AIR_CONDITIONER": 1,
        "am_PRIVATE_WC": 1,
        "am_BED": 1,
    }

    res = estimator.predict(sample)

    assert "price_p10" in res
    assert "price_p50" in res
    assert "price_p90" in res

    # Kiểm tra tính đơn điệu của Quantile: P10 <= P50 <= P90
    assert res["price_p10"] <= res["price_p50"] <= res["price_p90"]

    # Giá phòng 25m2 có điều hòa tại Q.10 phải trong dải hợp lý (2tr - 10tr)
    assert 2000000 <= res["price_p50"] <= 10000000


def test_amenity_impact():
    """Kiểm tra: Phòng có điều hòa và full nội thất phải có giá P50 cao hơn phòng trống."""
    from models.predict import HomeMatchPriceEstimator

    estimator = HomeMatchPriceEstimator()
    base_room = {
        "area_sqm": 25.0,
        "province": "Hà Nội",
        "district": "Quận Cầu Giấy",
        "lat": 21.0313,
        "lng": 105.7925,
        "room_type": "ROOM",
        "property_type": "BOARDING_HOUSE",
    }

    res_empty = estimator.predict(base_room)

    luxury_room = base_room.copy()
    luxury_room.update({
        "am_AIR_CONDITIONER": 1,
        "am_WATER_HEATER": 1,
        "am_WASHING_MACHINE": 1,
        "am_FURNISHED_FULL": 1,
    })
    res_luxury = estimator.predict(luxury_room)

    assert res_luxury["price_p50"] >= res_empty["price_p50"]
