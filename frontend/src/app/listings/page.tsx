"use client";

import React, { useState, useMemo, useEffect, Suspense } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { compareSelection } from "@/lib/compare-selection";
import { listingsApi, catalogsApi, getStoredToken } from "@/lib/api-client";
import { useAuth } from "@/features/auth/AuthContext";
import { PropertyListing, ProvinceDto, WardDto, AmenityDto } from "@/types";
import { ListingCard } from "@/features/listings/ListingCard";
import {
  Search,
  SlidersHorizontal,
  Grid,
  List,
  Scale,
  X,
  ArrowRight,
  Loader2,
} from "lucide-react";
import { cn, mapListingSummaryToCard } from "@/lib/utils";

const ROOM_TYPE_OPTIONS = [
  { code: "", label: "Tất cả loại phòng" },
  { code: "SINGLE", label: "Phòng trọ đơn" },
  { code: "STUDIO", label: "Phòng Studio / CCMN" },
  { code: "1B1B", label: "Căn hộ 1 phòng ngủ (1B1B)" },
  { code: "2B1B", label: "Căn hộ 2 phòng ngủ (2B1B)" },
  { code: "DORMITORY", label: "Ký túc xá / Ở ghép" },
] as const;

function normalizeRoomTypeParam(raw: string | null): string {
  if (!raw || raw === "all") return "";
  const upper = raw.trim().toUpperCase();
  if (upper === "SINGLE" || upper === "STUDIO" || upper === "1B1B" || upper === "2B1B" || upper === "DORMITORY") {
    return upper;
  }
  const lower = raw.trim().toLowerCase();
  if (lower === "phòng trọ") return "SINGLE";
  if (lower === "studio" || lower === "chung cư mini") return "STUDIO";
  if (lower === "căn hộ dịch vụ") return "1B1B";
  if (lower === "ở ghép") return "DORMITORY";
  return "";
}

function ListingsContent() {
  const searchParams = useSearchParams();
  const { user, role } = useAuth();

  const [listings, setListings] = useState<PropertyListing[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const [provinces, setProvinces] = useState<ProvinceDto[]>([]);
  const [wards, setWards] = useState<WardDto[]>([]);
  const [catalogAmenities, setCatalogAmenities] = useState<AmenityDto[]>([]);

  const [keyword, setKeyword] = useState(
    searchParams.get("keyword") || searchParams.get("q") || ""
  );
  const [debouncedKeyword, setDebouncedKeyword] = useState(
    searchParams.get("keyword") || searchParams.get("q") || ""
  );
  const [provinceCode, setProvinceCode] = useState(searchParams.get("provinceCode") || "");
  const [wardCode, setWardCode] = useState(searchParams.get("wardCode") || "");
  const [occupants, setOccupants] = useState<number>(0);
  const [selectedRoomType, setSelectedRoomType] = useState<string>(
    normalizeRoomTypeParam(searchParams.get("roomType"))
  );
  const [priceRange, setPriceRange] = useState(searchParams.get("price") || "all");
  const [areaRange, setAreaRange] = useState("all");
  const [selectedAmenityIds, setSelectedAmenityIds] = useState<number[]>([]);
  const [viewMode, setViewMode] = useState<"grid" | "list">("grid");
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const [compareList, setCompareList] = useState<string[]>([]);
  const [savedIds, setSavedIds] = useState<Set<string>>(new Set());
  const [isMobileFilterOpen, setIsMobileFilterOpen] = useState(false);

  const activeFilterCount = useMemo(() => {
    let count = 0;
    if (provinceCode) count++;
    if (wardCode) count++;
    if (selectedRoomType) count++;
    if (priceRange !== "all") count++;
    if (areaRange !== "all") count++;
    if (occupants > 0) count++;
    count += selectedAmenityIds.length;
    return count;
  }, [provinceCode, wardCode, selectedRoomType, priceRange, areaRange, occupants, selectedAmenityIds]);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedKeyword(keyword.trim());
    }, 250);
    return () => clearTimeout(timer);
  }, [keyword]);

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
    setCompareList(compareSelection.getIds());
    const handleCompareUpdated = (e: Event) => {
      const customEvent = e as CustomEvent<{ ids?: string[] }>;
      if (Array.isArray(customEvent.detail?.ids)) {
        setCompareList(customEvent.detail.ids);
      } else {
        setCompareList(compareSelection.getIds());
      }
    };
    window.addEventListener("homematch:compare-updated", handleCompareUpdated);
    return () => window.removeEventListener("homematch:compare-updated", handleCompareUpdated);
  }, []);

  useEffect(() => {
    let isMounted = true;
    if (user && role === "Tenant" && getStoredToken()) {
      listingsApi.getSavedListings().then((res) => {
        if (isMounted && res.isSuccess && Array.isArray(res.data)) {
          const ids = res.data.map((s) => String(s.listingId));
          setSavedIds(new Set(ids));
          window.dispatchEvent(new CustomEvent("homematch:favorites-updated", { detail: { ids } }));
        }
      }).catch(() => null);
    } else {
      setSavedIds(new Set());
      if (!getStoredToken()) {
        window.dispatchEvent(new CustomEvent("homematch:favorites-updated", { detail: { ids: [] } }));
      }
    }
    return () => {
      isMounted = false;
    };
  }, [user, role]);

  useEffect(() => {
    let isMounted = true;
    async function fetchListings() {
      setIsLoading(true);
      setErrorMsg(null);

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

      try {
        const searchRes = await listingsApi.searchListings({
          keyword: debouncedKeyword || undefined,
          provinceCode: provinceCode || undefined,
          wardCode: wardCode || undefined,
          minPrice,
          maxPrice,
          minArea,
          maxArea,
          roomType: selectedRoomType || undefined,
          occupants: occupants > 0 ? occupants : undefined,
          amenityIds: selectedAmenityIds.length > 0 ? selectedAmenityIds : undefined,
          page,
          pageSize,
        });

        if (!isMounted) return;
        if (searchRes.isSuccess && searchRes.data) {
          setListings(searchRes.data.items.map(mapListingSummaryToCard));
          setTotalCount(searchRes.data.totalCount);
        } else {
          setListings([]);
          setTotalCount(0);
          setErrorMsg(searchRes.error?.message || "Không thể tải danh sách phòng từ máy chủ.");
        }
      } catch {
        if (!isMounted) return;
        setListings([]);
        setTotalCount(0);
        setErrorMsg("Lỗi kết nối máy chủ khi tìm kiếm phòng.");
      } finally {
        if (isMounted) setIsLoading(false);
      }
    }

    fetchListings();
    return () => {
      isMounted = false;
    };
  }, [
    debouncedKeyword,
    provinceCode,
    wardCode,
    priceRange,
    areaRange,
    selectedRoomType,
    occupants,
    selectedAmenityIds,
    page,
  ]);

  const toggleAmenityId = (amenityId: number) => {
    setSelectedAmenityIds((prev) =>
      prev.includes(amenityId) ? prev.filter((id) => id !== amenityId) : [...prev, amenityId]
    );
    setPage(1);
  };

  const resetFilters = () => {
    setKeyword("");
    setDebouncedKeyword("");
    setProvinceCode("");
    setWardCode("");
    setOccupants(0);
    setSelectedRoomType("");
    setPriceRange("all");
    setAreaRange("all");
    setSelectedAmenityIds([]);
    setPage(1);
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  const quickAmenityIds = useMemo(() => {
    const findByKeyword = (kw: string) =>
      catalogAmenities.find((a) => a.name.toLowerCase().includes(kw.toLowerCase()));
    return {
      washingMachine: findByKeyword("máy giặt"),
      balcony: findByKeyword("ban công"),
      pets: findByKeyword("thú cưng"),
    };
  }, [catalogAmenities]);

  const renderFilterFormContent = () => (
    <>
      {/* Province (Backend Catalog) */}
      <div>
        <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
          Tỉnh / Thành phố
        </label>
        <select
          value={provinceCode}
          onChange={(e) => {
            setProvinceCode(e.target.value);
            setWardCode("");
            setPage(1);
          }}
          className="w-full p-2.5 text-xs rounded-xl border border-slate-200 bg-slate-50 font-medium"
        >
          <option value="">Tất cả địa điểm</option>
          {provinces.map((p) => (
            <option key={p.code} value={p.code}>
              {p.fullName || p.name}
            </option>
          ))}
        </select>
      </div>

      {/* Ward (Backend Catalog) */}
      <div>
        <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
          Phường / Xã
        </label>
        <select
          value={wardCode}
          disabled={!provinceCode}
          onChange={(e) => {
            setWardCode(e.target.value);
            setPage(1);
          }}
          className="w-full p-2.5 text-xs rounded-xl border border-slate-200 bg-slate-50 font-medium disabled:opacity-60"
        >
          <option value="">
            {provinceCode ? "Tất cả phường / xã" : "Chọn Tỉnh / Thành phố trước"}
          </option>
          {wards.map((w) => (
            <option key={w.code} value={w.code}>
              {w.fullName || w.name}
            </option>
          ))}
        </select>
      </div>

      {/* Occupants */}
      <div>
        <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
          Số người ở tối thiểu
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

      {/* Room Type (aligned with Backend enum) */}
      <div>
        <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
          Loại hình phòng
        </label>
        <div className="space-y-2 text-xs">
          {ROOM_TYPE_OPTIONS.map((opt) => (
            <label key={opt.code || "ALL"} className="flex items-center gap-2 cursor-pointer select-none">
              <input
                type="radio"
                name="roomType"
                checked={selectedRoomType === opt.code}
                onChange={() => {
                  setSelectedRoomType(opt.code);
                  setPage(1);
                }}
                className="w-4 h-4 text-blue-600 border-slate-300 focus:ring-blue-500"
              />
              <span className="text-slate-700">{opt.label}</span>
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

      {/* Amenities Pills (from Backend Catalog) */}
      {catalogAmenities.length > 0 && (
        <div>
          <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
            Tiện ích đi kèm
          </label>
          <div className="flex flex-wrap gap-1.5">
            {catalogAmenities.map((amenity) => {
              const isChecked = selectedAmenityIds.includes(amenity.id);
              return (
                <button
                  key={amenity.id}
                  type="button"
                  onClick={() => toggleAmenityId(amenity.id)}
                  className={cn(
                    "px-2.5 py-1 rounded-lg text-[11px] font-medium border transition-colors",
                    isChecked
                      ? "bg-blue-600 text-white border-blue-600"
                      : "bg-slate-50 text-slate-600 border-slate-200 hover:bg-slate-100"
                  )}
                >
                  {amenity.name}
                </button>
              );
            })}
          </div>
        </div>
      )}
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
            placeholder="Tìm theo từ khóa, tên đường, tiêu đề tin..."
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

          {/* View Mode */}
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
                  Xem {totalCount} phòng
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Listings Result View */}
        <div className="lg:col-span-3 space-y-6">
          {/* Quick Filter Chips */}
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
              <span>3 - 5 triệu/tháng</span>
              {priceRange === "3to5" && <X className="w-3 h-3 text-white/90 shrink-0" />}
            </button>
            {quickAmenityIds.washingMachine && (
              <button
                onClick={() => toggleAmenityId(quickAmenityIds.washingMachine!.id)}
                className={cn(
                  "px-3 py-1.5 rounded-full font-semibold border transition-all whitespace-nowrap flex items-center gap-1.5",
                  selectedAmenityIds.includes(quickAmenityIds.washingMachine.id)
                    ? "bg-blue-600 text-white border-blue-600 shadow-xs"
                    : "bg-white text-slate-700 border-slate-200 hover:bg-slate-50"
                )}
              >
                <span>{quickAmenityIds.washingMachine.name}</span>
                {selectedAmenityIds.includes(quickAmenityIds.washingMachine.id) && (
                  <X className="w-3 h-3 text-white/90 shrink-0" />
                )}
              </button>
            )}
            {quickAmenityIds.balcony && (
              <button
                onClick={() => toggleAmenityId(quickAmenityIds.balcony!.id)}
                className={cn(
                  "px-3 py-1.5 rounded-full font-semibold border transition-all whitespace-nowrap flex items-center gap-1.5",
                  selectedAmenityIds.includes(quickAmenityIds.balcony.id)
                    ? "bg-blue-600 text-white border-blue-600 shadow-xs"
                    : "bg-white text-slate-700 border-slate-200 hover:bg-slate-50"
                )}
              >
                <span>{quickAmenityIds.balcony.name}</span>
                {selectedAmenityIds.includes(quickAmenityIds.balcony.id) && (
                  <X className="w-3 h-3 text-white/90 shrink-0" />
                )}
              </button>
            )}
            {quickAmenityIds.pets && (
              <button
                onClick={() => toggleAmenityId(quickAmenityIds.pets!.id)}
                className={cn(
                  "px-3 py-1.5 rounded-full font-semibold border transition-all whitespace-nowrap flex items-center gap-1.5",
                  selectedAmenityIds.includes(quickAmenityIds.pets.id)
                    ? "bg-blue-600 text-white border-blue-600 shadow-xs"
                    : "bg-white text-slate-700 border-slate-200 hover:bg-slate-50"
                )}
              >
                <span>{quickAmenityIds.pets.name}</span>
                {selectedAmenityIds.includes(quickAmenityIds.pets.id) && (
                  <X className="w-3 h-3 text-white/90 shrink-0" />
                )}
              </button>
            )}
            <button
              onClick={() => {
                setSelectedRoomType((prev) => (prev === "1B1B" ? "" : "1B1B"));
                setPage(1);
              }}
              className={cn(
                "px-3 py-1.5 rounded-full font-semibold border transition-all whitespace-nowrap flex items-center gap-1.5",
                selectedRoomType === "1B1B"
                  ? "bg-blue-600 text-white border-blue-600 shadow-xs"
                  : "bg-white text-slate-700 border-slate-200 hover:bg-slate-50"
              )}
            >
              <span>Căn hộ 1B1B</span>
              {selectedRoomType === "1B1B" && <X className="w-3 h-3 text-white/90 shrink-0" />}
            </button>
          </div>

          {/* Results Summary */}
          <div className="flex items-center justify-between flex-wrap gap-4 pb-2">
            <div>
              <p className="text-sm font-bold text-slate-800">
                Tìm thấy{" "}
                <span className="text-blue-600 font-black">{totalCount}</span> phòng phù hợp
              </p>
              {selectedAmenityIds.length > 0 && (
                <p className="text-xs text-slate-500 mt-0.5">
                  Lọc theo:{" "}
                  {selectedAmenityIds
                    .map((id) => catalogAmenities.find((a) => a.id === id)?.name)
                    .filter(Boolean)
                    .join(", ")}
                </p>
              )}
            </div>
          </div>

          {/* Listings List */}
          {isLoading ? (
            <div className="bg-white rounded-2xl border border-slate-200/80 p-12 text-center space-y-3">
              <Loader2 className="w-8 h-8 animate-spin text-blue-600 mx-auto" />
              <p className="text-xs font-medium text-slate-500">Đang tải danh sách phòng từ máy chủ...</p>
            </div>
          ) : errorMsg ? (
            <div className="bg-rose-50 rounded-2xl border border-rose-200 p-10 text-center space-y-3">
              <p className="text-sm font-bold text-rose-700">{errorMsg}</p>
              <button
                onClick={resetFilters}
                className="px-4 py-2 bg-rose-600 text-white rounded-xl text-xs font-bold hover:bg-rose-700 transition-colors"
              >
                Đặt lại bộ lọc
              </button>
            </div>
          ) : listings.length === 0 ? (
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
              {listings.map((listing) => (
                <ListingCard
                  key={listing.id}
                  listing={listing}
                  isFavorite={savedIds.has(listing.id)}
                />
              ))}
            </div>
          )}

          {/* Server-side Pagination Controls */}
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
              compareSelection.clear();
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
