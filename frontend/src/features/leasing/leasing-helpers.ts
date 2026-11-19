import { LeasingContractStatus, LeasingDepositStatus } from "@/types";

export interface StatusMeta {
  label: string;
  badgeClass: string;
  dotClass: string;
  step: number; // 0..4 for lifecycle stepper, -1 for aborted
}

export const CONTRACT_STATUS_META: Record<string, StatusMeta> = {
  DRAFT: {
    label: "Bản nháp",
    badgeClass: "bg-slate-100 text-slate-700 border-slate-200",
    dotClass: "bg-slate-400",
    step: 0,
  },
  PENDING_SIGNATURE: {
    label: "Chờ ký OTP",
    badgeClass: "bg-amber-50 text-amber-700 border-amber-200",
    dotClass: "bg-amber-500",
    step: 1,
  },
  SIGNED: {
    label: "Đã ký 2 bên",
    badgeClass: "bg-indigo-50 text-indigo-700 border-indigo-200",
    dotClass: "bg-indigo-500",
    step: 2,
  },
  ACTIVE: {
    label: "Đang hiệu lực",
    badgeClass: "bg-emerald-50 text-emerald-700 border-emerald-200",
    dotClass: "bg-emerald-500",
    step: 3,
  },
  EXPIRED_PENDING_HANDOVER: {
    label: "Hết hạn · Chờ bàn giao",
    badgeClass: "bg-orange-50 text-orange-700 border-orange-200",
    dotClass: "bg-orange-500",
    step: 3,
  },
  HANDOVER_OVERDUE: {
    label: "Quá hạn bàn giao",
    badgeClass: "bg-rose-50 text-rose-700 border-rose-200",
    dotClass: "bg-rose-500",
    step: 3,
  },
  TERMINATED: {
    label: "Chấm dứt · Chờ bàn giao",
    badgeClass: "bg-rose-50 text-rose-700 border-rose-200",
    dotClass: "bg-rose-500",
    step: 3,
  },
  COMPLETED: {
    label: "Đã hoàn tất",
    badgeClass: "bg-teal-50 text-teal-700 border-teal-200",
    dotClass: "bg-teal-500",
    step: 4,
  },
  REJECTED: {
    label: "Bị từ chối",
    badgeClass: "bg-red-50 text-red-700 border-red-200",
    dotClass: "bg-red-500",
    step: -1,
  },
  SIGNATURE_EXPIRED: {
    label: "Quá hạn ký",
    badgeClass: "bg-slate-100 text-slate-600 border-slate-200",
    dotClass: "bg-slate-400",
    step: -1,
  },
  CANCELLED: {
    label: "Đã hủy",
    badgeClass: "bg-slate-100 text-slate-500 border-slate-200",
    dotClass: "bg-slate-400",
    step: -1,
  },
};

export const DEPOSIT_STATUS_META: Record<string, { label: string; className: string }> = {
  PENDING: { label: "Chưa thu cọc", className: "bg-amber-50 text-amber-700 border-amber-200" },
  PAID: { label: "Đã thu cọc", className: "bg-emerald-50 text-emerald-700 border-emerald-200" },
  REFUNDED: { label: "Đã hoàn cọc", className: "bg-blue-50 text-blue-700 border-blue-200" },
  FORFEITED: { label: "Đã khấu trừ hết", className: "bg-rose-50 text-rose-700 border-rose-200" },
};

export const STATUS_FILTER_TABS = [
  { key: "ALL", label: "Tất cả" },
  { key: "DRAFT", label: "Bản nháp" },
  { key: "PENDING_SIGNATURE", label: "Chờ ký OTP" },
  { key: "SIGNED", label: "Đã ký" },
  { key: "ACTIVE", label: "Đang hiệu lực" },
  { key: "EXPIRING", label: "Sắp hết hạn" },
  { key: "HANDOVER", label: "Chờ bàn giao" },
  { key: "COMPLETED", label: "Hoàn tất" },
  { key: "CANCELLED", label: "Đã hủy" },
];

export const LIFECYCLE_STEPS = ["Nháp", "Chờ ký OTP", "Đã ký", "Hiệu lực", "Hoàn tất"];

export function getStatusMeta(status: LeasingContractStatus): StatusMeta {
  return (
    CONTRACT_STATUS_META[status] ?? {
      label: status,
      badgeClass: "bg-slate-100 text-slate-700 border-slate-200",
      dotClass: "bg-slate-400",
      step: 0,
    }
  );
}

export function getDepositMeta(status: LeasingDepositStatus) {
  return (
    DEPOSIT_STATUS_META[status] ?? {
      label: status,
      className: "bg-slate-100 text-slate-600 border-slate-200",
    }
  );
}

export function formatVnd(amount: number): string {
  return `${Math.round(amount || 0).toLocaleString("vi-VN")} đ`;
}

export function formatDate(dateStr?: string | null): string {
  if (!dateStr) return "—";
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return dateStr;
  return d.toLocaleDateString("vi-VN");
}

export function formatDateTime(dateStr?: string | null): string {
  if (!dateStr) return "—";
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return dateStr;
  return d.toLocaleString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function getDaysRemaining(endDateStr: string): number {
  const end = new Date(endDateStr);
  const now = new Date();
  return Math.ceil((end.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
}

export function isHandoverStatus(status: LeasingContractStatus): boolean {
  return (
    status === "EXPIRED_PENDING_HANDOVER" ||
    status === "HANDOVER_OVERDUE" ||
    status === "TERMINATED"
  );
}

export function isDeletableContractStatus(status: LeasingContractStatus): boolean {
  return (
    status === "DRAFT" ||
    status === "CANCELLED" ||
    status === "REJECTED" ||
    status === "SIGNATURE_EXPIRED"
  );
}

export function isRestorableContractStatus(status: LeasingContractStatus): boolean {
  return status === "CANCELLED" || status === "REJECTED" || status === "SIGNATURE_EXPIRED";
}
