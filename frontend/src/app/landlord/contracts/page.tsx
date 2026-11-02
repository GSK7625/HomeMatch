"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { leasingService, listingService } from "@/lib/services";
import { LeaseContract, PropertyListing } from "@/types";
import { useAuth } from "@/features/auth/AuthContext";
import { useToast } from "@/components/Toast";
import { Modal } from "@/components/Modal";
import {
  FileText,
  Plus,
  AlertTriangle,
  CheckCircle2,
  Calendar,
  User,
  Phone,
  CreditCard,
  Building,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function LandlordContractsPage() {
  const { user } = useAuth();
  const { showToast } = useToast();
  const [contracts, setContracts] = useState<LeaseContract[]>([]);
  const [rooms, setRooms] = useState<PropertyListing[]>([]);
  const [isCreateOpen, setIsCreateOpen] = useState(false);

  // Form State
  const [tenantName, setTenantName] = useState("");
  const [tenantPhone, setTenantPhone] = useState("");
  const [tenantCccd, setTenantCccd] = useState("");
  const [selectedRoomId, setSelectedRoomId] = useState("");
  const [monthlyRent, setMonthlyRent] = useState(4500000);
  const [depositAmount, setDepositAmount] = useState(4500000);
  const [startDate, setStartDate] = useState("2026-11-01");
  const [endDate, setEndDate] = useState("2027-11-01");
  const [paymentCycle, setPaymentCycle] = useState(1);
  const [terms, setTerms] = useState(
    "Hợp đồng thuê 12 tháng. Đặt cọc 01 tháng. Thanh toán tiền thuê trước ngày 05 hàng tháng."
  );

  const loadData = () => {
    setContracts(leasingService.getAll());
    const rList = listingService.getAll();
    setRooms(rList);
    if (rList.length > 0 && !selectedRoomId) {
      setSelectedRoomId(rList[0].id);
      setMonthlyRent(rList[0].price);
      setDepositAmount(rList[0].deposit);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleRoomSelect = (id: string) => {
    setSelectedRoomId(id);
    const found = rooms.find((r) => r.id === id);
    if (found) {
      setMonthlyRent(found.price);
      setDepositAmount(found.deposit);
    }
  };

  const handleCreateContract = (e: React.FormEvent) => {
    e.preventDefault();
    const foundRoom = rooms.find((r) => r.id === selectedRoomId);
    const roomTitle = foundRoom ? foundRoom.title : "Phòng cho thuê";

    leasingService.create({
      contractNumber: `HD-2026-${Math.floor(100 + Math.random() * 900)}`,
      listingId: selectedRoomId,
      roomName: roomTitle,
      tenantId: `tenant-${Date.now()}`,
      tenantName,
      tenantPhone,
      tenantCccd,
      landlordId: user?.accountId || "demo-landlord-id",
      startDate,
      endDate,
      monthlyRent: Number(monthlyRent),
      depositAmount: Number(depositAmount),
      paymentCycle: Number(paymentCycle),
      terms,
    });

    setIsCreateOpen(false);
    loadData();
    showToast("Tạo và lưu hợp đồng thuê nhà thành công!", "success");
  };

  if (!user || user.role === "Tenant") {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <FileText className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Quản Lý Hợp Đồng Cho Thuê</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập với tài khoản <strong>Chủ nhà (Landlord)</strong> để tạo và quản lý hợp đồng thuê nhà với khách.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/landlord/contracts"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng nhập tài khoản Chủ nhà</span>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <FileText className="w-4 h-4" />
            <span>Nghiệp vụ hợp đồng & khách thuê (Module 3.7)</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Quản Lý Khách Thuê & Hợp Đồng Cho Thuê
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Soạn thảo hợp đồng gắn với phòng, theo dõi kỳ thuê, tiền cọc và cảnh báo hợp đồng sắp hết hạn trong 30 ngày.
          </p>
        </div>

        <button
          onClick={() => setIsCreateOpen(true)}
          className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20"
        >
          <Plus className="w-4 h-4" />
          <span>Soạn hợp đồng mới</span>
        </button>
      </div>

      {/* Contracts Table */}
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-x-auto">
        <table className="w-full text-xs text-left border-collapse min-w-[800px]">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50/70 text-slate-500 uppercase tracking-wider">
              <th className="p-4">Số HĐ & Tên Phòng</th>
              <th className="p-4">Khách thuê</th>
              <th className="p-4">Thời hạn thuê</th>
              <th className="p-4">Giá thuê / tháng</th>
              <th className="p-4">Tiền cọc giữ</th>
              <th className="p-4">Trạng thái hiệu lực</th>
            </tr>
          </thead>

          <tbody className="divide-y divide-slate-100">
            {contracts.map((ctr) => (
              <tr key={ctr.id} className="hover:bg-slate-50/60 transition-colors">
                <td className="p-4">
                  <div className="space-y-1">
                    <span className="font-mono text-[11px] font-bold bg-slate-100 px-2 py-0.5 rounded text-slate-800">
                      {ctr.contractNumber}
                    </span>
                    <h4 className="font-bold text-slate-900 text-xs line-clamp-1">{ctr.roomName}</h4>
                  </div>
                </td>

                <td className="p-4">
                  <div className="space-y-0.5">
                    <div className="font-bold text-slate-900 flex items-center gap-1.5">
                      <User className="w-3.5 h-3.5 text-slate-400" />
                      <span>{ctr.tenantName}</span>
                    </div>
                    <div className="text-slate-500 text-[11px] flex items-center gap-1.5">
                      <Phone className="w-3 h-3 text-slate-400" />
                      <span>{ctr.tenantPhone}</span>
                    </div>
                    {ctr.tenantCccd && (
                      <div className="text-slate-400 text-[10px]">CCCD: {ctr.tenantCccd}</div>
                    )}
                  </div>
                </td>

                <td className="p-4 text-slate-700">
                  <div className="font-medium">
                    {ctr.startDate} → {ctr.endDate}
                  </div>
                  <span className="text-[11px] text-slate-400">
                    Kỳ đóng: {ctr.paymentCycle} tháng/lần
                  </span>
                </td>

                <td className="p-4 font-black text-blue-600 text-sm">
                  {ctr.monthlyRent.toLocaleString()} đ
                </td>

                <td className="p-4 font-bold text-slate-800">
                  {ctr.depositAmount.toLocaleString()} đ
                </td>

                <td className="p-4">
                  <span
                    className={cn(
                      "px-3 py-1 rounded-full text-xs font-bold border inline-flex items-center gap-1",
                      ctr.status === "Còn hiệu lực"
                        ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                        : "bg-amber-50 text-amber-700 border-amber-200 animate-pulse"
                    )}
                  >
                    {ctr.status === "Sắp hết hạn" && <AlertTriangle className="w-3 h-3" />}
                    <span>{ctr.status}</span>
                    {ctr.daysRemaining !== undefined && (
                      <span className="text-[10px] opacity-80">({ctr.daysRemaining} ngày)</span>
                    )}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Create Contract Modal */}
      <Modal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        title="Soạn Hợp Đồng Thuê Nhà Điện Tử Mới"
        maxWidth="lg"
      >
        <form onSubmit={handleCreateContract} className="space-y-4">
          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
              Chọn phòng cho thuê
            </label>
            <select
              value={selectedRoomId}
              onChange={(e) => handleRoomSelect(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-semibold"
            >
              {rooms.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.title} ({r.district}, {r.city}) - {r.price.toLocaleString()} đ/tháng
                </option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Họ tên khách thuê
              </label>
              <input
                type="text"
                required
                placeholder="Nguyễn Văn A"
                value={tenantName}
                onChange={(e) => setTenantName(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Số điện thoại khách thuê
              </label>
              <input
                type="tel"
                required
                placeholder="0912 345 678"
                value={tenantPhone}
                onChange={(e) => setTenantPhone(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
              Số CCCD / CMND khách thuê
            </label>
            <input
              type="text"
              placeholder="001201008899"
              value={tenantCccd}
              onChange={(e) => setTenantCccd(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-mono"
            />
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Giá thuê (đ/tháng)
              </label>
              <input
                type="number"
                required
                value={monthlyRent}
                onChange={(e) => setMonthlyRent(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Tiền đặt cọc (đ)
              </label>
              <input
                type="number"
                required
                value={depositAmount}
                onChange={(e) => setDepositAmount(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Kỳ thanh toán (tháng)
              </label>
              <select
                value={paymentCycle}
                onChange={(e) => setPaymentCycle(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              >
                <option value={1}>1 tháng / lần</option>
                <option value={3}>3 tháng / lần</option>
                <option value={6}>6 tháng / lần</option>
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
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
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
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
            <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
              Điều khoản hợp đồng
            </label>
            <textarea
              rows={2}
              value={terms}
              onChange={(e) => setTerms(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
            />
          </div>

          <div className="flex gap-3 pt-3 border-t border-slate-100">
            <button
              type="button"
              onClick={() => setIsCreateOpen(false)}
              className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
            >
              Hủy
            </button>
            <button
              type="submit"
              className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 shadow-md shadow-blue-500/20"
            >
              Lưu & Ký hợp đồng
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
