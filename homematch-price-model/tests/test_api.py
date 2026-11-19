"""
Unit Tests for FastAPI Microservice - HomeMatch
Kiểm tra endpoint /health, /v1/price-estimate, /api/v1/estimate-price và /api/v1/supported-districts
"""
import pytest
from fastapi.testclient import TestClient

from service.app.main import app, load_estimator

client = TestClient(app)


@pytest.fixture(scope="module", autouse=True)
def setup_service():
    """Khởi tạo mô hình trước khi chạy test API."""
    load_estimator()


def test_health_endpoint():
    response = client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] in ("healthy", "ok")
    assert data["model_loaded"] is True


def test_supported_districts_endpoint():
    response = client.get("/api/v1/supported-districts")
    assert response.status_code == 200
    data = response.json()
    assert "Hồ Chí Minh" in data
    assert "Hà Nội" in data
    assert "Quận 10" in data["Hồ Chí Minh"]
    assert "Cầu Giấy" in data["Hà Nội"]


def test_estimate_price_v1_doc03_standard():
    """Test endpoint chuẩn theo Mục B2 tài liệu 03: POST /v1/price-estimate"""
    payload = {
        "province": "Hà Nội",
        "district": "Cầu Giấy",
        "ward": "Dịch Vọng Hậu",
        "lat": 21.0371,
        "lng": 105.7826,
        "area_sqm": 25.0,
        "room_type": "STUDIO",
        "property_type": "BOARDING_HOUSE",
        "floor": 3,
        "amenity_codes": ["AIR_CONDITIONER", "WATER_HEATER", "BED"],
        "explain": True,
    }
    response = client.post("/v1/price-estimate", json=payload)
    assert response.status_code == 200
    res = response.json()
    assert "p10" in res and "p50" in res and "p90" in res
    assert res["p10"] <= res["p50"] <= res["p90"]
    assert res["currency"] == "VND"
    assert res["unit"] == "per_month"
    assert "model_version" in res
    assert isinstance(res["warnings"], list)


def test_estimate_price_v1_unsupported_province():
    """Tỉnh thành ngoài Hà Nội / TP.HCM phải trả lỗi 422 theo đặc tả."""
    payload = {
        "province": "Đà Nẵng",
        "district": "Hải Châu",
        "area_sqm": 25.0,
        "room_type": "ROOM",
    }
    response = client.post("/v1/price-estimate", json=payload)
    assert response.status_code == 422


def test_estimate_price_legacy_endpoint():
    """Test endpoint tương thích ngược /api/v1/estimate-price"""
    payload = {
        "area_sqm": 25.0,
        "province": "Hồ Chí Minh",
        "district": "Quận 10",
        "lat": 10.7721,
        "lng": 106.6632,
        "floor": 2,
        "rooms": 1.0,
        "room_type": "ROOM",
        "property_type": "BOARDING_HOUSE",
        "am_AIR_CONDITIONER": 1,
        "am_PRIVATE_WC": 1,
        "am_WIFI": 1,
    }
    response = client.post("/api/v1/estimate-price", json=payload)
    assert response.status_code == 200
    res = response.json()
    assert res["success"] is True
    assert res["price_p10"] <= res["price_p50"] <= res["price_p90"]
    assert "suggested_interval" in res


def test_estimate_price_validation_error():
    """Diện tích < 5m2 trên legacy endpoint phải bị từ chối với mã 422."""
    payload = {
        "area_sqm": 2.0,  # Sai quy định (ge=5.0)
        "province": "Hồ Chí Minh",
        "district": "Quận 10",
        "lat": 10.7721,
        "lng": 106.6632,
    }
    response = client.post("/api/v1/estimate-price", json=payload)
    assert response.status_code == 422
