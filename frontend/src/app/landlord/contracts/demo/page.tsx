"use client";

import React from "react";
import Link from "next/link";
import { useToast } from "@/components/Toast";
import { ContractA4Document } from "@/features/leasing/ContractA4Document";
import { OtpSignPanel } from "@/features/leasing/OtpSignPanel";
import { ContractSnapshotData } from "@/features/leasing/contract-snapshot";
import { formatDateTime, formatVnd } from "@/features/leasing/leasing-helpers";
import { LeasingContractDetailDto } from "@/types";
import {
  ArrowLeft,
  Building2,
  Calendar,
  CheckCircle2,
  Clock,
  Cloud,
  FileCheck2,
  Sparkles,
  User,
  Wallet,
} from "lucide-react";

const DEMO_SNAPSHOT: ContractSnapshotData = {
  roomName: "Phòng 302 - Tầng 3",
  propertyName: "Tòa HomeMatch Green",
  propertyAddress: "Số 15 Duy Tân, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
  areaSqm: 28,
  floor: 3,
  landlord: {
    fullName: "Trần Văn Minh",
    phone: "0988.765.432",
    cccd: "001085019822",
    email: "landlord@homematch.vn",
    address: "Số 15 Duy Tân, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
    bankAccount: "190366889999 - Techcombank (Trần Văn Minh)",
  },
  tenant: {
    fullName: "Nguyễn Văn An",
    phone: "0912.345.678",
    cccd: "036201000182",
    email: "khachthue.an@gmail.com",
    address: "Huyện Nam Trực, Tỉnh Nam Định",
  },
  electricityPrice: "3.500 đ/kWh",
  waterPrice: "100.000 đ/người/tháng",
  serviceFee: "150.000 đ/phòng/tháng",
  terms: "Miễn phí vệ sinh hành lang chung và chỗ để 01 xe máy. Hoàn trả 100% cọc khi bàn giao đúng hiện trạng.",
};

const DEMO_DETAIL: LeasingContractDetailDto = {
  id: 89,
  contractCode: "HD-2026-089",
  roomId: 302,
  tenantId: 3,
  landlordId: 2,
  startDate: "2026-11-01",
  endDate: "2027-11-01",
  monthlyRent: 4500000,
  depositAmount: 4500000,
  depositStatus: "PENDING",
  billingDueDay: 5,
  contractFileUrl: "contracts/89/v1/HD-2026-089.pdf",
  documentSnapshot: JSON.stringify(DEMO_SNAPSHOT),
  documentVersion: 1,
  documentGeneratedAt: "2026-11-18T07:00:00Z",
  status: "PENDING_SIGNATURE",
  signatureExpiresAt: "2026-11-21T07:00:00Z",
  landlordSignedAt: "2026-11-18T07:10:00Z",
  tenantSignedAt: null,
  signedDocumentHash: "a3b6c41288f3c149efbf4c8960fb92427ae41e4649b934ca495991b7852b855",
  handoverDueAt: null,
  handoverConfirmedAt: null,
  handoverConfirmedBy: null,
  actualOccupancyEndDate: null,
  createdAt: "2026-11-18T07:00:00Z",
  updatedAt: "2026-11-18T07:10:00Z",
  totalDepositReceived: 0,
  totalDepositRefunded: 0,
  totalDepositDeducted: 0,
  currentDepositBalance: 0,
  depositTransactions: [],
};

export default function DemoEContractPage() {
  const { showToast } = useToast();

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6 w-full flex-1 space-y-5 print:max-w-none print:p-0 print:m-0 print:space-y-0">
      <div className="flex flex-wrap items-center justify-between gap-3 print:hidden">
        <Link
          href="/landlord/contracts"
          className="inline-flex items-center gap-1.5 text-xs font-bold text-slate-500 hover:text-blue-600"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Quay lại Quản lý Hợp đồng</span>
        </Link>

        <div className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-blue-50 border border-blue-200 text-blue-700 text-[11px] font-bold">
          <Sparkles className="w-3.5 h-3.5" />
          <span>Bản xem trước giao diện mẫu · Soạn hợp đồng thật để ký OTP</span>
        </div>
      </div>

      {/* Header Card */}
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
                  #{DEMO_DETAIL.contractCode}
                </span>
              </div>
              <p className="text-[11px] text-slate-500 mt-0.5">
                Xác lập pháp lý theo Nghị định số 52/2013/NĐ-CP & Luật Giao dịch Điện tử 2023
              </p>
            </div>
          </div>

          <span className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-full text-xs font-bold border bg-blue-50 text-blue-700 border-blue-200 self-start sm:self-center">
            <span className="w-2 h-2 rounded-full bg-blue-600" />
            PENDING_SIGNATURE (Chờ ký số)
          </span>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
          <div className="p-3.5 rounded-xl bg-slate-50/80 border border-slate-200/70">
            <div className="flex items-center gap-1.5 text-[10px] font-bold text-slate-400 uppercase">
              <Building2 className="w-3.5 h-3.5" /> Phòng thuê & Địa chỉ
            </div>
            <div className="text-sm font-black text-slate-900 mt-1">{DEMO_SNAPSHOT.roomName}</div>
            <div className="text-[11px] text-slate-500 truncate">{DEMO_SNAPSHOT.propertyAddress}</div>
          </div>

          <div className="p-3.5 rounded-xl bg-slate-50/80 border border-slate-200/70">
            <div className="flex items-center gap-1.5 text-[10px] font-bold text-slate-400 uppercase">
              <User className="w-3.5 h-3.5" /> Đại diện Bên A (Chủ trọ)
            </div>
            <div className="text-sm font-black text-slate-900 mt-1">{DEMO_SNAPSHOT.landlord?.fullName}</div>
            <div className="text-[11px] text-blue-600 font-semibold">{DEMO_SNAPSHOT.landlord?.phone} · Đã KYC</div>
          </div>

          <div className="p-3.5 rounded-xl bg-slate-50/80 border border-slate-200/70">
            <div className="flex items-center gap-1.5 text-[10px] font-bold text-slate-400 uppercase">
              <Wallet className="w-3.5 h-3.5" /> Giá thuê & Tiền cọc
            </div>
            <div className="text-sm font-black text-blue-600 mt-1">{formatVnd(DEMO_DETAIL.monthlyRent)} /tháng</div>
            <div className="text-[11px] text-slate-500">Cọc 1 tháng · Đóng ngày 05 hàng tháng</div>
          </div>

          <div className="p-3.5 rounded-xl bg-slate-50/80 border border-slate-200/70">
            <div className="flex items-center gap-1.5 text-[10px] font-bold text-slate-400 uppercase">
              <Calendar className="w-3.5 h-3.5" /> Thời hạn hợp đồng
            </div>
            <div className="text-sm font-black text-slate-900 mt-1">12 Tháng</div>
            <div className="text-[11px] text-slate-500">01/11/2026 – 01/11/2027</div>
          </div>
        </div>

        <div className="pt-3 border-t border-slate-100 flex flex-col md:flex-row md:items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-4 text-xs">
            <div className="flex items-center gap-2">
              <div className="w-7 h-7 rounded-full bg-emerald-100 text-emerald-600 flex items-center justify-center">
                <CheckCircle2 className="w-4 h-4" />
              </div>
              <div>
                <div className="font-bold text-slate-900">Chủ nhà (Bên A)</div>
                <div className="text-[10px] font-semibold text-emerald-600">
                  Đã ký số · {formatDateTime(DEMO_DETAIL.landlordSignedAt)}
                </div>
              </div>
            </div>

            <span className="text-slate-300 hidden sm:inline">───</span>

            <div className="flex items-center gap-2">
              <div className="w-7 h-7 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center">
                <Clock className="w-4 h-4" />
              </div>
              <div>
                <div className="font-bold text-slate-900">Khách thuê (Bên B - Bạn)</div>
                <div className="text-[10px] font-semibold text-blue-600">Đang chờ bạn ký</div>
              </div>
            </div>
          </div>

          <div className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-slate-100 text-slate-600 text-[11px] font-semibold">
            <Cloud className="w-3.5 h-3.5 text-blue-600" />
            <span>Cloudflare R2 Presigned Document Safe</span>
          </div>
        </div>
      </div>

      {/* Main 2-column E-Contract Preview */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start print:block">
        <div className="lg:col-span-8 print:w-full">
          <ContractA4Document
            detail={DEMO_DETAIL}
            snapshot={DEMO_SNAPSHOT}
            busyDoc={false}
            onDownloadOriginal={() => showToast("Bản mẫu: trên hợp đồng thật sẽ mở file PDF từ Cloudflare R2!", "info")}
            onCopyHash={(hash) => {
              navigator.clipboard?.writeText(hash);
              showToast("Đã sao chép mã SHA-256 mẫu!", "info");
            }}
          />
        </div>

        <div className="lg:col-span-4 print:hidden">
          <OtpSignPanel
            detail={DEMO_DETAIL}
            role="TENANT"
            onUpdated={() => showToast("Đây là bản xem trước giao diện mẫu.", "info")}
          />
        </div>
      </div>
    </div>
  );
}
