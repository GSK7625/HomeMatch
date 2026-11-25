"use client";

import React from "react";
import Link from "next/link";
import {
  AlertCircle,
  ArrowRight,
  CheckCircle2,
  DollarSign,
  Eye,
  FileText,
  Home,
  ShieldCheck,
  TrendingUp,
} from "lucide-react";
import { LandlordDashboardData } from "@/types";

interface DashboardOverviewSectionProps {
  data: LandlordDashboardData;
  selectedPeriod: string;
  filterQuery: string;
}

export function DashboardOverviewSection({
  data,
  selectedPeriod,
  filterQuery,
}: DashboardOverviewSectionProps) {
  return (
    <div className="space-y-6">
      {/* SECTION 1: Các Thẻ Thống Kê Tổng Quan (Chức năng 1) */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 sm:gap-4">
        {/* Card 1: Căn trống */}
        <div className="bg-white p-3.5 sm:p-5 rounded-2xl border border-slate-200/80 shadow-xs space-y-2 hover:border-blue-300 transition-all min-w-0">
          <div className="flex items-center justify-between gap-1 text-slate-500">
            <span className="text-xs font-semibold truncate">Căn trống</span>
            <span className="p-1.5 sm:p-2 rounded-xl bg-amber-50 text-amber-600 shrink-0">
              <Home className="w-4 h-4" />
            </span>
          </div>
          <div className="flex flex-wrap items-baseline gap-1.5 sm:gap-2">
            <span className="text-2xl sm:text-3xl font-black text-slate-900">{data.overview.vacantRooms}</span>
            <span className="text-xs text-slate-400 font-medium">/ {data.overview.totalRooms} căn</span>
          </div>
          <div className="flex items-center gap-1.5 text-[11px] text-amber-600 font-medium">
            <AlertCircle className="w-3.5 h-3.5 shrink-0" />
            <span>Cần đẩy mạnh tin đăng để lấp đầy</span>
          </div>
        </div>

        {/* Card 2: Đang thuê & Tỷ lệ lấp đầy */}
        <div className="bg-white p-3.5 sm:p-5 rounded-2xl border border-slate-200/80 shadow-xs space-y-2 hover:border-emerald-300 transition-all min-w-0">
          <div className="flex items-center justify-between gap-1 text-slate-500">
            <span className="text-xs font-semibold truncate">Căn đang thuê</span>
            <span className="p-1.5 sm:p-2 rounded-xl bg-emerald-50 text-emerald-600 shrink-0">
              <TrendingUp className="w-4 h-4" />
            </span>
          </div>
          <div className="flex flex-wrap items-baseline gap-1.5 sm:gap-2">
            <span className="text-2xl sm:text-3xl font-black text-slate-900">{data.overview.rentedRooms}</span>
            <span className="text-[11px] sm:text-xs text-emerald-600 font-bold bg-emerald-50 px-2 py-0.5 rounded-md">
              {data.overview.occupancyRate}% lấp đầy
            </span>
          </div>
          <div className="flex items-center gap-1.5 text-[11px] text-slate-400 font-medium">
            <CheckCircle2 className="w-3.5 h-3.5 text-emerald-500 shrink-0" />
            <span>Vận hành ổn định theo hợp đồng</span>
          </div>
        </div>

        {/* Card 3: Tin đang hiển thị */}
        <div className="bg-white p-3.5 sm:p-5 rounded-2xl border border-slate-200/80 shadow-xs space-y-2 hover:border-blue-300 transition-all min-w-0">
          <div className="flex items-center justify-between gap-1 text-slate-500">
            <span className="text-xs font-semibold truncate">Tin đang hiển thị</span>
            <span className="p-1.5 sm:p-2 rounded-xl bg-blue-50 text-blue-600 shrink-0">
              <FileText className="w-4 h-4" />
            </span>
          </div>
          <div className="flex flex-wrap items-baseline gap-1.5 sm:gap-2">
            <span className="text-2xl sm:text-3xl font-black text-slate-900">{data.overview.activeListings}</span>
            <span className="text-[11px] sm:text-xs text-blue-600 font-bold bg-blue-50 px-2 py-0.5 rounded-md">
              {data.marketingAndLeads.totalViewsCount} lượt xem
            </span>
          </div>
          <div className="flex items-center gap-1.5 text-[11px] text-slate-400 font-medium">
            <Eye className="w-3.5 h-3.5 text-blue-500 shrink-0" />
            <span>Tiếp cận khách thuê tìm phòng</span>
          </div>
        </div>

        {/* Card 4: Tổng tiền cọc đang giữ */}
        <div className="bg-white p-3.5 sm:p-5 rounded-2xl border border-slate-200/80 shadow-xs space-y-2 hover:border-indigo-300 transition-all min-w-0">
          <div className="flex items-center justify-between gap-1 text-slate-500">
            <span className="text-xs font-semibold truncate">Tiền cọc đang giữ</span>
            <span className="p-1.5 sm:p-2 rounded-xl bg-indigo-50 text-indigo-600 shrink-0">
              <ShieldCheck className="w-4 h-4" />
            </span>
          </div>
          <div className="flex items-baseline gap-1 min-w-0">
            <span className="text-base sm:text-2xl font-black text-slate-900 break-words">
              {data.deposits.totalActiveDeposits.toLocaleString("vi-VN")}
            </span>
            <span className="text-xs text-slate-400 font-bold shrink-0">đ</span>
          </div>
          <div className="flex items-center gap-1.5 text-[11px] text-indigo-600 font-medium">
            <ShieldCheck className="w-3.5 h-3.5 text-indigo-500 shrink-0" />
            <span>{data.deposits.activeContractsCount} hợp đồng hiệu lực</span>
          </div>
        </div>
      </div>

      {/* Tóm tắt nhanh chỉ số tài chính & Lối dẫn sang trang Báo cáo tài chính */}
      <div className="bg-white p-4 sm:p-5 rounded-2xl border border-slate-200/80 shadow-xs flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        <div className="flex flex-wrap items-center gap-4 sm:gap-6">
          <div className="flex items-center gap-2.5">
            <span className="p-2.5 rounded-xl bg-emerald-50 text-emerald-600">
              <DollarSign className="w-4 h-4" />
            </span>
            <div>
              <span className="text-[11px] text-slate-500 font-semibold block">
                Đã thu (Kỳ {selectedPeriod})
              </span>
              <span className="text-base font-black text-emerald-700">
                {data.financials.totalCollected.toLocaleString("vi-VN")} đ
              </span>
            </div>
          </div>

          <div className="h-8 w-px bg-slate-200 hidden sm:block" />

          <div>
            <span className="text-[11px] text-slate-500 font-semibold block">Tổng phải thu</span>
            <span className="text-base font-black text-slate-900">
              {data.financials.totalReceivables.toLocaleString("vi-VN")} đ
            </span>
          </div>

          <div className="h-8 w-px bg-slate-200 hidden sm:block" />

          <div>
            <span className="text-[11px] text-slate-500 font-semibold block">Còn phải thu / Nợ quá hạn</span>
            <div className="flex items-center gap-2">
              <span className="text-base font-black text-rose-600">
                {data.financials.remainingDebt.toLocaleString("vi-VN")} đ
              </span>
              <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200">
                Đạt {data.financials.collectionRate}%
              </span>
            </div>
          </div>
        </div>

        <Link
          href={`/landlord/reports${filterQuery}`}
          className="inline-flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl bg-blue-50 hover:bg-blue-600 text-blue-700 hover:text-white text-xs font-bold border border-blue-200 hover:border-blue-600 transition-all shrink-0"
        >
          <span>Xem báo cáo tài chính &amp; công nợ chi tiết</span>
          <ArrowRight className="w-3.5 h-3.5" />
        </Link>
      </div>
    </div>
  );
}
