"""
Runner CLI cho Scraper - HomeMatch
Cú pháp:
    python -m scraper.crawl --source phongtro123 --province ho-chi-minh --pages 5
    python -m scraper.crawl --source thuephongtro --province ha-noi --pages 5
    python -m scraper.crawl --parse-only
"""
import argparse
import json
import logging
from pathlib import Path
from tqdm import tqdm

from .phongtro123 import PhongTro123Scraper
from .thuephongtro import ThuePhongTroScraper

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
logger = logging.getLogger("scraper.main")


def get_scraper(source_name: str):
    if source_name == "phongtro123":
        return PhongTro123Scraper()
    elif source_name == "thuephongtro":
        return ThuePhongTroScraper()
    else:
        raise ValueError(f"Nguồn không hợp lệ: {source_name}. Chọn 'phongtro123' hoặc 'thuephongtro'.")


from concurrent.futures import ThreadPoolExecutor, as_completed

def run_pipeline(source: str, province: str, pages: int, workers: int = 3, parse_only: bool = False):
    scraper = get_scraper(source)
    interim_jsonl = scraper.interim_dir / "raw_listings.jsonl"

    # Nạp các ID đã parse trước đó để tránh ghi đè lặp lại
    existing_records = {}
    if interim_jsonl.exists():
        with open(interim_jsonl, "r", encoding="utf-8") as f:
            for line in f:
                if line.strip():
                    item = json.loads(line)
                    existing_records[f"{item['source']}_{item['source_id']}"] = item

    if not parse_only:
        provinces_to_crawl = [province] if province != "all" else ["ho-chi-minh", "ha-noi"]

        all_items = []
        for prov in provinces_to_crawl:
            items = scraper.crawl_list(prov, max_pages=pages)
            all_items.extend(items)

        logger.info(f"Bắt đầu tải HTML chi tiết cho {len(all_items)} tin với {workers} workers...")

        def download_item(item):
            sid = item["source_id"]
            url = item["url"]
            cached = scraper.get_cached_html(source, sid)
            if not cached:
                html = scraper.fetch(url)
                if html:
                    scraper.save_raw_html(source, sid, html)
                scraper.polite_sleep()
            return sid

        with ThreadPoolExecutor(max_workers=workers) as executor:
            futures = [executor.submit(download_item, item) for item in all_items]
            for _ in tqdm(as_completed(futures), total=len(futures), desc="Downloading Detail HTML"):
                pass

    # Bước Parse: Quét tất cả file HTML thô trong data/raw
    raw_files = list(scraper.raw_dir.glob(f"{source}_*.html"))
    logger.info(f"Tiến hành parse {len(raw_files)} file HTML thô của nguồn '{source}'...")

    new_count = 0
    with open(interim_jsonl, "w", encoding="utf-8") as f_out:
        for fpath in tqdm(raw_files, desc="Parsing HTML"):
            fname = fpath.stem
            source_id = fname.split("_")[-1]

            html_content = fpath.read_text(encoding="utf-8")
            detail = scraper.parse_detail(html_content, url="", source_id=source_id)

            if detail and (detail.get("price_raw") or detail.get("title")):
                record_key = f"{source}_{source_id}"
                existing_records[record_key] = detail
                new_count += 1

        # Ghi lại toàn bộ dữ liệu đã dedupe vào JSONL
        for rec in existing_records.values():
            f_out.write(json.dumps(rec, ensure_ascii=False) + "\n")

    logger.info(f"Hoàn tất! Tổng cộng {len(existing_records)} bản ghi trong: {interim_jsonl}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="HomeMatch Rental Price Scraper")
    parser.add_argument("--source", type=str, default="phongtro123", choices=["phongtro123", "thuephongtro"])
    parser.add_argument("--province", type=str, default="all", choices=["ho-chi-minh", "ha-noi", "all"])
    parser.add_argument("--pages", type=int, default=15, help="Số trang phân tầng cần quét mỗi tỉnh")
    parser.add_argument("--workers", type=int, default=3, help="Số luồng tải đồng thời")
    parser.add_argument("--parse-only", action="store_true", help="Chỉ parse lại các file HTML đã tải mà không cào mạng")

    args = parser.parse_args()
    run_pipeline(args.source, args.province, args.pages, args.workers, args.parse_only)
