"use client";

import React, { useMemo, useState } from "react";
import { AlertTriangle, CheckCircle2, Phone, Search } from "lucide-react";
import { LandlordDashboardData } from "@/types";

interface DebtAgingSectionProps {
  data: LandlordDashboardData;
}

export function DebtAgingSection({ data }: DebtAgingSectionProps) {
  const [debtFilter, setDebtFilter] = useState<string>("all");
  const [debtSearch, setDebtSearch] = useState("");

  const filteredDebtItems = useMemo(() => {
    if (!data.debtAging.items) return [];
    const query = debtSearch.trim().toLowerCase();
    return data.debtAging.items.filter((item) => {
      const matchBucket = debtFilter === "all" || item.agingBucket === debtFilter;
      const matchSearch =
        !query ||
        item.roomName.toLowerCase().includes(query) ||
        item.tenantName.toLowerCase().includes(query) ||
        item.tenantPhone.includes(debtSearch.trim());
      return matchBucket && matchSearch;
    });
  }, [data.debtAging.items, debtFilter, debtSearch]);

  return (
    <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h3 className="font-bold text-sm text-slate-900 flex items-center gap-2">
            <AlertTriangle className="w-4 h-4 text-amber-500" />
            <span>Báo Cáo Công Nợ &amp; Phân Loại Nhóm Tuổi Nợ</span>
          </h3>
          <p className="text-xs text-slate-500 mt-0.5">
            Theo dõi số tiền nợ còn lại, nợ quá hạn và phân loại theo 4 nhóm tuổi nợ để có kế hoạch
            thu hồi kịp thời.
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
            <span className="w-2.5 h-2.5 rounded-full bg-emerald-500" />
          </div>
          <span className="text-lg font-black text-slate-900 block">
            {data.debtAging.currentAmount.toLocaleString("vi-VN")} đ
          </span>
          <span className="text-[10px] text-emerald-700">Chưa đến hạn thanh toán</span>
        </button>

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
            <span className="w-2.5 h-2.5 rounded-full bg-amber-500" />
          </div>
          <span className="text-lg font-black text-slate-900 block">
            {data.debtAging.overdue1To30Days.toLocaleString("vi-VN")} đ
          </span>
          <span className="text-[10px] text-amber-700">Cần gửi nhắc nợ định kỳ</span>
        </button>

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
            <span className="w-2.5 h-2.5 rounded-full bg-orange-500" />
          </div>
          <span className="text-lg font-black text-slate-900 block">
            {data.debtAging.overdue31To60Days.toLocaleString("vi-VN")} đ
          </span>
          <span className="text-[10px] text-orange-700">Cảnh báo nhắc nhở trực tiếp</span>
        </button>

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
            <span className="w-2.5 h-2.5 rounded-full bg-rose-500" />
          </div>
          <span className="text-lg font-black text-slate-900 block">
            {data.debtAging.overdueOver60Days.toLocaleString("vi-VN")} đ
          </span>
          <span className="text-[10px] text-rose-700 font-semibold">
            Nguy cơ nợ xấu, xem xét cấn cọc
          </span>
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
                <button
                  type="button"
                  onClick={() => setDebtFilter("all")}
                  className="hover:text-rose-600 ml-1 cursor-pointer"
                >
                  ✕
                </button>
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
            <p className="font-semibold text-slate-700">
              Không có hóa đơn nợ nào phù hợp với bộ lọc hiện tại.
            </p>
            <p className="text-[11px] text-slate-400">
              Tất cả khách thuê trong nhóm này đã thanh toán đầy đủ.
            </p>
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
                      <span className="text-[11px] text-slate-400 font-medium">Chờ API nhắc nợ</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
