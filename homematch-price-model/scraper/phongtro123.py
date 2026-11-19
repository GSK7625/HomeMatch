"""
PhongTro123 Scraper Module - Nguồn cào chính cho HomeMatch
"""
import datetime
import logging
import re
from typing import Dict, List, Optional
from urllib.parse import urljoin
from bs4 import BeautifulSoup

from .base import BaseScraper

logger = logging.getLogger("scraper.phongtro123")


class PhongTro123Scraper(BaseScraper):
    SOURCE_NAME = "phongtro123"

    def __init__(self, config_path: str = "config.yaml"):
        super().__init__(config_path)
        self.base_url = self.config["sources"]["phongtro123"]["base_url"]
        self.provinces = self.config["sources"]["phongtro123"]["provinces"]

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
            # Tìm tất cả link chi tiết có định dạng pr<ID>.html
            links = soup.find_all("a", href=True)
            page_count = 0

            for a in links:
                href = a["href"]
                match = re.search(r"-pr(\d+)\.html", href)
                if match:
                    source_id = match.group(1)
                    if source_id not in seen_ids:
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

        # 2. Giá và diện tích từ các thẻ nổi bật
        price_raw = ""
        area_raw = ""

        # Tìm các thẻ span/div chứa giá (text-green, fs-5)
        price_el = soup.select_one("span.text-green.fs-5, span.fs-5.fw-bold.text-green, div.d-flex span.text-green")
        if price_el:
            price_raw = price_el.text.strip()

        # Tìm diện tích
        area_candidates = soup.find_all(lambda tag: tag.name in ["span", "div", "b"] and ("m2" in tag.text.lower() or "m²" in tag.text.lower()) and len(tag.text.strip()) < 30)
        for cand in area_candidates:
            text = cand.text.strip()
            # Bỏ qua nếu thẻ dính thông tin giá tiền
            if any(w in text.lower() for w in ["triệu", "tr/", "vnđ", "đồng", "tháng"]):
                continue
            m = re.search(r"(\d+(?:[.,]\d+)?\s*(?:m2|m²))", text, re.IGNORECASE)
            if m:
                area_raw = m.group(1)
                break

        # 3. Bảng tổng quan thông tin
        address_raw = ""
        posted_at = ""
        category = "Phòng trọ"
        district_raw = ""

        overview_table = soup.find("table")
        if overview_table:
            for tr in overview_table.find_all("tr"):
                text = tr.text.strip()
                t_lower = text.lower()
                if "địa chỉ:" in t_lower:
                    address_raw = text.split(":", 1)[-1].strip()
                elif "ngày đăng:" in t_lower:
                    posted_at = text.split(":", 1)[-1].strip()
                elif "quận huyện:" in t_lower:
                    district_raw = text.split(":", 1)[-1].strip()
                elif "chuyên mục:" in t_lower or "loại tin:" in t_lower:
                    category = text.split(":", 1)[-1].strip()

        # 4. Nội dung mô tả (nằm dưới h2 Thông tin mô tả)
        description = ""
        desc_h2 = soup.find(lambda tag: tag.name in ["h2", "h3"] and "mô tả" in tag.text.lower())
        if desc_h2:
            next_sibling = desc_h2.find_next_sibling()
            if next_sibling:
                description = next_sibling.text.strip()

        if not description:
            # Fallback tìm div có class liên quan
            desc_div = soup.select_one(".post-description, .section-content, .post-content")
            if desc_div:
                description = desc_div.text.strip()

        # Nếu chưa lấy được giá/diện tích từ thẻ, quét thêm từ text mô tả
        if not price_raw:
            m_price = re.search(r"(?:giá\s*(?:thuê)?[:\s]*)(\d+(?:[.,]\d+)?\s*(?:triệu|tr|nghìn|k|đ|vnd)(?:/tháng)?)", description, re.IGNORECASE)
            if m_price:
                price_raw = m_price.group(1)

        if not area_raw:
            m_area = re.search(r"(?:diện tích[:\s]*)(\d+(?:[.,]\d+)?\s*(?:m2|m²))", description, re.IGNORECASE)
            if m_area:
                area_raw = m_area.group(1)

        return {
            "source": self.SOURCE_NAME,
            "source_id": str(source_id),
            "url": url,
            "title": title,
            "category": category,
            "price_raw": price_raw,
            "area_raw": area_raw,
            "address_raw": address_raw,
            "district_raw": district_raw,
            "description": description,
            "posted_at": posted_at,
            "crawled_at": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        }
