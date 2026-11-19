"use client";

import React, { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import { useToast } from "@/components/Toast";
import { leasingApi } from "@/lib/leasing-api";
import {
  formatDate,
  formatVnd,
  getDepositMeta,
  getStatusMeta,
} from "@/features/leasing/leasing-helpers";
import { parseContractSnapshot } from "@/features/leasing/contract-snapshot";
import { LeasingContractDto } from "@/types";
import { cn } from "@/lib/utils";
import { Calendar, Eye, FileText, KeyRound, RefreshCw } from "lucide-react";

export default function TenantContractsPage() {
  const { user } = useAuth();
  const { showToast } = useToast();
  const [contracts, setContracts] = useState<LeasingContractDto[]>([]);
  const [loading, setLoading] = useState(true);

  const loadContracts = useCallback(async () => {
    setLoading(true);
    try {
      const res = await leasingApi.getContracts({ roleView: "TENANT", page: 1, pageSize: 50 });
      if (res.isSuccess && res.data) {
        setContracts(res.data.items);
      } else {
        showToast(res.error?.message || "Không tải được danh sách hợp đồng.", "error");
      }
    } finally {
      setLoading(false);
    }
  }, [showToast]);

  useEffect(() => {
    if (user) loadContracts();
  }, [user, loadContracts]);

  if (!user) {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto">
          <FileText className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Hợp Đồng Thuê Phòng Của Tôi</h2>
        <p className="text-xs text-slate-500">Vui lòng đăng nhập để xem hợp đồng, ký điện tử OTP và theo dõi tiền cọc.</p>
        <Link
          href="/auth/login?redirect=/tenant/contracts"
          className="inline-flex items-center px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700"
        >
          Đăng nhập ngay
        </Link>
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      <div className="flex items-center justify-between gap-4 pb-5 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <FileText className="w-4 h-4" />
            <span>Hợp đồng điện tử · Người thuê</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">Hợp Đồng Thuê Của Tôi</h1>
          <p className="text-xs text-slate-500 mt-1">
            Xem bản thảo pháp lý A4, ký điện tử bằng mã OTP 6 số và kiểm tra minh bạch sổ cái tiền cọc.
          </p>
        </div>

        <button
          onClick={loadContracts}
          disabled={loading}
          className="p-2.5 rounded-xl border border-slate-200 bg-white hover:bg-slate-50 text-slate-600"
        >
          <RefreshCw className={cn("w-4 h-4", loading && "animate-spin")} />
        </button>
      </div>

      {loading ? (
        <div className="space-y-3">
          {[1, 2].map((i) => (
            <div key={i} className="h-24 rounded-2xl bg-slate-100 animate-pulse" />
          ))}
        </div>
      ) : contracts.length === 0 ? (
        <div className="py-16 text-center bg-white rounded-2xl border border-slate-200 space-y-2">
          <FileText className="w-8 h-8 text-slate-300 mx-auto" />
          <h3 className="text-sm font-bold text-slate-900">Bạn chưa có hợp đồng thuê nào</h3>
          <p className="text-xs text-slate-500">Khi chủ nhà gửi hợp đồng mới, hợp đồng sẽ xuất hiện tại đây để bạn ký OTP.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {contracts.map((ctr) => {
            const statusMeta = getStatusMeta(ctr.status);
            const depositMeta = getDepositMeta(ctr.depositStatus);
            const snap = parseContractSnapshot(ctr.documentSnapshot);
            const needMySign = ctr.status === "PENDING_SIGNATURE" && !ctr.tenantSignedAt;

            return (
              <Link
                key={ctr.id}
                href={`/tenant/contracts/${ctr.id}`}
                className="block p-5 rounded-2xl bg-white border border-slate-200/80 hover:border-blue-300 shadow-xs transition-all space-y-4"
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <span className="font-mono text-[11px] font-bold px-2 py-0.5 rounded bg-slate-100 text-slate-800">
                      {ctr.contractCode}
                    </span>
                    <h3 className="text-sm font-black text-slate-900 mt-1.5">
                      {snap.roomName || `Phòng #${ctr.roomId}`}
                    </h3>
                  </div>
                  <span
                    className={cn(
                      "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold border",
                      statusMeta.badgeClass
                    )}
                  >
                    <span className={cn("w-1.5 h-1.5 rounded-full", statusMeta.dotClass)} />
                    {statusMeta.label}
                  </span>
                </div>

                <div className="grid grid-cols-2 gap-3 text-xs bg-slate-50 p-3 rounded-xl">
                  <div>
                    <div className="text-[10px] text-slate-400 font-semibold uppercase">Giá thuê / tháng</div>
                    <div className="font-black text-blue-600 mt-0.5">{formatVnd(ctr.monthlyRent)}</div>
                  </div>
                  <div>
                    <div className="text-[10px] text-slate-400 font-semibold uppercase">Tiền cọc</div>
                    <div className="font-bold text-slate-900 mt-0.5">
                      {formatVnd(ctr.depositAmount)}{" "}
                      <span className="text-[10px] font-semibold text-slate-500">({depositMeta.label})</span>
                    </div>
                  </div>
                </div>

                <div className="flex items-center justify-between text-xs pt-1">
                  <span className="text-slate-500 flex items-center gap-1">
                    <Calendar className="w-3.5 h-3.5" />
                    {formatDate(ctr.startDate)} → {formatDate(ctr.endDate)}
                  </span>
                  {needMySign ? (
                    <span className="inline-flex items-center gap-1 px-3 py-1.5 rounded-xl bg-blue-600 text-white font-bold text-[11px]">
                      <KeyRound className="w-3.5 h-3.5" />
                      Mở phòng ký OTP
                    </span>
                  ) : (
                    <span className="inline-flex items-center gap-1 text-blue-600 font-bold">
                      <Eye className="w-3.5 h-3.5" />
                      Xem hợp đồng
                    </span>
                  )}
                </div>
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
}
