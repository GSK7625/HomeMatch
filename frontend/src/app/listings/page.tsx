"use client";

import React, { useState, useMemo, useEffect, Suspense } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { listingService } from "@/lib/services";
import { listingsApi, catalogsApi } from "@/lib/api-client";
import { PropertyListing, ProvinceDto, WardDto, AmenityDto } from "@/types";
import { ListingCard } from "@/features/listings/ListingCard";
import { useToast } from "@/components/Toast";
import {
  Search,
  Filter,
  MapPin,
  SlidersHorizontal,
  Grid,
  List,
  Map as MapIcon,
  Scale,
  X,
  ArrowRight,
  Navigation,
} from "lucide-react";
import { cn, mapListingSummaryToCard } from "@/lib/utils";
import { ListingsInteractiveMap } from "@/components/ListingsInteractiveMap";

const ALL_AMENITIES = [
  "Điều hòa",
  "Nóng lạnh",
  "Máy giặt",
  "Máy giặt riêng",
  "Tủ lạnh",
  "Ban công",
  "Thang máy",
  "Gác lửng",
  "Giờ tự do",
  "Chỗ để xe",
  "Bảo vệ 24/7",
  "Vệ sinh riêng",
  "Nuôi thú cưng",
];

function ListingsContent() {
  const searchParams = useSearchParams();
  const { showToast } = useToast();

  const [allListings, setAllListings] = useState<PropertyListing[]>([]);
  const [totalServerCount, setTotalServerCount] = useState<number | null>(null);
  const [provinces, setProvinces] = useState<ProvinceDto[]>([]);
  const [wards, setWards] = useState<WardDto[]>([]);
  const [catalogAmenities, setCatalogAmenities] = useState<AmenityDto[]>([]);
  const [provinceCode, setProvinceCode] = useState("");
  const [wardCode, setWardCode] = useState("");
  const [occupants, setOccupants] = useState<number>(0);

  const [keyword, setKeyword] = useState(searchParams.get("keyword") || "");
  const [city, setCity] = useState(searchParams.get("city") || "Tất cả");
  const [district, setDistrict] = useState(searchParams.get("district") || "Tất cả");
  const [selectedTypes, setSelectedTypes] = useState<string[]>(
    searchParams.get("roomType") && searchParams.get("roomType") !== "all"
      ? [searchParams.get("roomType")!]
      : []
  );
  const [priceRange, setPriceRange] = useState(searchParams.get("price") || "all");
  const [areaRange, setAreaRange] = useState("all");
  const [selectedAmenities, setSelectedAmenities] = useState<string[]>([]);
  const [sortBy, setSortBy] = useState<"newest" | "price-asc" | "price-desc" | "area-desc">("newest");
  const [viewMode, setViewMode] = useState<"grid" | "list">("grid");
  const [showMap, setShowMap] = useState(false);
  const [page, setPage] = useState(1);
  const pageSize = 10; // Requirement Section 4.1: Max 10 items/page

  const [compareList, setCompareList] = useState<string[]>([]);
  const [isMobileFilterOpen, setIsMobileFilterOpen] = useState(false);

  const activeFilterCount = useMemo(() => {
    let count = 0;
    if (city !== "Tất cả" || provinceCode) count++;
    if (district !== "Tất cả" || wardCode) count++;
    if (selectedTypes.length > 0) count += selectedTypes.length;
    if (priceRange !== "all") count++;
    if (areaRange !== "all") count++;
    if (occupants > 0) count++;
    if (selectedAmenities.length > 0) count += selectedAmenities.length;
    return count;
  }, [city, provinceCode, district, wardCode, selectedTypes, priceRange, areaRange, occupants, selectedAmenities]);

  useEffect(() => {
    catalogsApi.getProvinces().then((res) => {
      if (res.isSuccess && res.data) setProvinces(res.data);
    }).catch(() => null);
    catalogsApi.getAmenities().then((res) => {
      if (res.isSuccess && res.data) setCatalogAmenities(res.data);
    }).catch(() => null);
  }, []);

  useEffect(() => {
    if (!provinceCode) {
      setWards([]);
      setWardCode("");
      return;
    }
    catalogsApi.getWards(provinceCode).then((res) => {
      if (res.isSuccess && res.data) setWards(res.data);
    }).catch(() => null);
  }, [provinceCode]);

  useEffect(() => {
    let isMounted = true;
    async function fetchPublicListings() {
      try {
        let minPrice: number | undefined;
        let maxPrice: number | undefined;
        if (priceRange === "under3") maxPrice = 3000000;
        else if (priceRange === "3to5") {
          minPrice = 3000000;
          maxPrice = 5000000;
        } else if (priceRange === "5to8") {
          minPrice = 5000000;
          maxPrice = 8000000;
        } else if (priceRange === "over8") minPrice = 8000000;

        let minArea: number | undefined;
        let maxArea: number | undefined;
        if (areaRange === "under25") maxArea = 25;
        else if (areaRange === "25to40") {
          minArea = 25;
          maxArea = 40;
        } else if (areaRange === "over40") minArea = 40;

        let roomTypeCode: string | undefined;
        if (selectedTypes.length === 1) {
          if (selectedTypes[0] === "Phòng trọ") roomTypeCode = "SINGLE";
          else if (selectedTypes[0] === "Studio") roomTypeCode = "STUDIO";
          else if (selectedTypes[0] === "Chung cư mini") roomTypeCode = "STUDIO";
          else if (selectedTypes[0] === "Căn hộ dịch vụ") roomTypeCode = "1B1B";
        }

        const matchedAmenityIds = selectedAmenities
          .map((name) => catalogAmenities.find((a) => a.name.toLowerCase() === name.toLowerCase())?.id)
          .filter((id): id is number => typeof id === "number");

        const searchRes = await listingsApi.searchListings({
          keyword: keyword.trim() || undefined,
          provinceCode: provinceCode || undefined,
          wardCode: wardCode || undefined,
          minPrice,
          maxPrice,
          minArea,
          maxArea,
          roomType: roomTypeCode,
          occupants: occupants > 0 ? occupants : undefined,
          amenityIds: matchedAmenityIds.length > 0 ? matchedAmenityIds : undefined,
          page: 1,
          pageSize: 50,
        });

        if (isMounted && searchRes.isSuccess && searchRes.data) {
          const backendMapped = searchRes.data.items.map((b) => mapListingSummaryToCard(b));
          setAllListings(backendMapped);
          setTotalServerCount(searchRes.data.totalCount);
          setCompareList(listingService.getCompareList());
          return;
        }

        const res = await listingsApi.getPublicListings(50);
        if (isMounted && res.isSuccess && res.data) {
          const backendMapped: PropertyListing[] = res.data.map((b) => mapListingSummaryToCard(b));
          setAllListings(backendMapped);
          setTotalServerCount(null);
          setCompareList(listingService.getCompareList());
          return;
        }
      } catch (err) {
        console.warn("Could not fetch public listings from backend", err);
      }
      if (isMounted) {
        setAllListings([]);
        setCompareList(listingService.getCompareList());
      }
    }
    fetchPublicListings();
    return () => {
      isMounted = false;
    };
  }, [keyword, provinceCode, wardCode, priceRange, areaRange, selectedTypes, occupants, selectedAmenities, catalogAmenities]);

  const [userCoords, setUserCoords] = useState<{ lat: number; lng: number } | null>(null);

  const handleNearMe = () => {
    setShowMap(true);
    showToast("Đang định vị vị trí của bạn qua GPS...", "info");
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (pos) => {
          showToast("Đã định vị thành công! Đang hiển thị vị trí của bạn trên bản đồ.", "success");
          setUserCoords({
            lat: Number(pos.coords.latitude.toFixed(6)),
            lng: Number(pos.coords.longitude.toFixed(6)),
          });
        },
        () => {
          showToast("Không thể lấy vị trí GPS. Đang hiển thị bản đồ mặc định.", "info");
          setUserCoords({ lat: 21.0285, lng: 105.8542 });
        },
        { timeout: 8000, enableHighAccuracy: true }
      );
    }
  };

  const toggleType = (type: string) => {
    setSelectedTypes((prev) =>
      prev.includes(type) ? prev.filter((t) => t !== type) : [...prev, type]
    );
  };

  const toggleAmenity = (amenity: string) => {
    setSelectedAmenities((prev) =>
      prev.includes(amenity) ? prev.filter((a) => a !== amenity) : [...prev, amenity]
    );
  };

  const resetFilters = () => {
    setKeyword("");
    setCity("Tất cả");
    setDistrict("Tất cả");
    setProvinceCode("");
    setWardCode("");
    setOccupants(0);
    setSelectedTypes([]);
    setPriceRange("all");
    setAreaRange("all");
    setSelectedAmenities([]);
    setPage(1);
  };

  // Filter listings
  const filteredListings = useMemo(() => {
    return allListings
      .filter((item) => {
        // Keyword
        if (keyword) {
          const kw = keyword.toLowerCase();
          const matchTitle = item.title.toLowerCase().includes(kw);
          const matchAddr = item.address.toLowerCase().includes(kw);
          if (!matchTitle && !matchAddr) return false;
        }

        // City
        if (city !== "Tất cả" && item.city !== city) return false;

        // District
        if (district !== "Tất cả" && item.district !== district) return false;

        // Room Type
        if (selectedTypes.length > 0 && !selectedTypes.includes(item.roomType)) return false;

        // Price
        if (priceRange === "under3" && item.price >= 3000000) return false;
        if (priceRange === "3to5" && (item.price < 3000000 || item.price > 5000000)) return false;
        if (priceRange === "5to8" && (item.price < 5000000 || item.price > 8000000)) return false;
        if (priceRange === "over8" && item.price <= 8000000) return false;

        // Area
        if (areaRange === "under25" && item.area >= 25) return false;
        if (areaRange === "25to40" && (item.area < 25 || item.area > 40)) return false;
        if (areaRange === "over40" && item.area <= 40) return false;

        // Amenities
        if (selectedAmenities.length > 0) {
          const hasAll = selectedAmenities.every((a) => item.amenities.includes(a));
          if (!hasAll) return false;
        }

        return true;
      })
      .sort((a, b) => {
        if (sortBy === "price-asc") return a.price - b.price;
        if (sortBy === "price-desc") return b.price - a.price;
        if (sortBy === "area-desc") return b.area - a.area;
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      });
  }, [allListings, keyword, city, district, selectedTypes, priceRange, areaRange, selectedAmenities, sortBy]);

  // Pagination slice
  const totalPages = Math.ceil(filteredListings.length / pageSize) || 1;
  const paginatedListings = useMemo(() => {
    const start = (page - 1) * pageSize;
    return filteredListings.slice(start, start + pageSize);
  }, [filteredListings, page, pageSize]);

  const renderFilterFormContent = () => (
    <>
      {/* City / Province (Backend Catalog) */}
      <div>
        <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
          Tỉnh / Thành phố
        </label>
        <select
          value={provinceCode || city}
          onChange={(e) => {
            const val = e.target.value;
            if (provinces.some((p) => p.code === val)) {
              setProvinceCode(val);
              setCity("Tất cả");
            } else {
              setProvinceCode("");
              setCity(val);
            }
            setWardCode("");
            setDistrict("Tất cả");
            setPage(1);
          }}
          className="w-full p-2.5 text-xs rounded-xl border border-slate-200 bg-slate-50 font-medium"
        >
          <option value="Tất cả">Tất cả địa điểm</option>
          {provinces.length > 0 ? (
            provinces.map((p) => (
              <option key={p.code} value={p.code}>
                {p.fullName || p.name}
              </option>
            ))
          ) : (
            <>
              <option value="Hà Nội">Hà Nội</option>
              <option value="Hồ Chí Minh">TP. Hồ Chí Minh</option>
            </>
          )}
        </select>
      </div>

      {/* Ward / District */}
      <div>
        <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
          Phường / Xã / Quận
        </label>
        <select
          value={wardCode || district}
          onChange={(e) => {
            const val = e.target.value;
            if (wards.some((w) => w.code === val)) {
              setWardCode(val);
              setDistrict("Tất cả");
            } else {
              setWardCode("");
              setDistrict(val);
            }
            setPage(1);
          }}
          className="w-full p-2.5 text-xs rounded-xl border border-slate-200 bg-slate-50 font-medium"
        >
          <option value="Tất cả">Tất cả khu vực</option>
          {wards.length > 0 &&
            wards.map((w) => (
              <option key={w.code} value={w.code}>
                {w.fullName || w.name}
              </option>
            ))}
          {city === "Hà Nội" && (
            <>
              <option value="Cầu Giấy">Cầu Giấy</option>
              <option value="Đống Đa">Đống Đa</option>
              <option value="Hai Bà Trưng">Hai Bà Trưng</option>
              <option value="Thanh Xuân">Thanh Xuân</option>
            </>
          )}
          {city === "Hồ Chí Minh" && (
            <>
              <option value="Bình Thạnh">Bình Thạnh</option>
              <option value="Quận 1">Quận 1</option>
              <option value="Quận 7">Quận 7</option>
            </>
          )}
        </select>
      </div>

      {/* Occupants */}
      <div>
        <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
          Số người ở dự kiến
        </label>
        <select
          value={occupants}
          onChange={(e) => {
            setOccupants(Number(e.target.value));
            setPage(1);
          }}
          className="w-full p-2.5 text-xs rounded-xl border border-slate-200 bg-slate-50 font-medium"
        >
          <option value={0}>Không giới hạn</option>
          <option value={1}>1 người</option>
          <option value={2}>Từ 2 người</option>
          <option value={3}>Từ 3 người</option>
          <option value={4}>Từ 4 người trở lên</option>
        </select>
      </div>

      {/* Room Type */}
      <div>
        <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
          Loại hình phòng
        </label>
        <div className="space-y-2 text-xs">
          {["Phòng trọ", "Studio", "Chung cư mini", "Căn hộ dịch vụ"].map((t) => (
            <label key={t} className="flex items-center gap-2 cursor-pointer select-none">
              <input
                type="checkbox"
                checked={selectedTypes.includes(t)}
                onChange={() => {
                  toggleType(t);
                  setPage(1);
                }}
                className="w-4 h-4 rounded text-blue-600 border-slate-300 focus:ring-blue-500"
              />
              <span className="text-slate-700">{t}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Price Range */}
      <div>
        <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
          Khoảng giá thuê
        </label>
        <div className="space-y-2 text-xs">
          {[
            { id: "all", label: "Tất cả mức giá" },
            { id: "under3", label: "Dưới 3 triệu" },
            { id: "3to5", label: "3 - 5 triệu" },
            { id: "5to8", label: "5 - 8 triệu" },
            { id: "over8", label: "Trên 8 triệu" },
          ].map((p) => (
            <label key={p.id} className="flex items-center gap-2 cursor-pointer select-none">
              <input
                type="radio"
                name="price"
                checked={priceRange === p.id}
                onChange={() => {
                  setPriceRange(p.id);
                  setPage(1);
                }}
                className="w-4 h-4 text-blue-600 border-slate-300 focus:ring-blue-500"
              />
              <span className="text-slate-700">{p.label}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Area Range */}
      <div>
        <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
          Diện tích
        </label>
        <div className="space-y-2 text-xs">
          {[
            { id: "all", label: "Tất cả diện tích" },
            { id: "under25", label: "Dưới 25 m²" },
            { id: "25to40", label: "25 - 40 m²" },
            { id: "over40", label: "Trên 40 m²" },
          ].map((a) => (
            <label key={a.id} className="flex items-center gap-2 cursor-pointer select-none">
              <input
                type="radio"
                name="area"
                checked={areaRange === a.id}
                onChange={() => {
                  setAreaRange(a.id);
                  setPage(1);
                }}
                className="w-4 h-4 text-blue-600 border-slate-300 focus:ring-blue-500"
              />
              <span className="text-slate-700">{a.label}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Amenities Pills */}
      <div>
        <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
          Tiện ích đi kèm
        </label>
        <div className="flex flex-wrap gap-1.5">
          {ALL_AMENITIES.map((amenity) => {
            const isChecked = selectedAmenities.includes(amenity);
            return (
              <button
                key={amenity}
                type="button"
                onClick={() => {
                  toggleAmenity(amenity);
                  setPage(1);
                }}
                className={cn(
                  "px-2.5 py-1 rounded-lg text-[11px] font-medium border transition-colors",
                  isChecked
                    ? "bg-blue-600 text-white border-blue-600"
                    : "bg-slate-50 text-slate-600 border-slate-200 hover:bg-slate-100"
                )}
              >
                {amenity}
              </button>
            );
          })}
        </div>
      </div>
    </>
  );

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-8 w-full flex-1">
      {/* Top Search & Actions bar */}
      <div className="bg-white p-3.5 sm:p-4 rounded-2xl border border-slate-200/80 shadow-xs mb-6 sm:mb-8 flex flex-col md:flex-row items-stretch md:items-center justify-between gap-3 sm:gap-4">
        <div className="relative w-full md:max-w-md">
          <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
          <input
            type="text"
            placeholder="Tìm theo tên đường, quận huyện, trường học..."
            value={keyword}
            onChange={(e) => {
              setKeyword(e.target.value);
              setPage(1);
            }}
            className="w-full h-10 pl-10 pr-4 text-xs rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-blue-500/20 transition-all"
          />
        </div>

        <div className="flex flex-wrap sm:flex-nowrap items-center gap-2 w-full sm:w-auto justify-end">
          {/* Mobile Filter Button */}
          <button
            onClick={() => setIsMobileFilterOpen(true)}
            className="h-10 lg:hidden flex-1 sm:flex-initial inline-flex items-center justify-center gap-1.5 px-3.5 rounded-xl text-xs font-bold bg-blue-600 hover:bg-blue-700 text-white shadow-xs transition-colors whitespace-nowrap shrink-0"
          >
            <SlidersHorizontal className="w-3.5 h-3.5 shrink-0" />
            <span>Bộ lọc</span>
            {activeFilterCount > 0 && (
              <span className="w-4 h-4 rounded-full bg-white text-blue-600 text-[10px] font-black flex items-center justify-center shrink-0">
                {activeFilterCount}
              </span>
            )}
          </button>

          {/* Near me button */}
          <button
            onClick={handleNearMe}
            className="h-10 flex-1 sm:flex-initial inline-flex items-center justify-center gap-1.5 px-3.5 rounded-xl text-xs font-bold bg-slate-100 hover:bg-blue-50 text-slate-700 hover:text-blue-700 border border-slate-200/80 transition-colors whitespace-nowrap shrink-0"
          >
            <Navigation className="w-3.5 h-3.5 text-blue-600 shrink-0" />
            <span className="hidden sm:inline">Tìm phòng gần tôi</span>
            <span className="sm:hidden">Gần tôi</span>
          </button>

          {/* Map toggle */}
          <button
            onClick={() => setShowMap(!showMap)}
            className={cn(
              "h-10 flex-1 sm:flex-initial inline-flex items-center justify-center gap-1.5 px-3.5 rounded-xl text-xs font-bold border transition-colors whitespace-nowrap shrink-0",
              showMap
                ? "bg-slate-900 text-white border-slate-900"
                : "bg-white text-slate-700 border-slate-200/80 hover:bg-slate-50"
            )}
          >
            <MapIcon className="w-3.5 h-3.5 shrink-0" />
            <span className="hidden sm:inline">{showMap ? "Ẩn bản đồ" : "Xem trên bản đồ"}</span>
            <span className="sm:hidden">{showMap ? "Ẩn map" : "Bản đồ"}</span>
          </button>

          {/* View Mode (Only on desktop/tablet where multi-column view actually toggles) */}
          <div className="hidden sm:inline-flex items-center h-10 border border-slate-200 rounded-xl p-1 bg-slate-50 shrink-0 gap-0.5">
            <button
              onClick={() => setViewMode("grid")}
              className={cn(
                "h-8 w-8 rounded-lg flex items-center justify-center text-slate-600 transition-colors",
                viewMode === "grid" && "bg-white text-blue-600 shadow-xs"
              )}
              title="Xem dạng lưới"
            >
              <Grid className="w-4 h-4" />
            </button>
            <button
              onClick={() => setViewMode("list")}
              className={cn(
                "h-8 w-8 rounded-lg flex items-center justify-center text-slate-600 transition-colors",
                viewMode === "list" && "bg-white text-blue-600 shadow-xs"
              )}
              title="Xem dạng danh sách"
            >
              <List className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>

      {/* Main Grid: Sidebar Filters + Listings */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
        {/* Sidebar Filter - Desktop only */}
        <aside className="hidden lg:block lg:col-span-1 space-y-6">
          <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs space-y-6 sticky top-20">
            <div className="flex items-center justify-between pb-3 border-b border-slate-100">
              <div className="flex items-center gap-2 font-bold text-slate-900 text-sm">
                <SlidersHorizontal className="w-4 h-4 text-blue-600" />
                <span>Bộ Lọc Tìm Kiếm</span>
              </div>
              <button
                onClick={resetFilters}
                className="text-xs font-medium text-blue-600 hover:text-blue-700"
              >
                Xóa tất cả
              </button>
            </div>

            {renderFilterFormContent()}
          </div>
        </aside>

        {/* Mobile Slide-Over Filter Drawer */}
        {isMobileFilterOpen && (
          <div className="fixed inset-0 z-50 lg:hidden flex justify-end">
            <div
              className="fixed inset-0 bg-slate-900/60 backdrop-blur-xs transition-opacity"
              onClick={() => setIsMobileFilterOpen(false)}
            />
            <div className="relative w-full max-w-sm sm:max-w-md bg-white h-full shadow-2xl flex flex-col z-10 animate-in slide-in-from-right duration-200">
              <div className="flex items-center justify-between p-4 border-b border-slate-200 bg-white">
                <div className="flex items-center gap-2">
                  <SlidersHorizontal className="w-4 h-4 text-blue-600" />
                  <h3 className="font-bold text-sm text-slate-900">Bộ Lọc Tìm Kiếm</h3>
                  {activeFilterCount > 0 && (
                    <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-blue-50 text-blue-700">
                      {activeFilterCount}
                    </span>
                  )}
                </div>
                <button
                  onClick={() => setIsMobileFilterOpen(false)}
                  className="p-1.5 rounded-xl hover:bg-slate-100 text-slate-500"
                  aria-label="Đóng bộ lọc"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>

              <div className="flex-1 overflow-y-auto p-4 space-y-5">
                {renderFilterFormContent()}
              </div>

              <div className="p-4 border-t border-slate-200 bg-white flex items-center gap-3">
                <button
                  type="button"
                  onClick={resetFilters}
                  className="flex-1 py-2.5 px-3 rounded-xl border border-slate-200 font-bold text-xs text-slate-700 hover:bg-slate-50"
                >
                  Xóa tất cả
                </button>
                <button
                  type="button"
                  onClick={() => setIsMobileFilterOpen(false)}
                  className="flex-1 py-2.5 px-3 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-sm"
                >
                  Xem {filteredListings.length} phòng
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Listings Result View */}
        <div className="lg:col-span-3 space-y-6">
          {/* Real Interactive PostGIS Leaflet Map */}
          {showMap && (
            <ListingsInteractiveMap
              listings={filteredListings}
              userLocation={userCoords}
              onLocateMe={handleNearMe}
            />
          )}

          {/* Commercial Quick Filter Chips */}
          <div className="flex items-center gap-2 overflow-x-auto pb-1 text-xs">
            <span className="text-slate-400 font-semibold text-[11px] whitespace-nowrap">Lọc nhanh:</span>
            <button
              onClick={() => {
                setPriceRange((prev) => (prev === "3to5" ? "all" : "3to5"));
                setPage(1);
              }}
              className={cn(
                "px-3 py-1.5 rounded-full font-semibold border transition-all whitespace-nowrap flex items-center gap-1.5",
                priceRange === "3to5"
                  ? "bg-blue-600 text-white border-blue-600 shadow-xs"
                  : "bg-white text-slate-700 border-slate-200 hover:bg-slate-50"
              )}
            >
              <span>💵 3 - 5 triệu/tháng</span>
              {priceRange === "3to5" && <X className="w-3 h-3 text-white/90 shrink-0" />}
            </button>
            <button
              onClick={() => {
                toggleAmenity("Máy giặt riêng");
                setPage(1);
              }}
              className={cn(
                "px-3 py-1.5 rounded-full font-semibold border transition-all whitespace-nowrap flex items-center gap-1.5",
                selectedAmenities.includes("Máy giặt riêng")
                  ? "bg-blue-600 text-white border-blue-600 shadow-xs"
                  : "bg-white text-slate-700 border-slate-200 hover:bg-slate-50"
              )}
            >
              <span>🧺 Máy giặt riêng</span>
              {selectedAmenities.includes("Máy giặt riêng") && <X className="w-3 h-3 text-white/90 shrink-0" />}
            </button>
            <button
              onClick={() => {
                toggleAmenity("Ban công");
                setPage(1);
              }}
              className={cn(
                "px-3 py-1.5 rounded-full font-semibold border transition-all whitespace-nowrap flex items-center gap-1.5",
                selectedAmenities.includes("Ban công")
                  ? "bg-blue-600 text-white border-blue-600 shadow-xs"
                  : "bg-white text-slate-700 border-slate-200 hover:bg-slate-50"
              )}
            >
              <span>🌿 Ban công thoáng</span>
              {selectedAmenities.includes("Ban công") && <X className="w-3 h-3 text-white/90 shrink-0" />}
            </button>
            <button
              onClick={() => {
                toggleAmenity("Nuôi thú cưng");
                setPage(1);
              }}
              className={cn(
                "px-3 py-1.5 rounded-full font-semibold border transition-all whitespace-nowrap flex items-center gap-1.5",
                selectedAmenities.includes("Nuôi thú cưng")
                  ? "bg-blue-600 text-white border-blue-600 shadow-xs"
                  : "bg-white text-slate-700 border-slate-200 hover:bg-slate-50"
              )}
            >
              <span>🐾 Cho nuôi pet</span>
              {selectedAmenities.includes("Nuôi thú cưng") && <X className="w-3 h-3 text-white/90 shrink-0" />}
            </button>
            <button
              onClick={() => {
                toggleType("Căn hộ dịch vụ");
                setPage(1);
              }}
              className={cn(
                "px-3 py-1.5 rounded-full font-semibold border transition-all whitespace-nowrap flex items-center gap-1.5",
                selectedTypes.includes("Căn hộ dịch vụ")
                  ? "bg-blue-600 text-white border-blue-600 shadow-xs"
                  : "bg-white text-slate-700 border-slate-200 hover:bg-slate-50"
              )}
            >
              <span>✨ Căn hộ dịch vụ</span>
              {selectedTypes.includes("Căn hộ dịch vụ") && <X className="w-3 h-3 text-white/90 shrink-0" />}
            </button>
          </div>

          {/* Results Summary & Sorting */}
          <div className="flex items-center justify-between flex-wrap gap-4 pb-2">
            <div>
              <p className="text-sm font-bold text-slate-800">
                Tìm thấy{" "}
                <span className="text-blue-600 font-black">{filteredListings.length}</span> phòng
                phù hợp
              </p>
              {selectedAmenities.length > 0 && (
                <p className="text-xs text-slate-500 mt-0.5">
                  Lọc theo: {selectedAmenities.join(", ")}
                </p>
              )}
            </div>

            <div className="flex items-center gap-2">
              <span className="text-xs text-slate-500 font-medium">Sắp xếp:</span>
              <select
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value as any)}
                className="px-3 py-1.5 rounded-xl border border-slate-200 bg-white text-xs font-semibold focus:outline-none focus:ring-2 focus:ring-blue-500/20"
              >
                <option value="newest">Mới nhất</option>
                <option value="price-asc">Giá tăng dần</option>
                <option value="price-desc">Giá giảm dần</option>
                <option value="area-desc">Diện tích lớn nhất</option>
              </select>
            </div>
          </div>

          {/* Listings List */}
          {paginatedListings.length === 0 ? (
            <div className="bg-white rounded-2xl border border-slate-200/80 p-12 text-center space-y-3">
              <div className="w-12 h-12 rounded-full bg-slate-100 flex items-center justify-center mx-auto text-slate-400">
                <Search className="w-6 h-6" />
              </div>
              <h3 className="font-bold text-slate-800 text-base">Không tìm thấy phòng nào</h3>
              <p className="text-xs text-slate-500 max-w-sm mx-auto">
                Không có tin đăng nào phù hợp với các tiêu chí lọc hiện tại. Hãy thử bỏ chọn một số
                tiện ích hoặc mở rộng khoảng giá.
              </p>
              <button
                onClick={resetFilters}
                className="px-4 py-2 bg-blue-50 text-blue-600 rounded-xl text-xs font-bold hover:bg-blue-100 transition-colors"
              >
                Đặt lại bộ lọc
              </button>
            </div>
          ) : (
            <div
              className={cn(
                "grid gap-6",
                viewMode === "grid" ? "grid-cols-1 sm:grid-cols-2 xl:grid-cols-3" : "grid-cols-1"
              )}
            >
              {paginatedListings.map((listing) => (
                <ListingCard
                  key={listing.id}
                  listing={listing}
                  onFavoriteChange={() => {
                    // Refresh
                  }}
                />
              ))}
            </div>
          )}

          {/* Pagination Controls */}
          {totalPages > 1 && (
            <div className="flex items-center justify-center gap-2 pt-6">
              <button
                disabled={page === 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                className="px-3 py-1.5 rounded-lg border border-slate-200 text-xs font-semibold disabled:opacity-40 hover:bg-slate-50"
              >
                Trước
              </button>
              {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
                <button
                  key={p}
                  onClick={() => setPage(p)}
                  className={cn(
                    "w-8 h-8 rounded-lg text-xs font-bold transition-colors",
                    page === p ? "bg-blue-600 text-white" : "text-slate-600 hover:bg-slate-100"
                  )}
                >
                  {p}
                </button>
              ))}
              <button
                disabled={page === totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                className="px-3 py-1.5 rounded-lg border border-slate-200 text-xs font-semibold disabled:opacity-40 hover:bg-slate-50"
              >
                Sau
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Floating Comparison Bar if items exist */}
      {compareList.length > 0 && (
        <div className="fixed bottom-6 left-1/2 -translate-x-1/2 z-30 bg-slate-900/95 backdrop-blur-md text-white px-5 py-3 rounded-2xl shadow-2xl border border-slate-700 flex items-center gap-4 animate-in fade-in slide-in-from-bottom-4">
          <div className="flex items-center gap-2">
            <Scale className="w-5 h-5 text-blue-400" />
            <span className="text-xs font-bold">
              Đang chọn <span className="text-blue-300 font-black">{compareList.length}/3</span> phòng
              so sánh
            </span>
          </div>
          <Link
            href="/compare"
            className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold transition-colors"
          >
            <span>So sánh ngay</span>
            <ArrowRight className="w-3.5 h-3.5" />
          </Link>
          <button
            onClick={() => {
              listingService.clearCompare();
              setCompareList([]);
            }}
            className="text-slate-400 hover:text-white p-1 rounded"
            title="Bỏ chọn so sánh"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      )}
    </div>
  );
}

export default function ListingsPage() {
  return (
    <Suspense fallback={<div className="p-12 text-center text-xs text-slate-500">Đang tải danh sách phòng...</div>}>
      <ListingsContent />
    </Suspense>
  );
}
