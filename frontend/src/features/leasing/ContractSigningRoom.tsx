"use client";

import React, { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import { useToast } from "@/components/Toast";
import { leasingApi } from "@/lib/leasing-api";
import { billingApi } from "@/lib/billing-api";
import {
  formatDate,
  formatDateTime,
  formatVnd,
  getStatusMeta,
} from "@/features/leasing/leasing-helpers";
import { calcLeaseMonths, parseContractSnapshot } from "@/features/leasing/contract-snapshot";
import { ContractA4Document } from "@/features/leasing/ContractA4Document";
import { OtpSignPanel } from "@/features/leasing/OtpSignPanel";
import { LeasingContractDetailDto } from "@/types";
import { cn } from "@/lib/utils";
import {
  ArrowLeft,
  Building2,
  Calendar,
  CheckCircle2,
  Clock,
  Cloud,
  CreditCard,
  FileCheck2,
  Loader2,
  User,
  Wallet,
} from "lucide-react";

interface ContractSigningRoomProps {
  contractId: number;
  role: "LANDLORD" | "TENANT";
}

export function ContractSigningRoom({ contractId, role }: ContractSigningRoomProps) {
  const { user } = useAuth();
  const { showToast } = useToast();
  const [detail, setDetail] = useState<LeasingContractDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [busyDoc, setBusyDoc] = useState(false);
  const [payingDeposit, setPayingDeposit] = useState(false);

  const backHref = role === "LANDLORD" ? "/landlord/contracts" : "/tenant/contracts";

  const loadDetail = useCallback(async () => {
    setLoading(true);
    try {
      const res = await leasingApi.getContractDetails(contractId);
      if (res.isSuccess && res.data) {
        setDetail(res.data);
      } else {
        showToast(res.error?.message || "Không tìm thấy hợp đồng.", "error");
      }
    } finally {
      setLoading(false);
    }
  }, [contractId, showToast]);

  useEffect(() => {
    if (user && contractId > 0) loadDetail();
  }, [user, contractId, loadDetail]);

  const snapshot = useMemo(
    () => parseContractSnapshot(detail?.documentSnapshot),
    [detail?.documentSnapshot]
  );

  const handleDownloadOriginal = async () => {
    setBusyDoc(true);
    try {
      const res = await leasingApi.getDocumentUrl(contractId);
      if (res.isSuccess && res.data?.downloadUrl) {
        window.open(res.data.downloadUrl, "_blank", "noopener,noreferrer");
      } else {
        showToast(res.error?.message || "Chưa có file PDF gốc đính kèm.", "info");
      }
    } finally {
      setBusyDoc(false);
    }
  };

  const handleCopyHash = (hash: string) => {
    navigator.clipboard?.writeText(hash);
    showToast("Đã sao chép mã kiểm chứng SHA-256!", "info");
  };

  if (!user) {
    return (
      <div className="max-w-md mx-auto py-20 px-4 text-center space-y-4">
        <h2 className="text-xl font-black text-slate-900">Hợp Đồng Thuê Nhà Điện Tử</h2>
        <p className="text-xs text-slate-500">Vui lòng đăng nhập để xem và ký xác thực hợp đồng.</p>
        <Link
          href={`/auth/login?redirect=${backHref}/${contractId}`}
          className="inline-flex px-5 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold"
        >
          Đăng nhập ngay
        </Link>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-20 flex flex-col items-center justify-center gap-3 text-slate-400">
        <Loader2 className="w-7 h-7 animate-spin text-blue-600" />
        <span className="text-xs font-semibold">Đang tải bản thảo hợp đồng điện tử...</span>
      </div>
    );
  }

  if (!detail) {
    return (
      <div className="max-w-md mx-auto py-20 px-4 text-center space-y-3">
        <h2 className="text-lg font-black text-slate-900">Không thể tải hợp đồng #{contractId}</h2>
        <p className="text-xs text-slate-500">Hợp đồng không tồn tại hoặc tài khoản của bạn không có quyền xem.</p>
        <Link href={backHref} className="inline-flex items-center gap-1.5 text-xs font-bold text-blue-600 hover:underline">
          <ArrowLeft className="w-4 h-4" />
          Quay lại danh sách hợp đồng
        </Link>
      </div>
    );
  }

  const statusMeta = getStatusMeta(detail.status);
  const months = calcLeaseMonths(detail.startDate, detail.endDate);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6 w-full flex-1 space-y-5 print:max-w-none print:p-0 print:m-0 print:space-y-0">
      {/* Back Link */}
      <Link
        href={backHref}
        className="inline-flex items-center gap-1.5 text-xs font-bold text-slate-500 hover:text-blue-600 transition-colors print:hidden"
      >
        <ArrowLeft className="w-4 h-4" />
        <span>Quay lại danh sách hợp đồng</span>
      </Link>

      {/* Top E-Contract Header Card */}
      <div className="bg-white rounded-2xl border border-slate-200/90 shadow-xs p-5 space-y-5 print:hidden">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-start gap-3">
            <div className="w-11 h-11 rounded-2xl bg-blue-600 text-white flex items-center justify-center shadow-md shadow-blue-500/20 shrink-0">
              <FileCheck2 className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2 flex-wrap">
                <h1 className="text-lg sm:text-xl font-black text-slate-900 tracking-tight">
                  Hợp Đồng Thuê Nhà Điện Tử
                </h1>
                <span className="font-mono text-xs font-bold px-2.5 py-0.5 rounded-lg bg-slate-100 text-slate-700 border border-slate-200">
                  #{detail.contractCode}
                </span>
              </div>
              <p className="text-[11px] text-slate-500 mt-0.5">
                Xác lập pháp lý theo Luật Nhà ở 2023, Bộ luật Dân sự 2015 & Luật Giao dịch Điện tử 2023
              </p>
            </div>
          </div>

          <span
            className={cn(
              "inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-full text-xs font-bold border self-start sm:self-center",
              statusMeta.badgeClass
            )}
          >
            <span className={cn("w-2 h-2 rounded-full", statusMeta.dotClass)} />
            <span>
              {detail.status} ({statusMeta.label})
            </span>
          </span>
        </div>

        {/* 4 Summary Tiles */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
          <SummaryTile
            icon={Building2}
            label="Phòng thuê & Địa chỉ"
            primary={snapshot.roomName || `Phòng #${detail.roomId}`}
            secondary={snapshot.propertyName || snapshot.propertyAddress || "Tài sản xác thực HomeMatch"}
          />
          <SummaryTile
            icon={User}
            label="Đại diện Bên A (Chủ trọ)"
            primary={snapshot.landlord?.fullName || `Chủ nhà #${detail.landlordId}`}
            secondary={snapshot.landlord?.phone ? `${snapshot.landlord.phone} · Đã KYC` : "Đã xác thực tài khoản"}
          />
          <SummaryTile
            icon={Wallet}
            label="Giá thuê & Tiền cọc"
            primary={`${formatVnd(detail.monthlyRent)} /tháng`}
            secondary={`Cọc ${formatVnd(detail.depositAmount)} · Đóng ngày ${detail.billingDueDay}`}
            highlight
          />
          <SummaryTile
            icon={Calendar}
            label="Thời hạn hợp đồng"
            primary={`${months} Tháng`}
            secondary={`${formatDate(detail.startDate)} – ${formatDate(detail.endDate)}`}
          />
        </div>

        {/* 2-Party Signing Progress Strip */}
        <div className="pt-3 border-t border-slate-100 flex flex-col md:flex-row md:items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-4 text-xs">
            <PartyProgressItem
              label={`Chủ nhà (Bên A)${role === "LANDLORD" ? " · Bạn" : ""}`}
              signedAt={detail.landlordSignedAt}
            />
            <span className="text-slate-300 hidden sm:inline">───</span>
            <PartyProgressItem
              label={`Khách thuê (Bên B)${role === "TENANT" ? " · Bạn" : ""}`}
              signedAt={detail.tenantSignedAt}
            />
          </div>

          <div className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-slate-100 text-slate-600 text-[11px] font-semibold self-start md:self-auto">
            <Cloud className="w-3.5 h-3.5 text-blue-600" />
            <span>Cloudflare R2 Presigned Document Safe</span>
          </div>
        </div>
      </div>

      {/* Main 2-Column Split: A4 Document (Left) + OTP & Deposit Panel (Right) */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start print:block">
        <div className="lg:col-span-8 print:w-full">
          <ContractA4Document
            detail={detail}
            snapshot={snapshot}
            busyDoc={busyDoc}
            onDownloadOriginal={handleDownloadOriginal}
            onCopyHash={handleCopyHash}
          />
        </div>

        <div className="lg:col-span-4 lg:sticky lg:top-20 space-y-4 print:hidden">
          {role === "TENANT" && detail.status === "SIGNED" && (
            <div className="p-5 rounded-2xl bg-gradient-to-br from-blue-600 to-indigo-700 text-white shadow-lg shadow-blue-500/20 space-y-3">
              <div className="flex items-center gap-2 text-blue-100 text-[11px] font-bold uppercase tracking-wider">
                <CreditCard className="w-4 h-4" />
                <span>Bước cuối: Thanh toán kích hoạt</span>
              </div>
              <h3 className="text-base font-black leading-snug">
                Thanh toán tiền cọc & Kích hoạt hợp đồng
              </h3>
              <p className="text-xs text-blue-100 leading-relaxed">
                Cả hai bên đã hoàn tất ký số. Thanh toán khoản cọc{" "}
                <strong className="text-white">
                  {formatVnd(detail.depositAmount > 0 ? detail.depositAmount : detail.monthlyRent)}
                </strong>{" "}
                để hệ thống tự động chuyển hợp đồng sang trạng thái <strong>ACTIVE</strong>.
              </p>
              <button
                type="button"
                disabled={payingDeposit}
                onClick={async () => {
                  setPayingDeposit(true);
                  try {
                    const res = await billingApi.payContractDeposit(detail.id);
                    if (res.isSuccess && res.data?.paymentUrl) {
                      window.location.href = res.data.paymentUrl;
                    } else {
                      showToast(
                        res.error?.message || "Không thể khởi tạo giao dịch thanh toán.",
                        "error"
                      );
                    }
                  } finally {
                    setPayingDeposit(false);
                  }
                }}
                className="w-full py-2.5 px-4 rounded-xl bg-white text-blue-700 hover:bg-blue-50 font-black text-xs flex items-center justify-center gap-2 shadow-sm transition-all disabled:opacity-60"
              >
                {payingDeposit ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    <span>Đang tạo cổng thanh toán...</span>
                  </>
                ) : (
                  <>
                    <CreditCard className="w-4 h-4" />
                    <span>Thanh toán qua VNPay ngay</span>
                  </>
                )}
              </button>
            </div>
          )}

          {detail.status === "ACTIVE" && (
            <div className="p-4 rounded-2xl bg-emerald-50 border border-emerald-200 flex items-start gap-3">
              <CheckCircle2 className="w-5 h-5 text-emerald-600 shrink-0 mt-0.5" />
              <div className="text-xs">
                <div className="font-black text-emerald-900">Hợp đồng đã kích hoạt (ACTIVE)</div>
                <p className="text-emerald-700 mt-0.5">
                  Hợp đồng đang có hiệu lực pháp lý. Tiền cọc:{" "}
                  <span className="font-bold">{detail.depositStatus}</span>.
                </p>
              </div>
            </div>
          )}

          <OtpSignPanel detail={detail} role={role} onUpdated={loadDetail} />
        </div>
      </div>
    </div>
  );
}

function SummaryTile({
  icon: Icon,
  label,
  primary,
  secondary,
  highlight,
}: {
  icon: React.ElementType;
  label: string;
  primary: string;
  secondary: string;
  highlight?: boolean;
}) {
  return (
    <div className="p-3.5 rounded-xl bg-slate-50/80 border border-slate-200/70 space-y-1">
      <div className="flex items-center gap-1.5 text-[10px] font-bold text-slate-400 uppercase tracking-wide">
        <Icon className="w-3.5 h-3.5 text-slate-400" />
        <span>{label}</span>
      </div>
      <div className={cn("text-sm font-black truncate", highlight ? "text-blue-600" : "text-slate-900")}>
        {primary}
      </div>
      <div className="text-[11px] text-slate-500 truncate">{secondary}</div>
    </div>
  );
}

function PartyProgressItem({ label, signedAt }: { label: string; signedAt?: string | null }) {
  const signed = Boolean(signedAt);
  return (
    <div className="flex items-center gap-2">
      <div
        className={cn(
          "w-7 h-7 rounded-full flex items-center justify-center shrink-0",
          signed ? "bg-emerald-100 text-emerald-600" : "bg-blue-100 text-blue-600"
        )}
      >
        {signed ? <CheckCircle2 className="w-4 h-4" /> : <Clock className="w-4 h-4" />}
      </div>
      <div>
        <div className="font-bold text-slate-900">{label}</div>
        <div className={cn("text-[10px] font-semibold", signed ? "text-emerald-600" : "text-blue-600")}>
          {signed ? `Đã ký số · ${formatDateTime(signedAt)}` : "Đang chờ ký OTP"}
        </div>
      </div>
    </div>
  );
}
