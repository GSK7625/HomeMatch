"""
Base Scraper Module - HomeMatch Price Model
Hỗ trợ cào dữ liệu lịch sự (polite crawl), retry backoff, xoay vòng User-Agent và lưu checkpoint.
"""
from abc import ABC, abstractmethod
import json
import logging
import os
from pathlib import Path
import random
import time
from typing import Dict, List, Optional
import requests
import yaml

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
logger = logging.getLogger("scraper.base")


class BaseScraper(ABC):
    def __init__(self, config_path: str = "config.yaml"):
        with open(config_path, "r", encoding="utf-8") as f:
            self.config = yaml.safe_load(f)

        self.crawler_cfg = self.config.get("crawler", {})
        self.user_agents = self.crawler_cfg.get(
            "user_agents",
            [
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36"
            ],
        )
        self.timeout = self.crawler_cfg.get("timeout_seconds", 20)
        self.delay_range = self.crawler_cfg.get("delay_range", [1.0, 3.0])
        self.max_retries = self.crawler_cfg.get("max_retries", 3)
        self.backoff_factor = self.crawler_cfg.get("backoff_factor", 2.0)

        # Đường dẫn lưu dữ liệu
        paths_cfg = self.config.get("paths", {})
        self.raw_dir = Path(paths_cfg.get("data_raw", "data/raw"))
        self.interim_dir = Path(paths_cfg.get("data_interim", "data/interim"))
        self.processed_dir = Path(paths_cfg.get("data_processed", "data/processed"))

        self.raw_dir.mkdir(parents=True, exist_ok=True)
        self.interim_dir.mkdir(parents=True, exist_ok=True)
        self.processed_dir.mkdir(parents=True, exist_ok=True)

        self.checkpoint_file = self.interim_dir / "crawled_checkpoints.json"
        self.crawled_ids = self._load_checkpoints()
        self.session = requests.Session()

    def _load_checkpoints(self) -> set:
        if self.checkpoint_file.exists():
            try:
                with open(self.checkpoint_file, "r", encoding="utf-8") as f:
                    data = json.load(f)
                    return set(data)
            except Exception as e:
                logger.warning(f"Không thể đọc checkpoint: {e}")
        return set()

    def _save_checkpoint(self, listing_id: str):
        self.crawled_ids.add(listing_id)
        try:
            with open(self.checkpoint_file, "w", encoding="utf-8") as f:
                json.dump(list(self.crawled_ids), f, ensure_ascii=False)
        except Exception as e:
            logger.warning(f"Lỗi ghi checkpoint: {e}")

    def get_random_headers(self) -> Dict[str, str]:
        return {
            "User-Agent": random.choice(self.user_agents),
            "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
            "Accept-Language": "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7",
            "Connection": "keep-alive",
        }

    def fetch(self, url: str) -> Optional[str]:
        """Tải HTML từ URL với chính sách retry và exponential backoff."""
        for attempt in range(1, self.max_retries + 1):
            try:
                headers = self.get_random_headers()
                response = self.session.get(url, headers=headers, timeout=self.timeout)

                if response.status_code == 200:
                    return response.text
                elif response.status_code in (403, 429):
                    wait_sec = (self.backoff_factor**attempt) * 10
                    logger.warning(
                        f"Gặp HTTP {response.status_code} tại {url}. Nghỉ {wait_sec:.1f}s trước khi thử lại..."
                    )
                    time.sleep(wait_sec)
                else:
                    logger.warning(
                        f"HTTP {response.status_code} tại {url} (lần {attempt}/{self.max_retries})"
                    )
                    time.sleep(self.backoff_factor**attempt)
            except requests.RequestException as e:
                logger.error(f"Lỗi kết nối khi tải {url}: {e} (lần {attempt}/{self.max_retries})")
                time.sleep(self.backoff_factor**attempt)

        logger.error(f"Thất bại hoàn toàn sau {self.max_retries} lần thử cho {url}")
        return None

    def polite_sleep(self):
        """Nghỉ ngẫu nhiên giữa các request để bảo vệ server nguồn."""
        sleep_time = random.uniform(self.delay_range[0], self.delay_range[1])
        time.sleep(sleep_time)

    def save_raw_html(self, source: str, listing_id: str, html_content: str) -> Path:
        """Lưu HTML thô vào thư mục data/raw."""
        file_path = self.raw_dir / f"{source}_{listing_id}.html"
        file_path.write_text(html_content, encoding="utf-8")
        self._save_checkpoint(listing_id)
        return file_path

    def get_cached_html(self, source: str, listing_id: str) -> Optional[str]:
        file_path = self.raw_dir / f"{source}_{listing_id}.html"
        if file_path.exists():
            return file_path.read_text(encoding="utf-8")
        return None

    @abstractmethod
    def crawl_list(self, province_key: str, max_pages: int = 5) -> List[Dict[str, str]]:
        """
        Cào danh sách các tin phòng theo tỉnh/thành.
        Trả về list dict: [{"url": ..., "source_id": ...}]
        """
        pass

    @abstractmethod
    def parse_detail(self, html_content: str, url: str, source_id: str) -> Optional[Dict]:
        """
        Bóc tách thông tin chi tiết của một tin đăng từ HTML.
        """
        pass
