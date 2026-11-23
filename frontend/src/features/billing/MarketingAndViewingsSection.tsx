"use client";

import React from "react";
import Link from "next/link";
import {
  ArrowUpRight,
  BarChart3,
  Calendar,
  CheckCircle2,
  Clock,
  Eye,
  FileText,
  Home,
  Phone,
  Users,
  Zap,
} from "lucide-react";
import { LandlordDashboardData } from "@/types";

interface MarketingAndViewingsSectionProps {
  data: LandlordDashboardData;
  filterQuery: string;
}

export function MarketingAndViewingsSection({
  data,
  filterQuery,
}: MarketingAndViewingsSectionProps) {
  const newLeads = data.marketingAndLeads.newLeads;
  const pct = (value: number) => (newLeads > 0 ? Math.round((value / newLeads) * 100) : 0);

  return (
    <div className="space-y-6">
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
            <div className="text-right space-y-1">
              <div>
                <span className="text-[10px] text-slate-400 block">Tin đang hiển thị</span>
                <span className="text-xs font-black text-blue-600">
                  {data.overview.activeListings} tin
                </span>
              </div>
              <Link
                href="/landlord/contracts?tab=leads"
                className="inline-flex items-center gap-1 text-[11px] font-bold text-blue-600 hover:text-blue-700 hover:underline"
              >
                <span>Xem danh sách khách</span>
                <ArrowUpRight className="w-3 h-3" />
              </Link>
            </div>
          </div>

          <div className="space-y-3 pt-2">
            {/* Stage 1 */}
            <div className="space-y-1">
              <div className="flex justify-between text-xs font-semibold text-slate-700">
                <span className="flex items-center gap-1.5">
                  <Eye className="w-3.5 h-3.5 text-blue-500" />
                  <span>1. Khách mới quan tâm</span>
                </span>
                <span className="font-bold text-slate-900">{newLeads} khách (100%)</span>
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
                  {pct(data.marketingAndLeads.contactedLeads)}%)
                </span>
              </div>
              <div className="w-full h-2 rounded-full bg-slate-100 overflow-hidden">
                <div
                  className="bg-indigo-500 h-full rounded-full"
                  style={{ width: `${pct(data.marketingAndLeads.contactedLeads)}%` }}
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
                  {pct(data.marketingAndLeads.viewingScheduledLeads)}%)
                </span>
              </div>
              <div className="w-full h-2 rounded-full bg-slate-100 overflow-hidden">
                <div
                  className="bg-amber-500 h-full rounded-full"
                  style={{ width: `${pct(data.marketingAndLeads.viewingScheduledLeads)}%` }}
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
                  {pct(data.marketingAndLeads.contractSignedLeads)}%)
                </span>
              </div>
              <div className="w-full h-2 rounded-full bg-slate-100 overflow-hidden">
                <div
                  className="bg-emerald-500 h-full rounded-full"
                  style={{ width: `${pct(data.marketingAndLeads.contractSignedLeads)}%` }}
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
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
          <Link
            href={`/landlord/reports${filterQuery}`}
            className="p-4 rounded-xl border border-slate-200 hover:border-blue-400 hover:bg-blue-50/30 transition-all flex items-start gap-3 group"
          >
            <div className="p-2.5 rounded-lg bg-emerald-100 text-emerald-600 group-hover:scale-110 transition-transform">
              <BarChart3 className="w-5 h-5" />
            </div>
            <div>
              <span className="font-bold text-xs text-slate-900 group-hover:text-blue-600 flex items-center gap-1">
                Báo cáo tài chính <ArrowUpRight className="w-3 h-3 opacity-0 group-hover:opacity-100 transition-opacity" />
              </span>
              <span className="text-[11px] text-slate-500 mt-0.5 block">
                Doanh thu, phân nhóm tuổi nợ và theo dõi tiền cọc.
              </span>
            </div>
          </Link>

          <Link
            href="/landlord/properties"
            className="p-4 rounded-xl border border-slate-200 hover:border-blue-400 hover:bg-blue-50/30 transition-all flex items-start gap-3 group"
          >
            <div className="p-2.5 rounded-lg bg-blue-100 text-blue-600 group-hover:scale-110 transition-transform">
              <Home className="w-5 h-5" />
            </div>
            <div>
              <span className="font-bold text-xs text-slate-900 group-hover:text-blue-600 flex items-center gap-1">
                Quản lý căn &amp; phòng <ArrowUpRight className="w-3 h-3 opacity-0 group-hover:opacity-100 transition-opacity" />
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
              <span className="font-bold text-xs text-slate-900 group-hover:text-blue-600 flex items-center gap-1">
                Ghi chỉ số &amp; Hóa đơn <ArrowUpRight className="w-3 h-3 opacity-0 group-hover:opacity-100 transition-opacity" />
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
              <span className="font-bold text-xs text-slate-900 group-hover:text-blue-600 flex items-center gap-1">
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
            <div className="p-2.5 rounded-lg bg-teal-100 text-teal-600 group-hover:scale-110 transition-transform">
              <Users className="w-5 h-5" />
            </div>
            <div>
              <span className="font-bold text-xs text-slate-900 group-hover:text-blue-600 flex items-center gap-1">
                Hợp đồng &amp; Khách thuê <ArrowUpRight className="w-3 h-3 opacity-0 group-hover:opacity-100 transition-opacity" />
              </span>
              <span className="text-[11px] text-slate-500 mt-0.5 block">
                Lập hợp đồng điện tử và theo dõi thời hạn hợp đồng.
              </span>
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}
