"use client";

import React, { useState, useEffect, useCallback, useMemo } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import { landlordDashboardService } from "@/lib/services";
import { LandlordDashboardData } from "@/types";
import {
  Building2,
  Home,
  FileText,
  DollarSign,
  AlertTriangle,
  Zap,
  Calendar,
  Users,
  Clock,
  TrendingUp,
  PieChart,
  Filter,
  CheckCircle2,
  AlertCircle,
  Phone,
  ShieldCheck,
  Eye,
  RefreshCw,
  Droplets,
  Wrench,
  Sparkles,
  Search,
  MessageSquare,
  ArrowUpRight,
  ExternalLink,
} from "lucide-react";

export default function LandlordDashboardPage() {
  const { user, role, switchDemoRole } = useAuth();
  const [data, setData] = useState<LandlordDashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedBuilding, setSelectedBuilding] = useState("all");
  const [selectedPeriod, setSelectedPeriod] = useState("11/2026");
  const [debtFilter, setDebtFilter] = useState<string>("all");
  const [actionNotice, setActionNotice] = useState<string | null>(null);
  const [debtSearch, setDebtSearch] = useState("");

  const loadDashboard = useCallback(async (building: string, period: string) => {
    setLoading(true);
    setError(null);
    try {
      const res = await landlordDashboardService.getDashboard(building, period);
      setData(res);
    } catch (err) {
      setData(null);
      setError(
        err instanceof Error
          ? err.message
          : "Không thể kết nối đến Backend API (http://localhost:5000)."
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadDashboard(selectedBuilding, selectedPeriod);
  }, [loadDashboard, selectedBuilding, selectedPeriod]);

  const handleRemindDebt = (tenantName: string, roomName: string, amount: number) => {
    setActionNotice(
      `Đã tạo và gửi tin nhắn nhắc nợ số tiền ${amount.toLocaleString("vi-VN")} đ đến khách ${tenantName} (${roomName}).`
    );
    setTimeout(() => setActionNotice(null), 4000);
  };

  const handleQuickDemoLandlord = () => {
    switchDemoRole?.("Landlord");
    setActionNotice("Đã chuyển sang tài khoản Chủ nhà (Nguyễn Văn Hùng). Dữ liệu được đồng bộ ngay lập tức.");
    setTimeout(() => setActionNotice(null), 3000);
  };

  // Filtered debt items
  const filteredDebtItems = useMemo(() => {
    if (!data?.debtAging.items) return [];
    return data.debtAging.items.filter((item) => {
      const matchBucket = debtFilter === "all" || item.agingBucket === debtFilter;
      const matchSearch =
        item.roomName.toLowerCase().includes(debtSearch.toLowerCase()) ||
        item.tenantName.toLowerCase().includes(debtSearch.toLowerCase()) ||
        item.tenantPhone.includes(debtSearch);
      return matchBucket && matchSearch;
    });
  }, [data?.debtAging.items, debtFilter, debtSearch]);

  // Auth Guard Screen with 1-Click Demo Login
  if (!user || role !== "Landlord") {
    return (
      <div className="max-w-xl mx-auto px-4 py-16 text-center space-y-6">
        <div className="w-20 h-20 rounded-3xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-sm ring-8 ring-blue-50/50">
          <Building2 className="w-10 h-10" />
        </div>
        <div className="space-y-2">
          <h2 className="text-2xl font-black text-slate-900 tracking-tight">
            Kênh Quản Lý Dành Cho Chủ Nhà
          </h2>
          <p className="text-xs text-slate-500 max-w-md mx-auto leading-relaxed">
            {role === "Tenant"
              ? "Bạn hiện đang ở vai trò Người thuê (Tenant). Vui lòng chuyển sang vai trò Chủ nhà để trải nghiệm toàn bộ bảng điều khiển tài chính, công nợ và quản lý phòng."
              : "Vui lòng đăng nhập với tài khoản Chủ nhà để theo dõi số phòng trống, doanh thu, phân loại công nợ theo tuổi nợ và quản lý tiền cọc."}
          </p>
        </div>

        {/* 1-Click Demo Switcher */}
        <div className="p-5 rounded-2xl bg-gradient-to-br from-blue-50/80 to-indigo-50/80 border border-blue-200/80 text-left space-y-3">
          <div className="flex items-center gap-2 text-blue-900 font-bold text-xs">
            <Sparkles className="w-4 h-4 text-blue-600" />
            <span>Trải nghiệm nhanh cho Giám khảo & Người dùng thử nghiệm:</span>
          </div>
          <p className="text-[11px] text-blue-700 leading-relaxed">
            Nhấn nút bên dưới để đăng nhập ngay với tài khoản <strong>Chủ nhà Demo (Nguyễn Văn Hùng)</strong> mà không cần nhập mật khẩu.
          </p>
          <button
            onClick={handleQuickDemoLandlord}
            className="w-full py-2.5 px-4 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs shadow-md shadow-blue-500/20 transition-all flex items-center justify-center gap-2 cursor-pointer"
          >
            <Building2 className="w-4 h-4" />
            <span>Đăng nhập nhanh với quyền Chủ Nhà (1 chạm)</span>
          </button>
        </div>

        <div className="flex items-center justify-center gap-4 text-xs text-slate-400">
          <span>hoặc</span>
          <Link href="/auth/login?redirect=/landlord/dashboard" className="text-blue-600 hover:underline font-semibold">
            Đăng nhập tài khoản cá nhân
          </Link>
          <span>•</span>
          <Link href="/auth/register" className="text-blue-600 hover:underline font-semibold">
            Đăng ký tài khoản mới
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-8">
      {/* Toast Notification */}
      {actionNotice && (
        <div className="p-4 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-800 text-xs font-semibold flex items-center justify-between shadow-sm animate-in fade-in duration-200">
          <div className="flex items-center gap-2">
            <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
            <span>{actionNotice}</span>
          </div>
          <button
            onClick={() => setActionNotice(null)}
            className="text-emerald-600 hover:text-emerald-900 font-bold ml-4 cursor-pointer"
          >
            ✕
          </button>
        </div>
      )}

      {/* Top Banner & Filter Controls (Chức năng 2: Lọc dashboard và báo cáo) */}
      <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs flex flex-col lg:flex-row items-start lg:items-center justify-between gap-6">
        <div>
          <div className="flex flex-wrap items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Building2 className="w-4 h-4" />
            <span>Phân Hệ Quản Lý Chủ Nhà</span>
            <span className="text-slate-300">•</span>
            <span className="text-slate-600 font-normal">Chủ nhà: {user.displayName}</span>
            {data && !error && (
              <>
                <span className="text-slate-300">•</span>
                <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-emerald-50 text-emerald-700 border border-emerald-200 normal-case tracking-normal">
                  <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                  Dữ liệu thời gian thực từ PostgreSQL (homematch_db)
                </span>
              </>
            )}
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Dashboard & Báo Cáo Tài Chính
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Giám sát thời gian thực số căn trống, doanh thu, phân loại công nợ theo tuổi nợ và theo dõi tiền cọc.
          </p>
        </div>

        {/* Filter Toolbar (Chức năng 2) */}
        <div className="flex flex-wrap items-center gap-3 w-full lg:w-auto bg-slate-50 p-2.5 rounded-xl border border-slate-200">
          <div className="flex items-center gap-2 text-xs font-semibold text-slate-600 pl-1">
            <Filter className="w-3.5 h-3.5 text-slate-400" />
            <span>Bộ lọc:</span>
          </div>

          {/* Building Filter */}
          <div className="flex-1 sm:flex-initial">
            <select
              value={selectedBuilding}
              onChange={(e) => setSelectedBuilding(e.target.value)}
              className="w-full text-xs font-medium rounded-lg border-slate-200 bg-white py-1.5 px-3 text-slate-700 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 shadow-xs cursor-pointer"
            >
              <option value="all">Tất cả tòa nhà & nhà trọ</option>
              <option value="bld-001">HomeMatch Green - Cầu Giấy</option>
              <option value="bld-002">Sunrise House - Đống Đa</option>
              <option value="bld-003">EcoLife Studio - Nam Từ Liêm</option>
            </select>
          </div>

          {/* Period Filter */}
          <div className="flex-1 sm:flex-initial">
            <select
              value={selectedPeriod}
              onChange={(e) => setSelectedPeriod(e.target.value)}
              className="w-full text-xs font-medium rounded-lg border-slate-200 bg-white py-1.5 px-3 text-slate-700 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 shadow-xs cursor-pointer"
            >
              <option value="11/2026">Kỳ 11/2026 (Hiện tại)</option>
              <option value="10/2026">Kỳ 10/2026 (Tháng trước)</option>
              <option value="09/2026">Kỳ 09/2026</option>
            </select>
          </div>

          {/* Refresh Button */}
          <button
            onClick={() => loadDashboard(selectedBuilding, selectedPeriod)}
            disabled={loading}
            className="p-1.5 text-slate-500 hover:text-blue-600 hover:bg-white rounded-lg transition-colors border border-transparent hover:border-slate-200 cursor-pointer"
            title="Làm mới số liệu"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? "animate-spin text-blue-600" : ""}`} />
          </button>
        </div>
      </div>

      {loading && !data ? (
        <div className="py-20 text-center space-y-3">
          <RefreshCw className="w-8 h-8 text-blue-600 animate-spin mx-auto" />
          <p className="text-xs text-slate-500 font-medium">Đang tải dữ liệu trực tiếp từ PostgreSQL...</p>
        </div>
      ) : error ? (
        <div className="bg-white p-8 rounded-2xl border border-rose-200 shadow-xs text-center max-w-xl mx-auto space-y-4">
          <div className="w-14 h-14 rounded-2xl bg-rose-50 text-rose-600 flex items-center justify-center mx-auto">
            <AlertTriangle className="w-7 h-7" />
          </div>
          <div className="space-y-1.5">
            <h3 className="text-base font-black text-slate-900">
              Chưa Kết Nối Được Backend API &amp; PostgreSQL
            </h3>
            <p className="text-xs text-rose-600 font-medium">{error}</p>
            <p className="text-[11px] text-slate-500 leading-relaxed pt-1">
              Hệ thống đã tắt chế độ Mock Data. Vui lòng khởi động Backend bằng lệnh sau rồi bấm thử lại:
            </p>
            <code className="block text-[11px] bg-slate-900 text-emerald-400 px-3 py-2 rounded-lg font-mono">
              dotnet run --project backend/src/HomeMatch.Api
            </code>
          </div>
          <button
            onClick={() => loadDashboard(selectedBuilding, selectedPeriod)}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs shadow-sm transition-all cursor-pointer"
          >
            <RefreshCw className="w-3.5 h-3.5" />
            <span>Thử lại kết nối</span>
          </button>
        </div>
      ) : data ? (
        <>
          {/* SECTION 1: Các Thẻ Thống Kê Tổng Quan (Chức năng 1) */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {/* Card 1: Căn trống */}
            <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs space-y-2 hover:border-blue-300 transition-all">
              <div className="flex items-center justify-between text-slate-500">
                <span className="text-xs font-semibold">Căn trống</span>
                <span className="p-2 rounded-xl bg-amber-50 text-amber-600">
                  <Home className="w-4 h-4" />
                </span>
              </div>
              <div className="flex items-baseline gap-2">
                <span className="text-3xl font-black text-slate-900">{data.overview.vacantRooms}</span>
                <span className="text-xs text-slate-400 font-medium">/ {data.overview.totalRooms} căn</span>
              </div>
              <div className="flex items-center gap-1.5 text-[11px] text-amber-600 font-medium">
                <AlertCircle className="w-3.5 h-3.5 shrink-0" />
                <span>Cần đẩy mạnh tin đăng để lấp đầy</span>
              </div>
            </div>

            {/* Card 2: Đang thuê & Tỷ lệ lấp đầy */}
            <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs space-y-2 hover:border-emerald-300 transition-all">
              <div className="flex items-center justify-between text-slate-500">
                <span className="text-xs font-semibold">Căn đang thuê</span>
                <span className="p-2 rounded-xl bg-emerald-50 text-emerald-600">
                  <TrendingUp className="w-4 h-4" />
                </span>
              </div>
              <div className="flex items-baseline gap-2">
                <span className="text-3xl font-black text-slate-900">{data.overview.rentedRooms}</span>
                <span className="text-xs text-emerald-600 font-bold bg-emerald-50 px-2 py-0.5 rounded-md">
                  {data.overview.occupancyRate}% lấp đầy
                </span>
              </div>
              <div className="flex items-center gap-1.5 text-[11px] text-slate-400 font-medium">
                <CheckCircle2 className="w-3.5 h-3.5 text-emerald-500 shrink-0" />
                <span>Vận hành ổn định theo hợp đồng</span>
              </div>
            </div>

            {/* Card 3: Tin đang hiển thị */}
            <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs space-y-2 hover:border-blue-300 transition-all">
              <div className="flex items-center justify-between text-slate-500">
                <span className="text-xs font-semibold">Tin đang hiển thị</span>
                <span className="p-2 rounded-xl bg-blue-50 text-blue-600">
                  <FileText className="w-4 h-4" />
                </span>
              </div>
              <div className="flex items-baseline gap-2">
                <span className="text-3xl font-black text-slate-900">{data.overview.activeListings}</span>
                <span className="text-xs text-blue-600 font-bold bg-blue-50 px-2 py-0.5 rounded-md">
                  {data.marketingAndLeads.totalViewsCount} lượt xem
                </span>
              </div>
              <div className="flex items-center gap-1.5 text-[11px] text-slate-400 font-medium">
                <Eye className="w-3.5 h-3.5 text-blue-500 shrink-0" />
                <span>Tiếp cận khách thuê tìm phòng</span>
              </div>
            </div>

            {/* Card 4: Tổng tiền cọc đang giữ */}
            <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs space-y-2 hover:border-indigo-300 transition-all">
              <div className="flex items-center justify-between text-slate-500">
                <span className="text-xs font-semibold">Tiền cọc đang giữ</span>
                <span className="p-2 rounded-xl bg-indigo-50 text-indigo-600">
                  <ShieldCheck className="w-4 h-4" />
                </span>
              </div>
              <div className="flex items-baseline gap-1">
                <span className="text-2xl font-black text-slate-900">
                  {data.deposits.totalActiveDeposits.toLocaleString("vi-VN")}
                </span>
                <span className="text-xs text-slate-400 font-bold">đ</span>
              </div>
              <div className="flex items-center gap-1.5 text-[11px] text-indigo-600 font-medium">
                <ShieldCheck className="w-3.5 h-3.5 text-indigo-500 shrink-0" />
                <span>{data.deposits.activeContractsCount} hợp đồng hiệu lực</span>
              </div>
            </div>
          </div>

          {/* SECTION 2 & 3: Báo Cáo Doanh Thu, Khoản Thu & Cơ Cấu Thu (Chức năng 3) */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Financial Overview & Progress */}
            <div className="lg:col-span-2 bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-6">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                <div>
                  <h3 className="font-bold text-sm text-slate-900 flex items-center gap-2">
                    <DollarSign className="w-4 h-4 text-emerald-600" />
                    <span>Báo Cáo Doanh Thu & Thu Tiền (Kỳ {selectedPeriod})</span>
                  </h3>
                  <p className="text-xs text-slate-500 mt-0.5">
                    Tổng hợp khoản phải thu, số tiền thực tế đã thu và tỷ lệ hoàn thành thu hồi dòng tiền.
                  </p>
                </div>
                <span className="px-3 py-1 rounded-full text-xs font-bold bg-emerald-50 text-emerald-700 border border-emerald-200/60 self-start sm:self-auto">
                  Thu đạt {data.financials.collectionRate}%
                </span>
              </div>

              {/* Progress bar */}
              <div className="space-y-2">
                <div className="flex justify-between text-xs font-semibold text-slate-600">
                  <span>Tiến độ thu hồi dòng tiền kỳ này</span>
                  <span className="font-bold text-slate-900">
                    {data.financials.totalCollected.toLocaleString("vi-VN")} đ / {data.financials.totalReceivables.toLocaleString("vi-VN")} đ
                  </span>
                </div>
                <div className="w-full h-3.5 rounded-full bg-slate-100 overflow-hidden flex p-0.5 border border-slate-200/60">
                  <div
                    className="bg-emerald-500 h-full transition-all duration-500 rounded-full"
                    style={{ width: `${Math.min(100, data.financials.collectionRate)}%` }}
                  />
                </div>
              </div>

              {/* Metric Stat Cards */}
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-1">
                <div className="p-4 rounded-xl bg-slate-50 border border-slate-100 space-y-1">
                  <span className="text-[11px] font-semibold text-slate-500 block">Tổng Phải Thu (Doanh thu)</span>
                  <span className="text-xl font-black text-slate-900 block">
                    {data.financials.totalReceivables.toLocaleString("vi-VN")} đ
                  </span>
                  <span className="text-[10px] text-slate-400">Tiền phòng + dịch vụ phát sinh</span>
                </div>

                <div className="p-4 rounded-xl bg-emerald-50/60 border border-emerald-100 space-y-1">
                  <span className="text-[11px] font-semibold text-emerald-700 block">Số Tiền Đã Thu</span>
                  <span className="text-xl font-black text-emerald-800 block">
                    {data.financials.totalCollected.toLocaleString("vi-VN")} đ
                  </span>
                  <span className="text-[10px] text-emerald-600 font-medium">Đã vào tài khoản / tiền mặt</span>
                </div>

                <div className="p-4 rounded-xl bg-rose-50/60 border border-rose-100 space-y-1">
                  <span className="text-[11px] font-semibold text-rose-700 block">Tổng Còn Phải Thu (Nợ)</span>
                  <span className="text-xl font-black text-rose-700 block">
                    {data.financials.remainingDebt.toLocaleString("vi-VN")} đ
                  </span>
                  <span className="text-[10px] text-rose-600 font-medium">Chưa thanh toán hoặc nợ tồn</span>
                </div>
              </div>
            </div>

            {/* Revenue Breakdown */}
            <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-4">
              <div className="flex items-center gap-2 text-slate-900 font-bold text-sm">
                <PieChart className="w-4 h-4 text-blue-600" />
                <span>Cơ Cấu Doanh Thu Kỳ Này</span>
              </div>

              <div className="space-y-3 pt-2">
                {/* Rent */}
                <div className="space-y-1">
                  <div className="flex justify-between text-xs">
                    <span className="flex items-center gap-1.5 text-slate-700 font-medium">
                      <Home className="w-3.5 h-3.5 text-blue-500" />
                      <span>Tiền thuê phòng:</span>
                    </span>
                    <span className="font-bold text-slate-900">
                      {data.financials.breakdown.rentAmount.toLocaleString("vi-VN")} đ
                    </span>
                  </div>
                  <div className="w-full h-1.5 rounded-full bg-slate-100 overflow-hidden">
                    <div
                      className="bg-blue-500 h-full"
                      style={{
                        width: `${data.financials.totalReceivables > 0 ? (data.financials.breakdown.rentAmount / data.financials.totalReceivables) * 100 : 0}%`,
                      }}
                    />
                  </div>
                </div>

                {/* Electricity */}
                <div className="space-y-1">
                  <div className="flex justify-between text-xs">
                    <span className="flex items-center gap-1.5 text-slate-700 font-medium">
                      <Zap className="w-3.5 h-3.5 text-amber-500" />
                      <span>Tiền điện:</span>
                    </span>
                    <span className="font-bold text-slate-900">
                      {data.financials.breakdown.electricityAmount.toLocaleString("vi-VN")} đ
                    </span>
                  </div>
                  <div className="w-full h-1.5 rounded-full bg-slate-100 overflow-hidden">
                    <div
                      className="bg-amber-500 h-full"
                      style={{
                        width: `${data.financials.totalReceivables > 0 ? (data.financials.breakdown.electricityAmount / data.financials.totalReceivables) * 100 : 0}%`,
                      }}
                    />
                  </div>
                </div>

                {/* Water */}
                <div className="space-y-1">
                  <div className="flex justify-between text-xs">
                    <span className="flex items-center gap-1.5 text-slate-700 font-medium">
                      <Droplets className="w-3.5 h-3.5 text-cyan-500" />
                      <span>Tiền nước:</span>
                    </span>
                    <span className="font-bold text-slate-900">
                      {data.financials.breakdown.waterAmount.toLocaleString("vi-VN")} đ
                    </span>
                  </div>
                  <div className="w-full h-1.5 rounded-full bg-slate-100 overflow-hidden">
                    <div
                      className="bg-cyan-500 h-full"
                      style={{
                        width: `${data.financials.totalReceivables > 0 ? (data.financials.breakdown.waterAmount / data.financials.totalReceivables) * 100 : 0}%`,
                      }}
                    />
                  </div>
                </div>

                {/* Services */}
                <div className="space-y-1">
                  <div className="flex justify-between text-xs">
                    <span className="flex items-center gap-1.5 text-slate-700 font-medium">
                      <Wrench className="w-3.5 h-3.5 text-indigo-500" />
                      <span>Phí dịch vụ:</span>
                    </span>
                    <span className="font-bold text-slate-900">
                      {data.financials.breakdown.serviceAmount.toLocaleString("vi-VN")} đ
                    </span>
                  </div>
                  <div className="w-full h-1.5 rounded-full bg-slate-100 overflow-hidden">
                    <div
                      className="bg-indigo-500 h-full"
                      style={{
                        width: `${data.financials.totalReceivables > 0 ? (data.financials.breakdown.serviceAmount / data.financials.totalReceivables) * 100 : 0}%`,
                      }}
                    />
                  </div>
                </div>
              </div>

              <div className="p-3.5 rounded-xl bg-slate-50 text-[11px] text-slate-600 leading-relaxed border border-slate-100 mt-2">
                💡 <strong>Gợi ý:</strong> Tiền thuê chiếm phần lớn dòng tiền. Nên chốt chỉ số điện nước trước ngày 25 hàng tháng để phát hành hóa đơn đồng loạt.
              </div>
            </div>
          </div>

          {/* SECTION 4: Báo Cáo Công Nợ & Phân Nhóm Tuổi Nợ (Chức năng 4) */}
          <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-6">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div>
                <h3 className="font-bold text-sm text-slate-900 flex items-center gap-2">
                  <AlertTriangle className="w-4 h-4 text-amber-500" />
                  <span>Báo Cáo Công Nợ & Phân Loại Nhóm Tuổi Nợ</span>
                </h3>
                <p className="text-xs text-slate-500 mt-0.5">
                  Theo dõi số tiền nợ còn lại, nợ quá hạn và phân loại theo 4 nhóm tuổi nợ để có kế hoạch thu hồi kịp thời.
                </p>
              </div>

              <div className="flex items-center gap-2">
                <span className="text-xs text-slate-500 font-medium">Tổng nợ quá hạn: </span>
                <span className="text-base font-black text-rose-600 bg-rose-50 px-2.5 py-1 rounded-lg border border-rose-200">
                  {data.debtAging.overdueDebt.toLocaleString("vi-VN")} đ
                </span>
              </div>
            </div>

            {/* 4 Nhóm tuổi nợ tương tác */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              {/* Bucket 1: Trong hạn */}
              <button
                type="button"
                onClick={() => setDebtFilter(debtFilter === "Trong hạn" ? "all" : "Trong hạn")}
                className={`p-4 rounded-xl text-left border transition-all cursor-pointer ${
                  debtFilter === "Trong hạn"
                    ? "bg-emerald-100/70 border-emerald-400 ring-2 ring-emerald-400/20"
                    : "bg-emerald-50/50 border-emerald-200/60 hover:bg-emerald-100/40"
                }`}
              >
                <div className="flex items-center justify-between mb-1">
                  <span className="text-xs font-bold text-emerald-800">Trong hạn</span>
                  <span className="w-2.5 h-2.5 rounded-full bg-emerald-500"></span>
                </div>
                <span className="text-lg font-black text-slate-900 block">
                  {data.debtAging.currentAmount.toLocaleString("vi-VN")} đ
                </span>
                <span className="text-[10px] text-emerald-700">Chưa đến hạn thanh toán</span>
              </button>

              {/* Bucket 2: Quá hạn 1 - 30 ngày */}
              <button
                type="button"
                onClick={() => setDebtFilter(debtFilter === "1-30 ngày" ? "all" : "1-30 ngày")}
                className={`p-4 rounded-xl text-left border transition-all cursor-pointer ${
                  debtFilter === "1-30 ngày"
                    ? "bg-amber-100/70 border-amber-400 ring-2 ring-amber-400/20"
                    : "bg-amber-50/50 border-amber-200/60 hover:bg-amber-100/40"
                }`}
              >
                <div className="flex items-center justify-between mb-1">
                  <span className="text-xs font-bold text-amber-800">Quá hạn 1 - 30 ngày</span>
                  <span className="w-2.5 h-2.5 rounded-full bg-amber-500"></span>
                </div>
                <span className="text-lg font-black text-slate-900 block">
                  {data.debtAging.overdue1To30Days.toLocaleString("vi-VN")} đ
                </span>
                <span className="text-[10px] text-amber-700">Cần gửi nhắc nợ định kỳ</span>
              </button>

              {/* Bucket 3: Quá hạn 31 - 60 ngày */}
              <button
                type="button"
                onClick={() => setDebtFilter(debtFilter === "31-60 ngày" ? "all" : "31-60 ngày")}
                className={`p-4 rounded-xl text-left border transition-all cursor-pointer ${
                  debtFilter === "31-60 ngày"
                    ? "bg-orange-100/70 border-orange-400 ring-2 ring-orange-400/20"
                    : "bg-orange-50/50 border-orange-200/60 hover:bg-orange-100/40"
                }`}
              >
                <div className="flex items-center justify-between mb-1">
                  <span className="text-xs font-bold text-orange-800">Quá hạn 31 - 60 ngày</span>
                  <span className="w-2.5 h-2.5 rounded-full bg-orange-500"></span>
                </div>
                <span className="text-lg font-black text-slate-900 block">
                  {data.debtAging.overdue31To60Days.toLocaleString("vi-VN")} đ
                </span>
                <span className="text-[10px] text-orange-700">Cảnh báo nhắc nhở trực tiếp</span>
              </button>

              {/* Bucket 4: Quá hạn > 60 ngày */}
              <button
                type="button"
                onClick={() => setDebtFilter(debtFilter === ">60 ngày" ? "all" : ">60 ngày")}
                className={`p-4 rounded-xl text-left border transition-all cursor-pointer ${
                  debtFilter === ">60 ngày"
                    ? "bg-rose-100/70 border-rose-400 ring-2 ring-rose-400/20"
                    : "bg-rose-50/50 border-rose-200/60 hover:bg-rose-100/40"
                }`}
              >
                <div className="flex items-center justify-between mb-1">
                  <span className="text-xs font-bold text-rose-800">Quá hạn &gt; 60 ngày</span>
                  <span className="w-2.5 h-2.5 rounded-full bg-rose-500"></span>
                </div>
                <span className="text-lg font-black text-slate-900 block">
                  {data.debtAging.overdueOver60Days.toLocaleString("vi-VN")} đ
                </span>
                <span className="text-[10px] text-rose-700 font-semibold">Nguy cơ nợ xấu, xem xét cấn cọc</span>
              </button>
            </div>

            {/* Danh sách các khoản nợ chi tiết & Search Toolbar */}
            <div className="space-y-3">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-bold text-slate-800">
                    Danh Sách Khoản Nợ Cần Thu Hồi ({filteredDebtItems.length})
                  </span>
                  {debtFilter !== "all" && (
                    <span className="text-[11px] bg-slate-100 text-slate-700 px-2 py-0.5 rounded-md flex items-center gap-1">
                      Lọc: <strong>{debtFilter}</strong>
                      <button onClick={() => setDebtFilter("all")} className="hover:text-rose-600 ml-1">✕</button>
                    </span>
                  )}
                </div>

                <div className="relative w-full sm:w-64">
                  <Search className="w-3.5 h-3.5 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                  <input
                    type="text"
                    value={debtSearch}
                    onChange={(e) => setDebtSearch(e.target.value)}
                    placeholder="Tìm theo phòng, tên khách..."
                    className="w-full text-xs pl-8 pr-3 py-1.5 rounded-lg border border-slate-200 focus:outline-hidden focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                  />
                </div>
              </div>

              {filteredDebtItems.length === 0 ? (
                <div className="p-8 text-center text-slate-400 text-xs bg-slate-50 rounded-xl space-y-1">
                  <CheckCircle2 className="w-6 h-6 text-emerald-500 mx-auto mb-1" />
                  <p className="font-semibold text-slate-700">Không có hóa đơn nợ nào phù hợp với bộ lọc hiện tại.</p>
                  <p className="text-[11px] text-slate-400">Tất cả khách thuê trong nhóm này đã thanh toán đầy đủ.</p>
                </div>
              ) : (
                <div className="overflow-x-auto border border-slate-100 rounded-xl">
                  <table className="w-full text-left text-xs">
                    <thead>
                      <tr className="border-b border-slate-200/80 bg-slate-50/70 text-slate-500 font-semibold">
                        <th className="py-2.5 px-3">Phòng / Tòa nhà</th>
                        <th className="py-2.5 px-3">Khách thuê</th>
                        <th className="py-2.5 px-3">Hạn chót</th>
                        <th className="py-2.5 px-3">Số tiền nợ</th>
                        <th className="py-2.5 px-3">Nhóm tuổi nợ</th>
                        <th className="py-2.5 px-3 text-right">Thao tác</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 bg-white">
                      {filteredDebtItems.map((item) => (
                        <tr key={item.invoiceId} className="hover:bg-slate-50/80 transition-colors">
                          <td className="py-3 px-3 font-bold text-slate-900">{item.roomName}</td>
                          <td className="py-3 px-3">
                            <span className="font-semibold text-slate-800 block">{item.tenantName}</span>
                            <span className="text-[10px] text-slate-400 flex items-center gap-1 mt-0.5">
                              <Phone className="w-3 h-3 text-slate-400" />
                              {item.tenantPhone}
                            </span>
                          </td>
                          <td className="py-3 px-3 text-slate-500">{item.dueDate}</td>
                          <td className="py-3 px-3 font-black text-rose-600">
                            {item.amount.toLocaleString("vi-VN")} đ
                          </td>
                          <td className="py-3 px-3">
                            <span
                              className={`inline-block px-2.5 py-0.5 rounded-full text-[10px] font-bold ${
                                item.agingBucket === "Trong hạn"
                                  ? "bg-emerald-50 text-emerald-700 border border-emerald-200"
                                  : item.agingBucket === "1-30 ngày"
                                    ? "bg-amber-50 text-amber-700 border border-amber-200"
                                    : item.agingBucket === "31-60 ngày"
                                      ? "bg-orange-50 text-orange-700 border border-orange-200"
                                      : "bg-rose-50 text-rose-700 border border-rose-200"
                              }`}
                            >
                              {item.agingBucket} {item.overdueDays > 0 ? `(${item.overdueDays} ngày)` : ""}
                            </span>
                          </td>
                          <td className="py-3 px-3 text-right">
                            <button
                              onClick={() => handleRemindDebt(item.tenantName, item.roomName, item.amount)}
                              className="px-3 py-1.5 rounded-lg bg-blue-50 text-blue-600 hover:bg-blue-600 hover:text-white font-bold text-[11px] transition-all cursor-pointer flex items-center gap-1.5 ml-auto"
                            >
                              <MessageSquare className="w-3 h-3" />
                              <span>Nhắc nợ</span>
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>

          {/* SECTION 5: Theo Dõi Tiền Cọc (Chức năng 5) */}
          <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-4">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div>
                <h3 className="font-bold text-sm text-slate-900 flex items-center gap-2">
                  <ShieldCheck className="w-4 h-4 text-indigo-600" />
                  <span>Theo Dõi Tiền Cọc Hợp Đồng Còn Hiệu Lực</span>
                </h3>
                <p className="text-xs text-slate-500 mt-0.5">
                  Tổng tiền cọc đang giữ và danh sách hợp đồng kèm cảnh báo hợp đồng sắp hết hạn để chuẩn bị hoàn trả.
                </p>
              </div>

              <div className="p-3 rounded-xl bg-indigo-50 border border-indigo-100 flex items-center gap-3">
                <span className="text-xs font-semibold text-indigo-900">Tổng cọc đang giữ:</span>
                <span className="text-lg font-black text-indigo-700">
                  {data.deposits.totalActiveDeposits.toLocaleString("vi-VN")} đ
                </span>
              </div>
            </div>

            <div className="space-y-2 pt-1">
              {data.deposits.items.map((ctr) => (
                <div
                  key={ctr.contractId}
                  className="p-4 rounded-xl border border-slate-200 hover:border-indigo-300 transition-all flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-white"
                >
                  <div className="space-y-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-bold text-xs text-slate-900">{ctr.roomName}</span>
                      <span className="text-[10px] text-slate-400 font-mono">({ctr.contractCode})</span>
                      {ctr.daysRemaining <= 30 && (
                        <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-50 text-amber-700 border border-amber-200 flex items-center gap-1">
                          <AlertCircle className="w-3 h-3 text-amber-500" />
                          Sắp hết hạn ({ctr.daysRemaining} ngày)
                        </span>
                      )}
                    </div>
                    <span className="text-xs text-slate-500 block">
                      Khách thuê: <strong>{ctr.tenantName}</strong> • Hiệu lực từ {ctr.startDate} đến {ctr.endDate}
                    </span>
                  </div>

                  <div className="flex items-center justify-between sm:justify-end gap-5">
                    <div className="text-right">
                      <span className="text-[10px] text-slate-400 block font-medium">Tiền cọc giữ:</span>
                      <span className="font-black text-xs text-slate-900">
                        {ctr.depositAmount.toLocaleString("vi-VN")} đ
                      </span>
                    </div>
                    <span className="px-2.5 py-1 rounded-lg text-[10px] font-bold bg-slate-100 text-slate-700">
                      {ctr.depositStatus}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* SECTION 6: Thống Kê Tin Đăng, Phễu Khách & Lịch Xem Phòng (Chức năng 6) */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Phễu Khách Quan Tâm */}
            <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="font-bold text-sm text-slate-900 flex items-center gap-2">
                    <Users className="w-4 h-4 text-blue-600" />
                    <span>Phễu Khách Quan Tâm (Leads Funnel)</span>
                  </h3>
                  <p className="text-xs text-slate-500 mt-0.5">
                    Hiệu quả chuyển đổi từ tin đăng đến hợp đồng đã ký.
                  </p>
                </div>
                <div className="text-right">
                  <span className="text-[10px] text-slate-400 block">Tin đang hiển thị</span>
                  <span className="text-xs font-black text-blue-600">{data.overview.activeListings} tin</span>
                </div>
              </div>

              {/* Funnel visual bars with rates */}
              <div className="space-y-3 pt-2">
                {/* Stage 1 */}
                <div className="space-y-1">
                  <div className="flex justify-between text-xs font-semibold text-slate-700">
                    <span className="flex items-center gap-1.5">
                      <Eye className="w-3.5 h-3.5 text-blue-500" />
                      <span>1. Khách mới quan tâm</span>
                    </span>
                    <span className="font-bold text-slate-900">{data.marketingAndLeads.newLeads} khách (100%)</span>
                  </div>
                  <div className="w-full h-2 rounded-full bg-slate-100 overflow-hidden">
                    <div className="bg-blue-500 h-full w-full rounded-full" />
                  </div>
                </div>

                {/* Stage 2 */}
                <div className="space-y-1">
                  <div className="flex justify-between text-xs font-semibold text-slate-700">
                    <span className="flex items-center gap-1.5">
                      <Phone className="w-3.5 h-3.5 text-indigo-500" />
                      <span>2. Đã liên hệ tư vấn</span>
                    </span>
                    <span className="font-bold text-slate-900">
                      {data.marketingAndLeads.contactedLeads} khách (
                      {data.marketingAndLeads.newLeads > 0
                        ? Math.round((data.marketingAndLeads.contactedLeads / data.marketingAndLeads.newLeads) * 100)
                        : 0}
                      %)
                    </span>
                  </div>
                  <div className="w-full h-2 rounded-full bg-slate-100 overflow-hidden">
                    <div
                      className="bg-indigo-500 h-full rounded-full"
                      style={{
                        width: `${data.marketingAndLeads.newLeads > 0 ? (data.marketingAndLeads.contactedLeads / data.marketingAndLeads.newLeads) * 100 : 0}%`,
                      }}
                    />
                  </div>
                </div>

                {/* Stage 3 */}
                <div className="space-y-1">
                  <div className="flex justify-between text-xs font-semibold text-slate-700">
                    <span className="flex items-center gap-1.5">
                      <Calendar className="w-3.5 h-3.5 text-amber-500" />
                      <span>3. Đặt lịch xem phòng</span>
                    </span>
                    <span className="font-bold text-slate-900">
                      {data.marketingAndLeads.viewingScheduledLeads} khách (
                      {data.marketingAndLeads.newLeads > 0
                        ? Math.round(
                            (data.marketingAndLeads.viewingScheduledLeads / data.marketingAndLeads.newLeads) * 100
                          )
                        : 0}
                      %)
                    </span>
                  </div>
                  <div className="w-full h-2 rounded-full bg-slate-100 overflow-hidden">
                    <div
                      className="bg-amber-500 h-full rounded-full"
                      style={{
                        width: `${data.marketingAndLeads.newLeads > 0 ? (data.marketingAndLeads.viewingScheduledLeads / data.marketingAndLeads.newLeads) * 100 : 0}%`,
                      }}
                    />
                  </div>
                </div>

                {/* Stage 4 */}
                <div className="space-y-1">
                  <div className="flex justify-between text-xs font-semibold text-slate-700">
                    <span className="flex items-center gap-1.5">
                      <CheckCircle2 className="w-3.5 h-3.5 text-emerald-500" />
                      <span>4. Đã ký hợp đồng thuê</span>
                    </span>
                    <span className="font-bold text-emerald-600">
                      {data.marketingAndLeads.contractSignedLeads} hợp đồng (
                      {data.marketingAndLeads.newLeads > 0
                        ? Math.round(
                            (data.marketingAndLeads.contractSignedLeads / data.marketingAndLeads.newLeads) * 100
                          )
                        : 0}
                      %)
                    </span>
                  </div>
                  <div className="w-full h-2 rounded-full bg-slate-100 overflow-hidden">
                    <div
                      className="bg-emerald-500 h-full rounded-full"
                      style={{
                        width: `${data.marketingAndLeads.newLeads > 0 ? (data.marketingAndLeads.contractSignedLeads / data.marketingAndLeads.newLeads) * 100 : 0}%`,
                      }}
                    />
                  </div>
                </div>
              </div>
            </div>

            {/* Lịch Xem Phòng Hôm Nay */}
            <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="font-bold text-sm text-slate-900 flex items-center gap-2">
                    <Calendar className="w-4 h-4 text-emerald-600" />
                    <span>Lịch Xem Phòng Trong Ngày</span>
                  </h3>
                  <p className="text-xs text-slate-500 mt-0.5">
                    Các cuộc hẹn với khách thuê phòng hôm nay.
                  </p>
                </div>
                <span className="px-2.5 py-1 rounded-full text-[10px] font-bold bg-emerald-50 text-emerald-700 border border-emerald-200">
                  {data.viewingsToday.length} cuộc hẹn
                </span>
              </div>

              <div className="space-y-2.5 pt-1">
                {data.viewingsToday.length === 0 ? (
                  <div className="p-6 text-center text-slate-400 text-xs bg-slate-50 rounded-xl">
                    Hôm nay chưa có lịch hẹn xem phòng nào.
                  </div>
                ) : (
                  data.viewingsToday.map((vw) => (
                    <div
                      key={vw.appointmentId}
                      className="p-3.5 rounded-xl border border-slate-100 hover:border-slate-300 bg-slate-50/50 hover:bg-white transition-all flex items-center justify-between gap-3"
                    >
                      <div className="flex items-center gap-3">
                        <div className="p-2.5 rounded-lg bg-blue-100 text-blue-700 font-bold text-xs shrink-0 flex items-center gap-1">
                          <Clock className="w-3.5 h-3.5" />
                          <span>{vw.scheduledTime}</span>
                        </div>
                        <div>
                          <span className="font-bold text-xs text-slate-900 block">{vw.roomName}</span>
                          <span className="text-[11px] text-slate-500 block">
                            Khách: <strong>{vw.tenantName}</strong> • {vw.tenantPhone}
                          </span>
                        </div>
                      </div>

                      <div className="text-right shrink-0">
                        <span className="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-blue-50 text-blue-700 border border-blue-200 block mb-1">
                          {vw.viewingType}
                        </span>
                        <span className="text-[10px] text-emerald-600 font-bold">{vw.status}</span>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>

          {/* Quick Operation Navigators */}
          <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-4">
            <h3 className="font-bold text-sm text-slate-900">Lối Tắt Vận Hành Nghiệp Vụ</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <Link
                href="/landlord/properties"
                className="p-4 rounded-xl border border-slate-200 hover:border-blue-400 hover:bg-blue-50/30 transition-all flex items-start gap-3 group"
              >
                <div className="p-2.5 rounded-lg bg-blue-100 text-blue-600 group-hover:scale-110 transition-transform">
                  <Home className="w-5 h-5" />
                </div>
                <div>
                  <span className="font-bold text-xs text-slate-900 group-hover:text-blue-600 block flex items-center gap-1">
                    Quản lý căn & phòng <ArrowUpRight className="w-3 h-3 opacity-0 group-hover:opacity-100 transition-opacity" />
                  </span>
                  <span className="text-[11px] text-slate-500 mt-0.5 block">
                    Cập nhật trạng thái Còn trống, Đã thuê hoặc Tạm ngưng.
                  </span>
                </div>
              </Link>

              <Link
                href="/landlord/utilities"
                className="p-4 rounded-xl border border-slate-200 hover:border-blue-400 hover:bg-blue-50/30 transition-all flex items-start gap-3 group"
              >
                <div className="p-2.5 rounded-lg bg-amber-100 text-amber-600 group-hover:scale-110 transition-transform">
                  <Zap className="w-5 h-5" />
                </div>
                <div>
                  <span className="font-bold text-xs text-slate-900 group-hover:text-blue-600 block flex items-center gap-1">
                    Ghi chỉ số & Hóa đơn <ArrowUpRight className="w-3 h-3 opacity-0 group-hover:opacity-100 transition-opacity" />
                  </span>
                  <span className="text-[11px] text-slate-500 mt-0.5 block">
                    Nhập số công tơ điện nước và tự động tính hóa đơn.
                  </span>
                </div>
              </Link>

              <Link
                href="/landlord/listings"
                className="p-4 rounded-xl border border-slate-200 hover:border-blue-400 hover:bg-blue-50/30 transition-all flex items-start gap-3 group"
              >
                <div className="p-2.5 rounded-lg bg-indigo-100 text-indigo-600 group-hover:scale-110 transition-transform">
                  <FileText className="w-5 h-5" />
                </div>
                <div>
                  <span className="font-bold text-xs text-slate-900 group-hover:text-blue-600 block flex items-center gap-1">
                    Đăng tin cho thuê mới <ArrowUpRight className="w-3 h-3 opacity-0 group-hover:opacity-100 transition-opacity" />
                  </span>
                  <span className="text-[11px] text-slate-500 mt-0.5 block">
                    Soạn tin đăng, đính kèm ảnh, bảng tiện ích và biểu phí.
                  </span>
                </div>
              </Link>

              <Link
                href="/landlord/contracts"
                className="p-4 rounded-xl border border-slate-200 hover:border-blue-400 hover:bg-blue-50/30 transition-all flex items-start gap-3 group"
              >
                <div className="p-2.5 rounded-lg bg-emerald-100 text-emerald-600 group-hover:scale-110 transition-transform">
                  <Users className="w-5 h-5" />
                </div>
                <div>
                  <span className="font-bold text-xs text-slate-900 group-hover:text-blue-600 block flex items-center gap-1">
                    Hợp đồng & Khách thuê <ArrowUpRight className="w-3 h-3 opacity-0 group-hover:opacity-100 transition-opacity" />
                  </span>
                  <span className="text-[11px] text-slate-500 mt-0.5 block">
                    Lập hợp đồng điện tử và theo dõi thời hạn hợp đồng.
                  </span>
                </div>
              </Link>
            </div>
          </div>
        </>
      ) : null}
    </div>
  );
}
