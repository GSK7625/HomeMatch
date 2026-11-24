"use client";

import React, { useCallback, useEffect, useState } from "react";
import { Modal } from "@/components/Modal";
import { useToast } from "@/components/Toast";
import { billingApi } from "@/lib/billing-api";
import { BillingInvoiceDetailDto } from "@/types";
import {
  ContractDisplayInfo,
  findActivePendingPayment,
  formatDate,
  formatDateTime,
  formatVnd,
  getInvoiceStatusMeta,
  getPaymentMethodLabel,
  getPaymentStatusMeta,
  getRemainingDebt,
  isInvoicePayable,
  resolveVnPayCheckoutUrl,
} from "@/features/billing/billing-helpers";
import { cn } from "@/lib/utils";
import {
  AlertTriangle,
  Calendar,
  CheckCircle2,
  Clock,
  CreditCard,
  ExternalLink,
  FileText,
  History,
  Loader2,
  Receipt,
  RefreshCw,
  Zap,
  Droplet,
} from "lucide-react";

interface InvoiceDetailModalProps {
  invoiceId: number | null;
  isOpen: boolean;
  onClose: () => void;
  contractMap: Record<number, ContractDisplayInfo>;
  onPaymentUpdated?: () => void;
  readOnlyPayment?: boolean;
}

export function InvoiceDetailModal({
  invoiceId,
  isOpen,
  onClose,
  contractMap,
  onPaymentUpdated,
  readOnlyPayment = false,
}: InvoiceDetailModalProps) {
  const { showToast } = useToast();
  const [detail, setDetail] = useState<BillingInvoiceDetailDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [paying, setPaying] = useState(false);

  const loadDetail = useCallback(async () => {
    if (!invoiceId) return;
    setLoading(true);
    setErrorMsg(null);
    try {
      const res = await billingApi.getInvoiceById(invoiceId);
      if (res.isSuccess && res.data) {
        setDetail(res.data);
      } else {
        setDetail(null);
        setErrorMsg(res.error?.message || "Không thể tải chi tiết hóa đơn.");
      }
    } catch {
      setDetail(null);
      setErrorMsg("Lỗi kết nối khi tải chi tiết hóa đơn.");
    } finally {
      setLoading(false);
    }
  }, [invoiceId]);

  useEffect(() => {
    if (isOpen && invoiceId) {
      loadDetail();
    } else if (!isOpen) {
      setDetail(null);
      setErrorMsg(null);
    }
  }, [isOpen, invoiceId, loadDetail]);

  const handlePayNow = async () => {
    if (!detail) return;

    setPaying(true);
    try {
      const outcome = await resolveVnPayCheckoutUrl(detail.id, detail.payments);
      if (outcome.kind === "redirect") {
        if (!outcome.resumed) {
          onPaymentUpdated?.();
        }
        window.location.href = outcome.paymentUrl;
        return;
      }

      showToast(outcome.message, "error");
      await loadDetail();
    } catch {
      showToast("Không thể kết nối cổng thanh toán VNPay. Vui lòng thử lại.", "error");
    } finally {
      setPaying(false);
    }
  };

  const contractInfo = detail ? contractMap[detail.contractId] : undefined;
  const statusMeta = detail ? getInvoiceStatusMeta(detail.status) : null;
  const remainingDebt = detail ? getRemainingDebt(detail) : 0;
  const activePending = detail ? findActivePendingPayment(detail.payments) : null;
  const payable = detail && !readOnlyPayment ? isInvoicePayable(detail) : false;
  const reading = detail?.utilityReading ?? null;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={detail ? `Chi tiết hóa đơn ${detail.invoiceCode}` : "Chi tiết hóa đơn"}
      description={
        detail
          ? `Kỳ tháng ${String(detail.billingMonth).padStart(2, "0")}/${detail.billingYear} · ${
              contractInfo?.roomName || `Phòng #${detail.roomId}`
            }`
          : "Thông tin các khoản thu và lịch sử giao dịch thanh toán"
      }
      maxWidth="2xl"
    >
      {loading ? (
        <div className="py-12 flex flex-col items-center justify-center gap-3 text-slate-500">
          <Loader2 className="w-7 h-7 animate-spin text-blue-600" />
          <span className="text-xs font-medium">Đang tải chi tiết hóa đơn...</span>
        </div>
      ) : errorMsg ? (
        <div className="p-6 rounded-2xl bg-rose-50 border border-rose-200 text-center space-y-3">
          <AlertTriangle className="w-8 h-8 text-rose-500 mx-auto" />
          <p className="text-xs font-semibold text-rose-800">{errorMsg}</p>
          <button
            type="button"
            onClick={loadDetail}
            className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-white border border-rose-200 text-xs font-bold text-rose-700 hover:bg-rose-100 transition-colors"
          >
            <RefreshCw className="w-3.5 h-3.5" />
            <span>Thử lại</span>
          </button>
        </div>
      ) : detail && statusMeta ? (
        <div className="space-y-6">
          {/* Header Metadata */}
          <div className="flex flex-wrap items-center justify-between gap-3 p-4 rounded-2xl bg-slate-50 border border-slate-200/80">
            <div className="space-y-1">
              <div className="flex items-center gap-2 flex-wrap">
                <span className="font-mono text-xs font-bold px-2.5 py-0.5 rounded-md bg-white border border-slate-200 text-slate-800">
                  {detail.invoiceCode}
                </span>
                <span
                  className={cn(
                    "inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[11px] font-bold border",
                    statusMeta.badgeClass
                  )}
                >
                  <span className={cn("w-1.5 h-1.5 rounded-full", statusMeta.dotClass)} />
                  {statusMeta.label}
                </span>
              </div>
              <div className="flex flex-wrap items-center gap-3 text-xs text-slate-600 pt-1">
                <span className="inline-flex items-center gap-1">
                  <FileText className="w-3.5 h-3.5 text-slate-400" />
                  <span>Hợp đồng: </span>
                  <strong className="text-slate-900">
                    {detail.contractCode || contractInfo?.contractCode || `#${detail.contractId}`}
                  </strong>
                </span>
                <span className="inline-flex items-center gap-1">
                  <Calendar className="w-3.5 h-3.5 text-slate-400" />
                  <span>Hạn thanh toán: </span>
                  <strong className={detail.status === "OVERDUE" ? "text-rose-600" : "text-slate-900"}>
                    {formatDate(detail.dueDate)}
                  </strong>
                </span>
              </div>
            </div>

            {detail.paidAt && (
              <div className="text-right text-xs">
                <span className="text-slate-400 block">Thanh toán lúc</span>
                <span className="font-bold text-emerald-700 inline-flex items-center gap-1">
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  {formatDateTime(detail.paidAt)}
                </span>
              </div>
            )}
          </div>

          {detail.status === "CANCELLED" && detail.cancelReason && (
            <div className="p-3.5 rounded-xl bg-slate-100 border border-slate-200 text-xs text-slate-700">
              <strong className="font-bold">Lý do hủy hóa đơn:</strong> {detail.cancelReason}
              {detail.cancelledAt && (
                <span className="block text-[11px] text-slate-500 mt-0.5">
                  Thời điểm hủy: {formatDateTime(detail.cancelledAt)}
                </span>
              )}
            </div>
          )}

          {/* Breakdown & Payment Summary */}
          <div className="space-y-3">
            <h4 className="text-xs font-bold uppercase tracking-wider text-slate-500 flex items-center gap-1.5">
              <Receipt className="w-3.5 h-3.5 text-blue-600" />
              <span>Chi tiết các khoản trong kỳ</span>
            </h4>

            <div className="rounded-2xl border border-slate-200 overflow-hidden">
              <div className="divide-y divide-slate-100 text-xs bg-white">
                <div className="flex items-center justify-between px-4 py-2.5">
                  <span className="text-slate-600">Tiền thuê phòng</span>
                  <span className="font-semibold text-slate-900">{formatVnd(detail.rentAmount)}</span>
                </div>
                <div className="flex items-center justify-between px-4 py-2.5">
                  <div>
                    <span className="text-slate-600">Tiền điện</span>
                    {reading && (
                      <span className="block text-[11px] text-slate-400">
                        Chỉ số: {reading.electricityOld} → {reading.electricityNew} ({reading.electricityUsage} kWh × {formatVnd(reading.electricityUnitPrice)}/kWh)
                      </span>
                    )}
                  </div>
                  <span className="font-semibold text-slate-900">{formatVnd(detail.electricityAmount)}</span>
                </div>
                <div className="flex items-center justify-between px-4 py-2.5">
                  <div>
                    <span className="text-slate-600">Tiền nước</span>
                    {reading && (
                      <span className="block text-[11px] text-slate-400">
                        Chỉ số: {reading.waterOld} → {reading.waterNew} ({reading.waterUsage} m³ × {formatVnd(reading.waterUnitPrice)}/m³)
                      </span>
                    )}
                  </div>
                  <span className="font-semibold text-slate-900">{formatVnd(detail.waterAmount)}</span>
                </div>
                <div className="flex items-center justify-between px-4 py-2.5">
                  <span className="text-slate-600">Phí dịch vụ khác</span>
                  <span className="font-semibold text-slate-900">{formatVnd(detail.serviceAmount)}</span>
                </div>
                {detail.discountAmount > 0 && (
                  <div className="flex items-center justify-between px-4 py-2.5 bg-emerald-50/40">
                    <span className="text-emerald-700 font-medium">Giảm trừ / Khấu trừ</span>
                    <span className="font-bold text-emerald-700">-{formatVnd(detail.discountAmount)}</span>
                  </div>
                )}
              </div>

              <div className="grid grid-cols-3 gap-2 bg-slate-50 border-t border-slate-200 px-4 py-3 text-xs">
                <div>
                  <span className="text-slate-400 block text-[11px]">Tổng phải trả</span>
                  <span className="font-black text-slate-900 text-sm">{formatVnd(detail.totalAmount)}</span>
                </div>
                <div>
                  <span className="text-slate-400 block text-[11px]">Đã thanh toán</span>
                  <span className="font-black text-emerald-600 text-sm">{formatVnd(detail.paidAmount)}</span>
                </div>
                <div>
                  <span className="text-slate-400 block text-[11px]">Còn nợ</span>
                  <span
                    className={cn(
                      "font-black text-sm",
                      remainingDebt > 0 ? "text-rose-600" : "text-slate-700"
                    )}
                  >
                    {formatVnd(remainingDebt)}
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Utility Meter Reading Breakdown Table (if available) */}
          {reading && (
            <div className="space-y-2.5">
              <h4 className="text-xs font-bold uppercase tracking-wider text-slate-500 flex items-center gap-1.5">
                <Zap className="w-3.5 h-3.5 text-amber-500" />
                <span>Bảng chỉ số tiêu thụ điện nước trong kỳ (Chốt ngày {formatDate(reading.readingDate)})</span>
              </h4>
              <div className="overflow-x-auto rounded-2xl border border-slate-200">
                <table className="w-full text-left text-xs bg-white">
                  <thead>
                    <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-semibold">
                      <th className="py-2 px-3">Khoản mục</th>
                      <th className="py-2 px-3 text-right">Chỉ số đầu</th>
                      <th className="py-2 px-3 text-right">Chỉ số cuối</th>
                      <th className="py-2 px-3 text-right">Tiêu thụ</th>
                      <th className="py-2 px-3 text-right">Đơn giá</th>
                      <th className="py-2 px-3 text-right">Thành tiền</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    <tr>
                      <td className="py-2.5 px-3 font-bold text-slate-800 flex items-center gap-1.5">
                        <Zap className="w-3.5 h-3.5 text-amber-500" />
                        <span>Điện sinh hoạt (kWh)</span>
                      </td>
                      <td className="py-2.5 px-3 text-right font-mono">{reading.electricityOld}</td>
                      <td className="py-2.5 px-3 text-right font-mono font-bold">{reading.electricityNew}</td>
                      <td className="py-2.5 px-3 text-right font-bold text-amber-700">{reading.electricityUsage} kWh</td>
                      <td className="py-2.5 px-3 text-right">{formatVnd(reading.electricityUnitPrice)}</td>
                      <td className="py-2.5 px-3 text-right font-bold text-slate-900">{formatVnd(reading.electricityCost)}</td>
                    </tr>
                    <tr>
                      <td className="py-2.5 px-3 font-bold text-slate-800 flex items-center gap-1.5">
                        <Droplet className="w-3.5 h-3.5 text-sky-500" />
                        <span>Nước sinh hoạt (m³)</span>
                      </td>
                      <td className="py-2.5 px-3 text-right font-mono">{reading.waterOld}</td>
                      <td className="py-2.5 px-3 text-right font-mono font-bold">{reading.waterNew}</td>
                      <td className="py-2.5 px-3 text-right font-bold text-sky-700">{reading.waterUsage} m³</td>
                      <td className="py-2.5 px-3 text-right">{formatVnd(reading.waterUnitPrice)}</td>
                      <td className="py-2.5 px-3 text-right font-bold text-slate-900">{formatVnd(reading.waterCost)}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Payment CTA */}
          {payable && (
            <div className="p-4 rounded-2xl bg-blue-50/70 border border-blue-200 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div className="space-y-1">
                <p className="text-xs font-bold text-blue-950">
                  {activePending
                    ? "Bạn đang có phiên thanh toán VNPay chờ hoàn tất"
                    : "Thanh toán hóa đơn trực tuyến qua cổng VNPay"}
                </p>
                <p className="text-[11px] text-blue-700">
                  {activePending ? (
                    <>
                      Mã giao dịch <span className="font-mono font-bold">{activePending.paymentCode}</span> còn hiệu lực đến{" "}
                      <strong>{formatDateTime(activePending.expiredAt)}</strong>.
                    </>
                  ) : (
                    <>Số tiền cần thanh toán toàn phần: <strong>{formatVnd(remainingDebt)}</strong>.</>
                  )}
                </p>
              </div>

              <button
                type="button"
                disabled={paying}
                onClick={handlePayNow}
                className="px-4 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white text-xs font-bold shadow-sm shadow-blue-500/25 transition-all inline-flex items-center justify-center gap-2 shrink-0 cursor-pointer"
              >
                {paying ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <CreditCard className="w-4 h-4" />
                )}
                <span>
                  {paying
                    ? "Đang chuyển hướng..."
                    : activePending
                    ? "Tiếp tục thanh toán VNPay"
                    : "Thanh toán qua VNPay"}
                </span>
              </button>
            </div>
          )}

          {/* Payment History */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <h4 className="text-xs font-bold uppercase tracking-wider text-slate-500 flex items-center gap-1.5">
                <History className="w-3.5 h-3.5 text-blue-600" />
                <span>Lịch sử giao dịch thanh toán ({detail.payments.length})</span>
              </h4>
              <button
                type="button"
                onClick={loadDetail}
                className="text-[11px] font-bold text-blue-600 hover:text-blue-700 inline-flex items-center gap-1"
              >
                <RefreshCw className="w-3 h-3" />
                <span>Cập nhật</span>
              </button>
            </div>

            {detail.payments.length === 0 ? (
              <div className="p-6 text-center rounded-2xl bg-slate-50 border border-slate-100 text-xs text-slate-500">
                Chưa có giao dịch thanh toán nào cho hóa đơn này.
              </div>
            ) : (
              <div className="space-y-2.5">
                {detail.payments.map((payment) => {
                  const pMeta = getPaymentStatusMeta(payment.status);
                  const canResume =
                    payment.status === "PENDING" &&
                    Boolean(payment.paymentUrl) &&
                    new Date(payment.expiredAt).getTime() > Date.now();

                  return (
                    <div
                      key={payment.id}
                      className="p-3.5 rounded-xl border border-slate-200/80 bg-white flex flex-col sm:flex-row sm:items-center justify-between gap-3"
                    >
                      <div className="space-y-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <span className="font-mono text-xs font-bold text-slate-900">
                            {payment.paymentCode}
                          </span>
                          <span
                            className={cn(
                              "inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold border",
                              pMeta.badgeClass
                            )}
                          >
                            <span className={cn("w-1.5 h-1.5 rounded-full", pMeta.dotClass)} />
                            {pMeta.label}
                          </span>
                          <span className="text-[11px] font-semibold text-slate-500">
                            • {getPaymentMethodLabel(payment.paymentMethod)}
                            {payment.bankCode ? ` (${payment.bankCode})` : ""}
                          </span>
                        </div>

                        <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-[11px] text-slate-500">
                          <span className="inline-flex items-center gap-1">
                            <Clock className="w-3 h-3" />
                            Tạo lúc: {formatDateTime(payment.createdAt)}
                          </span>
                          {payment.completedAt ? (
                            <span className="text-emerald-700 font-medium">
                              Hoàn tất: {formatDateTime(payment.completedAt)}
                            </span>
                          ) : (
                            <span>Hết hạn: {formatDateTime(payment.expiredAt)}</span>
                          )}
                          {payment.gatewayTransactionNo && (
                            <span className="font-mono">Mã GD cổng: {payment.gatewayTransactionNo}</span>
                          )}
                        </div>
                      </div>

                      <div className="flex sm:flex-col items-center sm:items-end justify-between gap-1.5 shrink-0">
                        <span className="font-black text-sm text-slate-900">
                          {formatVnd(payment.amount)}
                        </span>
                        {canResume && payment.paymentUrl && (
                          <a
                            href={payment.paymentUrl}
                            className="inline-flex items-center gap-1 text-[11px] font-bold text-blue-600 hover:text-blue-700"
                          >
                            <span>Mở lại VNPay</span>
                            <ExternalLink className="w-3 h-3" />
                          </a>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      ) : null}
    </Modal>
  );
}
