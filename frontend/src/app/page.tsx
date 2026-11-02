"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { listingService } from "@/lib/services";
import { fetchHealth, fetchSystemStatus } from "@/lib/api-client";
import { PropertyListing, HealthCheck, SystemStatus } from "@/types";
import { ListingCard } from "@/features/listings/ListingCard";
import {
  Search,
  MapPin,
  DollarSign,
  Building,
  ShieldCheck,
  Zap,
  Calendar,
  Sparkles,
  ArrowRight,
  CheckCircle,
  FileText,
  MessageSquare,
} from "lucide-react";

export default function HomePage() {
  const router = useRouter();
  const [listings, setListings] = useState<PropertyListing[]>([]);
  const [health, setHealth] = useState<HealthCheck | null>(null);
  const [systemStatus, setSystemStatus] = useState<SystemStatus | null>(null);

  // Search Bar State
  const [city, setCity] = useState("Tất cả");
  const [district, setDistrict] = useState("Tất cả");
  const [priceRange, setPriceRange] = useState("all");
  const [roomType, setRoomType] = useState("all");

  useEffect(() => {
    setListings(listingService.getAll());
    fetchHealth().then(setHealth).catch(() => null);
    fetchSystemStatus().then(setSystemStatus).catch(() => null);
  }, []);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const params = new URLSearchParams();
    if (city !== "Tất cả") params.set("city", city);
    if (district !== "Tất cả") params.set("district", district);
    if (priceRange !== "all") params.set("price", priceRange);
    if (roomType !== "all") params.set("roomType", roomType);
    router.push(`/listings?${params.toString()}`);
  };

  const featuredListings = listings.filter((x) => x.isFeatured).slice(0, 3);
  const recentListings = listings.slice(0, 4);

  return (
    <div className="flex flex-col gap-16 pb-16">
      {/* Hero Section */}
      <section className="relative bg-gradient-to-b from-blue-900 via-blue-800 to-slate-900 text-white pt-16 pb-24 px-4 sm:px-6 lg:px-8 overflow-hidden">
        {/* Background glow effects */}
        <div className="absolute top-0 left-1/2 -translate-x-1/2 w-full max-w-7xl h-full overflow-hidden pointer-events-none opacity-20">
          <div className="absolute top-1/4 left-1/4 w-96 h-96 bg-blue-400 rounded-full blur-3xl" />
          <div className="absolute bottom-1/4 right-1/4 w-96 h-96 bg-indigo-400 rounded-full blur-3xl" />
        </div>

        <div className="relative max-w-5xl mx-auto text-center space-y-6">
          {/* Status Badge */}
          <div className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-white/10 backdrop-blur-md border border-white/20 text-xs font-medium text-blue-200">
            <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
            <span>Kết nối Backend: {health?.status === "Healthy" ? "Trực Tuyến (.NET 10)" : "Sẵn Sàng"}</span>
            <span className="text-white/40">•</span>
            <span>8 Modules Độc Lập</span>
          </div>

          <h1 className="text-3xl sm:text-5xl lg:text-6xl font-black tracking-tight leading-tight sm:leading-none text-white">
            Tìm Phòng Nhanh Chóng <br className="hidden sm:inline" />
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-blue-300 via-sky-200 to-indigo-300">
              Quản Lý Cho Thuê Thông Minh
            </span>
          </h1>

          <p className="max-w-2xl mx-auto text-sm sm:text-base text-blue-100/80 font-normal leading-relaxed">
            Kết nối trực tiếp giữa người thuê và chủ nhà. Không qua trung gian, minh bạch chi phí
            điện nước, đặt lịch xem phòng trực tiếp và quản lý hợp đồng điện tử.
          </p>

          {/* Interactive Search Box */}
          <div className="mt-8 pt-4">
            <form
              onSubmit={handleSearchSubmit}
              className="bg-white rounded-2xl p-4 sm:p-5 shadow-2xl text-slate-800 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 border border-white/20 text-left"
            >
              {/* City / Province */}
              <div>
                <label className="block text-[11px] font-bold text-slate-500 uppercase tracking-wider mb-1">
                  Khu vực (Tỉnh/Thành)
                </label>
                <div className="relative">
                  <MapPin className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
                  <select
                    value={city}
                    onChange={(e) => setCity(e.target.value)}
                    className="w-full pl-9 pr-3 py-2 text-xs font-semibold rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-blue-500/20"
                  >
                    <option value="Tất cả">Tất cả địa điểm</option>
                    <option value="Hà Nội">Hà Nội</option>
                    <option value="Hồ Chí Minh">TP. Hồ Chí Minh</option>
                  </select>
                </div>
              </div>

              {/* Price range */}
              <div>
                <label className="block text-[11px] font-bold text-slate-500 uppercase tracking-wider mb-1">
                  Khoảng giá (VND)
                </label>
                <div className="relative">
                  <DollarSign className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
                  <select
                    value={priceRange}
                    onChange={(e) => setPriceRange(e.target.value)}
                    className="w-full pl-9 pr-3 py-2 text-xs font-semibold rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-blue-500/20"
                  >
                    <option value="all">Tất cả mức giá</option>
                    <option value="under3">Dưới 3 triệu/tháng</option>
                    <option value="3to5">3 - 5 triệu/tháng</option>
                    <option value="5to8">5 - 8 triệu/tháng</option>
                    <option value="over8">Trên 8 triệu/tháng</option>
                  </select>
                </div>
              </div>

              {/* Room type */}
              <div>
                <label className="block text-[11px] font-bold text-slate-500 uppercase tracking-wider mb-1">
                  Loại hình phòng
                </label>
                <div className="relative">
                  <Building className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
                  <select
                    value={roomType}
                    onChange={(e) => setRoomType(e.target.value)}
                    className="w-full pl-9 pr-3 py-2 text-xs font-semibold rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-none focus:ring-2 focus:ring-blue-500/20"
                  >
                    <option value="all">Tất cả loại phòng</option>
                    <option value="Phòng trọ">Phòng trọ</option>
                    <option value="Chung cư mini">Chung cư mini</option>
                    <option value="Căn hộ dịch vụ">Căn hộ dịch vụ</option>
                  </select>
                </div>
              </div>

              {/* Submit CTA */}
              <div className="flex items-end">
                <button
                  type="submit"
                  className="w-full py-2.5 px-4 rounded-xl font-bold text-xs text-white bg-blue-600 hover:bg-blue-700 active:bg-blue-800 shadow-lg shadow-blue-500/30 transition-all flex items-center justify-center gap-2"
                >
                  <Search className="w-4 h-4" />
                  <span>Tìm phòng ngay</span>
                </button>
              </div>
            </form>
          </div>

          {/* Key Metric Counters */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 pt-6 text-center max-w-4xl mx-auto">
            <div className="p-3 bg-white/5 backdrop-blur-md rounded-xl border border-white/10">
              <span className="block text-2xl font-black text-white">100%</span>
              <span className="text-xs text-blue-200">Tin chính chủ, có ảnh thật</span>
            </div>
            <div className="p-3 bg-white/5 backdrop-blur-md rounded-xl border border-white/10">
              <span className="block text-2xl font-black text-emerald-400">0 VNĐ</span>
              <span className="text-xs text-blue-200">Không phí môi giới</span>
            </div>
            <div className="p-3 bg-white/5 backdrop-blur-md rounded-xl border border-white/10">
              <span className="block text-2xl font-black text-white">≤ 5 Bước</span>
              <span className="text-xs text-blue-200">Đặt lịch xem phòng nhanh</span>
            </div>
            <div className="p-3 bg-white/5 backdrop-blur-md rounded-xl border border-white/10">
              <span className="block text-2xl font-black text-sky-400">Tối đa 3</span>
              <span className="text-xs text-blue-200">So sánh đối chiếu cùng lúc</span>
            </div>
          </div>
        </div>
      </section>

      {/* Featured Listings Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full">
        <div className="flex items-center justify-between mb-8">
          <div>
            <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
              <Sparkles className="w-4 h-4" />
              <span>Phòng nổi bật được đánh giá cao</span>
            </div>
            <h2 className="text-2xl font-black text-slate-900 tracking-tight">
              Phòng Trọ & Căn Hộ Hot Nhất
            </h2>
          </div>
          <Link
            href="/listings"
            className="flex items-center gap-1.5 text-xs font-bold text-blue-600 hover:text-blue-700 bg-blue-50 px-3.5 py-2 rounded-xl transition-colors"
          >
            <span>Xem tất cả tin</span>
            <ArrowRight className="w-3.5 h-3.5" />
          </Link>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {featuredListings.map((listing) => (
            <ListingCard key={listing.id} listing={listing} />
          ))}
        </div>
      </section>

      {/* Value Pillars for Both Roles */}
      <section className="bg-slate-100/70 py-16 px-4 sm:px-6 lg:px-8 border-y border-slate-200/80">
        <div className="max-w-7xl mx-auto space-y-12">
          <div className="text-center max-w-2xl mx-auto">
            <h2 className="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight">
              Giải Pháp Toàn Diện Cho Người Thuê & Chủ Nhà
            </h2>
            <p className="text-xs sm:text-sm text-slate-500 mt-2">
              Đáp ứng đầy đủ 11 phân hệ nghiệp vụ theo chuẩn kiến trúc Modular Monolith
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            {/* Column 1: For Tenant */}
            <div className="bg-white p-6 sm:p-8 rounded-2xl border border-slate-200 shadow-sm space-y-4">
              <div className="inline-flex p-3 rounded-xl bg-blue-50 text-blue-600">
                <Search className="w-6 h-6" />
              </div>
              <h3 className="text-xl font-bold text-slate-900">Dành Cho Người Thuê</h3>
              <p className="text-xs text-slate-600 leading-relaxed">
                Tìm kiếm phòng nhanh chóng theo bán kính, mức giá và tiện ích mong muốn. Dễ dàng so
                sánh giữa các lựa chọn trước khi đưa ra quyết định.
              </p>
              <ul className="space-y-2.5 text-xs text-slate-700 pt-2">
                <li className="flex items-center gap-2">
                  <CheckCircle className="w-4 h-4 text-emerald-500 shrink-0" />
                  <span>Bộ lọc chi tiết: giá thuê, diện tích, máy giặt, điều hòa, giờ tự do</span>
                </li>
                <li className="flex items-center gap-2">
                  <CheckCircle className="w-4 h-4 text-emerald-500 shrink-0" />
                  <span>Bảng so sánh tối đa 3 phòng đối chiếu trực quan</span>
                </li>
                <li className="flex items-center gap-2">
                  <CheckCircle className="w-4 h-4 text-emerald-500 shrink-0" />
                  <span>Đặt lịch xem phòng trực tiếp, nhận thông báo xác nhận từ chủ nhà</span>
                </li>
                <li className="flex items-center gap-2">
                  <CheckCircle className="w-4 h-4 text-emerald-500 shrink-0" />
                  <span>Theo dõi công nợ, hóa đơn điện nước hàng tháng chính xác</span>
                </li>
              </ul>
              <div className="pt-4">
                <Link
                  href="/listings"
                  className="inline-flex items-center gap-2 text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 px-4 py-2.5 rounded-xl shadow-sm transition-all"
                >
                  <span>Khám phá phòng ngay</span>
                  <ArrowRight className="w-4 h-4" />
                </Link>
              </div>
            </div>

            {/* Column 2: For Landlord */}
            <div className="bg-white p-6 sm:p-8 rounded-2xl border border-slate-200 shadow-sm space-y-4">
              <div className="inline-flex p-3 rounded-xl bg-indigo-50 text-indigo-600">
                <Building className="w-6 h-6" />
              </div>
              <h3 className="text-xl font-bold text-slate-900">Dành Cho Chủ Nhà</h3>
              <p className="text-xs text-slate-600 leading-relaxed">
                Tối ưu hóa quản lý phòng trọ, theo dõi dòng tiền doanh thu, tự động hóa tính tiền điện
                nước và quản lý hợp đồng thuê chuẩn xác.
              </p>
              <ul className="space-y-2.5 text-xs text-slate-700 pt-2">
                <li className="flex items-center gap-2">
                  <CheckCircle className="w-4 h-4 text-indigo-500 shrink-0" />
                  <span>Quản lý phòng: cập nhật trạng thái Còn trống, Đã thuê, Tạm ngừng</span>
                </li>
                <li className="flex items-center gap-2">
                  <CheckCircle className="w-4 h-4 text-indigo-500 shrink-0" />
                  <span>Nhập số điện nước đầu/cuối kỳ, tính tiền tự động theo đơn giá cấu hình</span>
                </li>
                <li className="flex items-center gap-2">
                  <CheckCircle className="w-4 h-4 text-indigo-500 shrink-0" />
                  <span>Quản lý hợp đồng, cảnh báo hợp đồng sắp hết hạn trong 30 ngày</span>
                </li>
                <li className="flex items-center gap-2">
                  <CheckCircle className="w-4 h-4 text-indigo-500 shrink-0" />
                  <span>Báo cáo doanh thu, công nợ phải thu và tổng tiền cọc đang giữ</span>
                </li>
              </ul>
              <div className="pt-4">
                <Link
                  href="/landlord/dashboard"
                  className="inline-flex items-center gap-2 text-xs font-bold text-slate-800 bg-slate-100 hover:bg-slate-200 px-4 py-2.5 rounded-xl transition-all"
                >
                  <span>Vào trang quản lý chủ nhà</span>
                  <ArrowRight className="w-4 h-4" />
                </Link>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Recent Listings Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full">
        <div className="flex items-center justify-between mb-8">
          <div>
            <h2 className="text-2xl font-black text-slate-900 tracking-tight">
              Phòng Mới Đăng Gần Đây
            </h2>
            <p className="text-xs text-slate-500 mt-1">Cập nhật liên tục các tin phòng mới nhất</p>
          </div>
          <Link
            href="/listings"
            className="text-xs font-bold text-blue-600 hover:text-blue-700"
          >
            Xem tất cả ({listings.length}) →
          </Link>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
          {recentListings.map((listing) => (
            <ListingCard key={listing.id} listing={listing} />
          ))}
        </div>
      </section>
    </div>
  );
}
