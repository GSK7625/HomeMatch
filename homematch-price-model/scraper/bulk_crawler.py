"""
Bulk Crawler Module - HomeMatch
Cào dữ liệu quy mô lớn (mục tiêu ~15.000 tin) từ phongtro123.com và thuephongtro.com
với cơ chế streaming append, xoay vòng User-Agent và ghi checkpoint liên tục.
"""
import argparse
import datetime
import json
import logging
from pathlib import Path
import random
import re
import time
from urllib.parse import urljoin
from bs4 import BeautifulSoup
import requests
import yaml

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
logger = logging.getLogger("scraper.bulk")


class BulkCrawler:
    def __init__(self, config_path: str = "config.yaml"):
        with open(config_path, "r", encoding="utf-8") as f:
            self.config = yaml.safe_load(f)

        self.user_agents = self.config.get("crawler", {}).get(
            "user_agents",
            ["Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0"]
        )
        self.interim_jsonl = Path("data/interim/raw_listings.jsonl")
        self.interim_jsonl.parent.mkdir(parents=True, exist_ok=True)

        self.seen_ids = self._load_existing_ids()
        self.session = requests.Session()
        logger.info(f"Đã nạp {len(self.seen_ids)} ID tin đã có từ trước trong raw_listings.jsonl")

    def _load_existing_ids(self) -> set:
        seen = set()
        if self.interim_jsonl.exists():
            with open(self.interim_jsonl, "r", encoding="utf-8") as f:
                for line in f:
                    if line.strip():
                        try:
                            item = json.loads(line)
                            seen.add(f"{item.get('source')}_{item.get('source_id')}")
                        except Exception:
                            pass
        return seen

    def _fetch_html(self, url: str, retries: int = 3) -> str | None:
        for attempt in range(1, retries + 1):
            try:
                headers = {
                    "User-Agent": random.choice(self.user_agents),
                    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
                    "Accept-Language": "vi-VN,vi;q=0.9,en-US;q=0.8",
                }
                resp = self.session.get(url, headers=headers, timeout=15)
                if resp.status_code == 200:
                    return resp.text
                elif resp.status_code in (403, 429):
                    wait = attempt * 8
                    logger.warning(f"HTTP {resp.status_code} tại {url}. Nghỉ {wait}s...")
                    time.sleep(wait)
            except Exception as e:
                time.sleep(attempt * 2)
        return None

    def crawl_phongtro123_page(self, base_path: str, page: int, province_name: str) -> list[dict]:
        url = f"https://phongtro123.com{base_path}"
        if page > 1:
            url = f"{url}?page={page}"

        html = self._fetch_html(url)
        if not html:
            return [], 0

        soup = BeautifulSoup(html, "html.parser")
        items = []

        # Các bài đăng đều có tiêu đề là h3
        cards = soup.find_all("h3")
        for h3 in cards:
            a = h3.find("a", href=True)
            if not a:
                continue

            href = a["href"]
            match = re.search(r"-pr(\d+)\.html", href)
            if not match:
                continue

            sid = match.group(1)
            key = f"phongtro123_{sid}"
            if key in self.seen_ids:
                continue

            title = h3.text.strip()
            full_card = h3.parent.text.strip() if h3.parent else ""

            # Giá
            price_raw = ""
            m_price = re.search(r"(\d+(?:[.,]\d+)?\s*(?:triệu|tr|nghìn|k)\b(?:/tháng)?)", full_card, re.I)
            if m_price:
                price_raw = m_price.group(1)

            # Diện tích
            area_raw = ""
            m_area = re.search(r"(\d+(?:[.,]\d+)?\s*(?:m2|m²))", full_card, re.I)
            if m_area:
                area_raw = m_area.group(1)

            # Địa chỉ / quận
            district_raw = ""
            for tag in (h3.parent.find_all(["span", "div", "a"]) if h3.parent else []):
                t = tag.text.strip()
                if any(k in t for k in ["Quận", "Huyện", "Thủ Đức", "TP."]) and len(t) < 50:
                    district_raw = t
                    break

            # Mô tả tóm tắt
            description = full_card

            item = {
                "source": "phongtro123",
                "source_id": sid,
                "url": urljoin("https://phongtro123.com", href),
                "title": title,
                "category": "Phòng trọ",
                "price_raw": price_raw,
                "area_raw": area_raw,
                "address_raw": f"{district_raw}, {province_name}".strip(", "),
                "district_raw": district_raw,
                "description": description,
                "posted_at": "",
                "crawled_at": datetime.datetime.now(datetime.timezone.utc).isoformat(),
            }
            items.append(item)
            self.seen_ids.add(key)

        return items, len(cards)

    def crawl_thuephongtro_page(self, base_path: str, page: int, province_name: str) -> tuple[list[dict], int]:
        url = f"https://thuephongtro.com{base_path}"
        if page > 1:
            url = f"{url}?page={page}"

        html = self._fetch_html(url)
        if not html:
            return [], 0

        soup = BeautifulSoup(html, "html.parser")
        items = []

        cards = [a for a in soup.find_all("a", href=True) if re.search(r"-(\d+)\.html$", a["href"]) and len(a.text.strip()) > 30]
        for a in cards:
            href = a["href"]
            match = re.search(r"-(\d+)\.html$", href)
            if not match:
                continue

            sid = match.group(1)
            key = f"thuephongtro_{sid}"
            if key in self.seen_ids:
                continue

            full_card = a.text.strip()
            lines = [l.strip() for l in full_card.split("\n") if l.strip()]
            title = next((l for l in lines if len(l) > 10 and not l.isdigit() and not re.search(r"triệu/tháng", l, re.I)), "")

            # Giá
            price_raw = next((l for l in lines if re.search(r"^\d+(?:[.,]\d+)?\s*(?:triệu|tr|nghìn|k)\b", l, re.I)), "")
            if not price_raw:
                m_price = re.search(r"(\d+(?:[.,]\d+)?\s*(?:triệu|tr|nghìn|k)\b(?:/tháng)?)", full_card, re.I)
                if m_price:
                    price_raw = m_price.group(1)

            # Diện tích
            area_raw = next((l for l in lines if re.search(r"^\d+(?:[.,]\d+)?\s*(?:m2|m²)", l, re.I)), "")
            if not area_raw:
                m_area = re.search(r"(\d+(?:[.,]\d+)?\s*(?:m2|m²))", full_card, re.I)
                if m_area:
                    area_raw = m_area.group(1)

            # Địa chỉ / quận
            loc = next((l for l in lines if any(k in l for k in ["Hồ Chí Minh", "Hà Nội", "Đà Nẵng", "Quận", "Huyện"])), "")
            district_raw = loc.split(",")[0].strip() if "," in loc else loc
            address_raw = loc if loc else f"{district_raw}, {province_name}".strip(", ")

            item = {
                "source": "thuephongtro",
                "source_id": sid,
                "url": urljoin("https://thuephongtro.com", href),
                "title": title,
                "category": "Phòng trọ",
                "price_raw": price_raw,
                "area_raw": area_raw,
                "address_raw": address_raw,
                "district_raw": district_raw,
                "description": full_card,
                "posted_at": "",
                "crawled_at": datetime.datetime.now(datetime.timezone.utc).isoformat(),
            }
            items.append(item)
            self.seen_ids.add(key)

        return items, len(cards)

    def run_bulk(self, target_total: int = 15000, max_pages_per_category: int = 250):
        """Chạy cào hàng loạt qua các danh mục của cả 2 nguồn đến khi đạt mục tiêu."""
        logger.info(f"Bắt đầu cào hàng loạt. Mục tiêu: {target_total} tin. Đã có sẵn: {len(self.seen_ids)}")

        categories = [
            # thuephongtro HCM & HN phòng trọ (rất nhiều tin mới)
            ("thuephongtro", "/cho-thue-phong-tro-ho-chi-minh", "Hồ Chí Minh", max_pages_per_category),
            ("thuephongtro", "/cho-thue-phong-tro-ha-noi", "Hà Nội", max_pages_per_category),
            ("thuephongtro", "/cho-thue-can-ho-ho-chi-minh", "Hồ Chí Minh", 100),
            ("thuephongtro", "/cho-thue-can-ho-ha-noi", "Hà Nội", 100),
            ("thuephongtro", "/cho-thue-nha-nguyen-can-ho-chi-minh", "Hồ Chí Minh", 100),
            ("thuephongtro", "/cho-thue-nha-nguyen-can-ha-noi", "Hà Nội", 100),
            # phongtro123 căn hộ & nhà nguyên căn
            ("phongtro123", "/cho-thue-can-ho-ho-chi-minh", "Hồ Chí Minh", 100),
            ("phongtro123", "/cho-thue-can-ho-ha-noi", "Hà Nội", 100),
            ("phongtro123", "/cho-thue-nha-nguyen-can-ho-chi-minh", "Hồ Chí Minh", 100),
            ("phongtro123", "/cho-thue-nha-nguyen-can-ha-noi", "Hà Nội", 100),
            # phongtro123 phòng trọ (đã quét 250 trang trước đó)
            ("phongtro123", "/tinh-thanh/ho-chi-minh", "Hồ Chí Minh", max_pages_per_category),
            ("phongtro123", "/tinh-thanh/ha-noi", "Hà Nội", max_pages_per_category),
        ]

        # Mở file ở chế độ append để không làm mất bất kỳ bản ghi nào đã có
        with open(self.interim_jsonl, "a", encoding="utf-8") as f_out:
            for source, path, prov, max_p in categories:
                if len(self.seen_ids) >= target_total:
                    logger.info(f"Đã đạt mục tiêu {target_total} tin!")
                    break

                logger.info(f"--- Đang cào nguồn [{source}] - {path} ({prov}) tối đa {max_p} trang ---")
                empty_consecutive_pages = 0

                for page in range(1, max_p + 1):
                    if len(self.seen_ids) >= target_total:
                        break

                    if source == "phongtro123":
                        new_items, total_cards = self.crawl_phongtro123_page(path, page, prov)
                    else:
                        new_items, total_cards = self.crawl_thuephongtro_page(path, page, prov)

                    if total_cards == 0:
                        empty_consecutive_pages += 1
                        if empty_consecutive_pages >= 3:
                            logger.info(f"Hết trang thực sự tại {path} (trang {page}), chuyển chuyên mục.")
                            break
                    else:
                        empty_consecutive_pages = 0

                    if new_items:
                        for it in new_items:
                            f_out.write(json.dumps(it, ensure_ascii=False) + "\n")
                        f_out.flush()

                    if page % 10 == 0 or len(self.seen_ids) % 200 == 0 or page == 1:
                        logger.info(f"Tiến độ: Đã tích lũy {len(self.seen_ids)}/{target_total} tin (Trang {page}/{max_p})")

                    time.sleep(random.uniform(0.7, 1.3))

                    # Nghỉ lịch sự giữa các trang
                    time.sleep(random.uniform(0.8, 1.6))

        logger.info(f"CÀO HOÀN TẤT! Tổng số tin tích lũy được: {len(self.seen_ids)} bản ghi tại {self.interim_jsonl}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Bulk Crawler for HomeMatch")
    parser.add_argument("--target", type=int, default=15000, help="Mục tiêu số lượng tin cần cào")
    parser.add_argument("--pages", type=int, default=250, help="Số trang tối đa mỗi danh mục")

    args = parser.parse_args()
    crawler = BulkCrawler()
    crawler.run_bulk(target_total=args.target, max_pages_per_category=args.pages)
