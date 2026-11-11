"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { fetchHealth, listingsApi, getStoredToken } from "@/lib/api-client";
import { listingService } from "@/lib/services";
import { PropertyListing, HealthCheck } from "@/types";
import { ListingCard } from "@/features/listings/ListingCard";
import {
  Search,
  MapPin,
  DollarSign,
  Building,
  ShieldCheck,
  Zap,
  Calendar,
  ArrowRight,
  CheckCircle2,
  FileText,
  Star,
  Calculator,
  Users,
  Flame,
  PhoneCall,
  Clock,
  ChevronDown,
} from "lucide-react";
import { cn, mapListingSummaryToCard } from "@/lib/utils";

const QUICK_LOCATIONS = [
  { label: "Cầu Giấy, HN", city: "Hà Nội", district: "Cầu Giấy" },
  { label: "Đống Đa, HN", city: "Hà Nội", district: "Đống Đa" },
  { label: "Bách - Kinh - Xây", city: "Hà Nội", district: "Hai Bà Trưng" },
  { label: "Bình Thạnh, HCM", city: "Hồ Chí Minh", district: "Bình Thạnh" },
  { label: "Quận 1, HCM", city: "Hồ Chí Minh", district: "Quận 1" },
  { label: "Thanh Xuân, HN", city: "Hà Nội", district: "Thanh Xuân" },
];

export default function HomePage() {
  const router = useRouter();
  const { role } = useAuth();
  const [listings, setListings] = useState<PropertyListing[]>([]);
  const [health, setHealth] = useState<HealthCheck | null>(null);

  useEffect(() => {
    if (role === "Landlord") {
      router.replace("/landlord/dashboard");
    }
  }, [role, router]);

  // Search State
  const [activeCategoryTab, setActiveCategoryTab] = useState<string>("all");
  const [city, setCity] = useState("Tất cả");
  const [district, setDistrict] = useState("Tất cả");
  const [priceRange, setPriceRange] = useState("all");
  const [keyword, setKeyword] = useState("");

  // Commercial Landlord Calculator State
  const [calcRooms, setCalcRooms] = useState(6);
  const [calcRent, setCalcRent] = useState(4500000);

  const [isLoadingListings, setIsLoadingListings] = useState(true);

  useEffect(() => {
    let isMounted = true;
    listingsApi
      .getPublicListings(20)
      .then((res) => {
        if (!isMounted) return;
        if (res.isSuccess && res.data && res.data.length > 0) {
          const mapped: PropertyListing[] = res.data.map((b) => mapListingSummaryToCard(b));
          setListings(mapped);
        } else {
          setListings([]);
        }
      })
      .catch(() => {
        if (isMounted) setListings([]);
      })
      .finally(() => {
        if (isMounted) setIsLoadingListings(false);
      });

    fetchHealth().then(setHealth).catch(() => null);

    if (getStoredToken()) {
      listingsApi.getSavedListings().then((res) => {
        if (isMounted && res.isSuccess && Array.isArray(res.data)) {
          listingService.syncFavorites(res.data.map((s) => String(s.listingId)));
        }
      }).catch(() => null);
    }

    return () => {
      isMounted = false;
    };
  }, []);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const params = new URLSearchParams();
    if (keyword) params.set("q", keyword);
    if (city !== "Tất cả") params.set("city", city);
    if (district !== "Tất cả") params.set("district", district);
    if (priceRange !== "all") params.set("price", priceRange);
    if (activeCategoryTab !== "all") params.set("roomType", activeCategoryTab);
    router.push(`/listings?${params.toString()}`);
  };

  const handleQuickLocation = (locCity: string, locDistrict: string) => {
    router.push(`/listings?city=${encodeURIComponent(locCity)}&district=${encodeURIComponent(locDistrict)}`);
  };

  const displayListings = listings.slice(0, 6);

  // Computed landlord projected income
  const monthlyRevenue = calcRooms * calcRent * 0.95; // 95% occupancy rate
  const annualRevenue = monthlyRevenue * 12;

  if (role === "Landlord") {
    return (
      <div className="min-h-[60vh] flex items-center justify-center p-8">
        <div className="text-center space-y-3">
          <div className="w-12 h-12 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto animate-pulse shadow-xs">
            <Building className="w-6 h-6" />
          </div>
          <p className="text-xs font-bold text-slate-700">Đang chuyển đến Kênh Quản Lý Dành Cho Chủ Nhà...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-16 pb-20">
      {/* Hero Section */}
      <section className="relative bg-gradient-to-b from-slate-950 via-blue-950 to-slate-900 text-white pt-14 pb-20 px-4 sm:px-6 lg:px-8 overflow-hidden">
        {/* Glow & subtle grid background */}
        <div className="absolute inset-0 opacity-15 bg-[radial-gradient(#3b82f6_1px,transparent_1px)] [background-size:24px_24px] pointer-events-none" />
        <div className="absolute -top-24 -left-24 w-96 h-96 bg-blue-600/20 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute top-1/2 -right-24 w-96 h-96 bg-indigo-600/20 rounded-full blur-3xl pointer-events-none" />

        <div className="relative max-w-5xl mx-auto text-center space-y-6">
          {/* Trust Badge */}
          <div className="inline-flex items-center gap-1.5 sm:gap-2 px-3 sm:px-4 py-1 sm:py-1.5 rounded-full bg-white/10 backdrop-blur-md border border-white/20 text-xs font-semibold text-blue-200 max-w-full">
            <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse shrink-0" />
            <span className="hidden sm:inline">Nền tảng thuê phòng số 1 •</span>
            <span className="truncate">100% phòng thật, không phí môi giới</span>
          </div>

          <h1 className="text-3xl sm:text-5xl lg:text-6xl font-black tracking-tight leading-tight sm:leading-none text-white">
            Thuê Phòng Trọ & Căn Hộ <br className="hidden sm:inline" />
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-blue-300 via-sky-200 to-indigo-300">
              Trực Tiếp Từ Chủ Nhà Uy Tín
            </span>
          </h1>

          <p className="max-w-xl mx-auto text-xs sm:text-base text-blue-100/80 font-normal leading-relaxed">
            Thuê phòng trực tiếp từ chủ nhà, minh bạch chi phí và an tâm tiền cọc.
          </p>

          {/* Search Box with Tabs */}
          <div className="mt-8 pt-2 max-w-4xl mx-auto">
            <div className="bg-white rounded-2xl sm:rounded-3xl p-4 sm:p-6 shadow-2xl text-slate-800 border border-white/20 text-left">
              {/* Category tabs */}
              <div className="flex items-center gap-2 overflow-x-auto pb-3 mb-4 border-b border-slate-100 scrollbar-none">
                {[
                  { id: "all", label: "Tất cả" },
                  { id: "Phòng trọ", label: "Phòng trọ" },
                  { id: "Studio", label: "Studio" },
                  { id: "Chung cư mini", label: "Chung cư mini" },
                  { id: "Căn hộ dịch vụ", label: "Căn hộ" },
                  { id: "Ở ghép", label: "Ở ghép" },
                ].map((tab) => (
                  <button
                    key={tab.id}
                    onClick={() => setActiveCategoryTab(tab.id)}
                    className={cn(
                      "px-3.5 py-1.5 rounded-full text-xs font-bold whitespace-nowrap transition-all",
                      activeCategoryTab === tab.id
                        ? "bg-blue-600 text-white shadow-sm"
                        : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                    )}
                  >
                    {tab.label}
                  </button>
                ))}
              </div>

              {/* Main inputs */}
              <form
                onSubmit={handleSearchSubmit}
                className="grid grid-cols-1 sm:grid-cols-2 lg:flex lg:flex-row lg:items-end gap-3"
              >
                {/* Keyword */}
                <div className="sm:col-span-2 lg:flex-1 min-w-0">
                  <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1.5">
                    Từ khóa tìm kiếm
                  </label>
                  <div className="relative">
                    <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
                    <input
                      type="text"
                      placeholder="Gần ĐH Bách Khoa, Cầu Giấy..."
                      value={keyword}
                      onChange={(e) => setKeyword(e.target.value)}
                      className="w-full h-10 pl-9 pr-3 text-xs font-semibold rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-blue-500/20 transition-all placeholder:font-normal placeholder:text-slate-400"
                    />
                  </div>
                </div>

                {/* City */}
                <div className="sm:col-span-1 lg:w-48 shrink-0">
                  <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1.5">
                    Tỉnh / Thành phố
                  </label>
                  <div className="relative">
                    <MapPin className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
                    <select
                      value={city}
                      onChange={(e) => setCity(e.target.value)}
                      className="w-full h-10 pl-9 pr-8 text-xs font-semibold rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-blue-500/20 transition-all cursor-pointer appearance-none"
                    >
                      <option value="Tất cả">Tất cả địa điểm</option>
                      <option value="Hà Nội">Hà Nội</option>
                      <option value="Hồ Chí Minh">TP. Hồ Chí Minh</option>
                    </select>
                    <ChevronDown className="w-3.5 h-3.5 text-slate-400 absolute right-3 top-1/2 -translate-y-1/2 pointer-events-none" />
                  </div>
                </div>

                {/* Price range */}
                <div className="sm:col-span-1 lg:w-44 shrink-0">
                  <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1.5">
                    Khoảng giá
                  </label>
                  <div className="relative">
                    <DollarSign className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
                    <select
                      value={priceRange}
                      onChange={(e) => setPriceRange(e.target.value)}
                      className="w-full h-10 pl-9 pr-8 text-xs font-semibold rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-blue-500/20 transition-all cursor-pointer appearance-none"
                    >
                      <option value="all">Tất cả mức giá</option>
                      <option value="under3">Dưới 3 triệu/tháng</option>
                      <option value="3to5">3 - 5 triệu/tháng</option>
                      <option value="5to8">5 - 8 triệu/tháng</option>
                      <option value="over8">Trên 8 triệu/tháng</option>
                    </select>
                    <ChevronDown className="w-3.5 h-3.5 text-slate-400 absolute right-3 top-1/2 -translate-y-1/2 pointer-events-none" />
                  </div>
                </div>

                {/* Submit button */}
                <div className="sm:col-span-2 lg:w-auto shrink-0 flex items-end">
                  <button
                    type="submit"
                    className="w-full lg:w-auto h-10 px-6 rounded-xl font-bold text-xs text-white bg-blue-600 hover:bg-blue-700 active:bg-blue-800 shadow-md shadow-blue-500/25 transition-all flex items-center justify-center gap-2 whitespace-nowrap"
                  >
                    <Search className="w-4 h-4" />
                    <span>Tìm phòng</span>
                  </button>
                </div>
              </form>

              {/* Quick Location Pills */}
              <div className="mt-4 pt-3 border-t border-slate-100 flex items-center gap-2 flex-wrap text-xs">
                <span className="text-slate-400 font-semibold text-[11px] flex items-center gap-1">
                  <Flame className="w-3.5 h-3.5 text-rose-500" />
                  <span>Khu vực tìm nhiều:</span>
                </span>
                {QUICK_LOCATIONS.map((loc) => (
                  <button
                    key={loc.label}
                    onClick={() => handleQuickLocation(loc.city, loc.district)}
                    className="px-2.5 py-1 rounded-lg bg-slate-100 hover:bg-blue-50 hover:text-blue-600 text-slate-700 font-medium text-[11px] transition-colors"
                  >
                    {loc.label}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Social Proof Stats */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 pt-6 max-w-4xl mx-auto text-left sm:text-center">
            <div className="p-3 bg-white/5 backdrop-blur-md rounded-2xl border border-white/10">
              <span className="block text-2xl font-black text-white">50,000+</span>
              <span className="text-xs text-blue-200">Khách thuê tin cậy</span>
            </div>
            <div className="p-3 bg-white/5 backdrop-blur-md rounded-2xl border border-white/10">
              <span className="block text-2xl font-black text-emerald-400">100%</span>
              <span className="text-xs text-blue-200">Phòng đã duyệt PCCC & an ninh</span>
            </div>
            <div className="p-3 bg-white/5 backdrop-blur-md rounded-2xl border border-white/10">
              <span className="block text-2xl font-black text-sky-400">0 VNĐ</span>
              <span className="text-xs text-blue-200">Hoàn toàn không phí môi giới</span>
            </div>
            <div className="p-3 bg-white/5 backdrop-blur-md rounded-2xl border border-white/10">
              <span className="block text-2xl font-black text-amber-300">4.9 / 5.0 ★</span>
              <span className="text-xs text-blue-200">Đánh giá hài lòng thực tế</span>
            </div>
          </div>
        </div>
      </section>

      {/* Section: 3 Gold Guarantees of HomeMatch */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full -mt-8">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/90 shadow-xl">
          <div className="flex items-start gap-4">
            <div className="w-12 h-12 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center shrink-0">
              <ShieldCheck className="w-6 h-6" />
            </div>
            <div>
              <h3 className="font-bold text-slate-900 text-sm mb-1">100% Ảnh Thật & Kiểm Duyệt</h3>
              <p className="text-xs text-slate-500 leading-relaxed">
                Đội ngũ HomeMatch trực tiếp xác minh thực địa giấy tờ chủ nhà và tình trạng phòng,
                nói không với tin ảo câu view.
              </p>
            </div>
          </div>

          <div className="flex items-start gap-4">
            <div className="w-12 h-12 rounded-2xl bg-emerald-50 text-emerald-600 flex items-center justify-center shrink-0">
              <Zap className="w-6 h-6" />
            </div>
            <div>
              <h3 className="font-bold text-slate-900 text-sm mb-1">Minh Bạch Đơn Giá Điện Nước</h3>
              <p className="text-xs text-slate-500 leading-relaxed">
                Toàn bộ đơn giá điện, nước, phí wifi, rác được công khai niêm yết rõ ràng trước khi
                đặt cọc, không phát sinh chi phí ngầm.
              </p>
            </div>
          </div>

          <div className="flex items-start gap-4">
            <div className="w-12 h-12 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center shrink-0">
              <FileText className="w-6 h-6" />
            </div>
            <div>
              <h3 className="font-bold text-slate-900 text-sm mb-1">Hợp Đồng Bảo Vệ Tiền Cọc</h3>
              <p className="text-xs text-slate-500 leading-relaxed">
                Hợp đồng thuê điện tử chuẩn pháp lý, số hóa lịch sử thanh toán giúp bảo vệ quyền lợi
                người thuê khi thanh lý phòng.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* Listings Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full">
        <div className="flex items-center justify-between mb-8">
          <div>
            <h2 className="text-2xl font-black text-slate-900 tracking-tight">
              Danh sách phòng trọ và căn hộ
            </h2>
          </div>
          <Link
            href="/listings"
            className="flex items-center gap-1.5 text-xs font-bold text-blue-600 hover:text-blue-700 bg-blue-50 px-4 py-2 rounded-xl transition-colors"
          >
            <span>Xem tất cả tin</span>
            <ArrowRight className="w-3.5 h-3.5" />
          </Link>
        </div>

        {displayListings.length > 0 ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {displayListings.map((listing) => (
              <ListingCard key={listing.id} listing={listing} />
            ))}
          </div>
        ) : (
          <div className="bg-slate-50 border border-dashed border-slate-200 rounded-2xl p-8 text-center">
            <Building className="w-8 h-8 text-slate-400 mx-auto mb-2" />
            <p className="text-sm font-semibold text-slate-700">Chưa có tin đăng nào</p>
            <p className="text-xs text-slate-500 mt-1">Các tin đăng mới nhất sẽ tự động hiển thị khi chủ nhà đăng phòng.</p>
          </div>
        )}
      </section>

      {/* Commercial Landlord ROI Calculator Widget */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full">
        <div className="bg-gradient-to-r from-blue-900 via-indigo-900 to-slate-900 rounded-3xl p-8 sm:p-12 text-white shadow-2xl relative overflow-hidden">
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-center">
            {/* Left 6 cols: Pitch */}
            <div className="lg:col-span-6 space-y-4">
              <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-white/10 text-blue-200">
                <Calculator className="w-3.5 h-3.5" />
                <span>Dành Riêng Cho Chủ Nhà & Nhà Đầu Tư Cho Thuê</span>
              </span>
              <h2 className="text-2xl sm:text-4xl font-black tracking-tight leading-snug">
                Ước Tính Thu Nhập & Tối Ưu Quản Lý Cùng HomeMatch
              </h2>
              <p className="text-xs sm:text-sm text-blue-100/80 leading-relaxed">
                Tự động hóa toàn bộ quy trình: Từ đăng tin tiếp cận 50,000+ khách thuê, ghi số điện nước
                tính tiền tự động, đến nhắc nợ và cảnh báo hợp đồng sắp hết hạn trong 30 ngày.
              </p>

              <div className="flex flex-wrap gap-4 pt-2">
                <Link
                  href="/landlord/dashboard"
                  className="px-6 py-3 rounded-xl bg-blue-500 hover:bg-blue-400 text-white font-bold text-xs shadow-lg shadow-blue-500/30 transition-all flex items-center gap-2"
                >
                  <span>Mở Dashboard Chủ Nhà</span>
                  <ArrowRight className="w-4 h-4" />
                </Link>
                <Link
                  href="/landlord/listings"
                  className="px-6 py-3 rounded-xl bg-white/10 hover:bg-white/20 text-white font-bold text-xs border border-white/20 transition-all"
                >
                  <span>Đăng tin cho thuê miễn phí</span>
                </Link>
              </div>
            </div>

            {/* Right 6 cols: Live Calculator Widget */}
            <div className="lg:col-span-6 bg-white/10 backdrop-blur-md p-6 sm:p-8 rounded-2xl border border-white/20 space-y-5">
              <h3 className="font-black text-lg text-white">Bảng Tính Dòng Tiền Dự Kiến</h3>

              <div className="space-y-4 text-xs">
                <div>
                  <div className="flex justify-between font-bold mb-1.5">
                    <span>Số lượng phòng cho thuê:</span>
                    <span className="text-blue-300 font-black text-sm">{calcRooms} phòng</span>
                  </div>
                  <input
                    type="range"
                    min={1}
                    max={30}
                    value={calcRooms}
                    onChange={(e) => setCalcRooms(Number(e.target.value))}
                    className="w-full accent-blue-400 cursor-pointer"
                  />
                </div>

                <div>
                  <div className="flex justify-between font-bold mb-1.5">
                    <span>Giá thuê bình quân / phòng / tháng:</span>
                    <span className="text-blue-300 font-black text-sm">
                      {(calcRent / 1000000).toFixed(1)} triệu đ
                    </span>
                  </div>
                  <input
                    type="range"
                    min={2000000}
                    max={15000000}
                    step={500000}
                    value={calcRent}
                    onChange={(e) => setCalcRent(Number(e.target.value))}
                    className="w-full accent-blue-400 cursor-pointer"
                  />
                </div>
              </div>

              {/* Calculated Outputs */}
              <div className="grid grid-cols-2 gap-4 p-4 rounded-xl bg-slate-900/80 border border-white/10 text-center">
                <div>
                  <span className="text-[10px] text-slate-400 uppercase font-bold block">
                    Doanh thu dự kiến / tháng
                  </span>
                  <span className="text-xl sm:text-2xl font-black text-emerald-400 mt-1 block">
                    {(monthlyRevenue / 1000000).toLocaleString("vi-VN", { maximumFractionDigits: 1 })} tr
                  </span>
                  <span className="text-[10px] text-slate-400">Tỷ lệ lấp đầy 95%</span>
                </div>

                <div>
                  <span className="text-[10px] text-slate-400 uppercase font-bold block">
                    Dòng tiền dự kiến / năm
                  </span>
                  <span className="text-xl sm:text-2xl font-black text-blue-300 mt-1 block">
                    {(annualRevenue / 1000000).toLocaleString("vi-VN", { maximumFractionDigits: 0 })} tr
                  </span>
                  <span className="text-[10px] text-slate-400">Tự động đối chiếu công nợ</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Social Proof & Customer Reviews */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full space-y-8">
        <div className="text-center max-w-2xl mx-auto">
          <div className="flex items-center justify-center gap-1 text-amber-500 mb-2">
            {[1, 2, 3, 4, 5].map((i) => (
              <Star key={i} className="w-5 h-5 fill-current" />
            ))}
          </div>
          <h2 className="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight">
            Khách Thuê & Chủ Nhà Nói Gì Về HomeMatch?
          </h2>
          <p className="text-xs sm:text-sm text-slate-500 mt-2">
            Hơn 1,200 đánh giá 5 sao từ cộng đồng người thuê và chủ trọ trên toàn quốc
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs flex flex-col justify-between h-full">
            <div className="space-y-3 flex-1 flex flex-col">
              <div className="flex items-center gap-1 text-amber-500">
                {[1, 2, 3, 4, 5].map((i) => (
                  <Star key={i} className="w-4 h-4 fill-current" />
                ))}
              </div>
              <p className="text-xs text-slate-600 leading-relaxed italic flex-1">
                "Tìm phòng qua HomeMatch rất an tâm vì thông tin và hình ảnh sát thực tế. Đặt lịch xem phòng nhanh chóng, trực tiếp với chủ trọ mà không lo bị môi giới lừa cọc."
              </p>
            </div>
            <div className="flex items-center gap-3 pt-4 border-t border-slate-100 mt-4">
              <div className="w-9 h-9 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold text-xs shrink-0">
                K
              </div>
              <div>
                <h4 className="font-bold text-xs text-slate-900">Đỗ Trung Kiên</h4>
                <span className="text-[10px] text-slate-500">Người thuê trọ Cầu Giấy</span>
              </div>
            </div>
          </div>

          <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs flex flex-col justify-between h-full">
            <div className="space-y-3 flex-1 flex flex-col">
              <div className="flex items-center gap-1 text-amber-500">
                {[1, 2, 3, 4, 5].map((i) => (
                  <Star key={i} className="w-4 h-4 fill-current" />
                ))}
              </div>
              <p className="text-xs text-slate-600 leading-relaxed italic flex-1">
                "Quản lý phòng trọ và hóa đơn điện nước tiện lợi hơn hẳn. Cuối tháng hệ thống tự động tổng hợp số liệu và gửi hóa đơn, theo dõi công nợ rất rõ ràng và chuẩn xác."
              </p>
            </div>
            <div className="flex items-center gap-3 pt-4 border-t border-slate-100 mt-4">
              <div className="w-9 h-9 rounded-full bg-indigo-600 text-white flex items-center justify-center font-bold text-xs shrink-0">
                H
              </div>
              <div>
                <h4 className="font-bold text-xs text-slate-900">Nguyễn Văn Hùng</h4>
                <span className="text-[10px] text-slate-500">Chủ tòa nhà 12 phòng Cầu Giấy</span>
              </div>
            </div>
          </div>

          <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs flex flex-col justify-between h-full">
            <div className="space-y-3 flex-1 flex flex-col">
              <div className="flex items-center gap-1 text-amber-500">
                {[1, 2, 3, 4, 5].map((i) => (
                  <Star key={i} className="w-4 h-4 fill-current" />
                ))}
              </div>
              <p className="text-xs text-slate-600 leading-relaxed italic flex-1">
                "Tính năng so sánh phòng cực kỳ hữu ích! Mình đối chiếu dễ dàng chi tiết giá điện, nước, diện tích và tiện ích giữa các phòng gần trường trước khi đặt cọc giữ chỗ."
              </p>
            </div>
            <div className="flex items-center gap-3 pt-4 border-t border-slate-100 mt-4">
              <div className="w-9 h-9 rounded-full bg-emerald-600 text-white flex items-center justify-center font-bold text-xs shrink-0">
                T
              </div>
              <div>
                <h4 className="font-bold text-xs text-slate-900">Trần Phương Thảo</h4>
                <span className="text-[10px] text-slate-500">Sinh viên ĐH Ngoại Thương</span>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
