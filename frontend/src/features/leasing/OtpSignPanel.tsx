"use client";

import React, { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useToast } from "@/components/Toast";
import { leasingApi } from "@/lib/leasing-api";
import {
  formatDateTime,
  formatVnd,
  getDepositMeta,
  isDeletableContractStatus,
  isHandoverStatus,
  isRestorableContractStatus,
} from "@/features/leasing/leasing-helpers";
import { LeasingContractDetailDto } from "@/types";
import { SignaturePad } from "@/features/leasing/SignaturePad";
import { EditDraftModal } from "@/features/leasing/EditDraftModal";
import { cn } from "@/lib/utils";
import {
  ArrowDownLeft,
  ArrowUpRight,
  Ban,
  CheckCircle2,
  ClipboardCheck,
  Clock,
  Fingerprint,
  KeyRound,
  Loader2,
  Lock,
  Pencil,
  PlayCircle,
  RotateCcw,
  Send,
  ShieldCheck,
  Trash2,
  Wallet,
  XCircle,
} from "lucide-react";

interface OtpSignPanelProps {
  detail: LeasingContractDetailDto;
  role: "LANDLORD" | "TENANT";
  onUpdated: () => Promise<void> | void;
}

async function computeContractSha256(detail: LeasingContractDetailDto): Promise<string> {
  const payload = [
    detail.contractCode,
    `v${detail.documentVersion}`,
    detail.roomId,
    detail.landlordId,
    detail.tenantId,
    detail.startDate,
    detail.endDate,
    detail.monthlyRent,
    detail.depositAmount,
    detail.billingDueDay,
    detail.documentSnapshot || "",
  ].join("|");

  if (typeof window !== "undefined" && window.crypto?.subtle) {
    const data = new TextEncoder().encode(payload);
    const digest = await window.crypto.subtle.digest("SHA-256", data);
    return Array.from(new Uint8Array(digest))
      .map((b) => b.toString(16).padStart(2, "0"))
      .join("");
  }
  return `SNAPSHOT-V${detail.documentVersion}-${detail.contractCode}`;
}

export function OtpSignPanel({ detail, role, onUpdated }: OtpSignPanelProps) {
  const router = useRouter();
  const { showToast } = useToast();
  const isLandlord = role === "LANDLORD";
  const backHref = isLandlord ? "/landlord/contracts" : "/tenant/contracts";
  const mySignedAt = isLandlord ? detail.landlordSignedAt : detail.tenantSignedAt;

  // 6 individual OTP inputs + signature image
  const [digits, setDigits] = useState<string[]>(["", "", "", "", "", ""]);
  const inputRefs = useRef<Array<HTMLInputElement | null>>([]);
  const [otpCountdown, setOtpCountdown] = useState(0);
  const [otpEmailHint, setOtpEmailHint] = useState<string | null>(null);
  const [signatureData, setSignatureData] = useState<string | null>(null);
  const [agreed, setAgreed] = useState(false);

  // Reject / Cancel / Terminate / Handover states
  const [showReasonBox, setShowReasonBox] = useState<"reject" | "cancel" | "terminate" | null>(null);
  const [reason, setReason] = useState("");
  const [handoverDate, setHandoverDate] = useState(new Date().toISOString().slice(0, 10));

  // Deposit quick form
  const [showDepositForm, setShowDepositForm] = useState(false);
  const [depositType, setDepositType] = useState<"RECEIVED" | "DEDUCTED" | "REFUNDED">("RECEIVED");
  const [depositAmount, setDepositAmount] = useState(String(detail.depositAmount));
  const [depositReason, setDepositReason] = useState("");

  const [busy, setBusy] = useState<string | null>(null);
  const [isEditOpen, setIsEditOpen] = useState(false);

  useEffect(() => {
    setDepositAmount(String(detail.depositAmount));
  }, [detail.depositAmount]);

  useEffect(() => {
    if (otpCountdown <= 0) return;
    const t = setInterval(() => setOtpCountdown((s) => Math.max(0, s - 1)), 1000);
    return () => clearInterval(t);
  }, [otpCountdown]);

  const run = async (key: string, fn: () => Promise<void>) => {
    setBusy(key);
    try {
      await fn();
    } finally {
      setBusy(null);
    }
  };

  const handleDigitChange = (idx: number, raw: string) => {
    const clean = raw.replace(/\D/g, "");
    if (!clean) {
      const next = [...digits];
      next[idx] = "";
      setDigits(next);
      return;
    }
    // Support pasting full 6 digits
    if (clean.length > 1) {
      const next = ["", "", "", "", "", ""];
      clean
        .slice(0, 6)
        .split("")
        .forEach((ch, i) => {
          next[i] = ch;
        });
      setDigits(next);
      inputRefs.current[Math.min(5, clean.length - 1)]?.focus();
      return;
    }
    const next = [...digits];
    next[idx] = clean;
    setDigits(next);
    if (idx < 5) inputRefs.current[idx + 1]?.focus();
  };

  const handleDigitKeyDown = (idx: number, e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Backspace" && !digits[idx] && idx > 0) {
      inputRefs.current[idx - 1]?.focus();
    }
  };

  const handleRequestOtp = () =>
    run("otp", async () => {
      const res = await leasingApi.requestOtp(detail.id);
      if (res.isSuccess && res.data) {
        setOtpCountdown(300);
        setOtpEmailHint(res.data.maskedEmail);
        showToast(`Đã gửi mã OTP 6 số tới ${res.data.maskedEmail}`, "success");
        inputRefs.current[0]?.focus();
      } else {
        showToast(res.error?.message || "Không thể gửi mã OTP.", "error");
      }
    });

  const handleSign = (e: React.FormEvent) => {
    e.preventDefault();
    const otp = digits.join("");
    if (otp.length < 4) {
      showToast("Vui lòng nhập đầy đủ 6 chữ số mã OTP.", "error");
      return;
    }
    if (!signatureData) {
      showToast("Vui lòng ký tay hoặc tạo chữ ký tên điện tử trước khi xác nhận.", "error");
      return;
    }
    if (!agreed) {
      showToast("Vui lòng tích xác nhận đồng ý toàn văn hợp đồng.", "error");
      return;
    }
    run("sign", async () => {
      const hash = detail.signedDocumentHash || (await computeContractSha256(detail));
      const res = await leasingApi.signContract(detail.id, hash, otp, signatureData);
      if (res.isSuccess) {
        showToast("Ký hợp đồng điện tử & lưu chữ ký thành công!", "success");
        setDigits(["", "", "", "", "", ""]);
        setSignatureData(null);
        await onUpdated();
      } else {
        showToast(res.error?.message || "Mã OTP không đúng hoặc đã hết hạn.", "error");
      }
    });
  };

  const handleSendForSignature = () =>
    run("send", async () => {
      const expiresAt = new Date(Date.now() + 72 * 60 * 60 * 1000).toISOString();
      const res = await leasingApi.sendForSignature(detail.id, expiresAt);
      if (res.isSuccess) {
        showToast("Đã gửi hợp đồng sang trạng thái Chờ ký OTP!", "success");
        await onUpdated();
      } else {
        showToast(res.error?.message || "Không thể gửi ký hợp đồng.", "error");
      }
    });

  const handleActivate = () =>
    run("activate", async () => {
      const res = await leasingApi.activateContract(detail.id);
      if (res.isSuccess) {
        showToast("Đã kích hoạt hợp đồng thành công! Phòng đã chuyển sang Đang thuê.", "success");
        await onUpdated();
      } else {
        showToast(res.error?.message || "Không thể kích hoạt hợp đồng.", "error");
      }
    });

  const handleRestore = () =>
    run("restore", async () => {
      const res = await leasingApi.restoreContract(detail.id);
      if (res.isSuccess) {
        showToast(`Đã khôi phục hợp đồng ${detail.contractCode} về Bản nháp!`, "success");
        await onUpdated();
      } else {
        showToast(res.error?.message || "Không thể khôi phục hợp đồng (có thể bị trùng lịch phòng).", "error");
      }
    });

  const handleDelete = () => {
    if (!window.confirm(`Xóa vĩnh viễn hợp đồng ${detail.contractCode}? Thao tác này không thể hoàn tác.`)) {
      return;
    }
    run("delete", async () => {
      const res = await leasingApi.deleteContract(detail.id);
      if (res.isSuccess) {
        showToast(`Đã xóa vĩnh viễn hợp đồng ${detail.contractCode}!`, "success");
        router.push(backHref);
      } else {
        showToast(res.error?.message || "Không thể xóa hợp đồng này.", "error");
      }
    });
  };

  const handleConfirmHandover = () =>
    run("handover", async () => {
      const res = await leasingApi.confirmHandover(detail.id, handoverDate || undefined);
      if (res.isSuccess) {
        showToast("Đã xác nhận bàn giao phòng & hoàn tất hợp đồng!", "success");
        await onUpdated();
      } else {
        showToast(res.error?.message || "Không thể xác nhận bàn giao.", "error");
      }
    });

  const handleReasonSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!showReasonBox || !reason.trim()) {
      showToast("Vui lòng nhập lý do.", "error");
      return;
    }
    const action = showReasonBox;
    run(action, async () => {
      const res =
        action === "reject"
          ? await leasingApi.rejectContract(detail.id, reason.trim())
          : action === "cancel"
          ? await leasingApi.cancelContract(detail.id, reason.trim())
          : await leasingApi.terminateContract(detail.id, reason.trim());
      if (res.isSuccess) {
        setReason("");
        setShowReasonBox(null);
        if (action === "cancel" || action === "reject") {
          showToast(
            action === "cancel" ? "Đã hủy hợp đồng thành công!" : "Đã từ chối ký hợp đồng!",
            "success"
          );
          router.push(backHref);
          return;
        }
        showToast("Đã chấm dứt hợp đồng sớm. Vui lòng xác nhận bàn giao phòng.", "success");
        await onUpdated();
      } else {
        showToast(res.error?.message || "Thao tác thất bại.", "error");
      }
    });
  };

  const handleDepositSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const amount = Number(depositAmount);
    if (!amount || amount <= 0) {
      showToast("Số tiền phải lớn hơn 0.", "error");
      return;
    }
    if (depositType === "DEDUCTED" && !depositReason.trim()) {
      showToast("Khấu trừ cọc bắt buộc phải nhập lý do.", "error");
      return;
    }
    run("deposit", async () => {
      const res =
        depositType === "RECEIVED"
          ? await leasingApi.recordDepositReceived(detail.id, amount)
          : depositType === "DEDUCTED"
          ? await leasingApi.recordDepositDeduction(detail.id, amount, depositReason.trim())
          : await leasingApi.recordDepositRefund(detail.id, amount, depositReason.trim());
      if (res.isSuccess) {
        showToast("Đã ghi nhận giao dịch tiền cọc vào sổ cái!", "success");
        setDepositReason("");
        setShowDepositForm(false);
        await onUpdated();
      } else {
        showToast(res.error?.message || "Không thể ghi nhận giao dịch cọc.", "error");
      }
    });
  };

  const depositMeta = getDepositMeta(detail.depositStatus);
  const mm = String(Math.floor(otpCountdown / 60)).padStart(2, "0");
  const ss = String(otpCountdown % 60).padStart(2, "0");

  return (
    <div className="space-y-4 print:hidden">
      {/* CARD 1: OTP SIGNING / LIFECYCLE ACTION */}
      <div className="bg-white rounded-2xl border border-slate-200/90 shadow-xs p-5 space-y-4">
        <div className="flex items-center justify-between gap-2">
          <span
            className={cn(
              "px-2.5 py-1 rounded-full text-[10px] font-extrabold uppercase tracking-wider",
              detail.status === "CANCELLED" || detail.status === "REJECTED" || detail.status === "SIGNATURE_EXPIRED"
                ? "bg-rose-50 text-rose-700"
                : detail.status === "COMPLETED"
                ? "bg-teal-50 text-teal-700"
                : "bg-blue-50 text-blue-700"
            )}
          >
            {detail.status === "PENDING_SIGNATURE"
              ? "Bước cuối: Ký xác thực"
              : detail.status === "DRAFT"
              ? "Bản nháp hợp đồng"
              : detail.status === "SIGNED"
              ? "Đã đủ chữ ký 2 bên"
              : detail.status === "CANCELLED"
              ? "Hợp đồng đã hủy"
              : detail.status === "REJECTED"
              ? "Đã bị từ chối ký"
              : detail.status === "SIGNATURE_EXPIRED"
              ? "Đã quá hạn ký"
              : detail.status === "COMPLETED"
              ? "Đã hoàn tất"
              : "Quản lý hiệu lực"}
          </span>
          <Lock className="w-4 h-4 text-blue-600" />
        </div>

        {/* DRAFT -> Send for signature or Cancel */}
        {detail.status === "DRAFT" && isLandlord && (
          <div className="space-y-3">
            <h3 className="text-base font-black text-slate-900">Gửi Ký Hợp Đồng Điện Tử</h3>
            <p className="text-xs text-slate-500 leading-relaxed">
              Hợp đồng đang ở dạng nháp. Gửi ký để mở cổng xác thực OTP cho cả Chủ nhà và Người thuê trong 72 giờ.
            </p>
            <button
              type="button"
              onClick={handleSendForSignature}
              disabled={busy === "send"}
              className="w-full py-3 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20 flex items-center justify-center gap-2"
            >
              {busy === "send" ? <Loader2 className="w-4 h-4 animate-spin" /> : <Send className="w-4 h-4" />}
              <span>Gửi Ký Hợp Đồng (72h)</span>
            </button>
            <button
              type="button"
              onClick={() => setIsEditOpen(true)}
              className="w-full py-2.5 rounded-xl border border-blue-200 bg-blue-50/60 hover:bg-blue-100/70 text-blue-700 text-xs font-bold flex items-center justify-center gap-1.5 transition-colors"
            >
              <Pencil className="w-3.5 h-3.5" />
              <span>Chỉnh sửa thông tin bản nháp</span>
            </button>
            <button
              type="button"
              onClick={() => setShowReasonBox("cancel")}
              className="w-full py-2 text-[11px] font-bold text-rose-600 hover:bg-rose-50 rounded-xl flex items-center justify-center gap-1.5 transition-colors"
            >
              <XCircle className="w-3.5 h-3.5" />
              <span>Hủy bản nháp này</span>
            </button>
            <button
              type="button"
              onClick={handleDelete}
              disabled={busy === "delete"}
              className="w-full py-2 text-[11px] font-bold text-slate-500 hover:text-rose-700 hover:bg-rose-50 rounded-xl flex items-center justify-center gap-1.5 transition-colors"
            >
              {busy === "delete" ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Trash2 className="w-3.5 h-3.5" />}
              <span>Xóa vĩnh viễn bản nháp</span>
            </button>
          </div>
        )}

        {/* Terminal states: CANCELLED / REJECTED / SIGNATURE_EXPIRED / COMPLETED */}
        {["CANCELLED", "REJECTED", "SIGNATURE_EXPIRED", "COMPLETED"].includes(detail.status) && (
          <div className="space-y-3">
            <div
              className={cn(
                "p-3.5 rounded-xl border text-xs space-y-1",
                detail.status === "COMPLETED"
                  ? "bg-teal-50 border-teal-200 text-teal-900"
                  : "bg-rose-50 border-rose-200 text-rose-900"
              )}
            >
              <div className="font-bold flex items-center gap-1.5">
                {detail.status === "COMPLETED" ? (
                  <CheckCircle2 className="w-4 h-4 text-teal-600" />
                ) : (
                  <XCircle className="w-4 h-4 text-rose-600" />
                )}
                <span>
                  {detail.status === "CANCELLED"
                    ? "Hợp đồng này đã được hủy bỏ"
                    : detail.status === "REJECTED"
                    ? "Hợp đồng đã bị từ chối ký"
                    : detail.status === "SIGNATURE_EXPIRED"
                    ? "Hợp đồng đã hết thời hạn ký OTP"
                    : "Hợp đồng đã hoàn tất & bàn giao phòng"}
                </span>
              </div>
              <p className="text-[11px] opacity-85">
                Hợp đồng đã đóng và không thể thực hiện ký số hay thay đổi trạng thái nữa.
              </p>
            </div>

            <Link
              href={backHref}
              className="w-full py-2.5 rounded-xl bg-slate-900 hover:bg-slate-800 text-white text-xs font-bold flex items-center justify-center gap-1.5"
            >
              <span>← Quay lại danh sách hợp đồng</span>
            </Link>

            {isLandlord && isRestorableContractStatus(detail.status) && (
              <button
                type="button"
                onClick={handleRestore}
                disabled={busy === "restore"}
                className="w-full py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white text-xs font-bold shadow-sm flex items-center justify-center gap-1.5 transition-colors"
              >
                {busy === "restore" ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <RotateCcw className="w-3.5 h-3.5" />}
                <span>Khôi phục về Bản nháp</span>
              </button>
            )}

            {isLandlord && isDeletableContractStatus(detail.status) && (
              <button
                type="button"
                onClick={handleDelete}
                disabled={busy === "delete"}
                className="w-full py-2 rounded-xl border border-rose-200 text-rose-600 hover:bg-rose-50 text-xs font-bold flex items-center justify-center gap-1.5 transition-colors"
              >
                {busy === "delete" ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Trash2 className="w-3.5 h-3.5" />}
                <span>Xóa vĩnh viễn hợp đồng này</span>
              </button>
            )}
          </div>
        )}

        {/* PENDING_SIGNATURE -> 6-box OTP Signing */}
        {detail.status === "PENDING_SIGNATURE" && !mySignedAt && (
          <form onSubmit={handleSign} className="space-y-4">
            <div>
              <h3 className="text-base font-black text-slate-900">Xác Thực Ký Điện Tử Bằng OTP</h3>
              <p className="text-[11px] text-slate-500 mt-1 leading-relaxed">
                {otpEmailHint
                  ? `Mã OTP 6 chữ số đã được gửi tới ${otpEmailHint}. (Dev: kiểm tra log backend nếu chưa bật SMTP)`
                  : "Bấm gửi mã để nhận OTP 6 chữ số xác thực ký số qua Email đã đăng ký."}
              </p>
            </div>

            <div className="space-y-2">
              <div className="flex items-center justify-between text-[11px]">
                <span className="font-bold text-slate-700">Nhập 6 số xác thực:</span>
                <span className="inline-flex items-center gap-1 text-emerald-600 font-semibold">
                  <ShieldCheck className="w-3.5 h-3.5" />
                  Bảo mật 256-bit
                </span>
              </div>

              {/* 6 OTP Boxes */}
              <div className="grid grid-cols-6 gap-2">
                {digits.map((digit, i) => (
                  <input
                    key={i}
                    ref={(el) => {
                      inputRefs.current[i] = el;
                    }}
                    type="text"
                    inputMode="numeric"
                    maxLength={6}
                    value={digit}
                    onChange={(e) => handleDigitChange(i, e.target.value)}
                    onKeyDown={(e) => handleDigitKeyDown(i, e)}
                    placeholder="•"
                    className="h-12 text-center text-lg font-black font-mono rounded-xl border border-slate-300 bg-slate-50/70 focus:bg-white focus:border-blue-600 focus:ring-2 focus:ring-blue-500/20 focus:outline-none transition-all"
                  />
                ))}
              </div>

              <div className="flex items-center justify-between text-[11px] pt-0.5">
                <span className={cn("font-semibold flex items-center gap-1", otpCountdown > 0 ? "text-amber-600" : "text-slate-400")}>
                  <Clock className="w-3.5 h-3.5" />
                  {otpCountdown > 0 ? `Hết hạn sau: ${mm}:${ss}` : "Chưa gửi hoặc đã hết hạn"}
                </span>
                <button
                  type="button"
                  onClick={handleRequestOtp}
                  disabled={busy === "otp"}
                  className="font-bold text-blue-600 hover:text-blue-700 hover:underline inline-flex items-center gap-1"
                >
                  {busy === "otp" && <Loader2 className="w-3 h-3 animate-spin" />}
                  <span>{otpEmailHint ? "Gửi lại mã OTP" : "Gửi mã OTP"}</span>
                </button>
              </div>
            </div>

            <SignaturePad onChange={setSignatureData} />

            <label className="flex items-start gap-2.5 p-3 rounded-xl bg-slate-50 border border-slate-200/80 text-[11px] text-slate-700 leading-relaxed cursor-pointer">
              <input
                type="checkbox"
                checked={agreed}
                onChange={(e) => setAgreed(e.target.checked)}
                className="mt-0.5 rounded border-slate-300 text-blue-600"
              />
              <span>
                Tôi đã đọc kỹ toàn văn hợp đồng, hiểu rõ các điều khoản về giá, biểu phí điện nước và đồng ý ký kết hợp
                đồng điện tử có giá trị ràng buộc tương đương văn bản giấy.
              </span>
            </label>

            <button
              type="submit"
              disabled={busy === "sign"}
              className="w-full py-3 rounded-xl bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white text-xs font-bold shadow-md shadow-blue-500/25 flex items-center justify-center gap-2 transition-all"
            >
              {busy === "sign" ? <Loader2 className="w-4 h-4 animate-spin" /> : <Fingerprint className="w-4 h-4" />}
              <span>Ký Hợp Đồng Bằng OTP</span>
            </button>

            <button
              type="button"
              onClick={() => setShowReasonBox(isLandlord ? "cancel" : "reject")}
              className="w-full py-2 text-[11px] font-bold text-rose-600 hover:bg-rose-50 rounded-xl flex items-center justify-center gap-1.5 transition-colors"
            >
              <XCircle className="w-3.5 h-3.5" />
              <span>{isLandlord ? "Hủy hợp đồng này" : "Từ chối ký hợp đồng"}</span>
            </button>
          </form>
        )}

        {/* Already signed by current party, waiting for other */}
        {detail.status === "PENDING_SIGNATURE" && mySignedAt && (
          <div className="p-3.5 rounded-xl bg-emerald-50 border border-emerald-200 text-xs text-emerald-800 space-y-1">
            <div className="font-bold flex items-center gap-1.5">
              <CheckCircle2 className="w-4 h-4 text-emerald-600" />
              <span>Bạn đã hoàn tất ký số điện tử</span>
            </div>
            <p className="text-[11px]">
              Thời gian ký: {formatDateTime(mySignedAt)}. Hệ thống đang chờ bên còn lại xác nhận OTP.
            </p>
          </div>
        )}

        {/* SIGNED -> Activate */}
        {detail.status === "SIGNED" && (
          <div className="space-y-3">
            <div className="p-3 rounded-xl bg-indigo-50 border border-indigo-200 text-xs text-indigo-900">
              Cả hai bên đã ký OTP thành công. {isLandlord ? "Sau khi ghi nhận đủ tiền cọc, bấm kích hoạt để bắt đầu hiệu lực." : "Đang chờ chủ nhà xác nhận cọc & kích hoạt hợp đồng."}
            </div>
            {isLandlord && (
              <button
                type="button"
                onClick={handleActivate}
                disabled={busy === "activate"}
                className="w-full py-3 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold shadow-md shadow-emerald-500/20 flex items-center justify-center gap-2"
              >
                {busy === "activate" ? <Loader2 className="w-4 h-4 animate-spin" /> : <PlayCircle className="w-4 h-4" />}
                <span>Kích Hoạt Hợp Đồng (ACTIVE)</span>
              </button>
            )}
          </div>
        )}

        {/* ACTIVE -> Terminate early */}
        {detail.status === "ACTIVE" && (
          <div className="space-y-3">
            <div className="p-3 rounded-xl bg-emerald-50 border border-emerald-200 text-xs text-emerald-800 flex items-center gap-2">
              <CheckCircle2 className="w-4 h-4 shrink-0 text-emerald-600" />
              <span>Hợp đồng đang có hiệu lực pháp lý đầy đủ.</span>
            </div>
            {isLandlord && (
              <button
                type="button"
                onClick={() => setShowReasonBox("terminate")}
                className="w-full py-2.5 rounded-xl border border-rose-200 text-rose-600 hover:bg-rose-50 text-xs font-bold flex items-center justify-center gap-1.5"
              >
                <Ban className="w-3.5 h-3.5" />
                <span>Chấm dứt hợp đồng trước hạn</span>
              </button>
            )}
          </div>
        )}

        {/* HANDOVER -> Confirm Handover */}
        {isHandoverStatus(detail.status) && isLandlord && (
          <div className="space-y-3">
            <h3 className="text-sm font-black text-slate-900 flex items-center gap-1.5">
              <ClipboardCheck className="w-4 h-4 text-blue-600" />
              <span>Xác Nhận Bàn Giao Phòng</span>
            </h3>
            <input
              type="date"
              value={handoverDate}
              onChange={(e) => setHandoverDate(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
            />
            <button
              type="button"
              onClick={handleConfirmHandover}
              disabled={busy === "handover"}
              className="w-full py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold flex items-center justify-center gap-1.5"
            >
              {busy === "handover" && <Loader2 className="w-4 h-4 animate-spin" />}
              <span>Xác nhận trả phòng & Hoàn tất</span>
            </button>
          </div>
        )}

        {/* Inline Reason Box (Reject / Cancel / Terminate) */}
        {showReasonBox && (
          <form onSubmit={handleReasonSubmit} className="p-3.5 rounded-xl bg-rose-50 border border-rose-200 space-y-2.5">
            <div className="text-xs font-bold text-rose-800">
              {showReasonBox === "reject"
                ? "Lý do từ chối ký hợp đồng"
                : showReasonBox === "cancel"
                ? "Lý do hủy hợp đồng"
                : "Lý do chấm dứt trước hạn"}
            </div>
            <input
              type="text"
              required
              placeholder="Nhập lý do cụ thể..."
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-lg border border-rose-200 bg-white"
            />
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => setShowReasonBox(null)}
                className="flex-1 py-1.5 rounded-lg border border-slate-200 bg-white text-xs font-bold text-slate-600"
              >
                Đóng
              </button>
              <button
                type="submit"
                disabled={busy === showReasonBox}
                className="flex-1 py-1.5 rounded-lg bg-rose-600 hover:bg-rose-700 text-white text-xs font-bold"
              >
                Xác nhận
              </button>
            </div>
          </form>
        )}

        <div className="pt-3 border-t border-slate-100 flex items-start gap-2 text-[10px] text-slate-500">
          <KeyRound className="w-3.5 h-3.5 text-slate-400 shrink-0 mt-0.5" />
          <span>Chữ ký số lưu kèm dấu thời gian (Timestamp) và mã băm SHA-256 chống giả mạo trên HomeMatch.</span>
        </div>
      </div>

      {/* CARD 2: SAFE DEPOSIT LEDGER */}
      <div className="bg-white rounded-2xl border border-slate-200/90 shadow-xs p-5 space-y-3.5">
        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-2 text-xs font-black text-slate-900">
            <Wallet className="w-4 h-4 text-emerald-600" />
            <span>Sổ Cái Tiền Cọc An Toàn</span>
          </div>
          {isLandlord && (
            <button
              type="button"
              onClick={() => setShowDepositForm((v) => !v)}
              className="text-[11px] font-bold text-blue-600 hover:underline"
            >
              {showDepositForm ? "Đóng" : "+ Ghi giao dịch"}
            </button>
          )}
        </div>

        <div className="space-y-2 text-xs">
          <div className="flex justify-between">
            <span className="text-slate-500">Tiền cọc thỏa thuận:</span>
            <strong className="text-slate-900">{formatVnd(detail.depositAmount)}</strong>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-slate-500">Trạng thái quỹ cọc:</span>
            <span className={cn("px-2 py-0.5 rounded-full text-[10px] font-bold border", depositMeta.className)}>
              {depositMeta.label}
            </span>
          </div>
          <div className="flex justify-between">
            <span className="text-slate-500">Số dư đang giữ:</span>
            <strong className="text-emerald-600">{formatVnd(detail.currentDepositBalance)}</strong>
          </div>
        </div>

        {/* Quick Deposit Transaction Form (Landlord only) */}
        {isLandlord && showDepositForm && (
          <form onSubmit={handleDepositSubmit} className="p-3 rounded-xl bg-slate-50 border border-slate-200 space-y-2.5">
            <div className="grid grid-cols-3 gap-1">
              {[
                { key: "RECEIVED" as const, label: "+ Thu cọc" },
                { key: "DEDUCTED" as const, label: "- Khấu trừ" },
                { key: "REFUNDED" as const, label: "↩ Hoàn cọc" },
              ].map((t) => (
                <button
                  key={t.key}
                  type="button"
                  onClick={() => setDepositType(t.key)}
                  className={cn(
                    "py-1.5 rounded-lg text-[10px] font-bold border transition-colors",
                    depositType === t.key
                      ? "bg-slate-900 text-white border-slate-900"
                      : "bg-white text-slate-600 border-slate-200"
                  )}
                >
                  {t.label}
                </button>
              ))}
            </div>
            <input
              type="number"
              min={1000}
              step={50000}
              required
              value={depositAmount}
              onChange={(e) => setDepositAmount(e.target.value)}
              placeholder="Số tiền (VNĐ)"
              className="w-full px-3 py-2 text-xs rounded-lg border border-slate-200 bg-white font-semibold"
            />
            <input
              type="text"
              required={depositType === "DEDUCTED"}
              value={depositReason}
              onChange={(e) => setDepositReason(e.target.value)}
              placeholder={depositType === "DEDUCTED" ? "Lý do khấu trừ (bắt buộc)..." : "Ghi chú (tùy chọn)..."}
              className="w-full px-3 py-2 text-xs rounded-lg border border-slate-200 bg-white"
            />
            <button
              type="submit"
              disabled={busy === "deposit"}
              className="w-full py-2 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold"
            >
              {busy === "deposit" ? "Đang lưu..." : "Lưu giao dịch cọc"}
            </button>
          </form>
        )}

        {/* Recent Deposit Transactions */}
        {detail.depositTransactions.length > 0 && (
          <div className="pt-2 border-t border-slate-100 space-y-2 max-h-48 overflow-y-auto">
            {detail.depositTransactions.map((tx) => {
              const isIn = tx.transactionType === "RECEIVED";
              return (
                <div key={tx.id} className="flex items-center justify-between gap-2 text-[11px]">
                  <div className="flex items-center gap-1.5 min-w-0">
                    {isIn ? (
                      <ArrowDownLeft className="w-3.5 h-3.5 text-emerald-600 shrink-0" />
                    ) : (
                      <ArrowUpRight className="w-3.5 h-3.5 text-rose-600 shrink-0" />
                    )}
                    <span className="truncate text-slate-700 font-medium">
                      {tx.reason || (isIn ? "Thu tiền cọc" : tx.transactionType === "DEDUCTED" ? "Khấu trừ" : "Hoàn cọc")}
                    </span>
                  </div>
                  <span className={cn("font-bold shrink-0", isIn ? "text-emerald-600" : "text-rose-600")}>
                    {isIn ? "+" : "-"}
                    {formatVnd(tx.amount)}
                  </span>
                </div>
              );
            })}
          </div>
        )}

        <div className="p-2.5 rounded-xl bg-emerald-50/70 border border-emerald-100 text-[10px] text-emerald-800 leading-relaxed flex items-start gap-1.5">
          <CheckCircle2 className="w-3.5 h-3.5 shrink-0 mt-0.5 text-emerald-600" />
          <span>Mọi biến động tiền cọc được lưu vết bất biến và đối soát minh bạch giữa Chủ nhà và Người thuê.</span>
        </div>
      </div>

      {isLandlord && detail.status === "DRAFT" && (
        <EditDraftModal
          isOpen={isEditOpen}
          onClose={() => setIsEditOpen(false)}
          detail={detail}
          onSaved={onUpdated}
        />
      )}
    </div>
  );
}
