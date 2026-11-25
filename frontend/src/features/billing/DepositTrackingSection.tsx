"use client";

import React from "react";
import { AlertCircle, ShieldCheck } from "lucide-react";
import { LandlordDashboardData } from "@/types";

interface DepositTrackingSectionProps {
  data: LandlordDashboardData;
}

export function DepositTrackingSection({ data }: DepositTrackingSectionProps) {
  return (
    <div className="bg-white p-4 sm:p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h3 className="font-bold text-sm text-slate-900 flex items-center gap-2">
            <ShieldCheck className="w-4 h-4 text-indigo-600" />
            <span>Theo Dõi Tiền Cọc Hợp Đồng Còn Hiệu Lực</span>
          </h3>
          <p className="text-xs text-slate-500 mt-0.5">
            Tổng tiền cọc đang giữ và danh sách hợp đồng kèm cảnh báo hợp đồng sắp hết hạn để chuẩn
            bị hoàn trả.
          </p>
        </div>

        <div className="p-3 rounded-xl bg-indigo-50 border border-indigo-100 flex flex-wrap items-center justify-between sm:justify-start gap-2 sm:gap-3">
          <span className="text-xs font-semibold text-indigo-900">Tổng cọc đang giữ:</span>
          <span className="text-base sm:text-lg font-black text-indigo-700">
            {data.deposits.totalActiveDeposits.toLocaleString("vi-VN")} đ
          </span>
        </div>
      </div>

      <div className="space-y-2 pt-1">
        {data.deposits.items.length === 0 ? (
          <div className="p-6 text-center text-slate-400 text-xs bg-slate-50 rounded-xl">
            Chưa có hợp đồng hiệu lực nào có khoản tiền cọc trong phạm vi bộ lọc.
          </div>
        ) : (
          data.deposits.items.map((ctr) => (
            <div
              key={ctr.contractId}
              className="p-3.5 sm:p-4 rounded-xl border border-slate-200 hover:border-indigo-300 transition-all flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-white"
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
                  Khách thuê: <strong>{ctr.tenantName}</strong> • Hiệu lực từ {ctr.startDate} đến{" "}
                  {ctr.endDate}
                </span>
              </div>

              <div className="flex items-center justify-between sm:justify-end gap-5">
                <div className="text-left sm:text-right">
                  <span className="text-[10px] text-slate-400 block font-medium">
                    Tiền cọc giữ:
                  </span>
                  <span className="font-black text-xs text-slate-900">
                    {ctr.depositAmount.toLocaleString("vi-VN")} đ
                  </span>
                </div>
                <span className="px-2.5 py-1 rounded-lg text-[10px] font-bold bg-slate-100 text-slate-700">
                  {ctr.depositStatus}
                </span>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
