# Báo cáo Thống kê & Phân tích Khám phá Dữ liệu (EDA) - HomeMatch

## 1. Tổng quan Bộ dữ liệu
* **Tổng số tin đăng sạch:** 26,041 tin
* **Số trường đặc trưng:** 37 cột
* **Nguồn dữ liệu:**
| source       |   count |
|:-------------|--------:|
| phongtro123  |   11858 |
| chotot       |    9384 |
| thuephongtro |    4799 |

## 2. Thống kê Giá & Diện tích theo Tỉnh / Thành
| province    |   ('price', 'count') |   ('price', 'median') |   ('price', 'mean') |   ('area_sqm', 'count') |   ('area_sqm', 'median') |   ('area_sqm', 'mean') |
|:------------|---------------------:|----------------------:|--------------------:|------------------------:|-------------------------:|-----------------------:|
| Hà Nội      |                 6979 |               3.7e+06 |         5.1623e+06  |                    6979 |                       26 |                31.2889 |
| Hồ Chí Minh |                19062 |               4.5e+06 |         5.55929e+06 |                   19062 |                       30 |                33.4668 |

## 3. Top Tiện ích phổ biến nhất thị trường
|                    | 0     |
|:-------------------|:------|
| am_AIR_CONDITIONER | 36.8% |
| am_FURNISHED_FULL  | 30.6% |
| am_WINDOW          | 26.6% |
| am_WARDROBE        | 23.7% |
| am_KITCHEN         | 22.4% |
| am_BALCONY         | 21.6% |
| am_LOFT            | 15.0% |
| am_WASHING_MACHINE | 13.5% |
| am_PRIVATE_WC      | 13.0% |
| am_BED             | 12.9% |
| am_ELEVATOR        | 11.3% |
| am_WATER_HEATER    | 10.8% |
| am_FRIDGE          | 10.4% |
| am_WIFI            | 8.5%  |
| am_PARKING         | 7.5%  |
| am_SMART_LOCK      | 3.2%  |

## 4. Tương quan Khoảng cách Không gian
* **Khoảng cách trung bình tới trung tâm:** 6.07 km
* **Khoảng cách trung bình tới trường ĐH gần nhất:** 4.89 km
