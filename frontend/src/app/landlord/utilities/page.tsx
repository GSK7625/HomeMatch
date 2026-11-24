"use client";

import React, { useState, useEffect, useCallback, useMemo } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import { propertiesApi, getStoredToken } from "@/lib/api-client";
import { leasingApi } from "@/lib/leasing-api";
import { billingApi } from "@/lib/billing-api";
import {
  BillingDebtSummaryDto,
  BillingInvoiceDto,
  BillingUtilityReadingDto,
  LeasingContractDto,
  PropertyDto,
  RoomDto,
  TariffDto,
} from "@/types";
import {
  buildContractDisplayMap,
  formatDate,
  formatVnd,
  getInvoiceStatusMeta,
  getRemainingDebt,
  INVOICE_STATUS_FILTER_OPTIONS,
} from "@/features/billing/billing-helpers";
import { InvoiceDetailModal } from "@/features/billing/InvoiceDetailModal";
import { useToast } from "@/components/Toast";
import { Modal } from "@/components/Modal";
import {
  Zap,
  Droplet,
  Calculator,
  DollarSign,
  Settings,
  Building2,
  Wifi,
  ShieldCheck,
  RefreshCw,
  Receipt,
  CheckCircle2,
  AlertCircle,
  Wallet,
  Eye,
  XCircle,
  FilePlus2,
  Loader2,
  DoorOpen,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function LandlordUtilitiesPage() {
  const { user, role } = useAuth();
  const { showToast } = useToast();

  const now = new Date();
  const [selectedMonth, setSelectedMonth] = useState<number>(now.getMonth() + 1);
  const [selectedYear, setSelectedYear] = useState<number>(now.getFullYear());
  const [invoiceStatusFilter, setInvoiceStatusFilter] = useState<string>("ALL");

  // Backend properties, rooms & tariffs
  const [properties, setProperties] = useState<PropertyDto[]>([]);
  const [selectedPropId, setSelectedPropId] = useState<number | null>(null);
  const [rooms, setRooms] = useState<RoomDto[]>([]);
  const [selectedRoomScope, setSelectedRoomScope] = useState<number | null>(null); // null = Toàn bộ tòa nhà, number = Phòng cụ thể
  const [tariffs, setTariffs] = useState<TariffDto[]>([]);
  const [loadingTariffs, setLoadingTariffs] = useState(false);

  // Contracts, Readings, Invoices & Debts
  const [contracts, setContracts] = useState<LeasingContractDto[]>([]);
  const [readings, setReadings] = useState<BillingUtilityReadingDto[]>([]);
  const [invoices, setInvoices] = useState<BillingInvoiceDto[]>([]);
  const [debtSummary, setDebtSummary] = useState<BillingDebtSummaryDto | null>(null);
  const [loadingData, setLoadingData] = useState(false);

  // Modals
  const [isTariffModalOpen, setIsTariffModalOpen] = useState(false);
  const [readingModalContract, setReadingModalContract] = useState<LeasingContractDto | null>(null);
  const [detailInvoiceId, setDetailInvoiceId] = useState<number | null>(null);
  const [cancellingInvoice, setCancellingInvoice] = useState<BillingInvoiceDto | null>(null);
  const [cancelReason, setCancelReason] = useState("");
  const [submittingCancel, setSubmittingCancel] = useState(false);
  const [creatingInvoiceContractId, setCreatingInvoiceContractId] = useState<number | null>(null);

  // Tariff Summary State
  const [elecUnitPrice, setElecUnitPrice] = useState(3800);
  const [waterUnitPrice, setWaterUnitPrice] = useState(28000);
  const [serviceFee, setServiceFee] = useState(150000);

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

  // Meter Reading Form State
  const [elecOldInput, setElecOldInput] = useState<number>(0);
  const [elecNewInput, setElecNewInput] = useState<number>(0);
  const [waterOldInput, setWaterOldInput] = useState<number>(0);
  const [waterNewInput, setWaterNewInput] = useState<number>(0);
  const [savingReading, setSavingReading] = useState(false);
  const [issueInvoiceImmediately, setIssueInvoiceImmediately] = useState(false);

  const CALC_METHOD_LABELS: Record<string, string> = {
    PER_KWH: "Theo số điện (kWh)",
    PER_M3: "Theo khối nước (m³)",
    PER_PERSON: "Theo đầu người / tháng",
    FIXED_PER_MONTH: "Cố định / phòng / tháng",
    FREE: "Miễn phí (0đ)",
  };

  const contractMap = useMemo(() => buildContractDisplayMap(contracts), [contracts]);

  const roomMap = useMemo(() => {
    const m: Record<number, RoomDto> = {};
    for (const r of rooms) {
      m[r.id] = r;
    }
    return m;
  }, [rooms]);

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

  // Load rooms for selected property
  const loadRooms = useCallback(async (propId: number) => {
    try {
      const res = await propertiesApi.getRooms(propId);
      if (res.isSuccess && res.data) {
        setRooms(res.data);
      } else {
        setRooms([]);
      }
    } catch {
      setRooms([]);
    }
  }, []);

  // Load tariffs for selected property and optional roomId
  const loadTariffs = useCallback(async (propId: number, roomId: number | null) => {
    setLoadingTariffs(true);
    try {
      const res = await propertiesApi.getTariffs(propId, roomId);
      if (res.isSuccess && res.data) {
        setTariffs(res.data);
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

        const totalFixedServices = res.data
          .filter(
            (t) =>
              t.tariffType !== "ELECTRICITY" &&
              t.tariffType !== "WATER" &&
              t.calcMethod !== "FREE"
          )
          .reduce((sum, t) => sum + (t.unitPrice || 0), 0);
        setServiceFee(totalFixedServices);
      }
    } catch (err) {
      console.warn("Could not load tariffs", err);
    } finally {
      setLoadingTariffs(false);
    }
  }, []);

  // Load contracts, utility readings, invoices and debt summary
  const loadBillingAndReadingsData = useCallback(async () => {
    const token = getStoredToken();
    if (!token) return;
    setLoadingData(true);
    try {
      const [contractsRes, readingsRes, invoicesRes, debtRes] = await Promise.all([
        leasingApi.getContracts({ roleView: "LANDLORD", page: 1, pageSize: 100 }),
        billingApi.getUtilityReadings({
          propertyId: selectedPropId ?? undefined,
          month: selectedMonth,
          year: selectedYear,
        }),
        billingApi.getInvoices({
          propertyId: selectedPropId ?? undefined,
          month: selectedMonth,
          year: selectedYear,
          page: 1,
          pageSize: 100,
        }),
        billingApi.getDebtSummary({
          propertyId: selectedPropId ?? undefined,
        }),
      ]);

      if (contractsRes.isSuccess && contractsRes.data) {
        setContracts(contractsRes.data.items);
      }
      if (readingsRes.isSuccess && readingsRes.data) {
        setReadings(readingsRes.data);
      } else {
        setReadings([]);
      }
      if (invoicesRes.isSuccess && invoicesRes.data) {
        setInvoices(invoicesRes.data.items);
      } else {
        setInvoices([]);
      }
      if (debtRes.isSuccess && debtRes.data) {
        setDebtSummary(debtRes.data);
      } else {
        setDebtSummary(null);
      }
    } catch {
      showToast("Không thể tải dữ liệu chỉ số điện nước và hóa đơn.", "error");
    } finally {
      setLoadingData(false);
    }
  }, [selectedPropId, selectedMonth, selectedYear, showToast]);

  useEffect(() => {
    loadProperties();
  }, [loadProperties]);

  useEffect(() => {
    if (selectedPropId) {
      setSelectedRoomScope(null);
      loadRooms(selectedPropId);
    }
  }, [selectedPropId, loadRooms]);

  useEffect(() => {
    if (selectedPropId) {
      loadTariffs(selectedPropId, selectedRoomScope);
    }
  }, [selectedPropId, selectedRoomScope, loadTariffs]);

  useEffect(() => {
    loadBillingAndReadingsData();
  }, [loadBillingAndReadingsData]);

  // Handle Save Tariffs to Backend (Property-wide or Room-specific)
  const handleSaveTariffs = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPropId) {
      showToast("Vui lòng chọn bất động sản cần cài đặt biểu phí!", "error");
      return;
    }
    setSavingTariffs(true);
    try {
      const payload = {
        roomId: selectedRoomScope,
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
      const res = await propertiesApi.setTariffs(selectedPropId, payload, selectedRoomScope);
      if (res.isSuccess) {
        const targetLabel = selectedRoomScope
          ? `Phòng ${roomMap[selectedRoomScope]?.roomNumber || `#${selectedRoomScope}`}`
          : "Toàn bộ tòa nhà";
        showToast(`Đã lưu đơn giá điện nước cho ${targetLabel} thành công!`, "success");
        setIsTariffModalOpen(false);
        loadTariffs(selectedPropId, selectedRoomScope);
      } else {
        showToast(res.error?.message || "Cập nhật biểu phí thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    } finally {
      setSavingTariffs(false);
    }
  };

  // Open Record Meter Reading Modal for a Contract
  const handleOpenReadingModal = async (ctr: LeasingContractDto) => {
    setReadingModalContract(ctr);
    setIssueInvoiceImmediately(false);

    const existingReading = readings.find((r) => r.roomId === ctr.roomId);
    if (existingReading) {
      setElecOldInput(existingReading.electricityOld);
      setElecNewInput(existingReading.electricityNew);
      setWaterOldInput(existingReading.waterOld);
      setWaterNewInput(existingReading.waterNew);
      return;
    }

    // Gợi ý chỉ số đầu kỳ bằng chỉ số cuối kỳ của tháng trước
    setElecOldInput(0);
    setElecNewInput(0);
    setWaterOldInput(0);
    setWaterNewInput(0);

    try {
      const prevRes = await billingApi.getLatestPreviousReading(
        ctr.roomId,
        selectedMonth,
        selectedYear
      );
      if (prevRes.isSuccess && prevRes.data) {
        setElecOldInput(prevRes.data.electricityNew);
        setElecNewInput(prevRes.data.electricityNew);
        setWaterOldInput(prevRes.data.waterNew);
        setWaterNewInput(prevRes.data.waterNew);
      }
    } catch {
      // Ignore fallback error
    }
  };

  // Save Utility Reading (and optionally create invoice immediately)
  const handleSaveReading = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!readingModalContract) return;

    if (elecNewInput < elecOldInput) {
      showToast("Chỉ số điện cuối kỳ phải lớn hơn hoặc bằng chỉ số đầu kỳ!", "error");
      return;
    }
    if (waterNewInput < waterOldInput) {
      showToast("Chỉ số nước cuối kỳ phải lớn hơn hoặc bằng chỉ số đầu kỳ!", "error");
      return;
    }

    setSavingReading(true);
    try {
      const res = await billingApi.recordUtilityReading({
        contractId: readingModalContract.id,
        billingMonth: selectedMonth,
        billingYear: selectedYear,
        electricityOld: Number(elecOldInput),
        electricityNew: Number(elecNewInput),
        waterOld: Number(waterOldInput),
        waterNew: Number(waterNewInput),
      });

      if (!res.isSuccess || !res.data) {
        showToast(res.error?.message || "Ghi chỉ số điện nước thất bại.", "error");
        return;
      }

      if (issueInvoiceImmediately) {
        const invRes = await billingApi.createInvoice({
          contractId: readingModalContract.id,
          billingMonth: selectedMonth,
          billingYear: selectedYear,
        });
        if (invRes.isSuccess) {
          showToast(
            `Đã chốt chỉ số (${res.data.electricityUsage} kWh, ${res.data.waterUsage} m³) và phát hành hóa đơn thành công!`,
            "success"
          );
        } else {
          showToast(
            `Đã lưu chỉ số điện nước, nhưng chưa thể phát hành hóa đơn: ${invRes.error?.message}`,
            "info"
          );
        }
      } else {
        showToast(
          `Đã lưu chỉ số công tơ (${res.data.electricityUsage} kWh điện, ${res.data.waterUsage} m³ nước) và tính thành tiền!`,
          "success"
        );
      }

      setReadingModalContract(null);
      await loadBillingAndReadingsData();
    } catch {
      showToast("Lỗi kết nối khi lưu chỉ số điện nước.", "error");
    } finally {
      setSavingReading(false);
    }
  };

  // Create Invoice for a Contract & Period
  const handleCreateInvoice = async (contractId: number) => {
    setCreatingInvoiceContractId(contractId);
    try {
      const res = await billingApi.createInvoice({
        contractId,
        billingMonth: selectedMonth,
        billingYear: selectedYear,
      });
      if (res.isSuccess && res.data) {
        showToast(
          `Đã phát hành hóa đơn ${res.data.invoiceCode} (Tổng: ${formatVnd(res.data.totalAmount)}) thành công!`,
          "success"
        );
        await loadBillingAndReadingsData();
      } else {
        showToast(res.error?.message || "Không thể phát hành hóa đơn.", "error");
      }
    } catch {
      showToast("Lỗi kết nối khi phát hành hóa đơn.", "error");
    } finally {
      setCreatingInvoiceContractId(null);
    }
  };

  // Cancel an unpaid/overdue invoice (BR-BIL-13, BR-BIL-14)
  const handleConfirmCancelInvoice = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!cancellingInvoice) return;
    if (!cancelReason.trim()) {
      showToast("Vui lòng nhập lý do hủy hóa đơn.", "error");
      return;
    }

    setSubmittingCancel(true);
    try {
      const res = await billingApi.cancelInvoice(cancellingInvoice.id, cancelReason.trim());
      if (res.isSuccess) {
        showToast(
          `Đã hủy hóa đơn ${cancellingInvoice.invoiceCode}. Bạn có thể cập nhật lại chỉ số điện nước và phát hành hóa đơn mới.`,
          "success"
        );
        setCancellingInvoice(null);
        setCancelReason("");
        await loadBillingAndReadingsData();
      } else {
        showToast(res.error?.message || "Không thể hủy hóa đơn.", "error");
      }
    } catch {
      showToast("Lỗi kết nối khi hủy hóa đơn.", "error");
    } finally {
      setSubmittingCancel(false);
    }
  };

  // Filter active/billable contracts belonging to selected property
  const billableContracts = useMemo(() => {
    const propRoomIds = new Set(rooms.map((r) => r.id));
    return contracts.filter((c) => {
      const statusOk =
        c.status === "ACTIVE" ||
        c.status === "EXPIRED" ||
        c.status === "EXPIRED_PENDING_HANDOVER" ||
        c.status === "HANDOVER_OVERDUE";
      if (!statusOk) return false;
      if (selectedPropId && propRoomIds.size > 0 && !propRoomIds.has(c.roomId)) {
        return false;
      }
      if (selectedRoomScope && c.roomId !== selectedRoomScope) {
        return false;
      }
      return true;
    });
  }, [contracts, rooms, selectedPropId, selectedRoomScope]);

  const filteredInvoices = useMemo(() => {
    return invoices.filter((inv) => {
      if (selectedRoomScope && inv.roomId !== selectedRoomScope) return false;
      if (invoiceStatusFilter !== "ALL" && inv.status !== invoiceStatusFilter) return false;
      return true;
    });
  }, [invoices, selectedRoomScope, invoiceStatusFilter]);

  const periodTotals = useMemo(() => {
    const activeInvs = invoices.filter(
      (i) =>
        i.status !== "CANCELLED" &&
        (!selectedRoomScope || i.roomId === selectedRoomScope)
    );
    const totalReceivable = activeInvs.reduce((s, i) => s + (i.totalAmount || 0), 0);
    const totalCollected = activeInvs.reduce((s, i) => s + (i.paidAmount || 0), 0);
    const totalUtilityReceivable = activeInvs.reduce(
      (s, i) => s + (i.electricityAmount || 0) + (i.waterAmount || 0) + (i.serviceAmount || 0),
      0
    );
    const totalRemaining = activeInvs.reduce((s, i) => s + getRemainingDebt(i), 0);
    return {
      count: activeInvs.length,
      totalReceivable,
      totalUtilityReceivable,
      totalCollected,
      totalRemaining,
    };
  }, [invoices, selectedRoomScope]);

  if (!user || role === "Tenant") {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-amber-50 text-amber-600 flex items-center justify-center mx-auto shadow-xs">
          <Zap className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Quản Lý Tiền Điện Nước</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập với tài khoản <strong>Chủ nhà (Landlord)</strong> để thiết lập đơn giá, ghi chỉ số điện nước và phát hành hóa đơn phòng.
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
  const selectedRoomObj = selectedRoomScope ? roomMap[selectedRoomScope] : null;
  const previewElecUsage = Math.max(0, Number(elecNewInput) - Number(elecOldInput));
  const previewWaterUsage = Math.max(0, Number(waterNewInput) - Number(waterOldInput));
  const previewElecCost = previewElecUsage * elecUnitPrice;
  const previewWaterCost = previewWaterUsage * waterUnitPrice;

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-amber-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Zap className="w-4 h-4" />
            <span>Mục 3.8: Quản lý tiền điện, nước & khoản thu dịch vụ</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Quản Lý Tiền Điện, Nước & Phát Hành Hóa Đơn
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Thiết lập đơn giá cho từng tòa nhà hoặc từng phòng, ghi chỉ số công tơ đầu kỳ/cuối kỳ, tự động tính tiền và theo dõi khoản thu.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {selectedPropId && (
            <button
              onClick={() => setIsTariffModalOpen(true)}
              className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-sm transition-colors cursor-pointer"
            >
              <Settings className="w-4 h-4" />
              <span>
                Thiết Lập Đơn Giá ({selectedRoomObj ? `Phòng ${selectedRoomObj.roomNumber}` : "Cả Tòa Nhà"})
              </span>
            </button>
          )}
        </div>
      </div>

      {/* Scope & Period Selector Bar */}
      <div className="bg-white rounded-2xl border border-slate-200/80 p-4 shadow-xs grid grid-cols-1 md:grid-cols-4 gap-4 items-center">
        {/* 1. Property Selector */}
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center shrink-0">
            <Building2 className="w-5 h-5" />
          </div>
          <div className="min-w-0 flex-1">
            <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wider block">
              Tòa nhà áp dụng
            </span>
            <select
              value={selectedPropId || ""}
              onChange={(e) => setSelectedPropId(Number(e.target.value))}
              className="font-bold text-slate-900 text-xs sm:text-sm bg-transparent border-none p-0 focus:ring-0 cursor-pointer w-full truncate"
            >
              {properties.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* 2. Room Scope Selector (Property-wide vs Specific Room) */}
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center shrink-0">
            <DoorOpen className="w-5 h-5" />
          </div>
          <div className="min-w-0 flex-1">
            <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wider block">
              Phạm vi đơn giá & lọc phòng
            </span>
            <select
              value={selectedRoomScope ?? "ALL"}
              onChange={(e) =>
                setSelectedRoomScope(e.target.value === "ALL" ? null : Number(e.target.value))
              }
              className="font-bold text-slate-900 text-xs sm:text-sm bg-transparent border-none p-0 focus:ring-0 cursor-pointer w-full truncate"
            >
              <option value="ALL">Toàn bộ tòa nhà ({rooms.length} phòng)</option>
              {rooms.map((r) => (
                <option key={r.id} value={r.id}>
                  Phòng {r.roomNumber} (Tầng {r.floor})
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* 3. Billing Period (Month / Year) */}
        <div className="flex items-center gap-2">
          <div className="flex-1">
            <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wider block mb-1">
              Kỳ ghi chỉ số & thu tiền
            </span>
            <div className="flex items-center gap-2">
              <select
                value={selectedMonth}
                onChange={(e) => setSelectedMonth(Number(e.target.value))}
                className="px-2.5 py-1.5 rounded-xl border border-slate-200 bg-slate-50 text-xs font-bold text-slate-800"
              >
                {Array.from({ length: 12 }, (_, idx) => idx + 1).map((m) => (
                  <option key={m} value={m}>
                    Tháng {String(m).padStart(2, "0")}
                  </option>
                ))}
              </select>
              <select
                value={selectedYear}
                onChange={(e) => setSelectedYear(Number(e.target.value))}
                className="px-2.5 py-1.5 rounded-xl border border-slate-200 bg-slate-50 text-xs font-bold text-slate-800"
              >
                {[now.getFullYear() - 1, now.getFullYear(), now.getFullYear() + 1].map((y) => (
                  <option key={y} value={y}>
                    Năm {y}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>

        {/* 4. Refresh action */}
        <div className="flex items-center justify-end gap-2">
          <button
            onClick={() => {
              if (selectedPropId) loadTariffs(selectedPropId, selectedRoomScope);
              loadBillingAndReadingsData();
            }}
            className="px-3.5 py-2 rounded-xl border border-slate-200 hover:bg-slate-50 text-xs font-bold text-slate-700 flex items-center gap-1.5 cursor-pointer"
          >
            <RefreshCw
              className={cn("w-3.5 h-3.5", (loadingTariffs || loadingData) && "animate-spin")}
            />
            <span>Làm mới dữ liệu</span>
          </button>
        </div>
      </div>

      {/* SECTION 1: Chức năng 1 - Đơn Giá Điện Nước Đang Áp Dụng */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center justify-between gap-4">
          <div className="flex items-center gap-4">
            <div className="w-12 h-12 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center shrink-0">
              <Zap className="w-6 h-6" />
            </div>
            <div>
              <span className="text-[11px] font-bold text-slate-400 uppercase">
                Đơn giá điện ({selectedRoomObj ? `Phòng ${selectedRoomObj.roomNumber}` : "Tòa nhà"})
              </span>
              <span className="text-xl font-black text-slate-900 block">
                {elecUnitPrice.toLocaleString("vi-VN")} đ/kWh
              </span>
              <span className="text-[10px] text-slate-500">
                {tariffs.find((t) => t.tariffType === "ELECTRICITY")?.roomId
                  ? "Đơn giá riêng của phòng"
                  : `Áp dụng chung: ${selectedProperty?.name || "Tòa nhà"}`}
              </span>
            </div>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center justify-between gap-4">
          <div className="flex items-center gap-4">
            <div className="w-12 h-12 rounded-xl bg-sky-50 text-sky-600 flex items-center justify-center shrink-0">
              <Droplet className="w-6 h-6" />
            </div>
            <div>
              <span className="text-[11px] font-bold text-slate-400 uppercase">
                Đơn giá nước ({selectedRoomObj ? `Phòng ${selectedRoomObj.roomNumber}` : "Tòa nhà"})
              </span>
              <span className="text-xl font-black text-slate-900 block">
                {waterUnitPrice.toLocaleString("vi-VN")} đ/m³
              </span>
              <span className="text-[10px] text-slate-500">
                {tariffs.find((t) => t.tariffType === "WATER")?.roomId
                  ? "Đơn giá riêng của phòng"
                  : "Đồng hồ nước theo khối (m³)"}
              </span>
            </div>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center justify-between gap-4">
          <div className="flex items-center gap-4">
            <div className="w-12 h-12 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center shrink-0">
              <DollarSign className="w-6 h-6" />
            </div>
            <div>
              <span className="text-[11px] font-bold text-slate-400 uppercase">
                Tổng phí dịch vụ cố định
              </span>
              <span className="text-xl font-black text-emerald-600 block">
                {serviceFee.toLocaleString("vi-VN")} đ/tháng
              </span>
              <span className="text-[10px] text-slate-500">
                Internet, vệ sinh, gửi xe, dịch vụ chung
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* SECTION 2: Chức năng 4 - Theo Dõi Khoản Thu Điện Nước & Tiền Phòng Trong Kỳ */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-3.5">
          <div className="w-10 h-10 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center shrink-0">
            <Receipt className="w-5 h-5" />
          </div>
          <div>
            <span className="text-[11px] font-semibold text-slate-500 block">
              Tổng phải thu kỳ {String(selectedMonth).padStart(2, "0")}/{selectedYear} ({periodTotals.count} HĐ)
            </span>
            <span className="text-base font-black text-slate-900">
              {formatVnd(periodTotals.totalReceivable)}
            </span>
            <span className="block text-[10px] text-amber-600 font-semibold">
              Trong đó Điện/Nước/DV: {formatVnd(periodTotals.totalUtilityReceivable)}
            </span>
          </div>
        </div>

        <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-3.5">
          <div className="w-10 h-10 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center shrink-0">
            <CheckCircle2 className="w-5 h-5" />
          </div>
          <div>
            <span className="text-[11px] font-semibold text-slate-500 block">
              Đã thu trong kỳ
            </span>
            <span className="text-base font-black text-emerald-600">
              {formatVnd(periodTotals.totalCollected)}
            </span>
          </div>
        </div>

        <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-3.5">
          <div className="w-10 h-10 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center shrink-0">
            <Wallet className="w-5 h-5" />
          </div>
          <div>
            <span className="text-[11px] font-semibold text-slate-500 block">
              Còn nợ kỳ này
            </span>
            <span className="text-base font-black text-amber-600">
              {formatVnd(periodTotals.totalRemaining)}
            </span>
          </div>
        </div>

        <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-3.5">
          <div className="w-10 h-10 rounded-xl bg-rose-50 text-rose-600 flex items-center justify-center shrink-0">
            <AlertCircle className="w-5 h-5" />
          </div>
          <div>
            <span className="text-[11px] font-semibold text-slate-500 block">
              Tổng nợ quá hạn lũy kế ({debtSummary?.totalOverdueInvoices ?? 0} HĐ)
            </span>
            <span className="text-base font-black text-rose-600">
              {formatVnd(debtSummary?.totalOverdueDebt ?? 0)}
            </span>
          </div>
        </div>
      </div>

      {/* SECTION 3: Chức năng 2 & 3 - Ghi Chỉ Số Điện Nước & Tính Tiền Tự Động Theo Hợp Đồng/Phòng */}
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
        <div className="p-5 border-b border-slate-100 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div>
            <h2 className="text-sm font-black text-slate-900 flex items-center gap-2">
              <Calculator className="w-4 h-4 text-amber-600" />
              <span>
                Ghi Chỉ Số Công Tơ & Tính Tiền Điện Nước Kỳ Tháng{" "}
                {String(selectedMonth).padStart(2, "0")}/{selectedYear}
              </span>
            </h2>
            <p className="text-xs text-slate-500 mt-0.5">
              Nhập chỉ số đầu kỳ và cuối kỳ cho từng phòng đang có hợp đồng thuê. Hệ thống tự động tính lượng tiêu thụ, thành tiền điện nước và tích hợp vào hóa đơn.
            </p>
          </div>
        </div>

        {loadingData ? (
          <div className="p-12 text-center text-xs text-slate-500 flex items-center justify-center gap-2">
            <Loader2 className="w-4 h-4 animate-spin text-blue-600" />
            <span>Đang tải danh sách phòng và chỉ số điện nước...</span>
          </div>
        ) : billableContracts.length === 0 ? (
          <div className="p-10 text-center space-y-2">
            <Calculator className="w-8 h-8 text-slate-300 mx-auto" />
            <h3 className="text-xs font-bold text-slate-800">
              Chưa có hợp đồng đang thuê (ACTIVE) nào trong phạm vi đã chọn
            </h3>
            <p className="text-xs text-slate-500 max-w-md mx-auto">
              Chỉ các phòng có hợp đồng đang hiệu lực (ACTIVE) hoặc hợp đồng vừa kết thúc chờ chốt kỳ cuối mới thực hiện ghi chỉ số điện nước và phát hành hóa đơn.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead>
                <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-semibold">
                  <th className="py-3 px-4">Phòng / Hợp đồng</th>
                  <th className="py-3 px-4">Chỉ số Điện (Đầu → Cuối)</th>
                  <th className="py-3 px-4 text-right">Tiền Điện</th>
                  <th className="py-3 px-4">Chỉ số Nước (Đầu → Cuối)</th>
                  <th className="py-3 px-4 text-right">Tiền Nước</th>
                  <th className="py-3 px-4 text-right">Phí DV</th>
                  <th className="py-3 px-4">Trạng thái Hóa đơn kỳ</th>
                  <th className="py-3 px-4 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white">
                {billableContracts.map((ctr) => {
                  const info = contractMap[ctr.id];
                  const roomObj = roomMap[ctr.roomId];
                  const reading = readings.find((r) => r.roomId === ctr.roomId);
                  const activeInvoice = invoices.find(
                    (i) => i.contractId === ctr.id && i.status !== "CANCELLED"
                  );
                  const invMeta = activeInvoice ? getInvoiceStatusMeta(activeInvoice.status) : null;

                  return (
                    <tr key={ctr.id} className="hover:bg-slate-50/80 transition-colors">
                      <td className="py-3.5 px-4">
                        <span className="font-black text-slate-900 block">
                          {roomObj ? `Phòng ${roomObj.roomNumber}` : info?.roomName || `Phòng #${ctr.roomId}`}
                        </span>
                        <span className="text-[11px] text-slate-500 font-mono">
                          {ctr.contractCode} • Thuê: {formatVnd(ctr.monthlyRent)}/tháng
                        </span>
                      </td>

                      <td className="py-3.5 px-4">
                        {reading ? (
                          <div>
                            <span className="font-mono text-slate-800">
                              {reading.electricityOld} → <strong>{reading.electricityNew}</strong>
                            </span>
                            <span className="ml-1.5 px-2 py-0.5 rounded-full bg-amber-50 text-amber-700 border border-amber-200 font-bold text-[10px]">
                              {reading.electricityUsage} kWh
                            </span>
                            <span className="block text-[10px] text-slate-400">
                              Đơn giá: {formatVnd(reading.electricityUnitPrice)}/kWh
                            </span>
                          </div>
                        ) : (
                          <span className="text-slate-400 italic">Chưa ghi chỉ số</span>
                        )}
                      </td>

                      <td className="py-3.5 px-4 text-right font-bold text-slate-900">
                        {reading ? formatVnd(reading.electricityCost) : "—"}
                      </td>

                      <td className="py-3.5 px-4">
                        {reading ? (
                          <div>
                            <span className="font-mono text-slate-800">
                              {reading.waterOld} → <strong>{reading.waterNew}</strong>
                            </span>
                            <span className="ml-1.5 px-2 py-0.5 rounded-full bg-sky-50 text-sky-700 border border-sky-200 font-bold text-[10px]">
                              {reading.waterUsage} m³
                            </span>
                            <span className="block text-[10px] text-slate-400">
                              Đơn giá: {formatVnd(reading.waterUnitPrice)}/m³
                            </span>
                          </div>
                        ) : (
                          <span className="text-slate-400 italic">Chưa ghi chỉ số</span>
                        )}
                      </td>

                      <td className="py-3.5 px-4 text-right font-bold text-slate-900">
                        {reading ? formatVnd(reading.waterCost) : "—"}
                      </td>

                      <td className="py-3.5 px-4 text-right font-semibold text-slate-700">
                        {reading ? formatVnd(reading.serviceCost) : formatVnd(serviceFee)}
                      </td>

                      <td className="py-3.5 px-4">
                        {activeInvoice && invMeta ? (
                          <div className="space-y-0.5">
                            <span
                              className={cn(
                                "inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[10px] font-bold border",
                                invMeta.badgeClass
                              )}
                            >
                              <span className={cn("w-1.5 h-1.5 rounded-full", invMeta.dotClass)} />
                              {invMeta.label}
                            </span>
                            <span className="block text-[11px] font-bold text-slate-800">
                              Tổng HĐ: {formatVnd(activeInvoice.totalAmount)}
                            </span>
                          </div>
                        ) : (
                          <span className="px-2.5 py-0.5 rounded-full text-[10px] font-semibold bg-slate-100 text-slate-600">
                            Chưa phát hành HĐ
                          </span>
                        )}
                      </td>

                      <td className="py-3.5 px-4 text-right">
                        <div className="inline-flex items-center justify-end gap-2">
                          <button
                            type="button"
                            disabled={Boolean(activeInvoice)}
                            onClick={() => handleOpenReadingModal(ctr)}
                            title={
                              activeInvoice
                                ? "Đã có hóa đơn trong kỳ (BR-BIL-13). Cần hủy hóa đơn nếu muốn sửa lại chỉ số."
                                : "Ghi hoặc cập nhật chỉ số công tơ điện nước"
                            }
                            className="px-3 py-1.5 rounded-xl bg-amber-50 hover:bg-amber-100 text-amber-800 border border-amber-200 font-bold text-xs transition-colors disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
                          >
                            {reading ? "Sửa chỉ số" : "Ghi chỉ số"}
                          </button>

                          {!activeInvoice ? (
                            <button
                              type="button"
                              disabled={creatingInvoiceContractId === ctr.id}
                              onClick={() => handleCreateInvoice(ctr.id)}
                              className="px-3 py-1.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs transition-colors inline-flex items-center gap-1 disabled:opacity-50 cursor-pointer"
                            >
                              {creatingInvoiceContractId === ctr.id ? (
                                <Loader2 className="w-3.5 h-3.5 animate-spin" />
                              ) : (
                                <FilePlus2 className="w-3.5 h-3.5" />
                              )}
                              <span>Phát hành HĐ</span>
                            </button>
                          ) : (
                            <button
                              type="button"
                              onClick={() => setDetailInvoiceId(activeInvoice.id)}
                              className="px-3 py-1.5 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-700 font-bold text-xs transition-colors inline-flex items-center gap-1 cursor-pointer"
                            >
                              <Eye className="w-3.5 h-3.5" />
                              <span>Xem HĐ</span>
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* SECTION 4: Danh Sách Hóa Đơn & Theo Dõi Khoản Phải Thu, Đã Thu, Còn Nợ */}
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
        <div className="p-5 border-b border-slate-100 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div>
            <h2 className="text-sm font-black text-slate-900 flex items-center gap-2">
              <Receipt className="w-4 h-4 text-blue-600" />
              <span>
                Danh Sách Khoản Phải Thu, Đã Thu & Còn Nợ (Kỳ{" "}
                {String(selectedMonth).padStart(2, "0")}/{selectedYear})
              </span>
            </h2>
            <p className="text-xs text-slate-500 mt-0.5">
              Theo dõi tiền thuê, tiền điện, tiền nước và trạng thái thanh toán VNPay của từng phòng.
            </p>
          </div>

          <div className="flex items-center gap-2">
            <select
              value={invoiceStatusFilter}
              onChange={(e) => setInvoiceStatusFilter(e.target.value)}
              className="px-3 py-1.5 rounded-xl border border-slate-200 bg-slate-50 text-xs font-bold text-slate-700"
            >
              {INVOICE_STATUS_FILTER_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        {filteredInvoices.length === 0 ? (
          <div className="p-10 text-center space-y-2">
            <Receipt className="w-8 h-8 text-slate-300 mx-auto" />
            <p className="text-xs font-bold text-slate-700">
              Chưa có hóa đơn nào trong kỳ {String(selectedMonth).padStart(2, "0")}/{selectedYear} khớp bộ lọc
            </p>
            <p className="text-[11px] text-slate-500">
              Hãy ghi chỉ số điện nước ở bảng trên và bấm &ldquo;Phát hành HĐ&rdquo; để tạo hóa đơn thu tiền cho kỳ này.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead>
                <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-semibold">
                  <th className="py-3 px-4">Mã HĐ / Phòng</th>
                  <th className="py-3 px-4 text-right">Tiền phòng</th>
                  <th className="py-3 px-4 text-right">Tiền điện</th>
                  <th className="py-3 px-4 text-right">Tiền nước</th>
                  <th className="py-3 px-4 text-right">Phí DV</th>
                  <th className="py-3 px-4 text-right">Phải thu</th>
                  <th className="py-3 px-4 text-right">Đã thu</th>
                  <th className="py-3 px-4 text-right">Còn nợ</th>
                  <th className="py-3 px-4">Trạng thái</th>
                  <th className="py-3 px-4 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white">
                {filteredInvoices.map((inv) => {
                  const meta = getInvoiceStatusMeta(inv.status);
                  const roomObj = roomMap[inv.roomId];
                  const info = contractMap[inv.contractId];
                  const remaining = getRemainingDebt(inv);
                  const canCancel = inv.status === "UNPAID" || inv.status === "OVERDUE";

                  return (
                    <tr key={inv.id} className="hover:bg-slate-50/80 transition-colors">
                      <td className="py-3.5 px-4">
                        <span className="font-mono font-bold text-slate-900 block">
                          {inv.invoiceCode}
                        </span>
                        <span className="text-[11px] text-slate-500">
                          {roomObj ? `Phòng ${roomObj.roomNumber}` : info?.roomName || `Phòng #${inv.roomId}`} • Hạn:{" "}
                          {formatDate(inv.dueDate)}
                        </span>
                      </td>
                      <td className="py-3.5 px-4 text-right font-medium text-slate-700">
                        {formatVnd(inv.rentAmount)}
                      </td>
                      <td className="py-3.5 px-4 text-right font-semibold text-amber-700">
                        {formatVnd(inv.electricityAmount)}
                      </td>
                      <td className="py-3.5 px-4 text-right font-semibold text-sky-700">
                        {formatVnd(inv.waterAmount)}
                      </td>
                      <td className="py-3.5 px-4 text-right text-slate-700">
                        {formatVnd(inv.serviceAmount)}
                      </td>
                      <td className="py-3.5 px-4 text-right font-black text-slate-900">
                        {formatVnd(inv.totalAmount)}
                      </td>
                      <td className="py-3.5 px-4 text-right font-bold text-emerald-600">
                        {formatVnd(inv.paidAmount)}
                      </td>
                      <td
                        className={cn(
                          "py-3.5 px-4 text-right font-black",
                          remaining > 0 ? "text-rose-600" : "text-slate-600"
                        )}
                      >
                        {formatVnd(remaining)}
                      </td>
                      <td className="py-3.5 px-4">
                        <span
                          className={cn(
                            "inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[10px] font-bold border",
                            meta.badgeClass
                          )}
                        >
                          <span className={cn("w-1.5 h-1.5 rounded-full", meta.dotClass)} />
                          {meta.label}
                        </span>
                      </td>
                      <td className="py-3.5 px-4 text-right">
                        <div className="inline-flex items-center justify-end gap-1.5">
                          <button
                            type="button"
                            onClick={() => setDetailInvoiceId(inv.id)}
                            className="px-2.5 py-1.5 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-700 font-bold text-xs inline-flex items-center gap-1 cursor-pointer"
                          >
                            <Eye className="w-3.5 h-3.5" />
                            <span>Chi tiết</span>
                          </button>
                          {canCancel && (
                            <button
                              type="button"
                              onClick={() => {
                                setCancellingInvoice(inv);
                                setCancelReason("");
                              }}
                              className="px-2.5 py-1.5 rounded-xl bg-rose-50 hover:bg-rose-100 text-rose-700 border border-rose-200 font-bold text-xs inline-flex items-center gap-1 cursor-pointer"
                            >
                              <XCircle className="w-3.5 h-3.5" />
                              <span>Hủy</span>
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Modal 1: Thiết lập đơn giá Điện Nước (Tòa nhà hoặc Phòng) */}
      {isTariffModalOpen && (
        <Modal
          isOpen={true}
          onClose={() => setIsTariffModalOpen(false)}
          title={`Thiết Lập Đơn Giá Điện Nước: ${
            selectedRoomObj
              ? `Phòng ${selectedRoomObj.roomNumber} (${selectedProperty?.name})`
              : `Toàn bộ tòa nhà ${selectedProperty?.name}`
          }`}
          description="Cấu hình đơn giá áp dụng riêng cho từng phòng hoặc chung cho cả tòa nhà. Hệ thống sẽ tự động nhân đơn giá này với chỉ số tiêu thụ hàng tháng."
          maxWidth="lg"
        >
          <form onSubmit={handleSaveTariffs} className="space-y-4">
            <div className="p-3 rounded-xl bg-blue-50/70 border border-blue-200 text-xs text-blue-900">
              <span className="font-bold block mb-1">Phạm vi áp dụng đơn giá:</span>
              <select
                value={selectedRoomScope ?? "ALL"}
                onChange={(e) =>
                  setSelectedRoomScope(e.target.value === "ALL" ? null : Number(e.target.value))
                }
                className="w-full px-3 py-2 rounded-lg border border-blue-200 bg-white font-bold text-xs text-slate-900"
              >
                <option value="ALL">Áp dụng chung cho toàn bộ tòa nhà ({selectedProperty?.name})</option>
                {rooms.map((r) => (
                  <option key={r.id} value={r.id}>
                    Áp dụng riêng cho Phòng {r.roomNumber} (Tầng {r.floor})
                  </option>
                ))}
              </select>
            </div>

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
                {savingTariffs ? "Đang lưu..." : "Lưu Đơn Giá Điện Nước"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Modal 2: Ghi Chỉ Số Điện Nước & Tính Tiền Tự Động */}
      {readingModalContract && (
        <Modal
          isOpen={true}
          onClose={() => setReadingModalContract(null)}
          title={`Ghi Chỉ Số Điện Nước Kỳ ${String(selectedMonth).padStart(2, "0")}/${selectedYear}`}
          description={`Hợp đồng ${readingModalContract.contractCode} • ${
            roomMap[readingModalContract.roomId]
              ? `Phòng ${roomMap[readingModalContract.roomId].roomNumber}`
              : contractMap[readingModalContract.id]?.roomName || `Phòng #${readingModalContract.roomId}`
          }`}
          maxWidth="lg"
        >
          <form onSubmit={handleSaveReading} className="space-y-4">
            {/* Electricity inputs */}
            <div className="p-4 rounded-2xl border border-amber-200 bg-amber-50/40 space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-xs font-black text-amber-900 flex items-center gap-1.5">
                  <Zap className="w-4 h-4 text-amber-500" />
                  <span>Chỉ Số Công Tơ Điện (kWh)</span>
                </span>
                <span className="text-[11px] font-bold text-amber-700">
                  Đơn giá: {formatVnd(elecUnitPrice)}/kWh
                </span>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-[11px] font-bold text-slate-600 mb-1">
                    Chỉ số đầu kỳ (kWh)
                  </label>
                  <input
                    type="number"
                    min={0}
                    required
                    value={elecOldInput}
                    onChange={(e) => setElecOldInput(Number(e.target.value))}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white text-xs font-mono font-bold"
                  />
                </div>
                <div>
                  <label className="block text-[11px] font-bold text-slate-600 mb-1">
                    Chỉ số cuối kỳ (kWh)
                  </label>
                  <input
                    type="number"
                    min={elecOldInput}
                    required
                    value={elecNewInput}
                    onChange={(e) => setElecNewInput(Number(e.target.value))}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white text-xs font-mono font-bold"
                  />
                </div>
              </div>

              <div className="flex items-center justify-between pt-2 border-t border-amber-200/60 text-xs">
                <span className="text-slate-600">
                  Tiêu thụ: <strong className="text-amber-800">{previewElecUsage} kWh</strong>
                </span>
                <span className="font-black text-slate-900">
                  Thành tiền điện: {formatVnd(previewElecCost)}
                </span>
              </div>
            </div>

            {/* Water inputs */}
            <div className="p-4 rounded-2xl border border-sky-200 bg-sky-50/40 space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-xs font-black text-sky-900 flex items-center gap-1.5">
                  <Droplet className="w-4 h-4 text-sky-500" />
                  <span>Chỉ Số Đồng Hồ Nước (m³)</span>
                </span>
                <span className="text-[11px] font-bold text-sky-700">
                  Đơn giá: {formatVnd(waterUnitPrice)}/m³
                </span>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-[11px] font-bold text-slate-600 mb-1">
                    Chỉ số đầu kỳ (m³)
                  </label>
                  <input
                    type="number"
                    min={0}
                    required
                    value={waterOldInput}
                    onChange={(e) => setWaterOldInput(Number(e.target.value))}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white text-xs font-mono font-bold"
                  />
                </div>
                <div>
                  <label className="block text-[11px] font-bold text-slate-600 mb-1">
                    Chỉ số cuối kỳ (m³)
                  </label>
                  <input
                    type="number"
                    min={waterOldInput}
                    required
                    value={waterNewInput}
                    onChange={(e) => setWaterNewInput(Number(e.target.value))}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white text-xs font-mono font-bold"
                  />
                </div>
              </div>

              <div className="flex items-center justify-between pt-2 border-t border-sky-200/60 text-xs">
                <span className="text-slate-600">
                  Tiêu thụ: <strong className="text-sky-800">{previewWaterUsage} m³</strong>
                </span>
                <span className="font-black text-slate-900">
                  Thành tiền nước: {formatVnd(previewWaterCost)}
                </span>
              </div>
            </div>

            {/* Auto-calculated Summary */}
            <div className="p-4 rounded-2xl bg-slate-50 border border-slate-200 space-y-2 text-xs">
              <div className="flex justify-between">
                <span className="text-slate-500">Tiền điện + Tiền nước kỳ này:</span>
                <span className="font-bold text-slate-900">
                  {formatVnd(previewElecCost + previewWaterCost)}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-500">Phí dịch vụ cố định theo biểu giá:</span>
                <span className="font-bold text-slate-900">{formatVnd(serviceFee)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-500">Tiền thuê phòng theo hợp đồng:</span>
                <span className="font-bold text-slate-900">
                  {formatVnd(readingModalContract.monthlyRent)}
                </span>
              </div>
              <div className="flex justify-between pt-2 border-t border-slate-200 text-sm">
                <span className="font-bold text-slate-800">Dự kiến tổng hóa đơn kỳ:</span>
                <span className="font-black text-blue-600">
                  {formatVnd(
                    readingModalContract.monthlyRent +
                      previewElecCost +
                      previewWaterCost +
                      serviceFee
                  )}
                </span>
              </div>
            </div>

            <label className="flex items-center gap-2.5 p-3 rounded-xl border border-slate-200 bg-white cursor-pointer">
              <input
                type="checkbox"
                checked={issueInvoiceImmediately}
                onChange={(e) => setIssueInvoiceImmediately(e.target.checked)}
                className="rounded border-slate-300 text-blue-600 focus:ring-blue-500"
              />
              <span className="text-xs font-semibold text-slate-800">
                Phát hành luôn hóa đơn kỳ {String(selectedMonth).padStart(2, "0")}/{selectedYear} cho người thuê ngay sau khi lưu chỉ số
              </span>
            </label>

            <div className="flex gap-3 pt-2">
              <button
                type="button"
                onClick={() => setReadingModalContract(null)}
                className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
              >
                Đóng
              </button>
              <button
                type="submit"
                disabled={savingReading}
                className="flex-1 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20 disabled:opacity-50"
              >
                {savingReading
                  ? "Đang xử lý..."
                  : issueInvoiceImmediately
                  ? "Lưu Chỉ Số & Phát Hành HĐ"
                  : "Lưu Chỉ Số & Tính Tiền"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Modal 3: Hủy hóa đơn để sửa lại chỉ số (BR-BIL-13, BR-BIL-14) */}
      {cancellingInvoice && (
        <Modal
          isOpen={true}
          onClose={() => setCancellingInvoice(null)}
          title={`Hủy Hóa Đơn ${cancellingInvoice.invoiceCode}`}
          description="Theo quy tắc BR-BIL-13 & BR-BIL-14, hóa đơn chưa thanh toán không được sửa trực tiếp. Sau khi hủy hóa đơn này, bạn có thể cập nhật lại chỉ số điện nước và phát hành hóa đơn mới."
          maxWidth="md"
        >
          <form onSubmit={handleConfirmCancelInvoice} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1.5">
                Lý do hủy hóa đơn <span className="text-rose-500">*</span>
              </label>
              <textarea
                rows={3}
                required
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                placeholder="Ví dụ: Nhập nhầm chỉ số công tơ điện tháng này, hủy để chốt lại chỉ số..."
                className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs focus:outline-none focus:ring-2 focus:ring-rose-500/20 focus:border-rose-500"
              />
            </div>

            <div className="flex gap-3 pt-2">
              <button
                type="button"
                onClick={() => setCancellingInvoice(null)}
                className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
              >
                Quay lại
              </button>
              <button
                type="submit"
                disabled={submittingCancel}
                className="flex-1 py-2.5 rounded-xl bg-rose-600 hover:bg-rose-700 text-white text-xs font-bold disabled:opacity-50"
              >
                {submittingCancel ? "Đang hủy..." : "Xác Nhận Hủy Hóa Đơn"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Modal 4: Chi tiết hóa đơn (Chế độ Chủ nhà xem) */}
      <InvoiceDetailModal
        invoiceId={detailInvoiceId}
        isOpen={detailInvoiceId !== null}
        onClose={() => setDetailInvoiceId(null)}
        contractMap={contractMap}
        onPaymentUpdated={loadBillingAndReadingsData}
        readOnlyPayment={true}
      />
    </div>
  );
}
