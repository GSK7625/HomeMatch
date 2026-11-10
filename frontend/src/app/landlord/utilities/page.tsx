"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import { billingService, leasingService, listingService } from "@/lib/services";
import { propertiesApi, getStoredToken } from "@/lib/api-client";
import { MonthlyBill, LeaseContract, PropertyListing, PropertyDto, TariffDto } from "@/types";
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
  Building2,
  Wifi,
  ShieldCheck,
  RefreshCw,
  Sparkles,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function LandlordUtilitiesPage() {
  const { user, role } = useAuth();
  const { showToast } = useToast();
  const [bills, setBills] = useState<MonthlyBill[]>([]);
  const [contracts, setContracts] = useState<LeaseContract[]>([]);
  const [rooms, setRooms] = useState<PropertyListing[]>([]);

  // Backend properties & tariffs
  const [properties, setProperties] = useState<PropertyDto[]>([]);
  const [selectedPropId, setSelectedPropId] = useState<number | null>(null);
  const [tariffs, setTariffs] = useState<TariffDto[]>([]);
  const [loadingTariffs, setLoadingTariffs] = useState(false);

  const [isRecordModalOpen, setIsRecordModalOpen] = useState(false);
  const [isTariffModalOpen, setIsTariffModalOpen] = useState(false);

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

  // Tariff Edit Form State
  const [formElecPrice, setFormElecPrice] = useState(3800);
  const [formElecMethod, setFormElecMethod] = useState("PER_KWH");
  const [formWaterPrice, setFormWaterPrice] = useState(28000);
  const [formWaterMethod, setFormWaterMethod] = useState("PER_M3");
  const [formInternetPrice, setFormInternetPrice] = useState(100000);
  const [formInternetMethod, setFormInternetMethod] = useState("FIXED_PER_MONTH");
  const [formServicePrice, setFormServicePrice] = useState(150000);
  const [formServiceMethod, setFormServiceMethod] = useState("FIXED_PER_MONTH");
  const [formCleaningPrice, setFormCleaningPrice] = useState(50000);
  const [formCleaningMethod, setFormCleaningMethod] = useState("FIXED_PER_MONTH");
  const [formParkingPrice, setFormParkingPrice] = useState(100000);
  const [formParkingMethod, setFormParkingMethod] = useState("FIXED_PER_MONTH");
  const [savingTariffs, setSavingTariffs] = useState(false);

  const CALC_METHOD_LABELS: Record<string, string> = {
    PER_KWH: "Theo số điện (kWh)",
    PER_M3: "Theo khối nước (m³)",
    PER_PERSON: "Theo đầu người / tháng",
    FIXED_PER_MONTH: "Cố định / phòng / tháng",
    FREE: "Miễn phí (0đ)",
  };

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

  // Load properties
  const loadProperties = useCallback(async () => {
    const token = getStoredToken();
    if (!token) return;
    try {
      const res = await propertiesApi.getMyProperties();
      if (res.isSuccess && res.data && res.data.length > 0) {
        setProperties(res.data);
        setSelectedPropId((prev) => (prev ? prev : res.data![0].id));
      }
    } catch (err) {
      console.warn("Could not load properties for tariffs", err);
    }
  }, []);

  // Load tariffs for selected property
  const loadTariffs = useCallback(async (propId: number) => {
    setLoadingTariffs(true);
    try {
      const res = await propertiesApi.getTariffs(propId);
      if (res.isSuccess && res.data && res.data.length > 0) {
        setTariffs(res.data);
        // Map to state
        const e = res.data.find((t) => t.tariffType === "ELECTRICITY");
        const w = res.data.find((t) => t.tariffType === "WATER");
        const net = res.data.find((t) => t.tariffType === "INTERNET");
        const s = res.data.find((t) => t.tariffType === "SERVICE");
        const c = res.data.find((t) => t.tariffType === "CLEANING");
        const p = res.data.find((t) => t.tariffType === "PARKING");

        if (e) {
          setElecUnitPrice(e.unitPrice);
          setFormElecPrice(e.unitPrice);
          setFormElecMethod(e.calcMethod || "PER_KWH");
        }
        if (w) {
          setWaterUnitPrice(w.unitPrice);
          setFormWaterPrice(w.unitPrice);
          setFormWaterMethod(w.calcMethod || "PER_M3");
        }
        if (net) {
          setFormInternetPrice(net.unitPrice);
          setFormInternetMethod(net.calcMethod || "FIXED_PER_MONTH");
        }
        if (s) {
          setServiceFee(s.unitPrice);
          setFormServicePrice(s.unitPrice);
          setFormServiceMethod(s.calcMethod || "FIXED_PER_MONTH");
        }
        if (c) {
          setFormCleaningPrice(c.unitPrice);
          setFormCleaningMethod(c.calcMethod || "FIXED_PER_MONTH");
        }
        if (p) {
          setFormParkingPrice(p.unitPrice);
          setFormParkingMethod(p.calcMethod || "FIXED_PER_MONTH");
        }
      }
    } catch (err) {
      console.warn("Could not load tariffs", err);
    } finally {
      setLoadingTariffs(false);
    }
  }, []);

  useEffect(() => {
    loadData();
    loadProperties();
  }, [loadProperties]);

  useEffect(() => {
    if (selectedPropId) {
      loadTariffs(selectedPropId);
    }
  }, [selectedPropId, loadTariffs]);

  // Handle Save Tariffs to Backend
  const handleSaveTariffs = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPropId) {
      showToast("Vui lòng chọn bất động sản cần cài đặt biểu phí!", "error");
      return;
    }
    setSavingTariffs(true);
    try {
      const payload = {
        tariffs: [
          {
            tariffType: "ELECTRICITY",
            calcMethod: formElecMethod,
            unitPrice: formElecMethod === "FREE" ? 0 : Number(formElecPrice),
          },
          {
            tariffType: "WATER",
            calcMethod: formWaterMethod,
            unitPrice: formWaterMethod === "FREE" ? 0 : Number(formWaterPrice),
          },
          {
            tariffType: "INTERNET",
            calcMethod: formInternetMethod,
            unitPrice: formInternetMethod === "FREE" ? 0 : Number(formInternetPrice),
          },
          {
            tariffType: "SERVICE",
            calcMethod: formServiceMethod,
            unitPrice: formServiceMethod === "FREE" ? 0 : Number(formServicePrice),
          },
          {
            tariffType: "CLEANING",
            calcMethod: formCleaningMethod,
            unitPrice: formCleaningMethod === "FREE" ? 0 : Number(formCleaningPrice),
          },
          {
            tariffType: "PARKING",
            calcMethod: formParkingMethod,
            unitPrice: formParkingMethod === "FREE" ? 0 : Number(formParkingPrice),
          },
        ],
      };
      const res = await propertiesApi.setTariffs(selectedPropId, payload);
      if (res.isSuccess) {
        showToast("Đã cập nhật biểu phí dịch vụ của tòa nhà thành công!", "success");
        setIsTariffModalOpen(false);
        loadTariffs(selectedPropId);
      } else {
        showToast(res.error?.message || "Cập nhật biểu phí thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    } finally {
      setSavingTariffs(false);
    }
  };

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

  if (!user || role === "Tenant") {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-amber-50 text-amber-600 flex items-center justify-center mx-auto shadow-xs">
          <Zap className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Quản Lý Tiền Điện Nước</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập với tài khoản <strong>Chủ nhà (Landlord)</strong> để ghi chỉ số điện nước và phát hành hóa đơn phòng.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/landlord/utilities"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng nhập tài khoản Chủ nhà</span>
          </Link>
        </div>
      </div>
    );
  }

  const selectedProperty = properties.find((p) => p.id === selectedPropId);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-amber-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Zap className="w-4 h-4" />
            <span>Nghiệp vụ điện nước & biểu phí dịch vụ (Module Properties & Billing)</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Quản Lý Tiền Điện, Nước & Biểu Phí Dịch Vụ
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Cài đặt biểu giá định mức cho từng tòa nhà trên backend và ghi chỉ số công tơ phát hành hóa đơn.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {selectedPropId && (
            <button
              onClick={() => setIsTariffModalOpen(true)}
              className="flex items-center gap-2 px-3.5 py-2.5 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-800 text-xs font-bold transition-colors"
            >
              <Settings className="w-4 h-4 text-slate-600" />
              <span>Cài Đặt Biểu Phí Định Mức</span>
            </button>
          )}

          <button
            onClick={() => setIsRecordModalOpen(true)}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20 transition-all"
          >
            <Calculator className="w-4 h-4" />
            <span>Ghi chỉ số & Tính tiền kỳ mới</span>
          </button>
        </div>
      </div>

      {/* Property Selector if Landlord has backend properties */}
      {properties.length > 0 && (
        <div className="bg-white rounded-2xl border border-slate-200/80 p-4 shadow-xs flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center shrink-0">
              <Building2 className="w-5 h-5" />
            </div>
            <div>
              <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wider block">
                Áp dụng cho tòa nhà:
              </span>
              <select
                value={selectedPropId || ""}
                onChange={(e) => setSelectedPropId(Number(e.target.value))}
                className="font-bold text-slate-900 text-sm bg-transparent border-none p-0 focus:ring-0 cursor-pointer"
              >
                {properties.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name} ({p.addressDetail})
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => selectedPropId && loadTariffs(selectedPropId)}
              className="text-xs font-bold text-slate-500 hover:text-blue-600 flex items-center gap-1"
            >
              <RefreshCw className={cn("w-3.5 h-3.5", loadingTariffs && "animate-spin")} />
              <span>Làm mới biểu phí</span>
            </button>
          </div>
        </div>
      )}

      {/* Metric summaries */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center shrink-0">
            <Zap className="w-6 h-6" />
          </div>
          <div>
            <span className="text-[11px] font-bold text-slate-400 uppercase">Đơn giá điện chuẩn</span>
            <span className="text-xl font-black text-slate-900 block">
              {elecUnitPrice.toLocaleString()} đ/kWh
            </span>
            <span className="text-[10px] text-slate-500">
              {selectedProperty ? `Thuộc: ${selectedProperty.name}` : "Mặc định hệ thống"}
            </span>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-sky-50 text-sky-600 flex items-center justify-center shrink-0">
            <Droplet className="w-6 h-6" />
          </div>
          <div>
            <span className="text-[11px] font-bold text-slate-400 uppercase">Đơn giá nước chuẩn</span>
            <span className="text-xl font-black text-slate-900 block">
              {waterUnitPrice.toLocaleString()} đ/m³
            </span>
            <span className="text-[10px] text-slate-500">Đồng hồ nước độc lập</span>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center shrink-0">
            <DollarSign className="w-6 h-6" />
          </div>
          <div>
            <span className="text-[11px] font-bold text-slate-400 uppercase">Phí dịch vụ chung</span>
            <span className="text-xl font-black text-emerald-600 block">
              {serviceFee.toLocaleString()} đ/tháng
            </span>
            <span className="text-[10px] text-slate-500">Vệ sinh, thang máy, an ninh</span>
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
              <th className="p-4 text-right">Hành động</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {bills.map((b) => (
              <tr key={b.id} className="hover:bg-slate-50/60 transition-colors">
                <td className="p-4">
                  <div className="font-bold text-slate-900">{b.roomName}</div>
                  <span className="text-[11px] text-slate-400">Kỳ {b.period}</span>
                </td>
                <td className="p-4 font-medium text-slate-700">{b.tenantName}</td>
                <td className="p-4 font-bold text-slate-700">{b.rentAmount.toLocaleString()} đ</td>
                <td className="p-4 text-slate-700">
                  {b.electricityCost.toLocaleString()} đ
                  <span className="block text-[10px] text-slate-400">({b.electricityUsage} kWh)</span>
                </td>
                <td className="p-4 text-slate-700">
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

      {/* Modal: Edit Tariffs on Backend */}
      {isTariffModalOpen && (
        <Modal
          isOpen={true}
          onClose={() => setIsTariffModalOpen(false)}
          title={`Cài Đặt Biểu Phí Định Mức: ${selectedProperty?.name}`}
          description="Đơn giá sẽ được lưu vào cơ sở dữ liệu và tự động áp dụng khi tạo tin đăng hoặc lập hóa đơn tiền phòng."
          maxWidth="lg"
        >
          <form onSubmit={handleSaveTariffs} className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="p-3 rounded-xl border border-slate-200 bg-slate-50/50 space-y-2">
                <label className="block text-xs font-bold text-slate-700 uppercase flex items-center gap-1.5">
                  <Zap className="w-3.5 h-3.5 text-amber-500" />
                  <span>Tiền Điện (ELECTRICITY)</span>
                </label>
                <select
                  value={formElecMethod}
                  onChange={(e) => {
                    setFormElecMethod(e.target.value);
                    if (e.target.value === "FREE") setFormElecPrice(0);
                  }}
                  className="w-full px-2.5 py-1.5 text-xs rounded-lg border border-slate-200 bg-white"
                >
                  {Object.entries(CALC_METHOD_LABELS).map(([k, v]) => (
                    <option key={k} value={k}>
                      {v}
                    </option>
                  ))}
                </select>
                <input
                  type="number"
                  min={0}
                  required
                  disabled={formElecMethod === "FREE"}
                  value={formElecMethod === "FREE" ? 0 : formElecPrice}
                  onChange={(e) => setFormElecPrice(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-bold bg-white disabled:bg-slate-100"
                />
              </div>

              <div className="p-3 rounded-xl border border-slate-200 bg-slate-50/50 space-y-2">
                <label className="block text-xs font-bold text-slate-700 uppercase flex items-center gap-1.5">
                  <Droplet className="w-3.5 h-3.5 text-sky-500" />
                  <span>Tiền Nước (WATER)</span>
                </label>
                <select
                  value={formWaterMethod}
                  onChange={(e) => {
                    setFormWaterMethod(e.target.value);
                    if (e.target.value === "FREE") setFormWaterPrice(0);
                  }}
                  className="w-full px-2.5 py-1.5 text-xs rounded-lg border border-slate-200 bg-white"
                >
                  {Object.entries(CALC_METHOD_LABELS).map(([k, v]) => (
                    <option key={k} value={k}>
                      {v}
                    </option>
                  ))}
                </select>
                <input
                  type="number"
                  min={0}
                  required
                  disabled={formWaterMethod === "FREE"}
                  value={formWaterMethod === "FREE" ? 0 : formWaterPrice}
                  onChange={(e) => setFormWaterPrice(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-bold bg-white disabled:bg-slate-100"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="p-3 rounded-xl border border-slate-200 bg-slate-50/50 space-y-2">
                <label className="block text-xs font-bold text-slate-700 uppercase flex items-center gap-1.5">
                  <Wifi className="w-3.5 h-3.5 text-indigo-500" />
                  <span>Mạng Internet (INTERNET)</span>
                </label>
                <select
                  value={formInternetMethod}
                  onChange={(e) => {
                    setFormInternetMethod(e.target.value);
                    if (e.target.value === "FREE") setFormInternetPrice(0);
                  }}
                  className="w-full px-2.5 py-1.5 text-xs rounded-lg border border-slate-200 bg-white"
                >
                  {Object.entries(CALC_METHOD_LABELS).map(([k, v]) => (
                    <option key={k} value={k}>
                      {v}
                    </option>
                  ))}
                </select>
                <input
                  type="number"
                  min={0}
                  required
                  disabled={formInternetMethod === "FREE"}
                  value={formInternetMethod === "FREE" ? 0 : formInternetPrice}
                  onChange={(e) => setFormInternetPrice(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-bold bg-white disabled:bg-slate-100"
                />
              </div>

              <div className="p-3 rounded-xl border border-slate-200 bg-slate-50/50 space-y-2">
                <label className="block text-xs font-bold text-slate-700 uppercase flex items-center gap-1.5">
                  <ShieldCheck className="w-3.5 h-3.5 text-emerald-500" />
                  <span>Dịch Vụ Chung (SERVICE)</span>
                </label>
                <select
                  value={formServiceMethod}
                  onChange={(e) => {
                    setFormServiceMethod(e.target.value);
                    if (e.target.value === "FREE") setFormServicePrice(0);
                  }}
                  className="w-full px-2.5 py-1.5 text-xs rounded-lg border border-slate-200 bg-white"
                >
                  {Object.entries(CALC_METHOD_LABELS).map(([k, v]) => (
                    <option key={k} value={k}>
                      {v}
                    </option>
                  ))}
                </select>
                <input
                  type="number"
                  min={0}
                  required
                  disabled={formServiceMethod === "FREE"}
                  value={formServiceMethod === "FREE" ? 0 : formServicePrice}
                  onChange={(e) => setFormServicePrice(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-bold bg-white disabled:bg-slate-100"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="p-3 rounded-xl border border-slate-200 bg-slate-50/50 space-y-2">
                <label className="block text-xs font-bold text-slate-700 uppercase">
                  Phí Vệ Sinh (CLEANING)
                </label>
                <select
                  value={formCleaningMethod}
                  onChange={(e) => {
                    setFormCleaningMethod(e.target.value);
                    if (e.target.value === "FREE") setFormCleaningPrice(0);
                  }}
                  className="w-full px-2.5 py-1.5 text-xs rounded-lg border border-slate-200 bg-white"
                >
                  {Object.entries(CALC_METHOD_LABELS).map(([k, v]) => (
                    <option key={k} value={k}>
                      {v}
                    </option>
                  ))}
                </select>
                <input
                  type="number"
                  min={0}
                  required
                  disabled={formCleaningMethod === "FREE"}
                  value={formCleaningMethod === "FREE" ? 0 : formCleaningPrice}
                  onChange={(e) => setFormCleaningPrice(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-bold bg-white disabled:bg-slate-100"
                />
              </div>

              <div className="p-3 rounded-xl border border-slate-200 bg-slate-50/50 space-y-2">
                <label className="block text-xs font-bold text-slate-700 uppercase">
                  Phí Gửi Xe (PARKING)
                </label>
                <select
                  value={formParkingMethod}
                  onChange={(e) => {
                    setFormParkingMethod(e.target.value);
                    if (e.target.value === "FREE") setFormParkingPrice(0);
                  }}
                  className="w-full px-2.5 py-1.5 text-xs rounded-lg border border-slate-200 bg-white"
                >
                  {Object.entries(CALC_METHOD_LABELS).map(([k, v]) => (
                    <option key={k} value={k}>
                      {v}
                    </option>
                  ))}
                </select>
                <input
                  type="number"
                  min={0}
                  required
                  disabled={formParkingMethod === "FREE"}
                  value={formParkingMethod === "FREE" ? 0 : formParkingPrice}
                  onChange={(e) => setFormParkingPrice(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-bold bg-white disabled:bg-slate-100"
                />
              </div>
            </div>

            <div className="flex gap-3 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setIsTariffModalOpen(false)}
                className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={savingTariffs}
                className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 shadow-md shadow-blue-500/20 disabled:opacity-50"
              >
                {savingTariffs ? "Đang lưu..." : "Lưu Biểu Phí Lên Hệ Thống"}
              </button>
            </div>
          </form>
        </Modal>
      )}

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
