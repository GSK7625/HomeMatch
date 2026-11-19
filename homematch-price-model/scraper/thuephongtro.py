"""
ThuePhongTro Scraper Module - Nguồn cào dự phòng cho HomeMatch
"""
import datetime
import logging
import re
from typing import Dict, List, Optional
from urllib.parse import urljoin
from bs4 import BeautifulSoup

from .base import BaseScraper

logger = logging.getLogger("scraper.thuephongtro")


class ThuePhongTroScraper(BaseScraper):
    SOURCE_NAME = "thuephongtro"

    def __init__(self, config_path: str = "config.yaml"):
        super().__init__(config_path)
        self.base_url = self.config["sources"]["thuephongtro"]["base_url"]
        self.provinces = self.config["sources"]["thuephongtro"]["provinces"]

    def crawl_list(self, province_key: str, max_pages: int = 5) -> List[Dict[str, str]]:
        """Duyệt danh sách tin phân trang tại một tỉnh/thành."""
        if province_key not in self.provinces:
            raise ValueError(f"Tỉnh {province_key} không có trong cấu hình: {list(self.provinces.keys())}")

        province_path = self.provinces[province_key]
        collected_items = []
        seen_ids = set()

        for page in range(1, max_pages + 1):
            url = f"{self.base_url}{province_path}"
            if page > 1:
                url = f"{url}?page={page}"

            logger.info(f"[{self.SOURCE_NAME}] Đang quét trang {page}/{max_pages}: {url}")
            html = self.fetch(url)
            if not html:
                logger.warning(f"Không thể tải trang {url}, bỏ qua.")
                continue

            soup = BeautifulSoup(html, "html.parser")
            links = soup.find_all("a", href=True)
            page_count = 0

            for a in links:
                href = a["href"]
                # Định dạng link: ...-<id>.html
                match = re.search(r"-(\d+)\.html$", href)
                if match:
                    source_id = match.group(1)
                    if source_id not in seen_ids and len(source_id) >= 5:
                        seen_ids.add(source_id)
                        full_url = urljoin(self.base_url, href)
                        collected_items.append({"source_id": source_id, "url": full_url})
                        page_count += 1

            logger.info(f"[{self.SOURCE_NAME}] Trang {page}: Tìm thấy {page_count} tin mới.")
            self.polite_sleep()

        logger.info(f"[{self.SOURCE_NAME}] Tổng số tin thu thập được ({province_key}): {len(collected_items)}")
        return collected_items

    def parse_detail(self, html_content: str, url: str, source_id: str) -> Optional[Dict]:
        """Bóc tách thông tin chi tiết một tin đăng từ HTML offline."""
        if not html_content:
            return None

        soup = BeautifulSoup(html_content, "html.parser")

        # 1. Tiêu đề
        h1 = soup.find("h1")
        title = h1.text.strip() if h1 else ""

        # 2. Giá và diện tích
        price_raw = ""
        area_raw = ""

        price_el = soup.select_one(".post-price, span.price")
        if price_el:
            price_raw = price_el.text.strip()

        acreage_el = soup.select_one(".post-acreage, span.acreage")
        if acreage_el:
            area_raw = acreage_el.text.strip()

        # 3. Địa chỉ
        address_raw = ""
        for tag in soup.find_all(lambda t: t.name in ["div", "span", "p"] and "địa chỉ:" in t.text.lower()):
            clean_txt = tag.text.strip().replace("\n", " ")
            if "địa chỉ:" in clean_txt.lower():
                parts = clean_txt.split("Địa chỉ:", 1)
                if len(parts) > 1:
                    address_raw = parts[1].replace("Xem bản đồ", "").strip()
                    break

        # 4. Mô tả
        description = ""
        desc_div = soup.select_one(".post-description, .post-content, .section-content")
        if desc_div:
            description = desc_div.text.strip()

        # Fallback regex nếu thiếu giá/diện tích
        if not price_raw and description:
            m_price = re.search(r"(?:giá\s*(?:thuê)?[:\s]*)(\d+(?:[.,]\d+)?\s*(?:triệu|tr|nghìn|k|đ|vnd))", description, re.IGNORECASE)
            if m_price:
                price_raw = m_price.group(1)

        if not area_raw and description:
            m_area = re.search(r"(?:diện tích[:\s]*)(\d+(?:[.,]\d+)?\s*(?:m2|m²))", description, re.IGNORECASE)
            if m_area:
                area_raw = m_area.group(1)

        return {
            "source": self.SOURCE_NAME,
            "source_id": str(source_id),
            "url": url,
            "title": title,
            "category": "Phòng trọ",
            "price_raw": price_raw,
            "area_raw": area_raw,
            "address_raw": address_raw,
            "district_raw": "",
            "description": description,
            "posted_at": "",
            "crawled_at": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        }
