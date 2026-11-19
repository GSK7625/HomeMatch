"use client";

import React, { useEffect, useState } from "react";
import { Modal } from "@/components/Modal";
import { useToast } from "@/components/Toast";
import { leasingApi } from "@/lib/leasing-api";
import {
  parseContractSnapshot,
  serializeContractSnapshot,
} from "@/features/leasing/contract-snapshot";
import { LeasingContractDetailDto } from "@/types";
import { Info, Loader2, Save } from "lucide-react";

interface EditDraftModalProps {
  isOpen: boolean;
  onClose: () => void;
  detail: LeasingContractDetailDto;
  onSaved: () => Promise<void> | void;
}

export function EditDraftModal({ isOpen, onClose, detail, onSaved }: EditDraftModalProps) {
  const { showToast } = useToast();

  const [startDate, setStartDate] = useState(detail.startDate);
  const [endDate, setEndDate] = useState(detail.endDate);
  const [monthlyRent, setMonthlyRent] = useState(detail.monthlyRent);
  const [depositAmount, setDepositAmount] = useState(detail.depositAmount);
  const [billingDueDay, setBillingDueDay] = useState(detail.billingDueDay);

  const [landlordCccd, setLandlordCccd] = useState("");
  const [landlordBank, setLandlordBank] = useState("");
  const [tenantName, setTenantName] = useState("");
  const [tenantPhone, setTenantPhone] = useState("");
  const [tenantCccd, setTenantCccd] = useState("");
  const [terms, setTerms] = useState("");

  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen) return;
    setErrorMsg(null);
    const snap = parseContractSnapshot(detail.documentSnapshot);
    setStartDate(detail.startDate);
    setEndDate(detail.endDate);
    setMonthlyRent(detail.monthlyRent);
    setDepositAmount(detail.depositAmount);
    setBillingDueDay(detail.billingDueDay);
    setLandlordCccd(snap.landlord?.cccd || "");
    setLandlordBank(snap.landlord?.bankAccount || "");
    setTenantName(snap.tenant?.fullName || "");
    setTenantPhone(snap.tenant?.phone || "");
    setTenantCccd(snap.tenant?.cccd || "");
    setTerms(snap.terms || "");
  }, [isOpen, detail]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);

    if (startDate >= endDate) {
      setErrorMsg("Ngày kết thúc phải sau ngày bắt đầu thuê.");
      return;
    }
    if (landlordCccd.trim() && !/^\d{12}$/.test(landlordCccd.trim())) {
      setErrorMsg("Số CCCD Chủ nhà phải gồm đúng 12 chữ số.");
      return;
    }
    if (tenantCccd.trim() && !/^\d{12}$/.test(tenantCccd.trim())) {
      setErrorMsg("Số CCCD Khách thuê phải gồm đúng 12 chữ số.");
      return;
    }

    const existing = parseContractSnapshot(detail.documentSnapshot);
    const updatedSnapshot = serializeContractSnapshot({
      ...existing,
      landlord: {
        ...existing.landlord,
        cccd: landlordCccd.trim() || undefined,
        bankAccount: landlordBank.trim() || undefined,
      },
      tenant: {
        ...existing.tenant,
        fullName: tenantName.trim() || existing.tenant?.fullName,
        phone: tenantPhone.trim() || existing.tenant?.phone,
        cccd: tenantCccd.trim() || undefined,
      },
      terms: terms.trim() || undefined,
    });

    setSubmitting(true);
    try {
      const res = await leasingApi.updateDraft(detail.id, {
        startDate,
        endDate,
        monthlyRent: Number(monthlyRent),
        depositAmount: Number(depositAmount),
        billingDueDay: Number(billingDueDay),
        documentSnapshot: updatedSnapshot,
      });

      if (!res.isSuccess) {
        const detailErr = res.error?.details?.[0]?.message;
        setErrorMsg(detailErr || res.error?.message || "Không thể cập nhật bản nháp.");
        return;
      }

      showToast(`Đã cập nhật bản nháp ${detail.contractCode}!`, "success");
      await onSaved();
      onClose();
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={`Chỉnh Sửa Bản Nháp #${detail.contractCode}`}
      description="Chỉ cho phép sửa khi hợp đồng ở trạng thái Nháp (chưa gửi ký OTP)"
      maxWidth="xl"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {errorMsg && (
          <div className="p-3 rounded-xl bg-rose-50 border border-rose-200 text-rose-700 text-xs font-medium flex items-start gap-2">
            <Info className="w-4 h-4 shrink-0 mt-0.5" />
            <span>{errorMsg}</span>
          </div>
        )}

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 p-3 rounded-xl bg-slate-50 border border-slate-200/80">
          <div>
            <label className="block text-[11px] font-bold text-slate-700 uppercase mb-1">
              CCCD Chủ nhà (12 số)
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
            <label className="block text-[11px] font-bold text-slate-700 uppercase mb-1">
              Tài khoản nhận tiền
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

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div>
            <label className="block text-[11px] font-bold text-slate-700 uppercase mb-1">
              Họ tên khách thuê
            </label>
            <input
              type="text"
              value={tenantName}
              onChange={(e) => setTenantName(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
            />
          </div>
          <div>
            <label className="block text-[11px] font-bold text-slate-700 uppercase mb-1">
              SĐT khách thuê
            </label>
            <input
              type="tel"
              value={tenantPhone}
              onChange={(e) => setTenantPhone(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
            />
          </div>
          <div>
            <label className="block text-[11px] font-bold text-slate-700 uppercase mb-1">
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

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div>
            <label className="block text-[11px] font-bold text-slate-700 uppercase mb-1">
              Giá thuê / tháng (VNĐ)
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
            <label className="block text-[11px] font-bold text-slate-700 uppercase mb-1">
              Tiền đặt cọc (VNĐ)
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
            <label className="block text-[11px] font-bold text-slate-700 uppercase mb-1">
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

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label className="block text-[11px] font-bold text-slate-700 uppercase mb-1">
              Ngày bắt đầu thuê
            </label>
            <input
              type="date"
              required
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
            />
          </div>
          <div>
            <label className="block text-[11px] font-bold text-slate-700 uppercase mb-1">
              Ngày kết thúc hợp đồng
            </label>
            <input
              type="date"
              required
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
            />
          </div>
        </div>

        <div>
          <label className="block text-[11px] font-bold text-slate-700 uppercase mb-1">
            Điều khoản bổ sung
          </label>
          <textarea
            rows={3}
            value={terms}
            onChange={(e) => setTerms(e.target.value)}
            className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
          />
        </div>

        <div className="flex gap-2.5 pt-2 border-t border-slate-100">
          <button
            type="button"
            onClick={onClose}
            disabled={submitting}
            className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
          >
            Hủy
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="flex-1 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white text-xs font-bold shadow-md shadow-blue-500/20 flex items-center justify-center gap-1.5"
          >
            {submitting ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Save className="w-3.5 h-3.5" />}
            <span>Lưu thay đổi bản nháp</span>
          </button>
        </div>
      </form>
    </Modal>
  );
}
