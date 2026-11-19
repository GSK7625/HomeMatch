"use client";

import React, { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { useToast } from "@/components/Toast";
import { propertiesApi } from "@/lib/api-client";
import { leasingApi } from "@/lib/leasing-api";
import {
  formatDate,
  formatVnd,
  getDaysRemaining,
  getDepositMeta,
  getStatusMeta,
  isDeletableContractStatus,
  isHandoverStatus,
  isRestorableContractStatus,
  STATUS_FILTER_TABS,
} from "@/features/leasing/leasing-helpers";
import { parseContractSnapshot } from "@/features/leasing/contract-snapshot";
import { CreateContractModal, RoomOption } from "@/features/leasing/CreateContractModal";
import { LeasingContractDto } from "@/types";
import { cn } from "@/lib/utils";
import {
  AlertTriangle,
  Calendar,
  Clock,
  Eye,
  FileCheck2,
  FileText,
  KeyRound,
  Plus,
  RefreshCw,
  RotateCcw,
  Search,
  Sparkles,
  Trash2,
  User,
} from "lucide-react";

export default function LandlordContractsPage() {
  const router = useRouter();
  const { user } = useAuth();
  const { showToast } = useToast();

  const [contracts, setContracts] = useState<LeasingContractDto[]>([]);
  const [expiring, setExpiring] = useState<LeasingContractDto[]>([]);
  const [roomOptions, setRoomOptions] = useState<RoomOption[]>([]);
  const [loading, setLoading] = useState(true);

  const [activeTab, setActiveTab] = useState("ALL");
  const [search, setSearch] = useState("");
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [deletingId, setDeletingId] = useState<number | null>(null);
  const [restoringId, setRestoringId] = useState<number | null>(null);

  const handleRestoreContract = async (ctr: LeasingContractDto) => {
    setRestoringId(ctr.id);
    try {
      const res = await leasingApi.restoreContract(ctr.id);
      if (res.isSuccess) {
        showToast(`Đã khôi phục ${ctr.contractCode} về Bản nháp!`, "success");
        await loadContracts();
      } else {
        showToast(res.error?.message || "Không thể khôi phục hợp đồng (có thể bị trùng lịch phòng).", "error");
      }
    } finally {
      setRestoringId(null);
    }
  };

  const handleDeleteContract = async (ctr: LeasingContractDto) => {
    if (!window.confirm(`Xóa vĩnh viễn hợp đồng ${ctr.contractCode}? Thao tác này không thể hoàn tác.`)) {
      return;
    }
    setDeletingId(ctr.id);
    try {
      const res = await leasingApi.deleteContract(ctr.id);
      if (res.isSuccess) {
        showToast(`Đã xóa hợp đồng ${ctr.contractCode}!`, "success");
        setContracts((prev) => prev.filter((c) => c.id !== ctr.id));
      } else {
        showToast(res.error?.message || "Không thể xóa hợp đồng này.", "error");
      }
    } finally {
      setDeletingId(null);
    }
  };

  const roomMap = useMemo(() => {
    const map = new Map<number, string>();
    roomOptions.forEach(({ room, property }) => {
      map.set(room.id, `${property.name} · P.${room.roomNumber}`);
    });
    return map;
  }, [roomOptions]);

  const loadRooms = useCallback(async () => {
    const propsRes = await propertiesApi.getMyProperties();
    if (!propsRes.isSuccess || !propsRes.data) return;

    const allOptions: RoomOption[] = [];
    await Promise.all(
      propsRes.data.map(async (property) => {
        const roomsRes = await propertiesApi.getRooms(property.id);
        if (roomsRes.isSuccess && roomsRes.data) {
          roomsRes.data.forEach((room) => allOptions.push({ room, property }));
        }
      })
    );
    setRoomOptions(allOptions);
  }, []);

  const loadContracts = useCallback(async () => {
    setLoading(true);
    try {
      const [listRes, expiringRes] = await Promise.all([
        leasingApi.getContracts({ roleView: "LANDLORD", page: 1, pageSize: 100 }),
        leasingApi.getExpiringContracts(30),
      ]);

      if (listRes.isSuccess && listRes.data) {
        setContracts(listRes.data.items);
      } else {
        showToast(listRes.error?.message || "Không tải được danh sách hợp đồng.", "error");
      }

      if (expiringRes.isSuccess && expiringRes.data) {
        setExpiring(expiringRes.data);
      }
    } finally {
      setLoading(false);
    }
  }, [showToast]);

  useEffect(() => {
    if (!user || user.role === "Tenant") return;
    loadContracts();
    loadRooms();
  }, [user, loadContracts, loadRooms]);

  const expiringIds = useMemo(() => new Set(expiring.map((c) => c.id)), [expiring]);

  const kpi = useMemo(() => {
    const active = contracts.filter((c) => c.status === "ACTIVE");
    const pendingSign = contracts.filter((c) => c.status === "PENDING_SIGNATURE");
    const handover = contracts.filter((c) => isHandoverStatus(c.status));
    const totalRent = active.reduce((sum, c) => sum + c.monthlyRent, 0);
    return {
      activeCount: active.length,
      totalRent,
      pendingSignCount: pendingSign.length,
      expiringCount: expiring.length,
      handoverCount: handover.length,
    };
  }, [contracts, expiring]);

  const filteredContracts = useMemo(() => {
    const q = search.trim().toLowerCase();
    return contracts.filter((c) => {
      if (activeTab === "EXPIRING" && !expiringIds.has(c.id)) return false;
      else if (activeTab === "HANDOVER" && !isHandoverStatus(c.status)) return false;
      else if (
        activeTab !== "ALL" &&
        activeTab !== "EXPIRING" &&
        activeTab !== "HANDOVER" &&
        c.status !== activeTab
      ) {
        return false;
      }

      if (!q) return true;
      const snap = parseContractSnapshot(c.documentSnapshot);
      const roomText = (roomMap.get(c.roomId) || snap.roomName || `room #${c.roomId}`).toLowerCase();
      const tenantText = (snap.tenant?.fullName || `khach #${c.tenantId}`).toLowerCase();
      return (
        c.contractCode.toLowerCase().includes(q) ||
        roomText.includes(q) ||
        tenantText.includes(q) ||
        String(c.tenantId).includes(q)
      );
    });
  }, [contracts, activeTab, search, expiringIds, roomMap]);

  if (!user || user.role === "Tenant") {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <FileText className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Quản Lý Hợp Đồng Cho Thuê</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập với tài khoản <strong>Chủ nhà (Landlord)</strong> để soạn thảo, ký điện tử OTP và theo dõi tiền cọc hợp đồng.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/landlord/contracts"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20"
          >
            Đăng nhập tài khoản Chủ nhà
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-5 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <FileText className="w-4 h-4" />
            <span>Nghiệp vụ hợp đồng điện tử & tiền cọc · Leasing</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Quản Lý Hợp Đồng Cho Thuê
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Soạn hợp đồng điện tử chuẩn pháp lý A4, ký OTP 2 bên, quản lý sổ cái tiền cọc và bàn giao phòng.
          </p>
        </div>

        <div className="flex items-center gap-2 flex-wrap">
          <Link
            href="/landlord/contracts/demo"
            className="inline-flex items-center gap-1.5 px-3.5 py-2.5 rounded-xl border border-blue-200 bg-blue-50/70 hover:bg-blue-100/70 text-blue-700 text-xs font-bold transition-colors"
          >
            <Sparkles className="w-3.5 h-3.5" />
            <span>Xem bản mẫu HĐ điện tử</span>
          </Link>
          <button
            onClick={loadContracts}
            disabled={loading}
            className="p-2.5 rounded-xl border border-slate-200 bg-white hover:bg-slate-50 text-slate-600 transition-colors"
            title="Làm mới danh sách"
          >
            <RefreshCw className={cn("w-4 h-4", loading && "animate-spin")} />
          </button>
          <button
            onClick={() => setIsCreateOpen(true)}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20 transition-all"
          >
            <Plus className="w-4 h-4" />
            <span>Soạn hợp đồng mới</span>
          </button>
        </div>
      </div>

      {/* 4 KPI Summary Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard
          title="Đang hiệu lực"
          value={String(kpi.activeCount)}
          sub={`Doanh thu thuê: ${formatVnd(kpi.totalRent)}/tháng`}
          icon={FileCheck2}
          tone="emerald"
          onClick={() => setActiveTab("ACTIVE")}
        />
        <KpiCard
          title="Chờ ký điện tử OTP"
          value={String(kpi.pendingSignCount)}
          sub="Cần xác thực 2 bên qua email"
          icon={KeyRound}
          tone="amber"
          onClick={() => setActiveTab("PENDING_SIGNATURE")}
        />
        <KpiCard
          title="Sắp hết hạn (≤ 30 ngày)"
          value={String(kpi.expiringCount)}
          sub="Cần gia hạn hoặc chuẩn bị bàn giao"
          icon={AlertTriangle}
          tone="orange"
          onClick={() => setActiveTab("EXPIRING")}
        />
        <KpiCard
          title="Chờ bàn giao phòng"
          value={String(kpi.handoverCount)}
          sub="Hết hạn hoặc chấm dứt trước hạn"
          icon={Clock}
          tone="rose"
          onClick={() => setActiveTab("HANDOVER")}
        />
      </div>

      {/* Expiring Banner */}
      {expiring.length > 0 && activeTab !== "EXPIRING" && (
        <div className="px-4 py-3 rounded-2xl bg-amber-50 border border-amber-200 flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-xs">
          <div className="flex items-center gap-2.5 text-amber-900 font-medium">
            <AlertTriangle className="w-4 h-4 text-amber-600 shrink-0" />
            <span>
              Có <strong>{expiring.length} hợp đồng</strong> sắp hết hạn trong vòng 30 ngày tới.
            </span>
          </div>
          <button
            onClick={() => setActiveTab("EXPIRING")}
            className="text-xs font-bold text-amber-800 hover:text-amber-950 underline self-start sm:self-auto"
          >
            Xem danh sách sắp hết hạn →
          </button>
        </div>
      )}

      {/* Filter Tabs & Search */}
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-3">
        <div className="flex items-center gap-1.5 overflow-x-auto pb-1">
          {STATUS_FILTER_TABS.map((t) => (
            <button
              key={t.key}
              onClick={() => setActiveTab(t.key)}
              className={cn(
                "px-3.5 py-2 rounded-xl text-xs font-bold whitespace-nowrap transition-all border",
                activeTab === t.key
                  ? "bg-slate-900 text-white border-slate-900 shadow-xs"
                  : "bg-white text-slate-600 border-slate-200 hover:bg-slate-50"
              )}
            >
              {t.label}
            </button>
          ))}
        </div>

        <div className="relative w-full lg:w-72">
          <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            placeholder="Tìm mã HĐ, phòng, khách thuê..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full pl-9 pr-3.5 py-2 text-xs rounded-xl border border-slate-200 bg-white focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
          />
        </div>
      </div>

      {/* Contracts Table */}
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
        {loading ? (
          <div className="p-8 space-y-3">
            {[1, 2, 3].map((i) => (
              <div key={i} className="h-14 rounded-xl bg-slate-100 animate-pulse" />
            ))}
          </div>
        ) : filteredContracts.length === 0 ? (
          <div className="py-16 px-6 text-center space-y-3">
            <div className="w-14 h-14 rounded-2xl bg-slate-100 text-slate-400 flex items-center justify-center mx-auto">
              <FileText className="w-7 h-7" />
            </div>
            <h3 className="text-sm font-bold text-slate-900">
              {contracts.length === 0
                ? "Chưa có hợp đồng cho thuê nào"
                : "Không tìm thấy hợp đồng phù hợp bộ lọc"}
            </h3>
            <p className="text-xs text-slate-500 max-w-sm mx-auto">
              {contracts.length === 0
                ? "Tạo hợp đồng điện tử đầu tiên hoặc xem trước giao diện phòng ký hợp đồng mẫu."
                : "Thử đổi tab trạng thái hoặc xóa từ khóa tìm kiếm."}
            </p>
            {contracts.length === 0 && (
              <div className="flex items-center justify-center gap-2 pt-1 flex-wrap">
                <button
                  onClick={() => setIsCreateOpen(true)}
                  className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold"
                >
                  <Plus className="w-4 h-4" />
                  <span>Soạn hợp đồng đầu tiên</span>
                </button>
                <Link
                  href="/landlord/contracts/demo"
                  className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl border border-slate-200 hover:bg-slate-50 text-slate-700 text-xs font-bold"
                >
                  <Sparkles className="w-4 h-4 text-blue-600" />
                  <span>Xem mẫu HĐ điện tử A4</span>
                </Link>
              </div>
            )}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs text-left border-collapse min-w-[920px]">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50/80 text-slate-500 uppercase tracking-wider text-[11px]">
                  <th className="px-4 py-3.5">Mã HĐ & Phòng</th>
                  <th className="px-4 py-3.5">Khách thuê</th>
                  <th className="px-4 py-3.5">Thời hạn & Kỳ đóng</th>
                  <th className="px-4 py-3.5">Giá thuê / tháng</th>
                  <th className="px-4 py-3.5">Tiền cọc</th>
                  <th className="px-4 py-3.5">Trạng thái</th>
                  <th className="px-4 py-3.5 text-right w-[184px]">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filteredContracts.map((ctr) => {
                  const statusMeta = getStatusMeta(ctr.status);
                  const depositMeta = getDepositMeta(ctr.depositStatus);
                  const daysLeft = getDaysRemaining(ctr.endDate);
                  const snap = parseContractSnapshot(ctr.documentSnapshot);
                  const roomTitle = roomMap.get(ctr.roomId) || snap.roomName || `Phòng #${ctr.roomId}`;
                  const tenantLabel = snap.tenant?.fullName || `Khách #${ctr.tenantId}`;
                  const detailHref = `/landlord/contracts/${ctr.id}`;

                  return (
                    <tr
                      key={ctr.id}
                      onClick={() => router.push(detailHref)}
                      className="hover:bg-slate-50/70 transition-colors cursor-pointer"
                    >
                      <td className="px-4 py-4">
                        <div className="space-y-1">
                          <div className="flex items-center gap-1.5">
                            <span className="font-mono text-[11px] font-bold bg-slate-100 px-2 py-0.5 rounded-md text-slate-800">
                              {ctr.contractCode}
                            </span>
                            {ctr.contractFileUrl && (
                              <span className="text-[10px] font-bold px-1.5 py-0.5 rounded bg-blue-50 text-blue-600">
                                PDF
                              </span>
                            )}
                          </div>
                          <div className="font-bold text-slate-900 line-clamp-1">{roomTitle}</div>
                        </div>
                      </td>

                      <td className="px-4 py-4">
                        <div className="flex items-center gap-1.5 font-semibold text-slate-800">
                          <User className="w-3.5 h-3.5 text-slate-400" />
                          <span>{tenantLabel}</span>
                        </div>
                        {snap.tenant?.phone && (
                          <div className="text-[11px] text-slate-400 mt-0.5">{snap.tenant.phone}</div>
                        )}
                      </td>

                      <td className="px-4 py-4">
                        <div className="font-semibold text-slate-800 flex items-center gap-1">
                          <Calendar className="w-3.5 h-3.5 text-slate-400" />
                          <span>
                            {formatDate(ctr.startDate)} → {formatDate(ctr.endDate)}
                          </span>
                        </div>
                        <div className="text-[11px] text-slate-500 mt-0.5 flex items-center gap-2">
                          <span>Đóng ngày {ctr.billingDueDay}</span>
                          {ctr.status === "ACTIVE" && daysLeft >= 0 && (
                            <span className={cn("font-bold", daysLeft <= 30 ? "text-amber-600" : "text-slate-400")}>
                              · Còn {daysLeft} ngày
                            </span>
                          )}
                        </div>
                      </td>

                      <td className="px-4 py-4 font-black text-blue-600 text-sm">
                        {formatVnd(ctr.monthlyRent)}
                      </td>

                      <td className="px-4 py-4">
                        <div className="font-bold text-slate-900">{formatVnd(ctr.depositAmount)}</div>
                        <span
                          className={cn(
                            "inline-block mt-1 px-2 py-0.5 rounded-full text-[10px] font-bold border",
                            depositMeta.className
                          )}
                        >
                          {depositMeta.label}
                        </span>
                      </td>

                      <td className="px-4 py-4">
                        <span
                          className={cn(
                            "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold border",
                            statusMeta.badgeClass
                          )}
                        >
                          <span className={cn("w-1.5 h-1.5 rounded-full", statusMeta.dotClass)} />
                          {statusMeta.label}
                        </span>

                        {ctr.status === "PENDING_SIGNATURE" && (
                          <div className="flex items-center gap-2 mt-1 text-[10px] font-semibold text-slate-500">
                            <span className={ctr.landlordSignedAt ? "text-emerald-600" : "text-amber-600"}>
                              Chủ {ctr.landlordSignedAt ? "✓" : "○"}
                            </span>
                            <span>·</span>
                            <span className={ctr.tenantSignedAt ? "text-emerald-600" : "text-amber-600"}>
                              Khách {ctr.tenantSignedAt ? "✓" : "○"}
                            </span>
                          </div>
                        )}
                      </td>

                      <td className="px-4 py-4 text-right whitespace-nowrap" onClick={(e) => e.stopPropagation()}>
                        <div className="inline-flex items-center justify-end gap-1.5">
                          <Link
                            href={detailHref}
                            className={cn(
                              "w-[88px] h-8 inline-flex items-center justify-center gap-1.5 rounded-lg text-[11px] font-bold transition-colors shrink-0",
                              ctr.status === "PENDING_SIGNATURE" && !ctr.landlordSignedAt
                                ? "bg-blue-600 hover:bg-blue-700 text-white shadow-xs"
                                : ctr.status === "SIGNED"
                                ? "bg-emerald-600 hover:bg-emerald-700 text-white shadow-xs"
                                : "border border-slate-200 bg-white hover:bg-slate-100 text-slate-700"
                            )}
                          >
                            <Eye className="w-3.5 h-3.5 shrink-0" />
                            <span>
                              {ctr.status === "PENDING_SIGNATURE" && !ctr.landlordSignedAt
                                ? "Ký OTP"
                                : ctr.status === "SIGNED"
                                ? "Kích hoạt"
                                : "Mở HĐ"}
                            </span>
                          </Link>

                          <div className="w-8 h-8 flex items-center justify-center shrink-0">
                            {isRestorableContractStatus(ctr.status) && (
                              <button
                                type="button"
                                onClick={() => handleRestoreContract(ctr)}
                                disabled={restoringId === ctr.id}
                                className="w-8 h-8 inline-flex items-center justify-center rounded-lg border border-blue-200 text-blue-600 hover:bg-blue-50 disabled:opacity-50 transition-colors"
                                title="Khôi phục về Bản nháp"
                              >
                                <RotateCcw className="w-3.5 h-3.5" />
                              </button>
                            )}
                          </div>

                          <div className="w-8 h-8 flex items-center justify-center shrink-0">
                            {isDeletableContractStatus(ctr.status) && (
                              <button
                                type="button"
                                onClick={() => handleDeleteContract(ctr)}
                                disabled={deletingId === ctr.id}
                                className="w-8 h-8 inline-flex items-center justify-center rounded-lg border border-rose-200 text-rose-600 hover:bg-rose-50 disabled:opacity-50 transition-colors"
                                title="Xóa hợp đồng"
                              >
                                <Trash2 className="w-3.5 h-3.5" />
                              </button>
                            )}
                          </div>
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

      <CreateContractModal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        roomOptions={roomOptions}
        onCreated={(created) => {
          loadContracts();
          router.push(`/landlord/contracts/${created.id}`);
        }}
      />
    </div>
  );
}

function KpiCard({
  title,
  value,
  sub,
  icon: Icon,
  tone,
  onClick,
}: {
  title: string;
  value: string;
  sub: string;
  icon: React.ElementType;
  tone: "emerald" | "amber" | "orange" | "rose";
  onClick: () => void;
}) {
  const tones = {
    emerald: "bg-emerald-50 text-emerald-600",
    amber: "bg-amber-50 text-amber-600",
    orange: "bg-orange-50 text-orange-600",
    rose: "bg-rose-50 text-rose-600",
  };

  return (
    <button
      onClick={onClick}
      className="text-left p-4 rounded-2xl bg-white border border-slate-200/80 shadow-xs hover:border-blue-300 hover:shadow-sm transition-all flex items-start justify-between gap-3"
    >
      <div className="min-w-0">
        <div className="text-[11px] font-bold text-slate-500 uppercase tracking-wide">{title}</div>
        <div className="text-2xl font-black text-slate-900 mt-1">{value}</div>
        <div className="text-[11px] text-slate-400 mt-1 truncate">{sub}</div>
      </div>
      <div className={cn("w-10 h-10 rounded-xl flex items-center justify-center shrink-0", tones[tone])}>
        <Icon className="w-5 h-5" />
      </div>
    </button>
  );
}
