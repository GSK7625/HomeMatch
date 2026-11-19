"""
Geocoding & Spatial Precision Module - HomeMatch
Chuyển đổi địa chỉ sang tọa độ (lat, lng), cache cục bộ, fallback tâm quận/phường và kiểm tra bounding box.
"""
import hashlib
import json
import logging
import re
import time
from pathlib import Path
from typing import Dict, Optional, Tuple
import requests
import yaml

from .clean import remove_accents

logger = logging.getLogger("pipeline.geocode")


class GeocodingService:
    def __init__(self, config_path: str = "config.yaml"):
        with open(config_path, "r", encoding="utf-8") as f:
            self.config = yaml.safe_load(f)

        self.cache_file = Path("data/interim/geocode_cache.json")
        self.cache_file.parent.mkdir(parents=True, exist_ok=True)
        self.cache: Dict[str, Dict] = self._load_cache()
        self.bbox_cfg = self.config.get("geo_bounding_box", {})

    def _load_cache(self) -> Dict:
        if self.cache_file.exists():
            try:
                with open(self.cache_file, "r", encoding="utf-8") as f:
                    return json.load(f)
            except Exception as e:
                logger.warning(f"Lỗi đọc geocode cache: {e}")
        return {}

    def _save_cache(self):
        try:
            with open(self.cache_file, "w", encoding="utf-8") as f:
                json.dump(self.cache, f, ensure_ascii=False, indent=2)
        except Exception as e:
            logger.warning(f"Lỗi ghi geocode cache: {e}")

    def is_within_bbox(self, lat: float, lng: float, province: str) -> bool:
        """Kiểm tra tọa độ có nằm trong hộp bao của tỉnh thành hay không."""
        p_key = "hanoi" if "hà nội" in province.lower() else "ho_chi_minh"
        box = self.bbox_cfg.get(p_key)
        if not box:
            return True

        return (
            box["lat_min"] <= lat <= box["lat_max"]
            and box["lng_min"] <= lng <= box["lng_max"]
        )

    def _call_nominatim(self, query: str) -> Optional[Tuple[float, float]]:
        """Gọi Nominatim OpenStreetMap với User-Agent hợp lệ (giới hạn 1 request/giây)."""
        url = "https://nominatim.openstreetmap.org/search"
        headers = {"User-Agent": "HomeMatchRentalPricingModel/1.0 (contact: research@homematch.local)"}
        params = {"q": query, "format": "json", "limit": 1}

        try:
            time.sleep(1.0)  # Tuân thủ giới hạn 1 req/giây của Nominatim
            resp = requests.get(url, params=params, headers=headers, timeout=10)
            if resp.status_code == 200:
                data = resp.json()
                if data:
                    lat = float(data[0]["lat"])
                    lng = float(data[0]["lon"])
                    return lat, lng
        except Exception as e:
            logger.debug(f"Nominatim lỗi với query '{query}': {e}")
        return None

    # Bảng tọa độ tâm quận/huyện chuẩn của Hà Nội và TP.HCM để ánh xạ tức thì (offline lookup)
    DISTRICT_CENTROIDS = {
        # Hà Nội
        ("ba dinh", "ha noi"): (21.0341, 105.8242),
        ("hoan kiem", "ha noi"): (21.0307, 105.8524),
        ("tay ho", "ha noi"): (21.0713, 105.8234),
        ("long bien", "ha noi"): (21.0361, 105.8942),
        ("cau giay", "ha noi"): (21.0313, 105.7925),
        ("dong da", "ha noi"): (21.0181, 105.8299),
        ("hai ba trung", "ha noi"): (21.0065, 105.8544),
        ("hoang mai", "ha noi"): (20.9754, 105.8523),
        ("thanh xuan", "ha noi"): (20.9937, 105.8118),
        ("soc son", "ha noi"): (21.2828, 105.8475),
        ("dong anh", "ha noi"): (21.1444, 105.8456),
        ("gia lam", "ha noi"): (21.0347, 105.9427),
        ("nam tu liem", "ha noi"): (21.0152, 105.7656),
        ("bac tu liem", "ha noi"): (21.0631, 105.7592),
        ("thanh tri", "ha noi"): (20.9482, 105.8491),
        ("ha dong", "ha noi"): (20.9632, 105.7661),
        ("son tay", "ha noi"): (21.1352, 105.5039),
        ("ba vi", "ha noi"): (21.1969, 105.3908),
        ("phuc tho", "ha noi"): (21.1072, 105.5714),
        ("dan phuong", "ha noi"): (21.1039, 105.6706),
        ("hoai duc", "ha noi"): (21.0183, 105.7067),
        ("quoc oai", "ha noi"): (20.9936, 105.6322),
        ("thach that", "ha noi"): (21.0108, 105.5567),
        ("chuong my", "ha noi"): (20.8972, 105.7061),
        ("thanh oai", "ha noi"): (20.8683, 105.7831),
        ("thuong tin", "ha noi"): (20.8719, 105.8622),
        ("phu xuyen", "ha noi"): (20.7381, 105.9083),
        ("ung hoa", "ha noi"): (20.7247, 105.7867),
        ("my duc", "ha noi"): (20.6781, 105.7369),
        ("me linh", "ha noi"): (21.1736, 105.7139),
        # TP. Hồ Chí Minh
        ("quan 1", "ho chi minh"): (10.7756, 106.7004),
        ("quan 3", "ho chi minh"): (10.7844, 106.6843),
        ("quan 4", "ho chi minh"): (10.7578, 106.7013),
        ("quan 5", "ho chi minh"): (10.7540, 106.6634),
        ("quan 6", "ho chi minh"): (10.7481, 106.6352),
        ("quan 7", "ho chi minh"): (10.7340, 106.7218),
        ("quan 8", "ho chi minh"): (10.7241, 106.6286),
        ("quan 10", "ho chi minh"): (10.7674, 106.6669),
        ("quan 11", "ho chi minh"): (10.7629, 106.6504),
        ("quan 12", "ho chi minh"): (10.8672, 106.6413),
        ("binh thanh", "ho chi minh"): (10.8106, 106.7091),
        ("go vap", "ho chi minh"): (10.8388, 106.6653),
        ("phu nhuan", "ho chi minh"): (10.7992, 106.6803),
        ("tan binh", "ho chi minh"): (10.8015, 106.6526),
        ("tan phu", "ho chi minh"): (10.7901, 106.6281),
        ("binh tan", "ho chi minh"): (10.7654, 106.6038),
        ("thu duc", "ho chi minh"): (10.8494, 106.7537),
        ("quan 2", "ho chi minh"): (10.7872, 106.7498),
        ("quan 9", "ho chi minh"): (10.8428, 106.7917),
        ("nha be", "ho chi minh"): (10.6953, 106.7347),
        ("hoc mon", "ho chi minh"): (10.8841, 106.5913),
        ("binh chanh", "ho chi minh"): (10.6874, 106.5939),
        ("cu chi", "ho chi minh"): (11.0067, 106.4946),
        ("can gio", "ho chi minh"): (10.4114, 106.9546),
    }

    def geocode_address(self, address_raw: str, province: str, district: str, ward: str, enable_online: bool = False) -> Dict:
        """
        Geocode địa chỉ với cơ chế Fallback tầng siêu tốc:
        1. Kiểm tra cache cục bộ
        2. Tra cứu tức thì từ bảng tâm Quận/Huyện offline (không cần mạng)
        3. Nếu bật enable_online=True thì mới fallback gọi Nominatim OpenStreetMap
        """
        cache_key = address_raw.strip().lower()
        if cache_key in self.cache:
            return self.cache[cache_key]

        # Tra cứu nhanh offline tâm quận
        c_dist = remove_accents(district).lower().strip()
        c_prov = "ha noi" if "ha noi" in remove_accents(province).lower() else "ho chi minh"
        if (c_dist, c_prov) in self.DISTRICT_CENTROIDS:
            lat, lng = self.DISTRICT_CENTROIDS[(c_dist, c_prov)]
            res = {"lat": lat, "lng": lng, "geo_precision": "district"}
            self.cache[cache_key] = res
            return res

        # Fallback online nếu được kích hoạt
        if enable_online:
            if ward and ward != "UNKNOWN":
                query_ward = f"{ward}, {district}, {province}, Việt Nam"
                coords = self._call_nominatim(query_ward)
                if coords and self.is_within_bbox(coords[0], coords[1], province):
                    res = {"lat": coords[0], "lng": coords[1], "geo_precision": "ward"}
                    self.cache[cache_key] = res
                    self._save_cache()
                    return res

        # Mặc định tâm tỉnh thành
        default_coords = (21.0285, 105.8542) if c_prov == "ha noi" else (10.8231, 106.6297)
        res = {"lat": default_coords[0], "lng": default_coords[1], "geo_precision": "province"}
        self.cache[cache_key] = res
        return res

    @staticmethod
    def generate_building_key(address_raw: str, district: str, province: str) -> str:
        """
        Tạo building_key duy nhất từ địa chỉ chuẩn hóa (đã loại bỏ số phòng)
        để phục vụ chia nhóm GroupShuffleSplit chống rò rỉ dữ liệu.
        """
        clean_addr = remove_accents(address_raw)
        # Loại bỏ các từ khóa số phòng/tầng
        clean_addr = re.sub(r"\b(phong|p\.|tang|lau)\s*\w+\b", "", clean_addr)
        normalized = f"{clean_addr.strip()}_{remove_accents(district)}_{remove_accents(province)}"
        return hashlib.md5(normalized.encode("utf-8")).hexdigest()[:16]
