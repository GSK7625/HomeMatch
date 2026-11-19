"use client";

import React, { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { billingApi } from "@/lib/billing-api";
import { formatVnd } from "@/features/leasing/leasing-helpers";
import {
  Building2,
  CheckCircle2,
  CreditCard,
  Loader2,
  ShieldCheck,
  XCircle,
} from "lucide-react";

const BANKS = [
  { code: "NCB", name: "Ngân hàng NCB (Thẻ nội địa ATM)" },
  { code: "VIETCOMBANK", name: "Vietcombank" },
  { code: "BIDV", name: "BIDV" },
  { code: "TECHCOMBANK", name: "Techcombank" },
];

function SandboxPayContent() {
  const searchParams = useSearchParams();
  const router = useRouter();

  const paymentCode = searchParams.get("paymentCode") || "";
  const amount = Number(searchParams.get("amount") || "0");
  const orderInfo = searchParams.get("orderInfo") || "Thanh toán hợp đồng HomeMatch";

  const [bankCode, setBankCode] = useState("NCB");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleConfirm = async (isSuccess: boolean) => {
    if (!paymentCode) {
      setError("Thiếu mã giao dịch thanh toán.");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      const res = await billingApi.sandboxConfirm({
        paymentCode,
        isSuccess,
        bankCode,
      });
      if (res.isSuccess && res.data?.callbackUrl) {
        router.push(res.data.callbackUrl);
      } else {
        setError(res.error?.message || "Không thể xử lý giao dịch giả lập.");
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="max-w-lg mx-auto px-4 py-12 w-full">
      <div className="bg-white rounded-3xl border border-slate-200 shadow-xl overflow-hidden">
        <div className="bg-gradient-to-r from-blue-700 to-indigo-700 px-6 py-5 text-white flex items-center justify-between">
          <div>
            <div className="text-[11px] font-bold uppercase tracking-wider text-blue-200">
              VNPay Sandbox Simulator (Dev Mode)
            </div>
            <h1 className="text-lg font-black mt-0.5">Cổng Thanh Toán Điện Tử</h1>
          </div>
          <CreditCard className="w-8 h-8 text-blue-200" />
        </div>

        <div className="p-6 space-y-5">
          <div className="p-4 rounded-2xl bg-slate-50 border border-slate-200/80 space-y-2 text-xs">
            <div className="flex justify-between">
              <span className="text-slate-500">Mã giao dịch</span>
              <span className="font-mono font-bold text-slate-900">{paymentCode || "—"}</span>
            </div>
            <div className="flex justify-between gap-4">
              <span className="text-slate-500 shrink-0">Nội dung</span>
              <span className="font-semibold text-slate-800 text-right">{orderInfo}</span>
            </div>
            <div className="pt-2 border-t border-slate-200 flex justify-between items-center">
              <span className="text-slate-600 font-bold">Số tiền thanh toán</span>
              <span className="text-xl font-black text-blue-600">{formatVnd(amount)}</span>
            </div>
          </div>

          <div className="space-y-2">
            <label className="text-xs font-bold text-slate-700 flex items-center gap-1.5">
              <Building2 className="w-4 h-4 text-slate-400" />
              <span>Chọn ngân hàng thanh toán (Sandbox Test)</span>
            </label>
            <select
              value={bankCode}
              onChange={(e) => setBankCode(e.target.value)}
              className="w-full h-10 px-3 rounded-xl border border-slate-200 text-xs font-semibold text-slate-800 bg-white focus:outline-none focus:ring-2 focus:ring-blue-500/20"
            >
              {BANKS.map((b) => (
                <option key={b.code} value={b.code}>
                  {b.name}
                </option>
              ))}
            </select>
          </div>

          {error && (
            <div className="p-3 rounded-xl bg-rose-50 border border-rose-200 text-xs text-rose-700 font-medium">
              {error}
            </div>
          )}

          <div className="flex items-center gap-2 text-[11px] text-slate-500 bg-emerald-50/70 border border-emerald-200/70 p-3 rounded-xl">
            <ShieldCheck className="w-4 h-4 text-emerald-600 shrink-0" />
            <span>
              Giao dịch được ký xác thực HMAC-SHA512 và tự động kích hoạt hợp đồng sang{" "}
              <strong>ACTIVE</strong> ngay khi hoàn tất.
            </span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
            <button
              type="button"
              disabled={submitting}
              onClick={() => handleConfirm(false)}
              className="py-2.5 px-4 rounded-xl border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 font-bold text-xs flex items-center justify-center gap-1.5 disabled:opacity-50"
            >
              <XCircle className="w-4 h-4 text-rose-500" />
              <span>Hủy thanh toán</span>
            </button>

            <button
              type="button"
              disabled={submitting}
              onClick={() => handleConfirm(true)}
              className="py-2.5 px-4 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-black text-xs flex items-center justify-center gap-1.5 shadow-md shadow-blue-500/20 disabled:opacity-50"
            >
              {submitting ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  <span>Đang xử lý...</span>
                </>
              ) : (
                <>
                  <CheckCircle2 className="w-4 h-4" />
                  <span>Xác nhận thanh toán</span>
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function SandboxPayPage() {
  return (
    <Suspense
      fallback={
        <div className="max-w-lg mx-auto px-4 py-20 text-center text-xs text-slate-400">
          Đang khởi tạo cổng thanh toán...
        </div>
      }
    >
      <SandboxPayContent />
    </Suspense>
  );
}
