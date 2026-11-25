"use client";

import React from "react";
import { DollarSign, Droplets, Home, PieChart, Wrench, Zap } from "lucide-react";
import { LandlordDashboardData } from "@/types";

interface RevenueReportSectionProps {
  data: LandlordDashboardData;
  selectedPeriod: string;
}

export function RevenueReportSection({ data, selectedPeriod }: RevenueReportSectionProps) {
  const { financials } = data;
  const totalReceivables = financials.totalReceivables;
  const widthPct = (amount: number) =>
    totalReceivables > 0 ? Math.min(100, (amount / totalReceivables) * 100) : 0;

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      {/* Financial Overview & Progress (Chức năng 3) */}
      <div className="lg:col-span-2 bg-white p-4 sm:p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-6">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
          <div>
            <h3 className="font-bold text-sm text-slate-900 flex items-center gap-2">
              <DollarSign className="w-4 h-4 text-emerald-600" />
              <span>Báo Cáo Doanh Thu &amp; Thu Tiền (Kỳ {selectedPeriod})</span>
            </h3>
            <p className="text-xs text-slate-500 mt-0.5">
              Tổng hợp khoản phải thu, số tiền thực tế đã thu và tỷ lệ hoàn thành thu hồi dòng tiền.
            </p>
          </div>
          <span className="px-3 py-1 rounded-full text-xs font-bold bg-emerald-50 text-emerald-700 border border-emerald-200/60 self-start sm:self-auto">
            Thu đạt {financials.collectionRate}%
          </span>
        </div>

        {/* Progress bar */}
        <div className="space-y-2">
          <div className="flex flex-wrap justify-between gap-1 text-xs font-semibold text-slate-600">
            <span>Tiến độ thu hồi dòng tiền kỳ này</span>
            <span className="font-bold text-slate-900">
              {financials.totalCollected.toLocaleString("vi-VN")} đ /{" "}
              {financials.totalReceivables.toLocaleString("vi-VN")} đ
            </span>
          </div>
          <div className="w-full h-3.5 rounded-full bg-slate-100 overflow-hidden flex p-0.5 border border-slate-200/60">
            <div
              className="bg-emerald-500 h-full transition-all duration-500 rounded-full"
              style={{ width: `${Math.min(100, financials.collectionRate)}%` }}
            />
          </div>
        </div>

        {/* Metric Stat Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 sm:gap-4 pt-1">
          <div className="p-3.5 sm:p-4 rounded-xl bg-slate-50 border border-slate-100 space-y-1">
            <span className="text-[11px] font-semibold text-slate-500 block">
              Tổng Phải Thu (Doanh thu)
            </span>
            <span className="text-lg sm:text-xl font-black text-slate-900 block">
              {financials.totalReceivables.toLocaleString("vi-VN")} đ
            </span>
            <span className="text-[10px] text-slate-400">Tiền phòng + dịch vụ phát sinh</span>
          </div>

          <div className="p-3.5 sm:p-4 rounded-xl bg-emerald-50/60 border border-emerald-100 space-y-1">
            <span className="text-[11px] font-semibold text-emerald-700 block">Số Tiền Đã Thu</span>
            <span className="text-lg sm:text-xl font-black text-emerald-800 block">
              {financials.totalCollected.toLocaleString("vi-VN")} đ
            </span>
            <span className="text-[10px] text-emerald-600 font-medium">
              Đã vào tài khoản / tiền mặt
            </span>
          </div>

          <div className="p-3.5 sm:p-4 rounded-xl bg-rose-50/60 border border-rose-100 space-y-1">
            <span className="text-[11px] font-semibold text-rose-700 block">
              Tổng Còn Phải Thu (Nợ)
            </span>
            <span className="text-lg sm:text-xl font-black text-rose-700 block">
              {financials.remainingDebt.toLocaleString("vi-VN")} đ
            </span>
            <span className="text-[10px] text-rose-600 font-medium">
              Chưa thanh toán hoặc nợ tồn
            </span>
          </div>
        </div>
      </div>

      {/* Revenue Breakdown */}
      <div className="bg-white p-4 sm:p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-4">
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
                {financials.breakdown.rentAmount.toLocaleString("vi-VN")} đ
              </span>
            </div>
            <div className="w-full h-1.5 rounded-full bg-slate-100 overflow-hidden">
              <div
                className="bg-blue-500 h-full"
                style={{ width: `${widthPct(financials.breakdown.rentAmount)}%` }}
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
                {financials.breakdown.electricityAmount.toLocaleString("vi-VN")} đ
              </span>
            </div>
            <div className="w-full h-1.5 rounded-full bg-slate-100 overflow-hidden">
              <div
                className="bg-amber-500 h-full"
                style={{ width: `${widthPct(financials.breakdown.electricityAmount)}%` }}
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
                {financials.breakdown.waterAmount.toLocaleString("vi-VN")} đ
              </span>
            </div>
            <div className="w-full h-1.5 rounded-full bg-slate-100 overflow-hidden">
              <div
                className="bg-cyan-500 h-full"
                style={{ width: `${widthPct(financials.breakdown.waterAmount)}%` }}
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
                {financials.breakdown.serviceAmount.toLocaleString("vi-VN")} đ
              </span>
            </div>
            <div className="w-full h-1.5 rounded-full bg-slate-100 overflow-hidden">
              <div
                className="bg-indigo-500 h-full"
                style={{ width: `${widthPct(financials.breakdown.serviceAmount)}%` }}
              />
            </div>
          </div>
        </div>

        <div className="p-3.5 rounded-xl bg-slate-50 text-[11px] text-slate-600 leading-relaxed border border-slate-100 mt-2">
          💡 <strong>Gợi ý:</strong> Tiền thuê chiếm phần lớn dòng tiền. Nên chốt chỉ số điện nước
          trước ngày 25 hàng tháng để phát hành hóa đơn đồng loạt.
        </div>
      </div>
    </div>
  );
}
