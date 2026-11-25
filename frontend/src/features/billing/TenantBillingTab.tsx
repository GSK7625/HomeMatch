"use client";

import React, { useCallback, useEffect, useMemo, useState } from "react";
import { useToast } from "@/components/Toast";
import { billingApi } from "@/lib/billing-api";
import {
  BillingDebtInvoiceDto,
  BillingDebtSummaryDto,
  BillingInvoiceDto,
  BillingUtilityReadingDto,
  LeasingContractDto,
} from "@/types";
import {
  AGING_GROUP_BADGE_MAP,
  buildContractDisplayMap,
  formatDate,
  formatVnd,
  getInvoiceStatusMeta,
  getRemainingDebt,
  INVOICE_STATUS_FILTER_OPTIONS,
  isInvoicePayable,
  resolveVnPayCheckoutUrl,
} from "@/features/billing/billing-helpers";
import { InvoiceDetailModal } from "@/features/billing/InvoiceDetailModal";
import { cn } from "@/lib/utils";
import {
  AlertCircle,
  Calendar,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  CreditCard,
  DollarSign,
  Eye,
  Filter,
  Loader2,
  Receipt,
  RefreshCw,
  RotateCcw,
  Wallet,
  Zap,
  Droplet,
} from "lucide-react";

interface TenantBillingTabProps {
  contracts: LeasingContractDto[];
  onDebtSummaryChange?: (summary: BillingDebtSummaryDto | null) => void;
}

const PAGE_SIZE = 10;

export function TenantBillingTab({
  contracts,
  onDebtSummaryChange,
}: TenantBillingTabProps) {
  const { showToast } = useToast();
  const contractMap = useMemo(() => buildContractDisplayMap(contracts), [contracts]);

  // Filters
  const [selectedContractId, setSelectedContractId] = useState<string>("ALL");
  const [selectedStatus, setSelectedStatus] = useState<string>("ALL");
  const [selectedMonth, setSelectedMonth] = useState<string>("ALL");
  const [selectedYear, setSelectedYear] = useState<string>("ALL");
  const [selectedAgingGroup, setSelectedAgingGroup] = useState<string | null>(null);
  const [page, setPage] = useState<number>(1);

  // Data state
  const [loading, setLoading] = useState<boolean>(true);
  const [debtSummary, setDebtSummary] = useState<BillingDebtSummaryDto | null>(null);
  const [summaryTotals, setSummaryTotals] = useState<{
    totalPayable: number;
    totalPaid: number;
    invoiceCount: number;
  }>({ totalPayable: 0, totalPaid: 0, invoiceCount: 0 });
  const [invoices, setInvoices] = useState<BillingInvoiceDto[]>([]);
  const [debtInvoices, setDebtInvoices] = useState<BillingDebtInvoiceDto[]>([]);
  const [utilityReadings, setUtilityReadings] = useState<BillingUtilityReadingDto[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [viewMode, setViewMode] = useState<"invoices" | "usage">("invoices");

  // Modal & action state
  const [detailInvoiceId, setDetailInvoiceId] = useState<number | null>(null);
  const [payingInvoiceId, setPayingInvoiceId] = useState<number | null>(null);

  const currentYear = new Date().getFullYear();
  const yearOptions = useMemo(() => {
    const years: number[] = [];
    for (let y = currentYear + 1; y >= Math.max(2024, currentYear - 3); y--) {
      years.push(y);
    }
    return years;
  }, [currentYear]);

  const parsedContractId =
    selectedContractId !== "ALL" && Number.isFinite(Number(selectedContractId))
      ? Number(selectedContractId)
      : undefined;

  const loadSummaryAndTotals = useCallback(async () => {
    const [summaryRes, allInvoicesRes, readingsRes] = await Promise.all([
      billingApi.getDebtSummary({ contractId: parsedContractId }),
      billingApi.getInvoices({ contractId: parsedContractId, page: 1, pageSize: 100 }),
      billingApi.getUtilityReadings({
        contractId: parsedContractId,
        month: selectedMonth !== "ALL" ? Number(selectedMonth) : undefined,
        year: selectedYear !== "ALL" ? Number(selectedYear) : undefined,
      }),
    ]);

    if (readingsRes.isSuccess && readingsRes.data) {
      setUtilityReadings(readingsRes.data);
    } else {
      setUtilityReadings([]);
    }

    if (summaryRes.isSuccess && summaryRes.data) {
      setDebtSummary(summaryRes.data);
      onDebtSummaryChange?.(summaryRes.data);
    } else {
      setDebtSummary(null);
      onDebtSummaryChange?.(null);
    }

    if (allInvoicesRes.isSuccess && allInvoicesRes.data) {
      const validItems = allInvoicesRes.data.items.filter((i) => i.status !== "CANCELLED");
      const totalPayable = validItems.reduce((sum, i) => sum + (i.totalAmount || 0), 0);
      const totalPaid = validItems.reduce((sum, i) => sum + (i.paidAmount || 0), 0);
      setSummaryTotals({
        totalPayable,
        totalPaid,
        invoiceCount: validItems.length,
      });
    }
  }, [parsedContractId, selectedMonth, selectedYear, onDebtSummaryChange]);

  const loadListData = useCallback(async () => {
    if (selectedAgingGroup) {
      const debtRes = await billingApi.getDebtInvoices({
        contractId: parsedContractId,
        agingGroup: selectedAgingGroup,
        page,
        pageSize: PAGE_SIZE,
      });
      if (debtRes.isSuccess && debtRes.data) {
        setDebtInvoices(debtRes.data.items);
        setInvoices([]);
        setTotalCount(debtRes.data.totalCount);
      } else {
        setDebtInvoices([]);
        setTotalCount(0);
        showToast(debtRes.error?.message || "Không thể tải danh sách hóa đơn công nợ.", "error");
      }
      return;
    }

    const invRes = await billingApi.getInvoices({
      contractId: parsedContractId,
      status: selectedStatus,
      month: selectedMonth !== "ALL" ? Number(selectedMonth) : undefined,
      year: selectedYear !== "ALL" ? Number(selectedYear) : undefined,
      page,
      pageSize: PAGE_SIZE,
    });

    if (invRes.isSuccess && invRes.data) {
      setInvoices(invRes.data.items);
      setDebtInvoices([]);
      setTotalCount(invRes.data.totalCount);
    } else {
      setInvoices([]);
      setTotalCount(0);
      showToast(invRes.error?.message || "Không thể tải danh sách hóa đơn.", "error");
    }
  }, [
    selectedAgingGroup,
    parsedContractId,
    selectedStatus,
    selectedMonth,
    selectedYear,
    page,
    showToast,
  ]);

  const refreshAll = useCallback(async () => {
    setLoading(true);
    try {
      await Promise.all([loadSummaryAndTotals(), loadListData()]);
    } catch {
      showToast("Có lỗi xảy ra khi tải dữ liệu hóa đơn & thanh toán.", "error");
    } finally {
      setLoading(false);
    }
  }, [loadSummaryAndTotals, loadListData, showToast]);

  useEffect(() => {
    refreshAll();
  }, [refreshAll]);

  const handleResetFilters = () => {
    setSelectedContractId("ALL");
    setSelectedStatus("ALL");
    setSelectedMonth("ALL");
    setSelectedYear("ALL");
    setSelectedAgingGroup(null);
    setPage(1);
  };

  const handleQuickPay = async (invoiceId: number) => {
    setPayingInvoiceId(invoiceId);
    try {
      const outcome = await resolveVnPayCheckoutUrl(invoiceId);
      if (outcome.kind === "redirect") {
        if (outcome.resumed) {
          showToast("Đang mở lại liên kết thanh toán VNPay còn hiệu lực...", "info");
        }
        window.location.href = outcome.paymentUrl;
        return;
      }

      showToast(outcome.message, "error");
    } catch {
      showToast("Lỗi kết nối khi khởi tạo thanh toán VNPay.", "error");
    } finally {
      setPayingInvoiceId(null);
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const hasActiveFilter =
    selectedContractId !== "ALL" ||
    selectedStatus !== "ALL" ||
    selectedMonth !== "ALL" ||
    selectedYear !== "ALL" ||
    selectedAgingGroup !== null;

  return (
    <div className="space-y-6">
      {/* 1. Payment & Debt Summary Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-4">
          <div className="w-11 h-11 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center shrink-0">
            <Receipt className="w-5 h-5" />
          </div>
          <div className="min-w-0">
            <span className="text-[11px] font-semibold text-slate-500 block">
              Tổng tiền phải trả ({summaryTotals.invoiceCount} kỳ)
            </span>
            <span className="text-lg font-black text-slate-900 truncate block">
              {formatVnd(summaryTotals.totalPayable)}
            </span>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-4">
          <div className="w-11 h-11 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center shrink-0">
            <CheckCircle2 className="w-5 h-5" />
          </div>
          <div className="min-w-0">
            <span className="text-[11px] font-semibold text-slate-500 block">
              Đã thanh toán
            </span>
            <span className="text-lg font-black text-emerald-600 truncate block">
              {formatVnd(summaryTotals.totalPaid)}
            </span>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-4">
          <div className="w-11 h-11 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center shrink-0">
            <Wallet className="w-5 h-5" />
          </div>
          <div className="min-w-0">
            <span className="text-[11px] font-semibold text-slate-500 block">
              Còn nợ hiện tại ({debtSummary?.totalUnpaidInvoices ?? 0} hóa đơn)
            </span>
            <span className="text-lg font-black text-amber-600 truncate block">
              {formatVnd(debtSummary?.totalUnpaidDebt ?? 0)}
            </span>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-4">
          <div className="w-11 h-11 rounded-xl bg-rose-50 text-rose-600 flex items-center justify-center shrink-0">
            <AlertCircle className="w-5 h-5" />
          </div>
          <div className="min-w-0">
            <span className="text-[11px] font-semibold text-slate-500 block">
              Nợ quá hạn ({debtSummary?.totalOverdueInvoices ?? 0} hóa đơn)
            </span>
            <span className="text-lg font-black text-rose-600 truncate block">
              {formatVnd(debtSummary?.totalOverdueDebt ?? 0)}
            </span>
          </div>
        </div>
      </div>

      {/* 2. Debt Aging Quick Filter Strip (only shown when tenant has unpaid debt) */}
      {debtSummary && debtSummary.totalUnpaidInvoices > 0 && (
        <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-xs space-y-3">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <span className="text-xs font-bold text-slate-800 flex items-center gap-1.5">
              <DollarSign className="w-4 h-4 text-amber-600" />
              <span>Theo dõi công nợ theo hạn thanh toán</span>
            </span>
            {selectedAgingGroup && (
              <button
                type="button"
                onClick={() => {
                  setSelectedAgingGroup(null);
                  setPage(1);
                }}
                className="text-[11px] font-bold text-blue-600 hover:text-blue-700"
              >
                Quay lại tất cả hóa đơn →
              </button>
            )}
          </div>

          <div className="grid grid-cols-2 lg:grid-cols-4 gap-2.5">
            {debtSummary.agingGroups.map((group) => {
              const isSelected = selectedAgingGroup === group.groupCode;
              const badgeStyle =
                AGING_GROUP_BADGE_MAP[group.groupCode] ??
                "bg-slate-50 text-slate-700 border-slate-200";
              return (
                <button
                  key={group.groupCode}
                  type="button"
                  disabled={group.invoiceCount === 0}
                  onClick={() => {
                    setSelectedAgingGroup(isSelected ? null : group.groupCode);
                    setPage(1);
                  }}
                  className={cn(
                    "p-3 rounded-xl border text-left transition-all disabled:opacity-50 disabled:cursor-not-allowed",
                    isSelected
                      ? "ring-2 ring-blue-500 border-blue-500 bg-blue-50/40"
                      : cn("hover:shadow-xs cursor-pointer", badgeStyle)
                  )}
                >
                  <div className="flex items-center justify-between gap-1 text-[11px] font-bold">
                    <span className="truncate">{group.groupName}</span>
                    <span className="px-1.5 py-0.5 rounded-md bg-white/80 text-[10px]">
                      {group.invoiceCount} HĐ
                    </span>
                  </div>
                  <div className="text-sm font-black mt-1">
                    {formatVnd(group.totalAmount)}
                  </div>
                </button>
              );
            })}
          </div>
        </div>
      )}

      {/* 3. Filter Toolbar & Sub-tab Switcher */}
      <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-xs space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3 pb-3 border-b border-slate-100">
          <div className="inline-flex p-1 rounded-xl bg-slate-100 border border-slate-200/70">
            <button
              type="button"
              onClick={() => setViewMode("invoices")}
              className={cn(
                "px-3.5 py-1.5 rounded-lg text-xs font-bold transition-all flex items-center gap-1.5 cursor-pointer",
                viewMode === "invoices"
                  ? "bg-white text-blue-600 shadow-xs"
                  : "text-slate-600 hover:text-slate-900"
              )}
            >
              <Receipt className="w-3.5 h-3.5" />
              <span>Hóa đơn & Thanh toán</span>
            </button>
            <button
              type="button"
              onClick={() => setViewMode("usage")}
              className={cn(
                "px-3.5 py-1.5 rounded-lg text-xs font-bold transition-all flex items-center gap-1.5 cursor-pointer",
                viewMode === "usage"
                  ? "bg-white text-blue-600 shadow-xs"
                  : "text-slate-600 hover:text-slate-900"
              )}
            >
              <Zap className="w-3.5 h-3.5 text-amber-500" />
              <span>Bảng chi phí sử dụng điện nước ({utilityReadings.length})</span>
            </button>
          </div>
        </div>

        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2 text-xs font-bold text-slate-800">
            <Filter className="w-4 h-4 text-blue-600" />
            <span>
              {selectedAgingGroup
                ? "Đang lọc hóa đơn còn nợ theo nhóm tuổi nợ"
                : "Lọc hóa đơn theo hợp đồng, trạng thái và kỳ thanh toán"}
            </span>
          </div>

          <div className="flex items-center gap-2">
            {hasActiveFilter && (
              <button
                type="button"
                onClick={handleResetFilters}
                className="inline-flex items-center gap-1 px-3 py-1.5 rounded-xl border border-slate-200 bg-slate-50 hover:bg-slate-100 text-xs font-bold text-slate-600 transition-colors cursor-pointer"
              >
                <RotateCcw className="w-3.5 h-3.5" />
                <span>Xóa bộ lọc</span>
              </button>
            )}
            <button
              type="button"
              onClick={refreshAll}
              disabled={loading}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl border border-slate-200 bg-white hover:bg-slate-50 text-xs font-bold text-slate-700 transition-colors cursor-pointer"
            >
              <RefreshCw className={cn("w-3.5 h-3.5", loading && "animate-spin")} />
              <span>Làm mới</span>
            </button>
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
          <select
            value={selectedContractId}
            onChange={(e) => {
              setSelectedContractId(e.target.value);
              setPage(1);
            }}
            className="px-3 py-2 rounded-xl border border-slate-200 bg-slate-50/70 text-xs font-medium text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
          >
            <option value="ALL">Tất cả hợp đồng / phòng</option>
            {contracts.map((ctr) => {
              const info = contractMap[ctr.id];
              return (
                <option key={ctr.id} value={String(ctr.id)}>
                  {info?.roomName || `Phòng #${ctr.roomId}`} ({ctr.contractCode})
                </option>
              );
            })}
          </select>

          <select
            value={selectedStatus}
            disabled={selectedAgingGroup !== null}
            onChange={(e) => {
              setSelectedStatus(e.target.value);
              setPage(1);
            }}
            className="px-3 py-2 rounded-xl border border-slate-200 bg-slate-50/70 text-xs font-medium text-slate-800 disabled:opacity-50 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
          >
            {INVOICE_STATUS_FILTER_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>

          <select
            value={selectedMonth}
            disabled={selectedAgingGroup !== null}
            onChange={(e) => {
              setSelectedMonth(e.target.value);
              setPage(1);
            }}
            className="px-3 py-2 rounded-xl border border-slate-200 bg-slate-50/70 text-xs font-medium text-slate-800 disabled:opacity-50 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
          >
            <option value="ALL">Tất cả các tháng</option>
            {Array.from({ length: 12 }, (_, idx) => idx + 1).map((m) => (
              <option key={m} value={String(m)}>
                Tháng {m}
              </option>
            ))}
          </select>

          <select
            value={selectedYear}
            disabled={selectedAgingGroup !== null}
            onChange={(e) => {
              setSelectedYear(e.target.value);
              setPage(1);
            }}
            className="px-3 py-2 rounded-xl border border-slate-200 bg-slate-50/70 text-xs font-medium text-slate-800 disabled:opacity-50 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
          >
            <option value="ALL">Tất cả các năm</option>
            {yearOptions.map((y) => (
              <option key={y} value={String(y)}>
                Năm {y}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* 4. Invoice / Debt List OR Utility Usage Table */}
      {loading ? (
        <div className="space-y-3">
          {[1, 2, 3].map((i) => (
            <div
              key={i}
              className="h-32 rounded-2xl bg-slate-100 animate-pulse border border-slate-200/60"
            />
          ))}
        </div>
      ) : viewMode === "usage" ? (
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
          <div className="p-5 border-b border-slate-100 flex flex-col sm:flex-row sm:items-center justify-between gap-2">
            <div>
              <h3 className="text-sm font-black text-slate-900 flex items-center gap-2">
                <Zap className="w-4 h-4 text-amber-500" />
                <span>Bảng Chi Phí Sử Dụng Điện, Nước & Dịch Vụ Theo Kỳ</span>
              </h3>
              <p className="text-xs text-slate-500 mt-0.5">
                Theo dõi chỉ số công tơ đầu kỳ, cuối kỳ, lượng tiêu thụ thực tế, đơn giá áp dụng và tổng chi phí sử dụng từng tháng.
              </p>
            </div>
          </div>

          {utilityReadings.length === 0 ? (
            <div className="p-6 sm:p-12 text-center space-y-2">
              <Zap className="w-9 h-9 text-slate-300 mx-auto" />
              <h4 className="text-sm font-bold text-slate-800">
                Chưa có dữ liệu ghi chỉ số điện nước cho kỳ đã chọn
              </h4>
              <p className="text-xs text-slate-500 max-w-md mx-auto">
                Khi chủ nhà ghi nhận chỉ số công tơ điện nước hàng tháng, bảng chi tiết chỉ số tiêu thụ, đơn giá và thành tiền sẽ hiển thị tại đây.
              </p>
            </div>
          ) : (
            <>
              {/* Mobile Card View (< md) */}
              <div className="md:hidden divide-y divide-slate-100">
                {utilityReadings.map((r) => {
                  const info = contractMap[r.contractId];
                  const totalUtilityCost = r.electricityCost + r.waterCost + r.serviceCost;
                  return (
                    <div key={r.id} className="p-4 space-y-3 text-xs bg-white">
                      <div className="flex items-start justify-between gap-2">
                        <div className="min-w-0">
                          <span className="font-black text-slate-900 block">
                            Tháng {String(r.billingMonth).padStart(2, "0")}/{r.billingYear}
                          </span>
                          <span className="text-[11px] text-slate-500 block truncate">
                            {info?.roomName || `Phòng #${r.roomId}`} • Chốt: {formatDate(r.readingDate)}
                          </span>
                        </div>
                        <div className="text-right shrink-0">
                          <span className="text-[10px] text-slate-400 uppercase font-semibold block">
                            Tổng DV
                          </span>
                          <span className="font-black text-blue-600 text-sm">
                            {formatVnd(totalUtilityCost)}
                          </span>
                        </div>
                      </div>

                      <div className="grid grid-cols-1 xs:grid-cols-2 gap-2">
                        <div className="p-2.5 rounded-xl bg-amber-50/70 border border-amber-200/70 space-y-1">
                          <div className="flex items-center justify-between gap-1">
                            <span className="font-bold text-amber-900">Điện ({r.electricityUsage} kWh)</span>
                            <span className="font-black text-slate-900">{formatVnd(r.electricityCost)}</span>
                          </div>
                          <div className="text-[11px] text-slate-600 font-mono">
                            {r.electricityOld} → {r.electricityNew} · {formatVnd(r.electricityUnitPrice)}/kWh
                          </div>
                        </div>

                        <div className="p-2.5 rounded-xl bg-sky-50/70 border border-sky-200/70 space-y-1">
                          <div className="flex items-center justify-between gap-1">
                            <span className="font-bold text-sky-900">Nước ({r.waterUsage} m³)</span>
                            <span className="font-black text-slate-900">{formatVnd(r.waterCost)}</span>
                          </div>
                          <div className="text-[11px] text-slate-600 font-mono">
                            {r.waterOld} → {r.waterNew} · {formatVnd(r.waterUnitPrice)}/m³
                          </div>
                        </div>
                      </div>

                      <div className="flex items-center justify-between text-[11px] pt-1 border-t border-slate-100">
                        <span className="text-slate-500">Phí dịch vụ khác</span>
                        <span className="font-bold text-slate-800">{formatVnd(r.serviceCost)}</span>
                      </div>
                    </div>
                  );
                })}
              </div>

              {/* Desktop Table (>= md) */}
              <div className="hidden md:block overflow-x-auto">
                <table className="w-full text-left text-xs">
                  <thead>
                    <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-semibold">
                      <th className="py-3 px-4">Kỳ / Phòng</th>
                      <th className="py-3 px-4">Chỉ số Điện (kWh)</th>
                      <th className="py-3 px-4 text-right">Đơn giá Điện</th>
                      <th className="py-3 px-4 text-right">Tiền Điện</th>
                      <th className="py-3 px-4">Chỉ số Nước (m³)</th>
                      <th className="py-3 px-4 text-right">Đơn giá Nước</th>
                      <th className="py-3 px-4 text-right">Tiền Nước</th>
                      <th className="py-3 px-4 text-right">Phí Dịch Vụ</th>
                      <th className="py-3 px-4 text-right">Tổng Chi Phí DV</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 bg-white">
                    {utilityReadings.map((r) => {
                      const info = contractMap[r.contractId];
                      const totalUtilityCost = r.electricityCost + r.waterCost + r.serviceCost;
                      return (
                        <tr key={r.id} className="hover:bg-slate-50/80 transition-colors">
                          <td className="py-3.5 px-4">
                            <span className="font-black text-slate-900 block">
                              Tháng {String(r.billingMonth).padStart(2, "0")}/{r.billingYear}
                            </span>
                            <span className="text-[11px] text-slate-500">
                              {info?.roomName || `Phòng #${r.roomId}`} • Chốt: {formatDate(r.readingDate)}
                            </span>
                          </td>
                          <td className="py-3.5 px-4">
                            <span className="font-mono text-slate-700">
                              {r.electricityOld} → <strong>{r.electricityNew}</strong>
                            </span>
                            <span className="ml-1.5 px-2 py-0.5 rounded-full bg-amber-50 text-amber-700 border border-amber-200 font-bold text-[10px]">
                              {r.electricityUsage} kWh
                            </span>
                          </td>
                          <td className="py-3.5 px-4 text-right text-slate-600">
                            {formatVnd(r.electricityUnitPrice)}/kWh
                          </td>
                          <td className="py-3.5 px-4 text-right font-bold text-slate-900">
                            {formatVnd(r.electricityCost)}
                          </td>
                          <td className="py-3.5 px-4">
                            <span className="font-mono text-slate-700">
                              {r.waterOld} → <strong>{r.waterNew}</strong>
                            </span>
                            <span className="ml-1.5 px-2 py-0.5 rounded-full bg-sky-50 text-sky-700 border border-sky-200 font-bold text-[10px]">
                              {r.waterUsage} m³
                            </span>
                          </td>
                          <td className="py-3.5 px-4 text-right text-slate-600">
                            {formatVnd(r.waterUnitPrice)}/m³
                          </td>
                          <td className="py-3.5 px-4 text-right font-bold text-slate-900">
                            {formatVnd(r.waterCost)}
                          </td>
                          <td className="py-3.5 px-4 text-right font-semibold text-slate-800">
                            {formatVnd(r.serviceCost)}
                          </td>
                          <td className="py-3.5 px-4 text-right font-black text-blue-600">
                            {formatVnd(totalUtilityCost)}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </div>
      ) : selectedAgingGroup ? (
        debtInvoices.length === 0 ? (
          <div className="p-6 sm:p-12 text-center space-y-2 bg-white rounded-2xl border border-slate-200/80 shadow-xs">
            <CheckCircle2 className="w-10 h-10 text-emerald-400 mx-auto" />
            <h4 className="text-sm font-bold text-slate-800">
              Không có hóa đơn nợ trong nhóm này
            </h4>
            <p className="text-xs text-slate-500">
              Bạn không có hóa đơn nào nằm trong nhóm tuổi nợ đã chọn.
            </p>
          </div>
        ) : (
          <div className="space-y-3">
            {debtInvoices.map((debt) => {
              const meta = getInvoiceStatusMeta(debt.status);
              const info = contractMap[debt.contractId];
              const payable = debt.status === "UNPAID" || debt.status === "OVERDUE";

              return (
                <div
                  key={debt.id}
                  className="bg-white rounded-2xl border border-slate-200/80 hover:border-blue-300 shadow-xs p-5 transition-all flex flex-col lg:flex-row lg:items-center justify-between gap-4"
                >
                  <div className="space-y-2 min-w-0">
                    <div className="flex items-center gap-2.5 flex-wrap">
                      <span className="font-mono text-xs font-bold bg-slate-100 px-2.5 py-0.5 rounded-md text-slate-800">
                        {debt.invoiceCode}
                      </span>
                      <span className="text-sm font-black text-slate-900">
                        Hóa đơn Tháng {String(debt.billingMonth).padStart(2, "0")}/{debt.billingYear}
                      </span>
                      <span
                        className={cn(
                          "inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[11px] font-bold border",
                          meta.badgeClass
                        )}
                      >
                        <span className={cn("w-1.5 h-1.5 rounded-full", meta.dotClass)} />
                        {meta.label}
                      </span>
                      {debt.daysOverdue > 0 && (
                        <span className="px-2 py-0.5 rounded-full bg-rose-100 text-rose-700 text-[10px] font-bold">
                          Quá hạn {debt.daysOverdue} ngày
                        </span>
                      )}
                    </div>

                    <div className="flex flex-wrap items-center gap-4 text-xs text-slate-500">
                      <span>
                        Phòng:{" "}
                        <strong className="text-slate-800">
                          {info?.roomName || `Phòng #${debt.roomId}`}
                        </strong>
                      </span>
                      {info?.contractCode && (
                        <span>
                          HĐ: <strong className="text-slate-700">{info.contractCode}</strong>
                        </span>
                      )}
                      <span className="inline-flex items-center gap-1">
                        <Calendar className="w-3.5 h-3.5 text-slate-400" />
                        Hạn đóng: <strong>{formatDate(debt.dueDate)}</strong>
                      </span>
                    </div>
                  </div>

                  <div className="flex flex-wrap items-center justify-between lg:justify-end gap-4 shrink-0">
                    <div className="text-left lg:text-right">
                      <span className="text-[11px] text-slate-400 block">Tổng phải trả</span>
                      <span className="text-base font-black text-rose-600">
                        {formatVnd(debt.totalAmount)}
                      </span>
                    </div>

                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        onClick={() => setDetailInvoiceId(debt.id)}
                        className="px-3.5 py-2 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-800 text-xs font-bold transition-colors inline-flex items-center gap-1.5 cursor-pointer"
                      >
                        <Eye className="w-3.5 h-3.5" />
                        <span>Chi tiết</span>
                      </button>

                      {payable && (
                        <button
                          type="button"
                          disabled={payingInvoiceId === debt.id}
                          onClick={() => handleQuickPay(debt.id)}
                          className="px-3.5 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white text-xs font-bold transition-colors inline-flex items-center gap-1.5 cursor-pointer"
                        >
                          {payingInvoiceId === debt.id ? (
                            <Loader2 className="w-3.5 h-3.5 animate-spin" />
                          ) : (
                            <CreditCard className="w-3.5 h-3.5" />
                          )}
                          <span>Thanh toán VNPay</span>
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )
      ) : invoices.length === 0 ? (
        <div className="p-12 text-center space-y-3 bg-white rounded-2xl border border-slate-200/80 shadow-xs">
          <Receipt className="w-10 h-10 text-slate-300 mx-auto" />
          <h4 className="text-sm font-bold text-slate-800">
            {hasActiveFilter ? "Không tìm thấy hóa đơn phù hợp" : "Chưa có hóa đơn tiền phòng nào"}
          </h4>
          <p className="text-xs text-slate-500 max-w-md mx-auto leading-relaxed">
            {hasActiveFilter
              ? "Thử thay đổi bộ lọc trạng thái, hợp đồng hoặc kỳ tháng/năm để xem các hóa đơn khác."
              : "Khi chủ nhà phát hành hóa đơn tiền thuê phòng và điện nước hàng tháng, thông tin chi tiết và nút thanh toán VNPay sẽ hiển thị tại đây."}
          </p>
        </div>
      ) : (
        <div className="space-y-3">
          {invoices.map((inv) => {
            const meta = getInvoiceStatusMeta(inv.status);
            const info = contractMap[inv.contractId];
            const remaining = getRemainingDebt(inv);
            const payable = isInvoicePayable(inv);

            return (
              <div
                key={inv.id}
                className="bg-white rounded-2xl border border-slate-200/80 hover:border-blue-300 shadow-xs p-5 space-y-4 transition-all"
              >
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 pb-3 border-b border-slate-100">
                  <div className="flex items-center gap-2.5 flex-wrap">
                    <span className="font-mono text-xs font-bold bg-slate-100 px-2.5 py-1 rounded-md text-slate-800">
                      {inv.invoiceCode}
                    </span>
                    <h3 className="font-bold text-slate-900 text-sm sm:text-base">
                      Kỳ tháng {String(inv.billingMonth).padStart(2, "0")}/{inv.billingYear} ·{" "}
                      {info?.roomName || `Phòng #${inv.roomId}`}
                    </h3>
                    {info?.contractCode && (
                      <span className="text-[11px] font-medium text-slate-500">
                        ({info.contractCode})
                      </span>
                    )}
                  </div>

                  <span
                    className={cn(
                      "inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border self-start sm:self-auto",
                      meta.badgeClass
                    )}
                  >
                    <span className={cn("w-1.5 h-1.5 rounded-full", meta.dotClass)} />
                    {meta.label}
                  </span>
                </div>

                {/* Breakdown mini pills */}
                <div className="flex flex-wrap items-center gap-2 text-[11px] text-slate-600">
                  <span className="px-2.5 py-1 rounded-lg bg-slate-50 border border-slate-200/70">
                    Tiền phòng: <strong className="text-slate-900">{formatVnd(inv.rentAmount)}</strong>
                  </span>
                  <span className="px-2.5 py-1 rounded-lg bg-slate-50 border border-slate-200/70">
                    Điện: <strong className="text-slate-900">{formatVnd(inv.electricityAmount)}</strong>
                  </span>
                  <span className="px-2.5 py-1 rounded-lg bg-slate-50 border border-slate-200/70">
                    Nước: <strong className="text-slate-900">{formatVnd(inv.waterAmount)}</strong>
                  </span>
                  {inv.serviceAmount > 0 && (
                    <span className="px-2.5 py-1 rounded-lg bg-slate-50 border border-slate-200/70">
                      Dịch vụ: <strong className="text-slate-900">{formatVnd(inv.serviceAmount)}</strong>
                    </span>
                  )}
                  {inv.discountAmount > 0 && (
                    <span className="px-2.5 py-1 rounded-lg bg-emerald-50 border border-emerald-200/70 text-emerald-700">
                      Giảm: <strong>-{formatVnd(inv.discountAmount)}</strong>
                    </span>
                  )}
                </div>

                {/* Payment Progress & Actions */}
                <div className="grid grid-cols-2 lg:grid-cols-5 gap-4 items-center pt-1 text-xs">
                  <div>
                    <span className="text-slate-400 block font-medium">Phải trả</span>
                    <span className="font-black text-slate-900 text-sm">
                      {formatVnd(inv.totalAmount)}
                    </span>
                  </div>

                  <div>
                    <span className="text-slate-400 block font-medium">Đã trả</span>
                    <span className="font-bold text-emerald-600 text-sm">
                      {formatVnd(inv.paidAmount)}
                    </span>
                  </div>

                  <div>
                    <span className="text-slate-400 block font-medium">Còn nợ</span>
                    <span
                      className={cn(
                        "font-black text-sm",
                        remaining > 0 ? "text-rose-600" : "text-slate-700"
                      )}
                    >
                      {formatVnd(remaining)}
                    </span>
                  </div>

                  <div>
                    <span className="text-slate-400 block font-medium">Hạn thanh toán</span>
                    <span
                      className={cn(
                        "font-semibold",
                        inv.status === "OVERDUE" ? "text-rose-600" : "text-slate-700"
                      )}
                    >
                      {formatDate(inv.dueDate)}
                    </span>
                  </div>

                  <div className="col-span-2 lg:col-span-1 flex items-center justify-start lg:justify-end gap-2">
                    <button
                      type="button"
                      onClick={() => setDetailInvoiceId(inv.id)}
                      className="px-3.5 py-2 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-800 text-xs font-bold transition-colors inline-flex items-center gap-1.5 cursor-pointer"
                    >
                      <Eye className="w-3.5 h-3.5" />
                      <span>Chi tiết</span>
                    </button>

                    {payable && (
                      <button
                        type="button"
                        disabled={payingInvoiceId === inv.id}
                        onClick={() => handleQuickPay(inv.id)}
                        className="px-3.5 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white text-xs font-bold shadow-xs transition-colors inline-flex items-center gap-1.5 cursor-pointer"
                      >
                        {payingInvoiceId === inv.id ? (
                          <Loader2 className="w-3.5 h-3.5 animate-spin" />
                        ) : (
                          <CreditCard className="w-3.5 h-3.5" />
                        )}
                        <span>Thanh toán</span>
                      </button>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* 5. Pagination */}
      {totalCount > PAGE_SIZE && (
        <div className="flex flex-wrap items-center justify-between gap-3 bg-white px-4 py-3 rounded-2xl border border-slate-200/80 text-xs">
          <span className="text-slate-500">
            Trang <strong>{page}</strong> / {totalPages} · Tổng{" "}
            <strong>{totalCount}</strong> hóa đơn
          </span>

          <div className="flex items-center gap-2">
            <button
              type="button"
              disabled={page <= 1 || loading}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              className="px-3 py-1.5 rounded-xl border border-slate-200 bg-white hover:bg-slate-50 disabled:opacity-40 font-bold text-slate-700 inline-flex items-center gap-1 cursor-pointer disabled:cursor-not-allowed"
            >
              <ChevronLeft className="w-3.5 h-3.5" />
              <span>Trước</span>
            </button>
            <button
              type="button"
              disabled={page >= totalPages || loading}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              className="px-3 py-1.5 rounded-xl border border-slate-200 bg-white hover:bg-slate-50 disabled:opacity-40 font-bold text-slate-700 inline-flex items-center gap-1 cursor-pointer disabled:cursor-not-allowed"
            >
              <span>Sau</span>
              <ChevronRight className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      )}

      {/* Detail & Payment History Modal */}
      <InvoiceDetailModal
        invoiceId={detailInvoiceId}
        isOpen={detailInvoiceId !== null}
        onClose={() => setDetailInvoiceId(null)}
        contractMap={contractMap}
        onPaymentUpdated={refreshAll}
      />
    </div>
  );
}
