import {
  BillingInvoiceDto,
  BillingInvoiceStatus,
  BillingPaymentDto,
  BillingPaymentMethod,
  BillingPaymentStatus,
  LeasingContractDto,
} from "@/types";
import { billingApi } from "@/lib/billing-api";
import { parseContractSnapshot } from "@/features/leasing/contract-snapshot";
import {
  formatDate,
  formatDateTime,
  formatVnd,
} from "@/features/leasing/leasing-helpers";

export { formatDate, formatDateTime, formatVnd };

export interface BillingStatusMeta {
  label: string;
  badgeClass: string;
  dotClass: string;
}

export const INVOICE_STATUS_META: Record<string, BillingStatusMeta> = {
  UNPAID: {
    label: "Chờ thanh toán",
    badgeClass: "bg-amber-50 text-amber-700 border-amber-200",
    dotClass: "bg-amber-500",
  },
  OVERDUE: {
    label: "Quá hạn",
    badgeClass: "bg-rose-50 text-rose-700 border-rose-200",
    dotClass: "bg-rose-500",
  },
  PAID: {
    label: "Đã thanh toán",
    badgeClass: "bg-emerald-50 text-emerald-700 border-emerald-200",
    dotClass: "bg-emerald-500",
  },
  CANCELLED: {
    label: "Đã hủy",
    badgeClass: "bg-slate-100 text-slate-600 border-slate-200",
    dotClass: "bg-slate-400",
  },
};

export const PAYMENT_STATUS_META: Record<string, BillingStatusMeta> = {
  PENDING: {
    label: "Đang chờ VNPay",
    badgeClass: "bg-amber-50 text-amber-700 border-amber-200",
    dotClass: "bg-amber-500",
  },
  SUCCESS: {
    label: "Thành công",
    badgeClass: "bg-emerald-50 text-emerald-700 border-emerald-200",
    dotClass: "bg-emerald-500",
  },
  FAILED: {
    label: "Thất bại",
    badgeClass: "bg-rose-50 text-rose-700 border-rose-200",
    dotClass: "bg-rose-500",
  },
  EXPIRED: {
    label: "Hết hạn",
    badgeClass: "bg-slate-100 text-slate-600 border-slate-200",
    dotClass: "bg-slate-400",
  },
};

export const PAYMENT_METHOD_LABELS: Record<string, string> = {
  VNPAY: "VNPay",
  BANK_TRANSFER: "Chuyển khoản",
  CASH: "Tiền mặt",
  MOMO: "Ví MoMo",
};

export const INVOICE_STATUS_FILTER_OPTIONS = [
  { value: "ALL", label: "Tất cả trạng thái" },
  { value: "UNPAID", label: "Chờ thanh toán" },
  { value: "OVERDUE", label: "Quá hạn" },
  { value: "PAID", label: "Đã thanh toán" },
  { value: "CANCELLED", label: "Đã hủy" },
];

export const AGING_GROUP_BADGE_MAP: Record<string, string> = {
  NotDue: "bg-blue-50 text-blue-700 border-blue-200",
  Overdue1To30: "bg-amber-50 text-amber-700 border-amber-200",
  Overdue31To60: "bg-orange-50 text-orange-700 border-orange-200",
  OverdueOver60: "bg-rose-50 text-rose-700 border-rose-200",
};

export function getInvoiceStatusMeta(status: BillingInvoiceStatus): BillingStatusMeta {
  return (
    INVOICE_STATUS_META[status] ?? {
      label: status,
      badgeClass: "bg-slate-100 text-slate-700 border-slate-200",
      dotClass: "bg-slate-400",
    }
  );
}

export function getPaymentStatusMeta(status: BillingPaymentStatus): BillingStatusMeta {
  return (
    PAYMENT_STATUS_META[status] ?? {
      label: status,
      badgeClass: "bg-slate-100 text-slate-700 border-slate-200",
      dotClass: "bg-slate-400",
    }
  );
}

export function getPaymentMethodLabel(method: BillingPaymentMethod): string {
  return PAYMENT_METHOD_LABELS[method] ?? method;
}

/**
 * Số tiền còn phải thanh toán của một hóa đơn (hóa đơn đã hủy có công nợ bằng 0).
 */
export function getRemainingDebt(invoice: Pick<BillingInvoiceDto, "totalAmount" | "paidAmount" | "status">): number {
  if (invoice.status === "CANCELLED") return 0;
  return Math.max(0, (invoice.totalAmount || 0) - (invoice.paidAmount || 0));
}

export function isInvoicePayable(invoice: Pick<BillingInvoiceDto, "totalAmount" | "paidAmount" | "status">): boolean {
  return (invoice.status === "UNPAID" || invoice.status === "OVERDUE") && getRemainingDebt(invoice) > 0;
}

export function findActivePendingPayment(payments?: BillingPaymentDto[] | null): BillingPaymentDto | null {
  if (!payments?.length) return null;
  const nowMs = Date.now();
  return (
    payments.find((p) => {
      if (p.status !== "PENDING" || !p.paymentUrl) return false;
      const expMs = new Date(p.expiredAt).getTime();
      return Number.isFinite(expMs) && expMs > nowMs;
    }) ?? null
  );
}

export interface ContractDisplayInfo {
  contractCode: string;
  roomName: string;
  propertyName?: string;
}

export function buildContractDisplayMap(contracts: LeasingContractDto[]): Record<number, ContractDisplayInfo> {
  const map: Record<number, ContractDisplayInfo> = {};
  for (const ctr of contracts) {
    const snap = parseContractSnapshot(ctr.documentSnapshot);
    map[ctr.id] = {
      contractCode: ctr.contractCode,
      roomName: snap.roomName || `Phòng #${ctr.roomId}`,
      propertyName: snap.propertyName,
    };
  }
  return map;
}

export type CheckoutResolveResult =
  | { kind: "redirect"; paymentUrl: string; resumed: boolean }
  | { kind: "error"; message: string };

/**
 * Khởi tạo giao dịch VNPay cho hóa đơn, tự động dùng lại liên kết PENDING còn hạn nếu có.
 */
export async function resolveVnPayCheckoutUrl(
  invoiceId: number,
  knownPayments?: BillingPaymentDto[] | null
): Promise<CheckoutResolveResult> {
  const existingPending = findActivePendingPayment(knownPayments);
  if (existingPending?.paymentUrl) {
    return { kind: "redirect", paymentUrl: existingPending.paymentUrl, resumed: true };
  }

  const res = await billingApi.createPayment(invoiceId);
  if (res.isSuccess && res.data?.paymentUrl) {
    return { kind: "redirect", paymentUrl: res.data.paymentUrl, resumed: false };
  }

  if (res.error?.code === "Billing.PendingPaymentAlreadyExists") {
    const detailRes = await billingApi.getInvoiceById(invoiceId);
    const pending = detailRes.isSuccess && detailRes.data
      ? findActivePendingPayment(detailRes.data.payments)
      : null;
    if (pending?.paymentUrl) {
      return { kind: "redirect", paymentUrl: pending.paymentUrl, resumed: true };
    }
  }

  return {
    kind: "error",
    message: res.error?.message || "Không thể khởi tạo giao dịch thanh toán VNPay.",
  };
}

