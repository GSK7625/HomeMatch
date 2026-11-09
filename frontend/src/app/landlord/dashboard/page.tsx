"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import { leasingService, billingService } from "@/lib/services";
import { propertiesApi, listingsApi } from "@/lib/api-client";
import { LeaseContract, MonthlyBill, PropertyDto, ListingSummaryDto } from "@/types";
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
  ArrowRight,
  TrendingUp,
  PieChart,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function LandlordDashboardPage() {
  const { user, role } = useAuth();
  const [properties, setProperties] = useState<PropertyDto[]>([]);
  const [listings, setListings] = useState<ListingSummaryDto[]>([]);
  const [contracts, setContracts] = useState<LeaseContract[]>([]);
  const [bills, setBills] = useState<MonthlyBill[]>([]);
  const [selectedBuilding, setSelectedBuilding] = useState("all");
  const [selectedPeriod, setSelectedPeriod] = useState("11/2026");

  useEffect(() => {
    propertiesApi.getMyProperties().then((res) => {
      if (res.isSuccess && res.data) setProperties(res.data);
    }).catch(() => null);

    listingsApi.getMyListings().then((res) => {
      if (res.isSuccess && res.data) setListings(res.data);
    }).catch(() => null);

    setContracts(leasingService.getAll());
    setBills(billingService.getAll());
  }, []);

  if (!user) {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <Building2 className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Kênh Quản Lý Dành Cho Chủ Nhà</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập với tài khoản Chủ nhà để quản lý phòng trọ, theo dõi dòng tiền và phát hành hóa đơn.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/landlord/dashboard"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng nhập tài khoản Chủ nhà</span>
          </Link>
        </div>
      </div>
    );
  }

  if (role === "Tenant") {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-amber-50 text-amber-600 flex items-center justify-center mx-auto shadow-xs">
          <Building2 className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Yêu Cầu Quyền Chủ Nhà</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Tài khoản của bạn hiện đang mang vai trò <strong>Người thuê (Tenant)</strong>. Để sử dụng các tính năng dành cho chủ nhà, vui lòng đăng ký tài khoản với vai trò Chủ nhà (Landlord).
        </p>
        <div className="pt-2">
          <Link
            href="/auth/register"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng ký tài khoản Chủ nhà</span>
          </Link>
        </div>
      </div>
    );
  }

  // Compute metrics from real data
  const totalRooms = listings.length;
  const vacantRooms = listings.filter((x) => x.status === "Published").length;
  const rentedRooms = totalRooms > vacantRooms ? totalRooms - vacantRooms : 0;
  const activeListings = listings.filter((x) => x.status === "Published").length;

  // Financial aggregates
  const totalReceivables = bills.reduce((acc, b) => acc + b.totalDue, 0);
  const totalCollected = bills.reduce((acc, b) => acc + b.paidAmount, 0);
  const totalDebt = bills.reduce((acc, b) => acc + b.remainingDebt, 0);
  const totalDeposits = contracts.reduce((acc, c) => acc + c.depositAmount, 0);

  // Contracts expiring in next 30 days (Section 3.7)
  const expiringContracts = contracts.filter((c) => c.status === "Sắp hết hạn" || (c.daysRemaining !== undefined && c.daysRemaining <= 30));

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-8">
      {/* Top Banner & Filter Controls (Section 3.9) */}
      <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Building2 className="w-4 h-4" />
            <span>Phân Hệ Quản Lý Chủ Nhà</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Dashboard Tài Chính & Vận Hành Cho Thuê
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Tổng quan căn phòng, doanh thu thực thu, theo dõi công nợ và tiền cọc theo thời gian thực.
          </p>
        </div>

        {/* Filters */}
        <div className="flex items-center gap-3 w-full md:w-auto">
          <div>
            <label className="block text-[10px] font-bold text-slate-400 uppercase mb-1">
              Tòa nhà / Khu vực
            </label>
            <select
              value={selectedBuilding}
              onChange={(e) => setSelectedBuilding(e.target.value)}
              className="px-3 py-1.5 text-xs font-semibold rounded-xl border border-slate-200 bg-slate-50"
            >
              <option value="all">Tất cả bất động sản ({properties.length})</option>
              {properties.map((p) => (
                <option key={p.id} value={String(p.id)}>
                  {p.name} ({p.addressDetail})
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-[10px] font-bold text-slate-400 uppercase mb-1">
              Kỳ thống kê
            </label>
            <select
              value={selectedPeriod}
              onChange={(e) => setSelectedPeriod(e.target.value)}
              className="px-3 py-1.5 text-xs font-semibold rounded-xl border border-slate-200 bg-slate-50"
            >
              <option value="11/2026">Tháng 11/2026</option>
              <option value="10/2026">Tháng 10/2026</option>
              <option value="09/2026">Tháng 09/2026</option>
            </select>
          </div>
        </div>
      </div>

      {/* Quick Action Alerts: Viewing Appointments & Contract Expiration */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Viewing appointments quick card */}
        <div className="bg-blue-50/80 border border-blue-200/90 rounded-2xl p-4 sm:p-5 flex items-center justify-between gap-4 text-blue-950 shadow-2xs">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-blue-600 text-white flex items-center justify-center shrink-0 shadow-sm">
              <Calendar className="w-5 h-5" />
            </div>
            <div>
              <h4 className="font-bold text-sm text-blue-950">
                Lịch Hẹn Xem Phòng Của Khách
              </h4>
              <p className="text-xs text-blue-800/80 mt-0.5">
                Xem yêu cầu đặt lịch cho các phòng của bạn và duyệt lịch hẹn.
              </p>
            </div>
          </div>
          <Link
            href="/landlord/viewings"
            className="px-3.5 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs shrink-0 shadow-xs transition-all flex items-center gap-1"
          >
            <span>Xem lịch</span>
            <ArrowRight className="w-3.5 h-3.5" />
          </Link>
        </div>

        {/* 30-Day Contract Expiration Alert (Section 3.7) */}
        {expiringContracts.length > 0 ? (
          <div className="bg-amber-50 border border-amber-200 rounded-2xl p-4 sm:p-5 flex items-center justify-between gap-4 text-amber-900 shadow-2xs">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-amber-500 text-white flex items-center justify-center shrink-0 shadow-sm">
                <AlertTriangle className="w-5 h-5" />
              </div>
              <div>
                <h4 className="font-bold text-sm text-amber-950">
                  Có {expiringContracts.length} hợp đồng sắp hết hạn!
                </h4>
                <p className="text-xs text-amber-800/90 mt-0.5">
                  Cần liên hệ gia hạn hoặc chuẩn bị bàn giao phòng.
                </p>
              </div>
            </div>
            <Link
              href="/landlord/contracts"
              className="px-3.5 py-2 rounded-xl bg-amber-600 hover:bg-amber-700 text-white font-bold text-xs shrink-0 shadow-xs transition-all"
            >
              Xem ngay →
            </Link>
          </div>
        ) : (
          <div className="bg-emerald-50 border border-emerald-200/80 rounded-2xl p-4 sm:p-5 flex items-center justify-between gap-4 text-emerald-950 shadow-2xs">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-emerald-600 text-white flex items-center justify-center shrink-0 shadow-sm">
                <Building2 className="w-5 h-5" />
              </div>
              <div>
                <h4 className="font-bold text-sm text-emerald-950">
                  Hợp Đồng Đang Ổn Định
                </h4>
                <p className="text-xs text-emerald-800/80 mt-0.5">
                  Không có hợp đồng nào đến hạn thanh lý trong 30 ngày.
                </p>
              </div>
            </div>
            <Link
              href="/landlord/contracts"
              className="px-3.5 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs shrink-0 shadow-xs transition-all"
            >
              Hợp đồng →
            </Link>
          </div>
        )}
      </div>

      {/* 6 Key Operational & Financial Metrics Cards (Section 3.9) */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
        {/* Total Rooms */}
        <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-xs">
          <span className="text-[11px] font-bold text-slate-400 uppercase block">Tổng số căn</span>
          <span className="text-2xl font-black text-slate-900 mt-1 block">{totalRooms}</span>
          <span className="text-[10px] text-slate-500 font-medium">Bất động sản quản lý</span>
        </div>

        {/* Vacant Rooms */}
        <div className="bg-white p-4 rounded-2xl border border-emerald-200/80 shadow-xs bg-emerald-50/20">
          <span className="text-[11px] font-bold text-emerald-700 uppercase block">Căn còn trống</span>
          <span className="text-2xl font-black text-emerald-600 mt-1 block">{vacantRooms}</span>
          <span className="text-[10px] text-emerald-600 font-medium">Sẵn sàng đón khách</span>
        </div>

        {/* Rented Rooms */}
        <div className="bg-white p-4 rounded-2xl border border-blue-200/80 shadow-xs bg-blue-50/20">
          <span className="text-[11px] font-bold text-blue-700 uppercase block">Căn đang thuê</span>
          <span className="text-2xl font-black text-blue-600 mt-1 block">{rentedRooms}</span>
          <span className="text-[10px] text-blue-600 font-medium">Tỷ lệ lấp đầy: {Math.round((rentedRooms / totalRooms) * 100)}%</span>
        </div>

        {/* Total Receivables */}
        <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-xs">
          <span className="text-[11px] font-bold text-slate-400 uppercase block">Tổng phải thu kỳ này</span>
          <span className="text-lg font-black text-slate-900 mt-1 block">
            {(totalReceivables / 1000000).toLocaleString("vi-VN", { maximumFractionDigits: 1 })} tr
          </span>
          <span className="text-[10px] text-slate-500 font-medium">Tiền phòng + dịch vụ</span>
        </div>

        {/* Total Collected */}
        <div className="bg-white p-4 rounded-2xl border border-emerald-200/80 shadow-xs bg-emerald-50/20">
          <span className="text-[11px] font-bold text-emerald-700 uppercase block">Đã thực thu</span>
          <span className="text-lg font-black text-emerald-600 mt-1 block">
            {(totalCollected / 1000000).toLocaleString("vi-VN", { maximumFractionDigits: 1 })} tr
          </span>
          <span className="text-[10px] text-emerald-600 font-medium">Tiền về tài khoản</span>
        </div>

        {/* Total Debt */}
        <div className="bg-white p-4 rounded-2xl border border-rose-200/80 shadow-xs bg-rose-50/20">
          <span className="text-[11px] font-bold text-rose-700 uppercase block">Công nợ còn thiếu</span>
          <span className="text-lg font-black text-rose-600 mt-1 block">
            {(totalDebt / 1000000).toLocaleString("vi-VN", { maximumFractionDigits: 1 })} tr
          </span>
          <span className="text-[10px] text-rose-600 font-medium">Cần nhắc khách đóng</span>
        </div>
      </div>

      {/* Security Deposit & Funnel Analytics */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Deposit breakdown */}
        <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="font-bold text-sm text-slate-900">Tiền Đặt Cọc Đang Giữ</h3>
            <DollarSign className="w-4 h-4 text-emerald-600" />
          </div>

          <div className="p-4 rounded-xl bg-slate-50 border border-slate-100 text-center space-y-1">
            <span className="text-xs text-slate-500">Tổng tiền cọc các hợp đồng hiệu lực:</span>
            <span className="text-2xl font-black text-slate-900 block">
              {totalDeposits.toLocaleString()} VNĐ
            </span>
            <span className="text-[11px] text-emerald-600 font-medium">
              Đảm bảo hoàn trả khi thanh lý hợp đồng
            </span>
          </div>

          <div className="space-y-2 text-xs pt-2">
            {contracts.map((c) => (
              <div key={c.id} className="flex items-center justify-between p-2 rounded-lg hover:bg-slate-50">
                <span className="font-medium text-slate-700 truncate max-w-[150px]">{c.roomName}</span>
                <span className="font-bold text-slate-900">{c.depositAmount.toLocaleString()} đ</span>
              </div>
            ))}
          </div>
        </div>

        {/* Quick Quick Access Navigators */}
        <div className="lg:col-span-2 bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-4">
          <h3 className="font-bold text-sm text-slate-900">Lối Tắt Vận Hành Nghiệp Vụ</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Link
              href="/landlord/properties"
              className="p-4 rounded-xl border border-slate-200 hover:border-blue-400 hover:bg-blue-50/30 transition-all flex items-start gap-3 group"
            >
              <div className="p-2.5 rounded-lg bg-blue-100 text-blue-600 group-hover:scale-110 transition-transform">
                <Home className="w-5 h-5" />
              </div>
              <div>
                <span className="font-bold text-xs text-slate-900 group-hover:text-blue-600 block">
                  Quản lý căn & phòng
                </span>
                <span className="text-[11px] text-slate-500 mt-0.5 block">
                  Cập nhật trạng thái Còn trống, Đã thuê hoặc Tạm ngừng.
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
                <span className="font-bold text-xs text-slate-900 group-hover:text-blue-600 block">
                  Ghi chỉ số điện nước & Lập hóa đơn
                </span>
                <span className="text-[11px] text-slate-500 mt-0.5 block">
                  Nhập số công tơ đầu/cuối kỳ, hệ thống tự động tính tiền.
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
                <span className="font-bold text-xs text-slate-900 group-hover:text-blue-600 block">
                  Đăng tin cho thuê mới
                </span>
                <span className="text-[11px] text-slate-500 mt-0.5 block">
                  Soạn tin, đính kèm ảnh, bảng tiện ích và biểu phí công khai.
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
                <span className="font-bold text-xs text-slate-900 group-hover:text-blue-600 block">
                  Quản lý khách thuê & Hợp đồng
                </span>
                <span className="text-[11px] text-slate-500 mt-0.5 block">
                  Lập hợp đồng điện tử và theo dõi thời hạn hợp đồng.
                </span>
              </div>
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
