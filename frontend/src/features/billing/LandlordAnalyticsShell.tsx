"use client";

import React from "react";
import Link from "next/link";
import {
  AlertTriangle,
  BarChart3,
  Building2,
  Filter,
  LayoutDashboard,
  RefreshCw,
} from "lucide-react";
import { LandlordDashboardData, PropertyDto } from "@/types";
import { PeriodOption } from "./useLandlordAnalytics";

interface LandlordAnalyticsShellProps {
  activeSection: "dashboard" | "reports";
  title: string;
  subtitle: string;
  redirectPath: string;
  user: { displayName: string } | null;
  role: string | null;
  properties: PropertyDto[];
  periodOptions: PeriodOption[];
  selectedBuilding: string;
  onBuildingChange: (value: string) => void;
  selectedPeriod: string;
  onPeriodChange: (value: string) => void;
  loading: boolean;
  error: string | null;
  data: LandlordDashboardData | null;
  onRefresh: () => void;
  filterQuery: string;
  children: (data: LandlordDashboardData) => React.ReactNode;
}

export function LandlordAnalyticsShell({
  activeSection,
  title,
  subtitle,
  redirectPath,
  user,
  role,
  properties,
  periodOptions,
  selectedBuilding,
  onBuildingChange,
  selectedPeriod,
  onPeriodChange,
  loading,
  error,
  data,
  onRefresh,
  filterQuery,
  children,
}: LandlordAnalyticsShellProps) {
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
              ? "Bạn hiện đang ở vai trò Người thuê (Tenant). Vui lòng chuyển sang tài khoản Chủ nhà để truy cập bảng điều khiển vận hành, báo cáo tài chính và công nợ."
              : "Vui lòng đăng nhập với tài khoản Chủ nhà để theo dõi số phòng trống, doanh thu, phân loại công nợ theo tuổi nợ và quản lý tiền cọc."}
          </p>
        </div>

        <div className="flex items-center justify-center gap-4 text-xs text-slate-500">
          <Link
            href={`/auth/login?redirect=${encodeURIComponent(redirectPath)}`}
            className="px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-bold shadow-md shadow-blue-500/20 transition-all"
          >
            Đăng nhập tài khoản Chủ nhà
          </Link>
          <Link
            href="/auth/register"
            className="px-5 py-2.5 rounded-xl border border-slate-200 hover:bg-slate-50 text-slate-700 font-bold transition-all"
          >
            Đăng ký tài khoản mới
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Top Banner, Module Switcher & Filter Controls (Chức năng 2: Lọc dashboard và báo cáo) */}
      <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-5">
        <div className="flex flex-col lg:flex-row items-start lg:items-center justify-between gap-6">
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
                    Dữ liệu thời gian thực từ PostgreSQL
                  </span>
                </>
              )}
            </div>
            <h1 className="text-2xl font-black text-slate-900 tracking-tight">{title}</h1>
            <p className="text-xs text-slate-500 mt-1">{subtitle}</p>
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
                onChange={(e) => onBuildingChange(e.target.value)}
                className="w-full text-xs font-medium rounded-lg border-slate-200 bg-white py-1.5 px-3 text-slate-700 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 shadow-xs cursor-pointer"
              >
                <option value="all">Tất cả tòa nhà & nhà trọ</option>
                {properties.map((prop) => (
                  <option key={prop.id} value={String(prop.id)}>
                    {prop.name}
                  </option>
                ))}
              </select>
            </div>

            {/* Period Filter */}
            <div className="flex-1 sm:flex-initial">
              <select
                value={selectedPeriod}
                onChange={(e) => onPeriodChange(e.target.value)}
                className="w-full text-xs font-medium rounded-lg border-slate-200 bg-white py-1.5 px-3 text-slate-700 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 shadow-xs cursor-pointer"
              >
                {periodOptions.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label}
                  </option>
                ))}
              </select>
            </div>

            {/* Refresh Button */}
            <button
              type="button"
              onClick={onRefresh}
              disabled={loading}
              className="p-1.5 text-slate-500 hover:text-blue-600 hover:bg-white rounded-lg transition-colors border border-transparent hover:border-slate-200 cursor-pointer"
              title="Làm mới số liệu"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${loading ? "animate-spin text-blue-600" : ""}`} />
            </button>
          </div>
        </div>

        {/* Quick Switcher between Dashboard and Financial Reports */}
        <div className="pt-4 border-t border-slate-100 flex flex-wrap items-center gap-2">
          <Link
            href={`/landlord/dashboard${filterQuery}`}
            className={`inline-flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-bold transition-all ${
              activeSection === "dashboard"
                ? "bg-blue-600 text-white shadow-sm shadow-blue-500/20"
                : "bg-slate-100 text-slate-600 hover:bg-slate-200/80 hover:text-slate-900"
            }`}
          >
            <LayoutDashboard className="w-3.5 h-3.5" />
            <span>Dashboard Tổng Quan</span>
          </Link>
          <Link
            href={`/landlord/reports${filterQuery}`}
            className={`inline-flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-bold transition-all ${
              activeSection === "reports"
                ? "bg-blue-600 text-white shadow-sm shadow-blue-500/20"
                : "bg-slate-100 text-slate-600 hover:bg-slate-200/80 hover:text-slate-900"
            }`}
          >
            <BarChart3 className="w-3.5 h-3.5" />
            <span>Báo Cáo Tài Chính & Công Nợ</span>
          </Link>
        </div>
      </div>

      {loading && !data ? (
        <div className="py-20 text-center space-y-3">
          <RefreshCw className="w-8 h-8 text-blue-600 animate-spin mx-auto" />
          <p className="text-xs text-slate-500 font-medium">
            Đang tải dữ liệu trực tiếp từ PostgreSQL...
          </p>
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
              Vui lòng khởi động Backend bằng lệnh sau rồi bấm thử lại:
            </p>
            <code className="block text-[11px] bg-slate-900 text-emerald-400 px-3 py-2 rounded-lg font-mono">
              dotnet run --project backend/src/HomeMatch.Api
            </code>
          </div>
          <button
            type="button"
            onClick={onRefresh}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs shadow-sm transition-all cursor-pointer"
          >
            <RefreshCw className="w-3.5 h-3.5" />
            <span>Thử lại kết nối</span>
          </button>
        </div>
      ) : data ? (
        children(data)
      ) : null}
    </div>
  );
}
