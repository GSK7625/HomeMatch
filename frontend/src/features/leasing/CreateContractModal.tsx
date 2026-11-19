"use client";

import React, { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { Modal } from "@/components/Modal";
import { useAuth } from "@/features/auth/AuthContext";
import { useToast } from "@/components/Toast";
import { leasingApi, LeasingTenantOption } from "@/lib/leasing-api";
import { serializeContractSnapshot } from "@/features/leasing/contract-snapshot";
import { formatVnd } from "@/features/leasing/leasing-helpers";
import { LeasingContractDto, PropertyDto, RoomDto } from "@/types";
import {
  Building2,
  Calendar,
  CheckCircle2,
  FileText,
  FileUp,
  Info,
  Loader2,
  ShieldCheck,
  Sparkles,
  User,
  X,
} from "lucide-react";

export interface RoomOption {
  room: RoomDto;
  property: PropertyDto;
}

interface CreateContractModalProps {
  isOpen: boolean;
  onClose: () => void;
  roomOptions: RoomOption[];
  onCreated: (contract: LeasingContractDto) => void;
}

function getDefaultDates() {
  const today = new Date();
  const end = new Date(today);
  end.setFullYear(end.getFullYear() + 1);
  return {
    start: today.toISOString().slice(0, 10),
    end: end.toISOString().slice(0, 10),
  };
}

export function CreateContractModal({
  isOpen,
  onClose,
  roomOptions,
  onCreated,
}: CreateContractModalProps) {
  const { user } = useAuth();
  const { showToast } = useToast();
  const defaults = useMemo(() => getDefaultDates(), []);

  const availableRooms = useMemo(
    () => roomOptions.filter((o) => o.room.status === "AVAILABLE"),
    [roomOptions]
  );

  const [roomId, setRoomId] = useState<string>("");
  const [landlordCccd, setLandlordCccd] = useState<string>("");
  const [landlordBank, setLandlordBank] = useState<string>("");
  const [tenants, setTenants] = useState<LeasingTenantOption[]>([]);
  const [tenantId, setTenantId] = useState<string>("");
  const [tenantName, setTenantName] = useState<string>("");
  const [tenantPhone, setTenantPhone] = useState<string>("");
  const [tenantCccd, setTenantCccd] = useState<string>("");
  const [startDate, setStartDate] = useState(defaults.start);
  const [endDate, setEndDate] = useState(defaults.end);
  const [monthlyRent, setMonthlyRent] = useState<number>(4500000);
  const [depositAmount, setDepositAmount] = useState<number>(4500000);
  const [billingDueDay, setBillingDueDay] = useState<number>(5);
  const [notes, setNotes] = useState(
    "Hợp đồng thuê 12 tháng. Đặt cọc 01 tháng tiền thuê. Thanh toán tiền nhà đúng hạn hàng tháng."
  );
  const [pdfFile, setPdfFile] = useState<File | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen) return;
    try {
      const saved = localStorage.getItem(`homematch_landlord_info_${user?.id || "default"}`);
      if (saved) {
        const parsed = JSON.parse(saved);
        if (parsed.cccd) setLandlordCccd(parsed.cccd);
        if (parsed.bank) setLandlordBank(parsed.bank);
      }
    } catch {
      // ignore storage errors
    }
  }, [isOpen, user?.id]);

  useEffect(() => {
    if (!isOpen) return;
    setErrorMsg(null);
    const first = availableRooms[0] ?? roomOptions[0];
    if (first && !roomId) {
      setRoomId(String(first.room.id));
      if (first.room.basePrice) {
        setMonthlyRent(first.room.basePrice);
        setDepositAmount(first.room.basePrice);
      }
    }

    leasingApi.getTenants().then((res) => {
      if (res.isSuccess && res.data) {
        setTenants(res.data);
        if (res.data.length > 0 && !tenantId) {
          const t = res.data[0];
          setTenantId(String(t.id));
          setTenantName(t.fullName.replace(/\s*\([^)]*\)\s*/g, "").trim());
          setTenantPhone(t.phoneNumber || "");
        }
      }
    });
  }, [isOpen, availableRooms, roomOptions, roomId, tenantId]);

  const handleTenantChange = (val: string) => {
    setTenantId(val);
    const found = tenants.find((t) => String(t.id) === val);
    if (found) {
      setTenantName(found.fullName.replace(/\s*\([^)]*\)\s*/g, "").trim());
      setTenantPhone(found.phoneNumber || "");
    }
  };

  const selectedRoom = roomOptions.find((o) => String(o.room.id) === roomId);

  const leaseMonths = useMemo(() => {
    const s = new Date(startDate);
    const e = new Date(endDate);
    if (Number.isNaN(s.getTime()) || Number.isNaN(e.getTime()) || e <= s) return 0;
    return Math.max(1, Math.round((e.getTime() - s.getTime()) / (1000 * 60 * 60 * 24 * 30)));
  }, [startDate, endDate]);

  const handleRoomChange = (val: string) => {
    setRoomId(val);
    const found = roomOptions.find((o) => String(o.room.id) === val);
    if (found?.room.basePrice) {
      setMonthlyRent(found.room.basePrice);
      setDepositAmount(found.room.basePrice);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (file.type !== "application/pdf" && !file.name.toLowerCase().endsWith(".pdf")) {
      showToast("Vui lòng chỉ tải lên file định dạng PDF (.pdf).", "error");
      return;
    }
    setPdfFile(file);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);

    const parsedRoomId = Number(roomId);
    const parsedTenantId = Number(tenantId);
    if (!parsedRoomId || parsedRoomId <= 0) {
      setErrorMsg("Vui lòng chọn hoặc nhập mã phòng hợp lệ.");
      return;
    }
    if (!parsedTenantId || parsedTenantId <= 0) {
      setErrorMsg("Vui lòng nhập mã tài khoản người thuê (Tenant ID > 0).");
      return;
    }
    if (startDate >= endDate) {
      setErrorMsg("Ngày kết thúc phải sau ngày bắt đầu thuê.");
      return;
    }
    if (landlordCccd.trim() && !/^\d{12}$/.test(landlordCccd.trim())) {
      setErrorMsg("Số CCCD của Chủ nhà phải gồm đúng 12 chữ số.");
      return;
    }
    if (tenantCccd.trim() && !/^\d{12}$/.test(tenantCccd.trim())) {
      setErrorMsg("Số CCCD của Khách thuê phải gồm đúng 12 chữ số.");
      return;
    }

    // Lưu nhớ CCCD & STK chủ nhà cho các lần soạn hợp đồng sau
    try {
      localStorage.setItem(
        `homematch_landlord_info_${user?.id || "default"}`,
        JSON.stringify({ cccd: landlordCccd.trim(), bank: landlordBank.trim() })
      );
    } catch {
      // ignore storage errors
    }

    const snapshotJson = serializeContractSnapshot({
      roomName: selectedRoom ? `Phòng ${selectedRoom.room.roomNumber}` : undefined,
      propertyName: selectedRoom?.property.name,
      areaSqm: selectedRoom?.room.areaSqm,
      floor: selectedRoom?.room.floor,
      maxOccupants: selectedRoom?.room.maxOccupants,
      landlord: {
        fullName: (user?.fullName || user?.displayName || "").replace(/\s*\([^)]*\)\s*/g, "").trim() || undefined,
        phone: user?.phoneNumber || undefined,
        email: user?.email || undefined,
        cccd: landlordCccd.trim() || undefined,
        bankAccount: landlordBank.trim() || undefined,
      },
      tenant: {
        fullName: tenantName.trim() || undefined,
        phone: tenantPhone.trim() || undefined,
        cccd: tenantCccd.trim() || undefined,
      },
      terms: notes.trim() || undefined,
    });

    setSubmitting(true);
    try {
      const signatureExpiresAt = new Date(Date.now() + 72 * 60 * 60 * 1000).toISOString();
      const res = await leasingApi.createContract(
        {
          roomId: parsedRoomId,
          tenantId: parsedTenantId,
          startDate,
          endDate,
          monthlyRent: Number(monthlyRent),
          depositAmount: Number(depositAmount),
          billingDueDay: Number(billingDueDay),
          documentSnapshot: snapshotJson,
          signatureExpiresAt: pdfFile ? signatureExpiresAt : undefined,
        },
        pdfFile
      );

      if (!res.isSuccess || !res.data) {
        const detail = res.error?.details?.[0]?.message;
        setErrorMsg(detail || res.error?.message || "Không thể tạo hợp đồng. Vui lòng kiểm tra lại.");
        return;
      }

      showToast(
        pdfFile
          ? `Đã tạo hợp đồng ${res.data.contractCode} và chuyển sang Chờ ký OTP!`
          : `Đã lưu bản nháp hợp đồng ${res.data.contractCode}!`,
        "success"
      );
      setPdfFile(null);
      onCreated(res.data);
      onClose();
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Soạn Hợp Đồng Thuê Nhà Điện Tử"
      description="Tự động điền vào bản thảo pháp lý A4 · Hỗ trợ lưu nháp hoặc đính kèm PDF gửi ký OTP ngay"
      maxWidth="2xl"
    >
      <form onSubmit={handleSubmit} className="grid grid-cols-1 lg:grid-cols-5 gap-6">
        {/* Left: Form Inputs */}
        <div className="lg:col-span-3 space-y-4">
          {errorMsg && (
            <div className="p-3 rounded-xl bg-rose-50 border border-rose-200 text-rose-700 text-xs font-medium flex items-start gap-2">
              <Info className="w-4 h-4 shrink-0 mt-0.5" />
              <span>{errorMsg}</span>
            </div>
          )}

          {/* Room Selector */}
          <div>
            <div className="flex items-center justify-between mb-1.5">
              <label className="text-[11px] font-bold text-slate-700 uppercase tracking-wide flex items-center gap-1.5">
                <Building2 className="w-3.5 h-3.5 text-blue-600" />
                <span>Phòng cho thuê</span>
              </label>
              <Link href="/landlord/properties" className="text-[11px] font-semibold text-blue-600 hover:underline">
                + Quản lý phòng
              </Link>
            </div>

            {roomOptions.length > 0 ? (
              <select
                value={roomId}
                onChange={(e) => handleRoomChange(e.target.value)}
                required
                className="w-full px-3.5 py-2.5 text-xs rounded-xl border border-slate-200 bg-white font-medium text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
              >
                <option value="">-- Chọn phòng cho thuê --</option>
                {roomOptions.map(({ room, property }) => (
                  <option key={room.id} value={room.id}>
                    [{room.status === "AVAILABLE" ? "Trống" : room.status}] {property.name} · Phòng {room.roomNumber} (#
                    {room.id})
                    {room.basePrice ? ` — ${formatVnd(room.basePrice)}` : ""}
                  </option>
                ))}
              </select>
            ) : (
              <input
                type="number"
                min={1}
                required
                placeholder="Nhập mã phòng (Room ID)..."
                value={roomId}
                onChange={(e) => setRoomId(e.target.value)}
                className="w-full px-3.5 py-2.5 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
              />
            )}
          </div>

          {/* Landlord (Bên A) Legal Info */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 p-3 rounded-xl bg-slate-50 border border-slate-200/80">
            <div>
              <label className="block text-[11px] font-bold text-slate-700 uppercase tracking-wide mb-1">
                CCCD Chủ nhà (Bên A · 12 số)
              </label>
              <input
                type="text"
                inputMode="numeric"
                maxLength={12}
                value={landlordCccd}
                onChange={(e) => setLandlordCccd(e.target.value.replace(/\D/g, "").slice(0, 12))}
                placeholder="001085019822"
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white font-mono"
              />
            </div>
            <div>
              <label className="block text-[11px] font-bold text-slate-700 uppercase tracking-wide mb-1">
                Tài khoản nhận tiền thuê
              </label>
              <input
                type="text"
                value={landlordBank}
                onChange={(e) => setLandlordBank(e.target.value)}
                placeholder="190366889999 - Techcombank"
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white"
              />
            </div>
          </div>

          {/* Tenant Selector */}
          <div>
            <label className="block text-[11px] font-bold text-slate-700 uppercase tracking-wide mb-1.5">
              <span className="inline-flex items-center gap-1.5">
                <User className="w-3.5 h-3.5 text-blue-600" />
                Chọn khách thuê (Chỉ tài khoản Người thuê)
              </span>
            </label>
            {tenants.length > 0 ? (
              <select
                value={tenantId}
                onChange={(e) => handleTenantChange(e.target.value)}
                required
                className="w-full px-3.5 py-2.5 text-xs rounded-xl border border-slate-200 bg-white font-medium text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
              >
                <option value="">-- Chọn tài khoản khách thuê --</option>
                {tenants.map((t) => (
                  <option key={t.id} value={t.id}>
                    {(t.fullName || `Khách #${t.id}`).replace(/\s*\([^)]*\)\s*/g, "").trim()} — {t.email}
                    {t.phoneNumber ? ` (${t.phoneNumber})` : ""}
                  </option>
                ))}
              </select>
            ) : (
              <input
                type="number"
                min={1}
                required
                value={tenantId}
                onChange={(e) => setTenantId(e.target.value)}
                placeholder="Nhập mã tài khoản Người thuê (Tenant ID)..."
                className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200"
              />
            )}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div>
              <label className="block text-[11px] font-bold text-slate-700 uppercase tracking-wide mb-1.5">
                Họ tên khách thuê
              </label>
              <input
                type="text"
                value={tenantName}
                onChange={(e) => setTenantName(e.target.value)}
                placeholder="Nguyễn Văn An"
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            <div>
              <label className="block text-[11px] font-bold text-slate-700 uppercase tracking-wide mb-1.5">
                SĐT khách thuê
              </label>
              <input
                type="tel"
                value={tenantPhone}
                onChange={(e) => setTenantPhone(e.target.value)}
                placeholder="0912 345 678"
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            <div>
              <label className="block text-[11px] font-bold text-slate-700 uppercase tracking-wide mb-1.5">
                CCCD khách thuê (12 số)
              </label>
              <input
                type="text"
                inputMode="numeric"
                maxLength={12}
                value={tenantCccd}
                onChange={(e) => setTenantCccd(e.target.value.replace(/\D/g, "").slice(0, 12))}
                placeholder="036201000182"
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-mono"
              />
            </div>
          </div>

          {/* Rent, Deposit, Billing Day */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div>
              <label className="block text-[11px] font-bold text-slate-700 uppercase tracking-wide mb-1.5">
                Giá thuê / tháng
              </label>
              <input
                type="number"
                min={100000}
                step={100000}
                required
                value={monthlyRent}
                onChange={(e) => setMonthlyRent(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-semibold"
              />
            </div>

            <div>
              <label className="block text-[11px] font-bold text-slate-700 uppercase tracking-wide mb-1.5">
                Tiền đặt cọc
              </label>
              <input
                type="number"
                min={0}
                step={100000}
                required
                value={depositAmount}
                onChange={(e) => setDepositAmount(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-semibold"
              />
            </div>

            <div>
              <label className="block text-[11px] font-bold text-slate-700 uppercase tracking-wide mb-1.5">
                Ngày đóng tiền
              </label>
              <select
                value={billingDueDay}
                onChange={(e) => setBillingDueDay(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white"
              >
                {[1, 5, 10, 15, 20, 25, 28].map((day) => (
                  <option key={day} value={day}>
                    Ngày {day}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Dates */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="block text-[11px] font-bold text-slate-700 uppercase tracking-wide mb-1.5">
                Ngày bắt đầu thuê
              </label>
              <input
                type="date"
                required
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            <div>
              <label className="block text-[11px] font-bold text-slate-700 uppercase tracking-wide mb-1.5">
                Ngày kết thúc hợp đồng
              </label>
              <input
                type="date"
                required
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
          </div>

          {/* PDF Upload */}
          <div>
            <label className="block text-[11px] font-bold text-slate-700 uppercase tracking-wide mb-1.5">
              File PDF đính kèm (tùy chọn)
            </label>
            {pdfFile ? (
              <div className="flex items-center justify-between px-3.5 py-2 rounded-xl bg-blue-50 border border-blue-200 text-xs">
                <div className="flex items-center gap-2 min-w-0">
                  <FileText className="w-4 h-4 text-blue-600 shrink-0" />
                  <span className="font-semibold text-blue-900 truncate">{pdfFile.name}</span>
                </div>
                <button
                  type="button"
                  onClick={() => setPdfFile(null)}
                  className="p-1 text-blue-600 hover:bg-blue-100 rounded-lg"
                >
                  <X className="w-3.5 h-3.5" />
                </button>
              </div>
            ) : (
              <label className="flex items-center justify-center gap-2 px-4 py-3 rounded-xl border-2 border-dashed border-slate-200 hover:border-blue-400 hover:bg-blue-50/40 cursor-pointer transition-colors text-xs text-slate-600 font-semibold">
                <FileUp className="w-4 h-4 text-blue-500" />
                <span>Tải lên bản PDF gốc (tự sinh mã SHA-256)</span>
                <input type="file" accept=".pdf,application/pdf" onChange={handleFileChange} className="hidden" />
              </label>
            )}
          </div>

          {/* Terms */}
          <div>
            <label className="block text-[11px] font-bold text-slate-700 uppercase tracking-wide mb-1.5">
              Điều khoản bổ sung
            </label>
            <textarea
              rows={2}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200"
            />
          </div>
        </div>

        {/* Right: Live Summary Preview */}
        <div className="lg:col-span-2 flex flex-col justify-between bg-slate-50 rounded-2xl border border-slate-200/80 p-4 space-y-4">
          <div className="space-y-4">
            <div className="flex items-center gap-2 text-slate-900 font-bold text-xs">
              <Sparkles className="w-4 h-4 text-blue-600" />
              <span>Tóm tắt hợp đồng điện tử</span>
            </div>

            <div className="bg-white rounded-xl border border-slate-200/70 p-3.5 space-y-2.5 text-xs">
              <div className="flex justify-between gap-2">
                <span className="text-slate-500">Phòng</span>
                <span className="font-bold text-slate-900 text-right">
                  {selectedRoom
                    ? `${selectedRoom.property.name} · P.${selectedRoom.room.roomNumber}`
                    : roomId
                    ? `Room #${roomId}`
                    : "Chưa chọn"}
                </span>
              </div>
              <div className="flex justify-between gap-2">
                <span className="text-slate-500">Khách thuê</span>
                <span className="font-semibold text-slate-800">
                  {tenantName.trim() || `Tenant #${tenantId || "—"}`}
                </span>
              </div>
              <div className="flex justify-between gap-2">
                <span className="text-slate-500">Thời hạn</span>
                <span className="font-semibold text-slate-800 flex items-center gap-1">
                  <Calendar className="w-3 h-3 text-slate-400" />
                  {leaseMonths > 0 ? `~${leaseMonths} tháng` : "Không hợp lệ"}
                </span>
              </div>
              <div className="border-t border-slate-100 pt-2.5 flex justify-between gap-2">
                <span className="text-slate-500">Tiền thuê / tháng</span>
                <span className="font-black text-blue-600">{formatVnd(monthlyRent)}</span>
              </div>
              <div className="flex justify-between gap-2">
                <span className="text-slate-500">Tiền đặt cọc</span>
                <span className="font-bold text-slate-900">{formatVnd(depositAmount)}</span>
              </div>
              <div className="flex justify-between gap-2">
                <span className="text-slate-500">Hạn đóng hàng tháng</span>
                <span className="font-semibold text-slate-800">Ngày {billingDueDay}</span>
              </div>
            </div>

            <div className="p-3 rounded-xl bg-white border border-slate-200/70 space-y-1.5">
              <div className="flex items-center gap-1.5 text-[11px] font-bold text-slate-800">
                <ShieldCheck className="w-4 h-4 text-emerald-600" />
                <span>Sau khi tạo hợp đồng</span>
              </div>
              {pdfFile ? (
                <p className="text-[11px] text-emerald-700 leading-relaxed flex items-start gap-1.5">
                  <CheckCircle2 className="w-3.5 h-3.5 shrink-0 mt-0.5" />
                  <span>
                    Mở ngay <strong>Phòng Ký Hợp Đồng Điện Tử A4</strong> ở trạng thái <strong>Chờ ký OTP</strong>.
                  </span>
                </p>
              ) : (
                <p className="text-[11px] text-slate-500 leading-relaxed">
                  Lưu ở trạng thái <strong>Bản nháp (DRAFT)</strong> và mở thẳng bản thảo pháp lý A4 để soát & gửi ký.
                </p>
              )}
            </div>
          </div>

          <div className="flex gap-2.5 pt-2 border-t border-slate-200/70">
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="flex-1 py-2.5 rounded-xl border border-slate-200 bg-white text-xs font-bold text-slate-700 hover:bg-slate-100"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="flex-1 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white text-xs font-bold shadow-md shadow-blue-500/20 flex items-center justify-center gap-1.5"
            >
              {submitting && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
              <span>{pdfFile ? "Tạo & Ký OTP" : "Tạo & Xem HĐ"}</span>
            </button>
          </div>
        </div>
      </form>
    </Modal>
  );
}
