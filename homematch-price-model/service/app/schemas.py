"""
Pydantic Schemas - HomeMatch Rental Price Service
Định nghĩa cấu trúc dữ liệu đầu vào và đầu ra cho API định giá chuẩn Pydantic V2.
Tuân thủ đầy đủ đặc tả Mục B2 trong tài liệu 03_HUAN_LUYEN_KAGGLE_VA_TICH_HOP.md.
"""
from typing import Optional, List
from pydantic import BaseModel, Field


# ==============================================================================
# 1. Schemas Chuẩn theo Tài liệu 03_HUAN_LUYEN_KAGGLE_VA_TICH_HOP (Mục B2)
# ==============================================================================

class Factor(BaseModel):
    label: str
    impact_vnd: int  # dương = tăng giá, âm = giảm giá


class EstimateRequest(BaseModel):
    province: str = Field(..., description="Tỉnh/Thành phố ('Hà Nội' hoặc 'Hồ Chí Minh')", json_schema_extra={"example": "Hà Nội"})
    district: str = Field(..., description="Quận/Huyện", json_schema_extra={"example": "Cầu Giấy"})
    ward: Optional[str] = Field(None, description="Phường/Xã", json_schema_extra={"example": "Dịch Vọng Hậu"})
    lat: Optional[float] = Field(None, description="Vĩ độ", json_schema_extra={"example": 21.0371})
    lng: Optional[float] = Field(None, description="Kinh độ", json_schema_extra={"example": 105.7826})
    area_sqm: float = Field(..., gt=0, le=500, description="Diện tích phòng (m2)", json_schema_extra={"example": 25.0})
    room_type: str = Field("ROOM", description="STUDIO | 1B1B | 2B1B | DORMITORY | ROOM", json_schema_extra={"example": "STUDIO"})
    property_type: str = Field("BOARDING_HOUSE", description="BOARDING_HOUSE | APARTMENT | SERVICED_APARTMENT | HOMESTAY", json_schema_extra={"example": "BOARDING_HOUSE"})
    floor: Optional[int] = Field(None, description="Tầng", json_schema_extra={"example": 3})
    max_occupants: Optional[int] = Field(None, description="Số người tối đa")
    amenity_codes: List[str] = Field(default_factory=list, description="Danh sách mã tiện ích: AIR_CONDITIONER, WATER_HEATER...", json_schema_extra={"example": ["AIR_CONDITIONER", "WATER_HEATER", "BED"]})
    allow_pets: Optional[bool] = Field(None, description="Cho phép nuôi thú cưng")
    explain: bool = Field(True, description="Trả về các yếu tố giải thích SHAP")


class EstimateResponse(BaseModel):
    p10: int = Field(..., description="Cận dưới khoảng giá (VNĐ/tháng)")
    p50: int = Field(..., description="Mức giá kỳ vọng phổ biến (VNĐ/tháng)")
    p90: int = Field(..., description="Cận trên khoảng giá (VNĐ/tháng)")
    currency: str = "VND"
    unit: str = "per_month"
    factors: List[Factor] = Field(default_factory=list, description="Top yếu tố tăng/giảm giá")
    warnings: List[str] = Field(default_factory=list, description="Cảnh báo ngoài phạm vi dữ liệu")
    model_version: str = "20261101-v1"


# ==============================================================================
# 2. Schemas Mở Rộng / Tương Thích Ngược
# ==============================================================================

class PriceEstimationRequest(BaseModel):
    area_sqm: float = Field(..., ge=5.0, le=250.0, description="Diện tích phòng (m2)", json_schema_extra={"example": 25.0})
    province: str = Field(..., description="Tỉnh / Thành phố ('Hà Nội' hoặc 'Hồ Chí Minh')", json_schema_extra={"example": "Hồ Chí Minh"})
    district: str = Field(..., description="Quận / Huyện", json_schema_extra={"example": "Quận 10"})
    ward: Optional[str] = Field(None, description="Phường / Xã", json_schema_extra={"example": "Phường 12"})
    lat: float = Field(..., description="Vĩ độ", json_schema_extra={"example": 10.7721})
    lng: float = Field(..., description="Kinh độ", json_schema_extra={"example": 106.6632})
    floor: Optional[int] = Field(1, ge=1, le=50, description="Tầng phòng tọa lạc", json_schema_extra={"example": 2})
    rooms: Optional[float] = Field(1.0, ge=1.0, le=10.0, description="Số phòng ngủ", json_schema_extra={"example": 1.0})
    max_occupants: Optional[int] = Field(None, description="Số người ở tối đa")
    room_type: Optional[str] = Field("ROOM", description="Loại phòng (ROOM, STUDIO, 1B1B, 2B1B, DORMITORY)", json_schema_extra={"example": "ROOM"})
    property_type: Optional[str] = Field("BOARDING_HOUSE", description="Loại hình BĐS (BOARDING_HOUSE, APARTMENT, SERVICED_APARTMENT, HOMESTAY)", json_schema_extra={"example": "BOARDING_HOUSE"})

    # Hỗ trợ cả danh sách amenity_codes và 16 trường tiện ích chi tiết
    amenity_codes: List[str] = Field(default_factory=list)
    am_AIR_CONDITIONER: int = Field(0, ge=0, le=1, description="Điều hòa", json_schema_extra={"example": 1})
    am_WATER_HEATER: int = Field(0, ge=0, le=1, description="Bình nóng lạnh", json_schema_extra={"example": 1})
    am_WASHING_MACHINE: int = Field(0, ge=0, le=1, description="Máy giặt", json_schema_extra={"example": 0})
    am_SMART_LOCK: int = Field(0, ge=0, le=1, description="Khóa vân tay / thẻ từ", json_schema_extra={"example": 0})
    am_PRIVATE_WC: int = Field(1, ge=0, le=1, description="Vệ sinh khép kín", json_schema_extra={"example": 1})
    am_BED: int = Field(0, ge=0, le=1, description="Giường ngủ", json_schema_extra={"example": 1})
    am_WARDROBE: int = Field(0, ge=0, le=1, description="Tủ quần áo", json_schema_extra={"example": 1})
    am_KITCHEN: int = Field(0, ge=0, le=1, description="Kệ bếp / nấu ăn", json_schema_extra={"example": 0})
    am_BALCONY: int = Field(0, ge=0, le=1, description="Ban công", json_schema_extra={"example": 0})
    am_ELEVATOR: int = Field(0, ge=0, le=1, description="Thang máy", json_schema_extra={"example": 0})
    am_PARKING: int = Field(1, ge=0, le=1, description="Chỗ để xe", json_schema_extra={"example": 1})
    am_LOFT: int = Field(0, ge=0, le=1, description="Gác lửng / gác xép", json_schema_extra={"example": 0})
    am_WINDOW: int = Field(1, ge=0, le=1, description="Cửa sổ thoáng", json_schema_extra={"example": 1})
    am_WIFI: int = Field(1, ge=0, le=1, description="Internet Wifi", json_schema_extra={"example": 1})
    am_FRIDGE: int = Field(0, ge=0, le=1, description="Tủ lạnh", json_schema_extra={"example": 0})
    am_FURNISHED_FULL: int = Field(0, ge=0, le=1, description="Full nội thất chỉ việc vào ở", json_schema_extra={"example": 0})


class PriceEstimationResponse(BaseModel):
    success: bool = True
    p10: Optional[int] = None
    p50: Optional[int] = None
    p90: Optional[int] = None
    price_p10: int = Field(..., description="Giá cận dưới P10 (VNĐ/tháng)")
    price_p50: int = Field(..., description="Giá trung vị kỳ vọng P50 (VNĐ/tháng)")
    price_p90: int = Field(..., description="Giá cận trên P90 (VNĐ/tháng)")
    formatted_p10: str
    formatted_p50: str
    formatted_p90: str
    suggested_interval: str = Field(..., description="Khoảng giá gợi ý chào thuê")
    currency: str = "VND"
    unit: str = "per_month"
    factors: List[Factor] = Field(default_factory=list)
    warnings: List[str] = Field(default_factory=list)
    model_version: str = "20261101-v1"
