"use client";

import React, { useState, useEffect } from "react";
import { billingService, leasingService, listingService } from "@/lib/services";
import { MonthlyBill, LeaseContract, PropertyListing } from "@/types";
import { useToast } from "@/components/Toast";
import { Modal } from "@/components/Modal";
import {
  Zap,
  Droplet,
  Plus,
  Calculator,
  CheckCircle2,
  DollarSign,
  Calendar,
  AlertCircle,
  Settings,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function LandlordUtilitiesPage() {
  const { showToast } = useToast();
  const [bills, setBills] = useState<MonthlyBill[]>([]);
  const [contracts, setContracts] = useState<LeaseContract[]>([]);
  const [rooms, setRooms] = useState<PropertyListing[]>([]);

  const [isRecordModalOpen, setIsRecordModalOpen] = useState(false);
  const [isRateModalOpen, setIsRateModalOpen] = useState(false);

  // Meter Reading Form State
  const [selectedContractId, setSelectedContractId] = useState("");
  const [period, setPeriod] = useState("11/2026");
  const [elecStart, setElecStart] = useState(1200);
  const [elecEnd, setElecEnd] = useState(1345);
  const [waterStart, setWaterStart] = useState(85);
  const [waterEnd, setWaterEnd] = useState(92);
  const [elecUnitPrice, setElecUnitPrice] = useState(3800);
  const [waterUnitPrice, setWaterUnitPrice] = useState(28000);
  const [serviceFee, setServiceFee] = useState(150000);
  const [dueDate, setDueDate] = useState("2026-11-10");

  const loadData = () => {
    setBills(billingService.getAll());
    const cList = leasingService.getAll();
    setContracts(cList);
    const rList = listingService.getAll();
    setRooms(rList);
    if (cList.length > 0 && !selectedContractId) {
      setSelectedContractId(cList[0].id);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  // Calculated values
  const elecUsage = Math.max(0, elecEnd - elecStart);
  const waterUsage = Math.max(0, waterEnd - waterStart);
  const elecTotal = elecUsage * elecUnitPrice;
  const waterTotal = waterUsage * waterUnitPrice;

  const currentContract = contracts.find((c) => c.id === selectedContractId);
  const rentAmount = currentContract ? currentContract.monthlyRent : 4500000;
  const grandTotal = rentAmount + elecTotal + waterTotal + serviceFee;

  const handleRecordSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!currentContract) {
      showToast("Vui lòng chọn hợp đồng của phòng cần ghi số!", "error");
      return;
    }

    billingService.createFromReading({
      contractId: currentContract.id,
      roomName: currentContract.roomName,
      tenantName: currentContract.tenantName,
      period,
      rentAmount,
      electricityUsage: elecUsage,
      electricityUnitPrice: elecUnitPrice,
      waterUsage,
      waterUnitPrice,
      serviceCost: serviceFee,
      dueDate,
    });

    setIsRecordModalOpen(false);
    loadData();
    showToast(
      `Đã tính toán và phát hành hóa đơn kỳ ${period} thành công! (Tổng: ${grandTotal.toLocaleString()} đ)`,
      "success"
    );
  };

  const handlePayBillLandlord = (id: string, amount: number) => {
    billingService.payBill(id, amount);
    loadData();
    showToast("Đã xác nhận thu tiền từ khách thành công!", "success");
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-amber-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Zap className="w-4 h-4" />
            <span>Nghiệp vụ điện nước & hóa đơn (Module 3.8)</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Quản Lý Tiền Điện, Nước & Phát Hành Hóa Đơn
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Ghi chỉ số công tơ đầu/cuối kỳ, hệ thống tự động tính tiền điện nước và theo dõi công nợ thu.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => setIsRecordModalOpen(true)}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20"
          >
            <Calculator className="w-4 h-4" />
            <span>Ghi chỉ số & Tính tiền kỳ mới</span>
          </button>
        </div>
      </div>

      {/* Metric summaries */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center shrink-0">
            <Zap className="w-6 h-6" />
          </div>
          <div>
            <span className="text-[11px] font-bold text-slate-400 uppercase">Đơn giá điện chuẩn</span>
            <span className="text-xl font-black text-slate-900 block">3.800 đ/kWh</span>
            <span className="text-[10px] text-slate-500">Cấu hình theo công tơ</span>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-sky-50 text-sky-600 flex items-center justify-center shrink-0">
            <Droplet className="w-6 h-6" />
          </div>
          <div>
            <span className="text-[11px] font-bold text-slate-400 uppercase">Đơn giá nước chuẩn</span>
            <span className="text-xl font-black text-slate-900 block">28.000 đ/m³</span>
            <span className="text-[10px] text-slate-500">Đồng hồ nước độc lập</span>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center shrink-0">
            <DollarSign className="w-6 h-6" />
          </div>
          <div>
            <span className="text-[11px] font-bold text-slate-400 uppercase">Công nợ thu nợ</span>
            <span className="text-xl font-black text-emerald-600 block">Tự động đối chiếu</span>
            <span className="text-[10px] text-slate-500">Còn nợ = Phải thu − Đã thu</span>
          </div>
        </div>
      </div>

      {/* Bills Table */}
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-x-auto">
        <table className="w-full text-xs text-left border-collapse min-w-[850px]">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50/70 text-slate-500 uppercase tracking-wider">
              <th className="p-4">Kỳ & Tên phòng</th>
              <th className="p-4">Khách thuê</th>
              <th className="p-4">Tiền phòng</th>
              <th className="p-4">Tiền điện</th>
              <th className="p-4">Tiền nước</th>
              <th className="p-4">Tổng phải thu</th>
              <th className="p-4">Còn nợ</th>
              <th className="p-4">Trạng thái</th>
              <th className="p-4 text-right">Thao tác</th>
            </tr>
          </thead>

          <tbody className="divide-y divide-slate-100">
            {bills.map((b) => (
              <tr key={b.id} className="hover:bg-slate-50/60 transition-colors">
                <td className="p-4">
                  <span className="font-mono text-xs font-bold text-blue-600 block">
                    Kỳ {b.period}
                  </span>
                  <span className="font-bold text-slate-900 text-xs">{b.roomName}</span>
                </td>

                <td className="p-4 font-semibold text-slate-800">{b.tenantName}</td>

                <td className="p-4 font-medium text-slate-700">
                  {b.rentAmount.toLocaleString()} đ
                </td>

                <td className="p-4 text-amber-700 font-semibold">
                  {b.electricityCost.toLocaleString()} đ
                  <span className="block text-[10px] text-slate-400">({b.electricityUsage} kWh)</span>
                </td>

                <td className="p-4 text-sky-700 font-semibold">
                  {b.waterCost.toLocaleString()} đ
                  <span className="block text-[10px] text-slate-400">({b.waterUsage} m³)</span>
                </td>

                <td className="p-4 font-black text-slate-900 text-sm">
                  {b.totalDue.toLocaleString()} đ
                </td>

                <td className="p-4 font-black text-rose-600 text-sm">
                  {b.remainingDebt.toLocaleString()} đ
                </td>

                <td className="p-4">
                  <span
                    className={cn(
                      "px-2.5 py-1 rounded-full text-xs font-bold border",
                      b.status === "Đã thanh toán"
                        ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                        : "bg-rose-50 text-rose-700 border-rose-200"
                    )}
                  >
                    {b.status}
                  </span>
                </td>

                <td className="p-4 text-right">
                  {b.remainingDebt > 0 && (
                    <button
                      onClick={() => handlePayBillLandlord(b.id, b.remainingDebt)}
                      className="px-3 py-1 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs shadow-xs transition-colors"
                    >
                      Xác nhận đã thu
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Meter Reading & Bill Calculation Modal */}
      <Modal
        isOpen={isRecordModalOpen}
        onClose={() => setIsRecordModalOpen(false)}
        title="Ghi Chỉ Số Điện Nước & Tự Động Tính Tiền"
        description="Nhập chỉ số công tơ đầu kỳ và cuối kỳ. Hệ thống sẽ áp dụng đơn giá để tính tiền tự động theo công thức nghiệp vụ."
        maxWidth="lg"
      >
        <form onSubmit={handleRecordSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Chọn hợp đồng / phòng
              </label>
              <select
                value={selectedContractId}
                onChange={(e) => setSelectedContractId(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-semibold"
              >
                {contracts.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.roomName} — {c.tenantName}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Kỳ tính hóa đơn
              </label>
              <input
                type="text"
                required
                placeholder="11/2026"
                value={period}
                onChange={(e) => setPeriod(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-bold"
              />
            </div>
          </div>

          {/* Electricity */}
          <div className="p-3.5 rounded-xl bg-amber-50/60 border border-amber-200 space-y-3">
            <div className="flex items-center justify-between text-xs">
              <span className="font-bold text-amber-900 flex items-center gap-1.5">
                <Zap className="w-4 h-4 text-amber-600" />
                <span>Chỉ số điện (kWh)</span>
              </span>
              <span className="text-[11px] font-semibold text-amber-800">
                Đơn giá: {elecUnitPrice.toLocaleString()} đ/kWh
              </span>
            </div>

            <div className="grid grid-cols-3 gap-2">
              <div>
                <label className="block text-[10px] font-bold text-slate-500 uppercase mb-1">
                  Số đầu kỳ
                </label>
                <input
                  type="number"
                  value={elecStart}
                  onChange={(e) => setElecStart(Number(e.target.value))}
                  className="w-full px-2.5 py-1.5 text-xs rounded-lg border border-slate-200 font-bold"
                />
              </div>
              <div>
                <label className="block text-[10px] font-bold text-slate-500 uppercase mb-1">
                  Số cuối kỳ
                </label>
                <input
                  type="number"
                  value={elecEnd}
                  onChange={(e) => setElecEnd(Number(e.target.value))}
                  className="w-full px-2.5 py-1.5 text-xs rounded-lg border border-slate-200 font-bold"
                />
              </div>
              <div>
                <label className="block text-[10px] font-bold text-slate-500 uppercase mb-1">
                  Thành tiền điện
                </label>
                <div className="px-2.5 py-1.5 text-xs font-black text-amber-950 bg-white rounded-lg border border-amber-200">
                  {elecTotal.toLocaleString()} đ ({elecUsage} kWh)
                </div>
              </div>
            </div>
          </div>

          {/* Water */}
          <div className="p-3.5 rounded-xl bg-sky-50/60 border border-sky-200 space-y-3">
            <div className="flex items-center justify-between text-xs">
              <span className="font-bold text-sky-900 flex items-center gap-1.5">
                <Droplet className="w-4 h-4 text-sky-600" />
                <span>Chỉ số nước (m³)</span>
              </span>
              <span className="text-[11px] font-semibold text-sky-800">
                Đơn giá: {waterUnitPrice.toLocaleString()} đ/m³
              </span>
            </div>

            <div className="grid grid-cols-3 gap-2">
              <div>
                <label className="block text-[10px] font-bold text-slate-500 uppercase mb-1">
                  Số đầu kỳ
                </label>
                <input
                  type="number"
                  value={waterStart}
                  onChange={(e) => setWaterStart(Number(e.target.value))}
                  className="w-full px-2.5 py-1.5 text-xs rounded-lg border border-slate-200 font-bold"
                />
              </div>
              <div>
                <label className="block text-[10px] font-bold text-slate-500 uppercase mb-1">
                  Số cuối kỳ
                </label>
                <input
                  type="number"
                  value={waterEnd}
                  onChange={(e) => setWaterEnd(Number(e.target.value))}
                  className="w-full px-2.5 py-1.5 text-xs rounded-lg border border-slate-200 font-bold"
                />
              </div>
              <div>
                <label className="block text-[10px] font-bold text-slate-500 uppercase mb-1">
                  Thành tiền nước
                </label>
                <div className="px-2.5 py-1.5 text-xs font-black text-sky-950 bg-white rounded-lg border border-sky-200">
                  {waterTotal.toLocaleString()} đ ({waterUsage} m³)
                </div>
              </div>
            </div>
          </div>

          {/* Grand summary & Due date */}
          <div className="p-4 rounded-xl bg-slate-900 text-white flex items-center justify-between text-xs">
            <div>
              <span className="text-slate-400 block">Tổng tiền hóa đơn tháng:</span>
              <span className="text-xl font-black text-white">{grandTotal.toLocaleString()} VNĐ</span>
            </div>

            <div>
              <label className="block text-[10px] text-slate-400 uppercase font-bold mb-1">
                Hạn đóng tiền
              </label>
              <input
                type="date"
                value={dueDate}
                onChange={(e) => setDueDate(e.target.value)}
                className="text-xs bg-slate-800 text-white px-2.5 py-1 rounded-lg border border-slate-700"
              />
            </div>
          </div>

          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={() => setIsRecordModalOpen(false)}
              className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
            >
              Hủy
            </button>
            <button
              type="submit"
              className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 shadow-md shadow-blue-500/20"
            >
              Phát hành hóa đơn
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
