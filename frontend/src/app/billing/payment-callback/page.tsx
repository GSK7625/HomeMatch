"use client";

import React, { Suspense, useEffect, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { billingApi } from "@/lib/billing-api";
import { getStoredToken } from "@/lib/api-client";
import { BillingInvoiceDetailDto } from "@/types";
import {
  formatDateTime,
  formatVnd,
  getInvoiceStatusMeta,
} from "@/features/billing/billing-helpers";
import { cn } from "@/lib/utils";
import {
  ArrowLeft,
  CheckCircle2,
  CreditCard,
  Loader2,
  Receipt,
  XCircle,
} from "lucide-react";

interface CallbackViewState {
  loading: boolean;
  isSuccess: boolean;
  title: string;
  message: string;
  paymentCode?: string;
  invoiceId?: number;
  amount?: number;
  bankCode?: string;
  gatewayTransactionNo?: string;
  paymentStatus?: string;
  invoice?: BillingInvoiceDetailDto | null;
}

function mapVnpayErrorMessage(responseCode: string | null, ipnRspCode?: string, fallback?: string): string {
  if (ipnRspCode === "97") return "Chữ ký xác thực từ VNPay không hợp lệ.";
  if (ipnRspCode === "01") return "Không tìm thấy giao dịch tương ứng trong hệ thống.";
  if (ipnRspCode === "04") return "Số tiền thanh toán không khớp với hóa đơn.";

  switch (responseCode) {
    case "24":
      return "Bạn đã hủy giao dịch thanh toán trên cổng VNPay.";
    case "11":
      return "Giao dịch đã hết hạn chờ thanh toán. Vui lòng tạo giao dịch mới.";
    case "51":
      return "Tài khoản không đủ số dư để thực hiện giao dịch.";
    case "65":
      return "Tài khoản đã vượt quá hạn mức giao dịch trong ngày.";
    case "75":
      return "Ngân hàng thanh toán đang bảo trì. Vui lòng thử lại sau.";
    default:
      return fallback || "Giao dịch thanh toán không thành công. Vui lòng kiểm tra lại hoặc thử lại.";
  }
}

function PaymentCallbackContent() {
  const searchParams = useSearchParams();
  const [state, setState] = useState<CallbackViewState>({
    loading: true,
    isSuccess: false,
    title: "Đang xác thực thanh toán...",
    message: "Vui lòng chờ trong giây lát khi hệ thống đối soát giao dịch với VNPay.",
  });

  useEffect(() => {
    let active = true;

    async function processCallback() {
      const hasRawVnpayParams =
        searchParams.has("vnp_TxnRef") || searchParams.has("vnp_SecureHash");

      if (hasRawVnpayParams) {
        const paymentCode = searchParams.get("vnp_TxnRef") || undefined;
        const vnpResponseCode = searchParams.get("vnp_ResponseCode");
        const vnpTransactionStatus = searchParams.get("vnp_TransactionStatus");
        const bankCode = searchParams.get("vnp_BankCode") || undefined;
        const gatewayTransactionNo = searchParams.get("vnp_TransactionNo") || undefined;
        const rawAmount = Number(searchParams.get("vnp_Amount"));
        const amount = Number.isFinite(rawAmount) && rawAmount > 0 ? rawAmount / 100 : undefined;

        try {
          const ipnResult = await billingApi.forwardVnpayIpn(searchParams.toString());
          if (!active) return;

          const ipnValid = ipnResult.rspCode === "00" || ipnResult.rspCode === "02";
          const gatewayOk =
            vnpResponseCode === "00" &&
            (!vnpTransactionStatus || vnpTransactionStatus === "00");
          const succeeded = ipnValid && gatewayOk;

          setState({
            loading: false,
            isSuccess: succeeded,
            title: succeeded ? "Thanh Toán Hóa Đơn Thành Công" : "Thanh Toán Chưa Hoàn Tất",
            message: succeeded
              ? "Giao dịch VNPay đã được xác thực và ghi nhận vào hệ thống."
              : mapVnpayErrorMessage(vnpResponseCode, ipnResult.rspCode, ipnResult.message),
            paymentCode,
            amount,
            bankCode,
            gatewayTransactionNo,
            paymentStatus: succeeded ? "SUCCESS" : "FAILED",
            invoice: null,
          });
        } catch {
          if (!active) return;
          setState({
            loading: false,
            isSuccess: false,
            title: "Không Thể Xác Thực Giao Dịch",
            message: "Không thể kết nối máy chủ để xác nhận giao dịch VNPay. Vui lòng kiểm tra lại trong mục Hóa đơn.",
            paymentCode,
            amount,
            bankCode,
            gatewayTransactionNo,
          });
        }
        return;
      }

      // Trường hợp được backend /api/v1/billing/payments/vnpay-return chuyển hướng về
      const paymentCode = searchParams.get("paymentCode") || undefined;
      const invoiceIdParam = Number(searchParams.get("invoiceId"));
      const invoiceId = Number.isFinite(invoiceIdParam) && invoiceIdParam > 0 ? invoiceIdParam : undefined;
      const paymentStatus = searchParams.get("status") || undefined;
      const isSuccess = (searchParams.get("isSuccess") || "").toLowerCase() === "true";
      const messageParam = searchParams.get("message");

      let invoiceDetail: BillingInvoiceDetailDto | null = null;
      if (invoiceId && getStoredToken()) {
        const invRes = await billingApi.getInvoiceById(invoiceId).catch(() => null);
        if (invRes?.isSuccess && invRes.data) {
          invoiceDetail = invRes.data;
        }
      }

      if (!active) return;

      const matchedPayment = paymentCode
        ? invoiceDetail?.payments.find((p) => p.paymentCode === paymentCode)
        : undefined;

      setState({
        loading: false,
        isSuccess,
        title: isSuccess ? "Thanh Toán Hóa Đơn Thành Công" : "Thanh Toán Chưa Hoàn Tất",
        message: isSuccess
          ? "Giao dịch của bạn đã được xác nhận thành công."
          : messageParam || "Giao dịch thanh toán không thành công hoặc đã bị hủy.",
        paymentCode,
        invoiceId,
        amount: matchedPayment?.amount ?? invoiceDetail?.totalAmount,
        bankCode: matchedPayment?.bankCode ?? undefined,
        gatewayTransactionNo: matchedPayment?.gatewayTransactionNo ?? undefined,
        paymentStatus: matchedPayment?.status ?? paymentStatus,
        invoice: invoiceDetail,
      });
    }

    processCallback();
    return () => {
      active = false;
    };
  }, [searchParams]);

  if (state.loading) {
    return (
      <div className="max-w-lg mx-auto px-4 py-20">
        <div className="bg-white rounded-3xl border border-slate-200/80 shadow-sm p-8 text-center space-y-4">
          <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto">
            <Loader2 className="w-8 h-8 animate-spin" />
          </div>
          <h1 className="text-lg font-black text-slate-900">{state.title}</h1>
          <p className="text-xs text-slate-500 leading-relaxed">{state.message}</p>
        </div>
      </div>
    );
  }

  const invoiceMeta = state.invoice ? getInvoiceStatusMeta(state.invoice.status) : null;

  return (
    <div className="max-w-xl mx-auto px-4 py-12 sm:py-16 w-full flex-1">
      <div className="bg-white rounded-3xl border border-slate-200/80 shadow-sm p-6 sm:p-8 space-y-6">
        <div className="text-center space-y-3">
          <div
            className={cn(
              "w-16 h-16 rounded-2xl flex items-center justify-center mx-auto",
              state.isSuccess
                ? "bg-emerald-50 text-emerald-600"
                : "bg-rose-50 text-rose-600"
            )}
          >
            {state.isSuccess ? (
              <CheckCircle2 className="w-9 h-9" />
            ) : (
              <XCircle className="w-9 h-9" />
            )}
          </div>

          <h1 className="text-xl sm:text-2xl font-black text-slate-900">
            {state.title}
          </h1>
          <p className="text-xs sm:text-sm text-slate-600 max-w-md mx-auto leading-relaxed">
            {state.message}
          </p>
        </div>

        {(state.paymentCode || state.amount || state.invoice) && (
          <div className="rounded-2xl bg-slate-50 border border-slate-200/80 p-4 space-y-2.5 text-xs">
            {state.paymentCode && (
              <div className="flex items-center justify-between gap-2">
                <span className="text-slate-500">Mã giao dịch</span>
                <span className="font-mono font-bold text-slate-900">{state.paymentCode}</span>
              </div>
            )}
            {typeof state.amount === "number" && state.amount > 0 && (
              <div className="flex items-center justify-between gap-2">
                <span className="text-slate-500">Số tiền giao dịch</span>
                <span className="font-black text-blue-600 text-sm">{formatVnd(state.amount)}</span>
              </div>
            )}
            {state.bankCode && (
              <div className="flex items-center justify-between gap-2">
                <span className="text-slate-500">Ngân hàng</span>
                <span className="font-bold text-slate-800">{state.bankCode}</span>
              </div>
            )}
            {state.gatewayTransactionNo && (
              <div className="flex items-center justify-between gap-2">
                <span className="text-slate-500">Mã tham chiếu VNPay</span>
                <span className="font-mono font-semibold text-slate-800">
                  {state.gatewayTransactionNo}
                </span>
              </div>
            )}
            {state.invoice && invoiceMeta && (
              <>
                <div className="border-t border-slate-200 my-1 pt-2.5 flex items-center justify-between gap-2">
                  <span className="text-slate-500">Hóa đơn</span>
                  <span className="font-bold text-slate-900">
                    {state.invoice.invoiceCode} (Tháng{" "}
                    {String(state.invoice.billingMonth).padStart(2, "0")}/
                    {state.invoice.billingYear})
                  </span>
                </div>
                <div className="flex items-center justify-between gap-2">
                  <span className="text-slate-500">Trạng thái hóa đơn</span>
                  <span
                    className={cn(
                      "px-2.5 py-0.5 rounded-full text-[11px] font-bold border",
                      invoiceMeta.badgeClass
                    )}
                  >
                    {invoiceMeta.label}
                  </span>
                </div>
                {state.invoice.paidAt && (
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-slate-500">Thời gian thanh toán</span>
                    <span className="font-medium text-slate-700">
                      {formatDateTime(state.invoice.paidAt)}
                    </span>
                  </div>
                )}
              </>
            )}
          </div>
        )}

        <div className="flex flex-col sm:flex-row items-center justify-center gap-3 pt-2">
          <Link
            href="/tenant/dashboard?tab=bills"
            className="w-full sm:w-auto px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-sm shadow-blue-500/20 transition-all inline-flex items-center justify-center gap-2"
          >
            {state.isSuccess ? (
              <Receipt className="w-4 h-4" />
            ) : (
              <CreditCard className="w-4 h-4" />
            )}
            <span>
              {state.isSuccess ? "Xem hóa đơn & thanh toán" : "Quay lại thử thanh toán"}
            </span>
          </Link>

          <Link
            href="/tenant/dashboard"
            className="w-full sm:w-auto px-5 py-2.5 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold transition-all inline-flex items-center justify-center gap-2"
          >
            <ArrowLeft className="w-4 h-4" />
            <span>Về Dashboard Người thuê</span>
          </Link>
        </div>
      </div>
    </div>
  );
}

export default function BillingPaymentCallbackPage() {
  return (
    <Suspense
      fallback={
        <div className="max-w-lg mx-auto px-4 py-20 text-center text-xs text-slate-500">
          Đang tải kết quả thanh toán...
        </div>
      }
    >
      <PaymentCallbackContent />
    </Suspense>
  );
}
