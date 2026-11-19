"""
Data Cleaning & Schema Normalization Module - HomeMatch
Chuẩn hóa giá, diện tích, phân loại phòng, trích xuất tiện ích khớp Database HomeMatch.
"""
import hashlib
import json
import logging
import re
import unicodedata
from typing import Dict, Optional, Tuple
import numpy as np
import pandas as pd
import yaml

logger = logging.getLogger("pipeline.clean")


def remove_accents(s: str) -> str:
    """Loại bỏ dấu tiếng Việt để so khớp từ khóa dễ dàng hơn."""
    if not s:
        return ""
    s = unicodedata.normalize("NFD", s.lower())
    s = "".join(c for c in s if unicodedata.category(c) != "Mn").replace("đ", "d")
    return s


def parse_price(s: str) -> Optional[int]:
    """
    Chuẩn hóa chuỗi giá sang số nguyên VND/tháng.
    Ví dụ:
        '2,5 triệu/tháng' -> 2_500_000
        '2.5 tr'          -> 2_500_000
        '800 nghìn'       -> 800_000
        '800k'            -> 800_000
        '3.200.000đ'      -> 3_200_000
        '3,200,000'       -> 3_200_000
    """
    if not s or not isinstance(s, str):
        return None

    t = s.lower().strip().replace(",", ".")

    # Trường hợp 1: Có đơn vị triệu / tr / nghìn / k
    m = re.search(r"(\d+(?:\.\d+)?)\s*(triệu|tr|nghìn|ngan|k)\b", t)
    if m:
        val = float(m.group(1))
        unit = m.group(2)
        if unit in ("triệu", "tr"):
            return int(round(val * 1_000_000))
        elif unit in ("nghìn", "ngan", "k"):
            return int(round(val * 1_000))

    # Trường hợp 2: Định dạng số đầy đủ (ví dụ: 3.500.000, 3500000)
    digits = re.sub(r"[^\d]", "", s)
    if digits:
        val = int(digits)
        # Nếu nhập tắt như 3500 (nghĩa là 3.5 triệu)
        if 500 <= val <= 50000:
            return val * 1_000
        if 500_000 <= val <= 200_000_000:
            return val

    return None


def parse_area(s: str) -> Optional[float]:
    """
    Chuẩn hóa chuỗi diện tích sang float (m²).
    Ví dụ: '25 m2' -> 25.0 ; '30,5 m²' -> 30.5 ; '2.5 triệu/tháng 25 m2' -> 25.0
    """
    if not s or not isinstance(s, str):
        return None

    s_clean = s.lower().replace(",", ".")

    # Ưu tiên tìm số đứng ngay trước m2 hoặc m²
    m = re.search(r"(\d+(?:\.\d+)?)\s*(?:m2|m²)", s_clean)
    if m:
        try:
            return float(m.group(1))
        except ValueError:
            pass

    # Nếu chuỗi chỉ thuần số (ví dụ: '25')
    m_pure = re.search(r"^\s*(\d+(?:\.\d+)?)\s*$", s_clean)
    if m_pure:
        try:
            return float(m_pure.group(1))
        except ValueError:
            pass

    return None


def map_room_type(title: str, category: str, description: str = "") -> str:
    """
    Ánh xạ loại phòng về đúng CHECK constraints của DB HomeMatch:
    'STUDIO', '1B1B', '2B1B', 'DORMITORY', 'ROOM'
    """
    text = remove_accents(f"{title} {category} {description}")

    if any(k in text for k in ["ky tuc xa", "ktx", "sleepbox", "sleep box", "giuong tang"]):
        return "DORMITORY"
    if any(k in text for k in ["studio", "chung cu mini", "ccmn"]):
        return "STUDIO"
    if any(k in text for k in ["2 phong ngu", "2pn", "2 pn", "2b1b", "2b2b"]):
        return "2B1B"
    if any(k in text for k in ["1 phong ngu", "1pn", "1 pn", "1b1b"]):
        return "1B1B"

    return "ROOM"


def map_property_type(title: str, category: str) -> str:
    """
    Ánh xạ loại bất động sản về đúng CHECK constraints của DB HomeMatch:
    'APARTMENT', 'BOARDING_HOUSE', 'SERVICED_APARTMENT', 'HOMESTAY'
    """
    text = remove_accents(f"{title} {category}")

    if any(k in text for k in ["can ho dich vu", "chdv", "serviced apartment"]):
        return "SERVICED_APARTMENT"
    if any(k in text for k in ["chung cu", "can ho", "apartment"]):
        return "APARTMENT"
    if any(k in text for k in ["homestay"]):
        return "HOMESTAY"

    return "BOARDING_HOUSE"


# Từ khóa tiện ích khớp với bảng properties.amenities
AMENITY_KEYWORDS = {
    "AIR_CONDITIONER": ["dieu hoa", "may lanh"],
    "WATER_HEATER": ["nong lanh", "binh nong lanh", "nuoc nong"],
    "WASHING_MACHINE": ["may giat"],
    "SMART_LOCK": ["khoa van tay", "khoa thong minh", "khoa tu"],
    "PRIVATE_WC": ["wc rieng", "ve sinh khep kin", "toilet rieng", "ve sinh rieng", "khep kin"],
    "BED": ["giuong"],
    "WARDROBE": ["tu quan ao", "tu do", "tu quan ao"],
    "KITCHEN": ["bep", "khu nau an", "ke bep", "tu bep"],
    "BALCONY": ["ban cong", "logia"],
    "ELEVATOR": ["thang may"],
    "PARKING": ["cho de xe", "gui xe", "ham xe", "cho dau xe"],
    "LOFT": ["gac lung", "gac xep", "gac"],
    "WINDOW": ["cua so", "thoang mat"],
    "WIFI": ["wifi", "internet", "mang"],
    "FRIDGE": ["tu lanh"],
    "FURNISHED_FULL": ["full noi that", "day du noi that", "full do"],
}

NEGATION_WORDS = ["khong", "ko", "chua", "chua co", "ko co", "khong co"]


def extract_amenities(text: str) -> Dict[str, int]:
    """
    Trích xuất danh sách tiện ích nhị phân từ mô tả, có xử lý bẫy phủ định.
    Ví dụ: 'không có điều hòa' sẽ không bị đánh dấu là có điều hòa.
    """
    if not text:
        return {f"am_{k}": 0 for k in AMENITY_KEYWORDS}

    t_norm = remove_accents(text)
    # Loại bỏ dấu câu (phẩy, chấm, chấm phẩy, gạch đầu dòng) thành khoảng trắng
    t_clean = re.sub(r"[^\w\s]", " ", t_norm)
    words = t_clean.split()
    results = {}

    for code, keywords in AMENITY_KEYWORDS.items():
        has_amenity = 0
        for kw in keywords:
            kw_words = kw.split()
            kw_len = len(kw_words)

            # Tìm kiếm từ khóa theo chuỗi
            for i in range(len(words) - kw_len + 1):
                if words[i : i + kw_len] == kw_words:
                    # Kiểm tra 3 từ đứng trước có từ phủ định hay không
                    preceding = words[max(0, i - 3) : i]
                    if any(neg in preceding for neg in NEGATION_WORDS):
                        continue  # Bị phủ định -> bỏ qua
                    has_amenity = 1
                    break
            if has_amenity:
                break
        results[f"am_{code}"] = has_amenity

    return results


HANOI_DISTRICTS = [
    "Ba Đình", "Hoàn Kiếm", "Tây Hồ", "Long Biên", "Cầu Giấy", "Đống Đa", "Hai Bà Trưng",
    "Hoàng Mai", "Thanh Xuân", "Sóc Sơn", "Đông Anh", "Gia Lâm", "Nam Từ Liêm", "Bắc Từ Liêm",
    "Thanh Trì", "Mê Linh", "Hà Đông", "Sơn Tây", "Ba Vì", "Phúc Thọ", "Đan Phượng", "Hoài Đức",
    "Quốc Oai", "Thạch Thất", "Chương Mỹ", "Thanh Oai", "Thường Tín", "Phú Xuyên", "Ứng Hòa", "Mỹ Đức"
]

HCM_DISTRICTS = [
    "Quận 1", "Quận 2", "Quận 3", "Quận 4", "Quận 5", "Quận 6", "Quận 7", "Quận 8", "Quận 9",
    "Quận 10", "Quận 11", "Quận 12", "Bình Thạnh", "Gò Vấp", "Tân Bình", "Tân Phú", "Phú Nhuận",
    "Bình Tân", "Thủ Đức", "Bình Chánh", "Hóc Môn", "Củ Chi", "Nhà Bè", "Cần Giờ"
]


def parse_address_components(address_raw: str, district_raw: str = "", text_fallback: str = "") -> Tuple[str, str, str]:
    """
    Tách tỉnh/thành, quận/huyện, phường/xã từ chuỗi địa chỉ, district_raw hoặc text tóm tắt.
    Trả về: (province, district, ward)
    """
    district = "UNKNOWN"
    ward = "UNKNOWN"

    # 1. Ưu tiên bóc tách từ district_raw nếu có
    if district_raw:
        d_clean = district_raw
        for prefix in ["cho thuê phòng trọ", "cho thuê căn hộ dịch vụ", "cho thuê căn hộ", "cho thuê nhà trọ", "cho thuê nhà", "cho thuê mặt bằng", "cho thuê"]:
            d_clean = re.sub(re.escape(prefix), "", d_clean, flags=re.IGNORECASE)
        d_clean = d_clean.strip()
        if d_clean and d_clean.lower() not in ["hồ chí minh", "hà nội"]:
            district = d_clean

    # 2. Bóc tách từ address_raw
    if district == "UNKNOWN" and address_raw:
        parts = [p.strip() for p in address_raw.split(",") if p.strip()]
        for part in reversed(parts):
            p_norm = remove_accents(part)
            if district == "UNKNOWN" and any(w in p_norm for w in ["quan ", "huyen ", "thanh pho ", "tp.", "q.", "thu duc"]):
                district = part
            elif ward == "UNKNOWN" and any(w in p_norm for w in ["phuong ", "xa ", "thi tran ", "p."]):
                ward = part

    # 3. Fallback trích xuất từ văn bản tóm tắt (description / title)
    if district == "UNKNOWN" and text_fallback:
        m = re.search(r"(?:m2|m²)\s*([A-Za-zÀ-ỹ0-9\s]+?),\s*(Hồ Chí Minh|Hà Nội)", text_fallback)
        if m:
            district = m.group(1).strip()
        else:
            for dist in (HANOI_DISTRICTS + HCM_DISTRICTS):
                if re.search(r"\b" + re.escape(dist) + r"\b", text_fallback, re.IGNORECASE):
                    district = dist
                    break

    # Chuẩn hóa viết tắt tên quận
    if district != "UNKNOWN":
        district = re.sub(r"^[Qq]\.\s*", "Quận ", district)
        district = re.sub(r"^[Qq](\d+)", r"Quận \1", district)
        if any(district.lower() == k for k in ["thu duc", "thủ đức", "tp. thủ đức", "thành phố thủ đức"]):
            district = "Thành phố Thủ Đức"
        elif district in ["Bình Thạnh", "Gò Vấp", "Tân Bình", "Tân Phú", "Phú Nhuận", "Bình Tân"]:
            district = f"Quận {district}"
        elif district in ["Hà Đông", "Cầu Giấy", "Đống Đa", "Ba Đình", "Hoàn Kiếm", "Hai Bà Trưng", "Hoàng Mai", "Thanh Xuân", "Tây Hồ", "Long Biên", "Nam Từ Liêm", "Bắc Từ Liêm"]:
            district = f"Quận {district}"
        elif district in ["Gia Lâm", "Đông Anh", "Sóc Sơn", "Thanh Trì", "Mê Linh", "Hoài Đức", "Đan Phượng", "Bình Chánh", "Hóc Môn", "Củ Chi", "Nhà Bè", "Cần Giờ"]:
            district = f"Huyện {district}"

    if ward != "UNKNOWN":
        ward = re.sub(r"^[Pp]\.\s*", "Phường ", ward)
        ward = re.sub(r"^[Pp](\d+)", r"Phường \1", ward)

    # Xác định tỉnh/thành
    full_text = f"{address_raw} {district_raw} {text_fallback}".lower()
    if any(k in full_text for k in ["hà nội", "ha noi", "cầu giấy", "đống đa", "thanh xuân", "hai bà trưng", "nam từ liêm", "bắc từ liêm", "hoàng mai", "ba đình", "hà đông", "tây hồ", "long biên"]):
        province = "Hà Nội"
    else:
        province = "Hồ Chí Minh"

    return (province, district, ward)


def drop_outliers(df: pd.DataFrame, config_path: str = "config.yaml") -> pd.DataFrame:
    """Lọc bỏ dữ liệu ngoài phạm vi hợp lý và ngoại lệ giá/m²."""
    with open(config_path, "r", encoding="utf-8") as f:
        cfg = yaml.safe_load(f)["cleaning"]

    p_min, p_max = cfg["price_min"], cfg["price_max"]
    a_min, a_max = cfg["area_min"], cfg["area_max"]
    q_lo, q_hi = cfg["ppm2_quantiles"]

    # 1. Lọc miền giá trị cứng
    valid_mask = (df["price"] >= p_min) & (df["price"] <= p_max) & (df["area_sqm"] >= a_min) & (df["area_sqm"] <= a_max)
    df = df[valid_mask].copy()

    # 2. Lọc ngoại lệ giá/m2 theo từng (district, room_type)
    df["ppm2"] = df["price"] / df["area_sqm"]
    groups = df.groupby(["district", "room_type"])["ppm2"]

    # Áp dụng quantile nếu nhóm có đủ >= 10 mẫu
    df["lo"] = groups.transform(lambda s: s.quantile(q_lo) if len(s) >= 10 else 0)
    df["hi"] = groups.transform(lambda s: s.quantile(q_hi) if len(s) >= 10 else 1e9)

    clean_df = df[(df["ppm2"] >= df["lo"]) & (df["ppm2"] <= df["hi"])].drop(columns=["lo", "hi", "ppm2"])
    return clean_df
