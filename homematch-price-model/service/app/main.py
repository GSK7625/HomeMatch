"""
FastAPI Microservice - HomeMatch Rental Price Estimation Service
Cung cấp REST API định giá phòng trọ theo thời gian thực (P10 - P50 - P90)
Tuân thủ đầy đủ đặc tả Mục B2 trong tài liệu 03_HUAN_LUYEN_KAGGLE_VA_TICH_HOP.md.
"""
import os
import sys
import logging
from pathlib import Path
from typing import Dict, Any, List
from fastapi import FastAPI, HTTPException, Header, status
from fastapi.middleware.cors import CORSMiddleware
from contextlib import asynccontextmanager

# Thêm đường dẫn gốc để import models
BASE_DIR = Path(__file__).resolve().parent.parent.parent
sys.path.append(str(BASE_DIR))

from models.predict import HomeMatchPriceEstimator
from service.app.schemas import (
    EstimateRequest,
    EstimateResponse,
    PriceEstimationRequest,
    PriceEstimationResponse,
)

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
logger = logging.getLogger("service.api")

INTERNAL_KEY = os.getenv("INTERNAL_API_KEY", "")
MODEL_DIR = os.getenv("MODEL_DIR", str(BASE_DIR / "models" / "artifacts"))

# Khởi tạo mô hình một lần duy nhất lúc khởi động
estimator: HomeMatchPriceEstimator = None


def load_estimator():
    global estimator
    try:
        logger.info(f"Đang tải mô hình từ {MODEL_DIR}...")
        estimator = HomeMatchPriceEstimator(MODEL_DIR)
        logger.info("Mô hình HomeMatchPriceEstimator đã sẵn sàng phục vụ!")
    except Exception as e:
        logger.error(f"Lỗi khởi tạo mô hình: {e}")


@asynccontextmanager
async def lifespan(app: FastAPI):
    load_estimator()
    yield


app = FastAPI(
    title="HomeMatch Price Service",
    description="Microservice ML dự đoán khoảng giá chào thuê phòng trọ P10 - P50 - P90 dựa trên diện tích, vị trí và 16 tiện ích.",
    version="1.0.0",
    lifespan=lifespan,
)

# Kích hoạt CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/health", tags=["Health"])
def health_check():
    """Kiểm tra trạng thái hoạt động của microservice."""
    return {
        "status": "ok",
        "service": "homematch-price-model",
        "model_loaded": estimator is not None,
        "model_version": estimator.model_version if estimator else "none",
    }


# ==============================================================================
# Endpoint chuẩn theo Mục B2 trong 03_HUAN_LUYEN_KAGGLE_VA_TICH_HOP.md
# ==============================================================================
@app.post(
    "/v1/price-estimate",
    response_model=EstimateResponse,
    status_code=status.HTTP_200_OK,
    tags=["Price Estimation"],
)
def price_estimate_v1(req: EstimateRequest, x_internal_key: str = Header(default="")):
    """
    Endpoint chuẩn theo Mục B2 tài liệu 03:
    Backend HomeMatch gọi nội bộ kèm xác thực X-Internal-Key.
    """
    if INTERNAL_KEY and x_internal_key != INTERNAL_KEY:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="unauthorized")

    if estimator is None:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="Mô hình ML đang khởi tạo hoặc chưa tải xong artifacts."
        )

    # Kiểm tra tỉnh thành hợp lệ
    norm_prov = req.province.lower()
    if "hà nội" not in norm_prov and "hồ chí minh" not in norm_prov and "hcm" not in norm_prov:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail="Khu vực chưa được hỗ trợ. Hiện tại chỉ hỗ trợ Hà Nội và TP.HCM."
        )

    try:
        result = estimator.predict(req.model_dump())
        return EstimateResponse(
            p10=result["p10"],
            p50=result["p50"],
            p90=result["p90"],
            currency=result["currency"],
            unit=result["unit"],
            factors=result.get("factors", []),
            warnings=result.get("warnings", []),
            model_version=estimator.model_version,
        )
    except Exception as e:
        logger.error(f"Lỗi khi dự đoán giá: {e}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Lỗi tính toán định giá: {str(e)}"
        )


# ==============================================================================
# Endpoint tương thích ngược
# ==============================================================================
@app.post(
    "/api/v1/estimate-price",
    response_model=PriceEstimationResponse,
    status_code=status.HTTP_200_OK,
    tags=["Price Estimation"],
)
def estimate_price(payload: PriceEstimationRequest):
    """Ước lượng khoảng giá chào thuê phòng [P10, P50, P90]."""
    if estimator is None:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="Mô hình ML đang khởi tạo hoặc chưa tải xong artifacts."
        )

    try:
        result = estimator.predict(payload.model_dump())
        return PriceEstimationResponse(
            success=True,
            p10=result["p10"],
            p50=result["p50"],
            p90=result["p90"],
            price_p10=result["price_p10"],
            price_p50=result["price_p50"],
            price_p90=result["price_p90"],
            formatted_p10=result["formatted_p10"],
            formatted_p50=result["formatted_p50"],
            formatted_p90=result["formatted_p90"],
            suggested_interval=result["suggested_interval"],
            currency=result["currency"],
            unit=result["unit"],
            factors=result.get("factors", []),
            warnings=result.get("warnings", []),
            model_version=estimator.model_version,
        )
    except Exception as e:
        logger.error(f"Lỗi khi dự đoán giá: {e}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Lỗi tính toán định giá: {str(e)}"
        )


@app.get("/api/v1/supported-districts", tags=["Metadata"])
def get_supported_districts():
    """Danh sách các quận/huyện được hỗ trợ định giá tại Hà Nội và TP.HCM."""
    return {
        "Hồ Chí Minh": [
            "Quận 1", "Quận 3", "Quận 4", "Quận 5", "Quận 6", "Quận 7", "Quận 8", "Quận 10",
            "Quận 11", "Quận 12", "Bình Thạnh", "Gò Vấp", "Phú Nhuận", "Tân Bình", "Tân Phú",
            "Bình Tân", "Thành phố Thủ Đức", "Bình Chánh", "Hóc Môn", "Nhà Bè"
        ],
        "Hà Nội": [
            "Ba Đình", "Hoàn Kiếm", "Tây Hồ", "Long Biên", "Cầu Giấy", "Đống Đa", "Hai Bà Trưng",
            "Hoàng Mai", "Thanh Xuân", "Nam Từ Liêm", "Bắc Từ Liêm", "Hà Đông", "Thanh Trì", "Gia Lâm"
        ]
    }
