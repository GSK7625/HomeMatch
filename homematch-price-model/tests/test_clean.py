"""
Unit Tests for Data Cleaning Functions - HomeMatch
Kiểm tra tính đúng đắn của parse_price, parse_area, extract_amenities và map_room_type.
"""
import pytest
from pipeline.clean import (
    parse_price,
    parse_area,
    map_room_type,
    map_property_type,
    extract_amenities,
    remove_accents
)


def test_parse_price_various_formats():
    assert parse_price("2,5 triệu/tháng") == 2_500_000
    assert parse_price("2.5 triệu") == 2_500_000
    assert parse_price("2,5tr") == 2_500_000
    assert parse_price("3tr5") == 3_500_000 or parse_price("3.5 tr") == 3_500_000
    assert parse_price("800 nghìn") == 800_000
    assert parse_price("800k") == 800_000
    assert parse_price("800 k") == 800_000
    assert parse_price("3.200.000đ") == 3_200_000
    assert parse_price("4,500,000") == 4_500_000
    assert parse_price("4500000") == 4_500_000
    assert parse_price("Thoả thuận") is None
    assert parse_price("") is None
    assert parse_price(None) is None


def test_parse_area_various_formats():
    assert parse_area("25 m2") == 25.0
    assert parse_area("25 m²") == 25.0
    assert parse_area("30,5 m2") == 30.5
    assert parse_area("30.5m2") == 30.5
    assert parse_area("18m²") == 18.0
    assert parse_area("không rõ") is None
    assert parse_area(None) is None


def test_map_room_type():
    assert map_room_type("Cho thuê phòng studio đầy đủ tiện nghi", "Căn hộ mini") == "STUDIO"
    assert map_room_type("Ký túc xá máy lạnh gần ĐH Bách Khoa", "Phòng trọ") == "DORMITORY"
    assert map_room_type("Sleepbox cao cấp quận 10", "Phòng trọ") == "DORMITORY"
    assert map_room_type("Cho thuê căn hộ 1PN 1WC", "Căn hộ") == "1B1B"
    assert map_room_type("Căn 2 phòng ngủ sạch sẽ", "Chung cư") == "2B1B"
    assert map_room_type("Phòng trọ giá rẻ cho sinh viên", "Phòng trọ") == "ROOM"


def test_map_property_type():
    assert map_property_type("Căn hộ dịch vụ full đồ", "CHDV") == "SERVICED_APARTMENT"
    assert map_property_type("Chung cư mini cao cấp", "Căn hộ") == "APARTMENT"
    assert map_property_type("Nhà trọ bình dân", "Phòng trọ") == "BOARDING_HOUSE"
    assert map_property_type("Homestay trung tâm", "Homestay") == "HOMESTAY"


def test_extract_amenities_positive():
    desc = "Phòng có máy lạnh, máy giặt riêng, wc riêng khép kín, có gác lửng và ban công thoáng mát."
    amenities = extract_amenities(desc)
    assert amenities["am_AIR_CONDITIONER"] == 1
    assert amenities["am_WASHING_MACHINE"] == 1
    assert amenities["am_PRIVATE_WC"] == 1
    assert amenities["am_LOFT"] == 1
    assert amenities["am_BALCONY"] == 1
    assert amenities["am_SMART_LOCK"] == 0  # Không có trong mô tả


def test_extract_amenities_negation():
    # Kiểm tra bẫy phủ định: "không có điều hòa", "ko có máy giặt"
    desc = "Phòng giá rẻ, phòng không có điều hòa, ko có máy giặt, nhưng có ban công và wifi miễn phí."
    amenities = extract_amenities(desc)
    assert amenities["am_AIR_CONDITIONER"] == 0  # Bị phủ định
    assert amenities["am_WASHING_MACHINE"] == 0  # Bị phủ định
    assert amenities["am_BALCONY"] == 1          # Không bị phủ định
    assert amenities["am_WIFI"] == 1             # Không bị phủ định
