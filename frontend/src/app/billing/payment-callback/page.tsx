"use client";

import React, { Suspense, useEffect, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { billingApi, VerifyReturnResultDto } from "@/lib/billing-api";
import { formatVnd } from "@/features/leasing/leasing-helpers";
import {
  ArrowRight,
  CheckCircle2,
  FileCheck2,
  Loader2,
  XCircle,
} from "lucide-react";

function PaymentCallbackContent() {
  const searchParams = useSearchParams();
  const [loading, setLoading] = useState(true);
  const [result, setResult] = useState<VerifyReturnResultDto | null>(null);

  useEffect(() => {
    let active = true;

    async function verify() {
      const hasVnpHash = searchParams.has("vnp_SecureHash");
      if (hasVnpHash) {
        const qs = `?${searchParams.toString()}`;
        const res = await billingApi.verifyVnpayReturn(qs);
        if (active) {
          if (res.isSuccess && res.data) {
            setResult(res.data);
          } else {
            setResult({
              isSuccess: false,
              errorMessage: res.error?.message || "Không thể xác thực giao dịch VNPay.",
              contractActivated: false,
            });
          }
          setLoading(false);
        }
        return;
      }

      // Trường hợp được redirect trực tiếp từ backend /payments/vnpay-return
      const isSuccess = searchParams.get("isSuccess")?.toLowerCase() === "true";
      const message = searchParams.get("message");
      const paymentCode = searchParams.get("paymentCode") || "";
      const invoiceId = Number(searchParams.get("invoiceId") || "0");
      if (active) {
        setResult({
          isSuccess,
          errorMessage: message || (isSuccess ? null : "Giao dịch không thành công."),
          contractActivated: isSuccess,
          payment: paymentCode
            ? {
                id: 0,
                paymentCode,
                invoiceId,
                amount: 0,
                paymentMethod: "VNPAY",
                status: searchParams.get("status") || (isSuccess ? "SUCCESS" : "FAILED"),
                expiredAt: "",
                createdAt: "",
              }
            : null,
        });
        setLoading(false);
      }
    }

    verify();
    return () => {
      active = false;
    };
  }, [searchParams]);

  if (loading) {
    return (
      <div className="max-w-md mx-auto px-4 py-24 text-center space-y-3">
        <Loader2 className="w-10 h-10 animate-spin text-blue-600 mx-auto" />
        <h2 className="text-base font-black text-slate-900">Đang xác thực kết quả thanh toán...</h2>
        <p className="text-xs text-slate-500">Vui lòng không đóng trình duyệt trong giây lát.</p>
      </div>
    );
  }

  const ok = Boolean(result?.isSuccess);
  const contractHref = result?.contractId
    ? `/tenant/contracts/${result.contractId}`
    : "/tenant/contracts";

  return (
    <div className="max-w-lg mx-auto px-4 py-16 w-full">
      <div className="bg-white rounded-3xl border border-slate-200 shadow-lg p-7 text-center space-y-5">
        <div
          className={`w-16 h-16 rounded-2xl flex items-center justify-center mx-auto ${
            ok ? "bg-emerald-50 text-emerald-600" : "bg-rose-50 text-rose-600"
          }`}
        >
          {ok ? <CheckCircle2 className="w-9 h-9" /> : <XCircle className="w-9 h-9" />}
        </div>

        <div className="space-y-1.5">
          <h1 className="text-xl font-black text-slate-900">
            {ok ? "Thanh Toán Thành Công!" : "Thanh Toán Không Thành Công"}
          </h1>
          <p className="text-xs text-slate-500 leading-relaxed">
            {ok
              ? result?.contractActivated
                ? "Giao dịch đã được xác nhận. Hợp đồng thuê phòng của bạn đã tự động chuyển sang trạng thái Đang có hiệu lực (ACTIVE)."
                : "Giao dịch thanh toán hóa đơn của bạn đã được ghi nhận hoàn tất."
              : result?.errorMessage || "Giao dịch đã bị hủy hoặc xảy ra lỗi trong quá trình xử lý."}
          </p>
        </div>

        {result?.payment && (
          <div className="p-4 rounded-2xl bg-slate-50 border border-slate-200/80 text-xs space-y-2 text-left">
            <div className="flex justify-between">
              <span className="text-slate-500">Mã giao dịch</span>
              <span className="font-mono font-bold text-slate-900">{result.payment.paymentCode}</span>
            </div>
            {result.payment.amount > 0 && (
              <div className="flex justify-between">
                <span className="text-slate-500">Số tiền</span>
                <span className="font-black text-blue-600">{formatVnd(result.payment.amount)}</span>
              </div>
            )}
            <div className="flex justify-between">
              <span className="text-slate-500">Trạng thái</span>
              <span className={`font-bold ${ok ? "text-emerald-600" : "text-rose-600"}`}>
                {result.payment.status}
              </span>
            </div>
          </div>
        )}

        <div className="flex flex-col sm:flex-row gap-3 pt-2">
          <Link
            href={contractHref}
            className="flex-1 py-2.5 px-4 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs flex items-center justify-center gap-1.5 shadow-sm"
          >
            <FileCheck2 className="w-4 h-4" />
            <span>Xem hợp đồng của tôi</span>
            <ArrowRight className="w-3.5 h-3.5" />
          </Link>
          <Link
            href="/tenant/dashboard?tab=bills"
            className="py-2.5 px-4 rounded-xl border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 font-bold text-xs"
          >
            Về Dashboard
          </Link>
        </div>
      </div>
    </div>
  );
}

export default function PaymentCallbackPage() {
  return (
    <Suspense
      fallback={
        <div className="max-w-md mx-auto px-4 py-24 text-center text-xs text-slate-400">
          Đang tải kết quả thanh toán...
        </div>
      }
    >
      <PaymentCallbackContent />
    </Suspense>
  );
}
