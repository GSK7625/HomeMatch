"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { useToast } from "@/components/Toast";
import { viewingsApi, getStoredToken } from "@/lib/api-client";
import { ViewingAppointmentDto, BackendViewingStatus } from "@/types";
import {
  Calendar,
  Clock,
  MapPin,
  CheckCircle2,
  XCircle,
  AlertCircle,
  RefreshCw,
  Video,
  ExternalLink,
  MessageSquare,
  Phone,
  User,
  Building2,
  Filter,
  ShieldAlert,
  ArrowRight,
  Eye,
} from "lucide-react";
import { cn } from "@/lib/utils";

const STATUS_BADGES: Record<
  string,
  { label: string; bg: string; text: string; border: string }
> = {
  PENDING: {
    label: "Chờ bạn duyệt",
    bg: "bg-amber-50",
    text: "text-amber-700",
    border: "border-amber-200/80",
  },
  CONFIRMED: {
    label: "Đã duyệt lịch",
    bg: "bg-emerald-50",
    text: "text-emerald-700",
    border: "border-emerald-200/80",
  },
  COMPLETED: {
    label: "Đã hoàn thành",
    bg: "bg-blue-50",
    text: "text-blue-700",
    border: "border-blue-200/80",
  },
  CANCELLED: {
    label: "Đã hủy",
    bg: "bg-slate-100",
    text: "text-slate-600",
    border: "border-slate-200",
  },
};

export default function LandlordViewingsPage() {
  const { user, role } = useAuth();
  const router = useRouter();
  const { showToast } = useToast();

  const [appointments, setAppointments] = useState<ViewingAppointmentDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [statusFilter, setStatusFilter] = useState<string>("ALL");
  const [isLoading, setIsLoading] = useState(true);
  const [actionLoadingId, setActionLoadingId] = useState<number | null>(null);

  // Reject / Cancel Modal
  const [cancelTarget, setCancelTarget] = useState<ViewingAppointmentDto | null>(null);
  const [cancelReason, setCancelReason] = useState("");
  const [isRejecting, setIsRejecting] = useState(false);

  const loadAppointments = useCallback(async () => {
    if (!user) return;
    setIsLoading(true);
    try {
      const token = getStoredToken();
      if (token) {
        const res = await viewingsApi.getAppointments({
          status: statusFilter,
          roleView: "LANDLORD",
          page: 1,
          pageSize: 50,
        });
        if (res.isSuccess && res.data) {
          setAppointments(res.data.items);
          setTotalCount(res.data.totalCount);
          return;
        }
      }

      setAppointments([]);
      setTotalCount(0);
    } finally {
      setIsLoading(false);
    }
  }, [user, statusFilter]);

  useEffect(() => {
    loadAppointments();
  }, [loadAppointments]);

  const handleConfirmViewing = async (item: ViewingAppointmentDto) => {
    setActionLoadingId(item.id);
    try {
      const res = await viewingsApi.confirmAppointment(item.id);
      if (res.isSuccess) {
        showToast("Đã xác nhận lịch hẹn! Khách thuê đã nhận được thông báo.", "success");
        await loadAppointments();
      } else {
        showToast(res.error?.message || "Không thể xác nhận lịch hẹn.", "error");
      }
    } finally {
      setActionLoadingId(null);
    }
  };

  const handleCompleteViewing = async (item: ViewingAppointmentDto) => {
    setActionLoadingId(item.id);
    try {
      const res = await viewingsApi.completeAppointment(item.id);
      if (res.isSuccess) {
        showToast("Đã đánh dấu hoàn thành buổi xem phòng!", "success");
        await loadAppointments();
      } else {
        showToast(res.error?.message || "Không thể cập nhật trạng thái hoàn thành.", "error");
      }
    } finally {
      setActionLoadingId(null);
    }
  };

  const handleConfirmCancel = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!cancelTarget) return;
    setActionLoadingId(cancelTarget.id);
    try {
      const token = getStoredToken();
      if (token) {
        const res = await viewingsApi.cancelAppointment(
          cancelTarget.id,
          cancelReason.trim() || undefined
        );
        if (res.isSuccess) {
          showToast("Đã hủy lịch hẹn xem phòng thành công.", "success");
          setCancelTarget(null);
          setCancelReason("");
          await loadAppointments();
          return;
        } else {
          showToast(res.error?.message || "Không thể hủy lịch hẹn.", "error");
          return;
        }
      }

      showToast("Vui lòng đăng nhập lại để thực hiện thao tác.", "error");
    } finally {
      setActionLoadingId(null);
    }
  };

  if (!user) {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <Calendar className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Kênh Quản Lý Dành Cho Chủ Nhà</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập với tài khoản Chủ nhà để xem và phê duyệt các lịch hẹn xem phòng của khách thuê.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/landlord/viewings"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng nhập tài khoản Chủ nhà</span>
          </Link>
        </div>
      </div>
    );
  }

  if (role === "Tenant") {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-amber-50 text-amber-600 flex items-center justify-center mx-auto shadow-xs">
          <ShieldAlert className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Yêu Cầu Quyền Chủ Nhà</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Tài khoản của bạn hiện là <strong>Người thuê (Tenant)</strong>. Trang này chỉ dành cho Chủ nhà quản lý các lịch hẹn xem phòng thuộc quyền sở hữu của mình.
        </p>
        <div className="pt-2">
          <Link
            href="/tenant/viewings"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Về trang Lịch xem phòng người thuê</span>
          </Link>
        </div>
      </div>
    );
  }

  const pendingCount = appointments.filter((x) => x.status === "PENDING").length;

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Top Header */}
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4 pb-5 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Building2 className="w-4 h-4" />
            <span>Kênh Chủ Nhà • Quản lý lịch hẹn</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Lịch Hẹn Xem Phòng ({totalCount})
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Theo dõi yêu cầu đặt lịch xem phòng từ khách thuê cho các phòng thuộc sở hữu của bạn, phê duyệt và liên hệ trực tiếp.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2.5">
          <button
            type="button"
            onClick={loadAppointments}
            className="p-2 rounded-xl border border-slate-200 text-slate-600 hover:text-blue-600 hover:bg-slate-50 transition-colors"
            title="Làm mới danh sách"
          >
            <RefreshCw className={cn("w-4 h-4", isLoading && "animate-spin")} />
          </button>

          <Link
            href="/landlord/listings"
            className="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-800 text-xs font-bold transition-all"
          >
            <Eye className="w-4 h-4 text-slate-600" />
            <span>Xem tin đăng của tôi</span>
          </Link>

          <Link
            href="/landlord/dashboard"
            className="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Tổng quan tài chính</span>
            <ArrowRight className="w-3.5 h-3.5" />
          </Link>
        </div>
      </div>

      {/* Status Filter Tabs */}
      <div className="flex flex-wrap items-center gap-2">
        {[
          { key: "ALL", label: `Tất cả (${totalCount})` },
          { key: "PENDING", label: `Chờ duyệt (${pendingCount})`, highlight: pendingCount > 0 },
          { key: "CONFIRMED", label: "Đã xác nhận" },
          { key: "COMPLETED", label: "Đã hoàn thành" },
          { key: "CANCELLED", label: "Đã hủy" },
        ].map((tab) => (
          <button
            key={tab.key}
            type="button"
            onClick={() => setStatusFilter(tab.key)}
            className={cn(
              "px-4 py-2 rounded-xl text-xs font-bold border transition-all relative",
              statusFilter === tab.key
                ? "bg-blue-600 text-white border-blue-600 shadow-xs"
                : "bg-white text-slate-600 border-slate-200 hover:bg-slate-50",
              tab.highlight && statusFilter !== tab.key && "border-amber-300 text-amber-700 bg-amber-50/50"
            )}
          >
            {tab.label}
            {tab.highlight && statusFilter !== tab.key && (
              <span className="w-2 h-2 rounded-full bg-amber-500 absolute -top-1 -right-1" />
            )}
          </button>
        ))}
      </div>

      {/* Appointments List */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {[1, 2, 3, 4].map((i) => (
            <div
              key={i}
              className="bg-white rounded-2xl border border-slate-200 p-6 animate-pulse space-y-3"
            >
              <div className="h-5 bg-slate-100 rounded w-1/3" />
              <div className="h-4 bg-slate-100 rounded w-2/3" />
              <div className="h-16 bg-slate-50 rounded-xl" />
            </div>
          ))}
        </div>
      ) : appointments.length === 0 ? (
        <div className="bg-white rounded-3xl border border-slate-200/80 p-14 text-center space-y-4 shadow-xs">
          <div className="w-16 h-16 rounded-2xl bg-slate-50 text-slate-400 flex items-center justify-center mx-auto">
            <Calendar className="w-8 h-8" />
          </div>
          <div className="space-y-1">
            <h3 className="text-base font-bold text-slate-800">
              Không có lịch hẹn xem phòng nào
            </h3>
            <p className="text-xs text-slate-500 max-w-md mx-auto leading-relaxed">
              {statusFilter === "ALL"
                ? "Hiện chưa có khách thuê nào đặt lịch xem các phòng trọ của bạn."
                : `Không có lịch hẹn nào ở trạng thái '${statusFilter}'.`}
            </p>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6">
          {appointments.map((item) => {
            const badge = STATUS_BADGES[item.status] || STATUS_BADGES.PENDING;
            const appDate = new Date(item.appointmentTime);
            const timeStr = appDate.toLocaleTimeString("vi-VN", {
              hour: "2-digit",
              minute: "2-digit",
            });
            const dateStr = appDate.toLocaleDateString("vi-VN", {
              weekday: "short",
              day: "2-digit",
              month: "2-digit",
              year: "numeric",
            });

            const isBusy = actionLoadingId === item.id;

            return (
              <div
                key={item.id}
                className="bg-white rounded-2xl border border-slate-200/90 hover:border-blue-200 shadow-xs hover:shadow-md transition-all p-5 flex flex-col justify-between gap-4"
              >
                <div className="space-y-3.5">
                  {/* Top Status & Viewing Format Badge */}
                  <div className="flex items-center justify-between gap-2 flex-wrap">
                    <div className="flex items-center gap-2">
                      <span
                        className={cn(
                          "px-3 py-1 rounded-full text-xs font-bold border",
                          badge.bg,
                          badge.text,
                          badge.border
                        )}
                      >
                        {badge.label}
                      </span>
                      {item.virtualViewing ? (
                        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-indigo-50 text-indigo-700 border border-indigo-200/80">
                          <Video className="w-3 h-3 text-indigo-600" />
                          <span>Video Call Trực Tuyến</span>
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-slate-100 text-slate-700 border border-slate-200">
                          <MapPin className="w-3 h-3 text-slate-500" />
                          <span>Xem Trực Tiếp Tại Phòng</span>
                        </span>
                      )}
                    </div>

                    <span className="text-[11px] text-slate-400 font-medium">
                      Mã #{item.id} • {new Date(item.createdAt).toLocaleDateString("vi-VN")}
                    </span>
                  </div>

                  {/* Room Info */}
                  <div>
                    <Link href={`/listings/lst-00${item.listingId}`} target="_blank">
                      <h3 className="font-bold text-slate-900 text-base hover:text-blue-600 transition-colors line-clamp-1">
                        {item.listingTitle}
                      </h3>
                    </Link>
                    <div className="flex items-center justify-between gap-2 mt-1">
                      <div className="flex items-center gap-1.5 text-xs text-slate-500 min-w-0">
                        <MapPin className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                        <span className="truncate">
                          {item.listingAddress || "TP. Hồ Chí Minh"}
                        </span>
                      </div>
                      {item.listingPrice > 0 && (
                        <span className="text-xs font-black text-blue-600 shrink-0">
                          {item.listingPrice.toLocaleString("vi-VN")} đ/tháng
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Scheduled Time Box */}
                  <div className="p-3.5 bg-blue-50/50 border border-blue-100 rounded-xl space-y-2 text-xs">
                    <div className="flex items-center justify-between gap-2 text-blue-950 font-bold">
                      <div className="flex items-center gap-2">
                        <Clock className="w-4 h-4 text-blue-600 shrink-0" />
                        <span>
                          {timeStr} — {dateStr}
                        </span>
                      </div>
                      <span className="px-2 py-0.5 rounded-md bg-blue-100/80 text-blue-800 text-[11px]">
                        {item.durationMinutes} phút
                      </span>
                    </div>

                    {item.notes && (
                      <p className="text-xs text-slate-600 bg-white/90 p-2.5 rounded-lg border border-slate-200/60">
                        <span className="font-bold text-slate-800">Ghi chú của khách:</span> {item.notes}
                      </p>
                    )}

                    {item.virtualViewing && (
                      <div className="p-2.5 rounded-lg bg-indigo-50/90 border border-indigo-200/80 flex items-center justify-between gap-2">
                        <div className="flex items-center gap-2 min-w-0">
                          <Video className="w-4 h-4 text-indigo-600 shrink-0" />
                          <span className="text-[11px] font-semibold text-indigo-950 truncate">
                            Phòng họp: {item.virtualViewing.roomUrl}
                          </span>
                        </div>
                        <a
                          href={item.virtualViewing.roomUrl}
                          target="_blank"
                          rel="noreferrer"
                          className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md bg-indigo-600 text-white text-[11px] font-bold hover:bg-indigo-700 shrink-0"
                        >
                          <span>Mở phòng họp</span>
                          <ExternalLink className="w-3 h-3" />
                        </a>
                      </div>
                    )}
                  </div>

                  {/* Tenant Information Row */}
                  <div className="p-3 rounded-xl bg-slate-50 border border-slate-100 text-xs space-y-1">
                    <span className="text-[10px] uppercase font-bold text-slate-400 block">
                      Khách thuê đặt lịch
                    </span>
                    <div className="flex items-center justify-between gap-2 flex-wrap">
                      <div className="flex items-center gap-2">
                        <div className="w-7 h-7 rounded-full bg-blue-100 text-blue-700 font-bold flex items-center justify-center text-xs">
                          {item.tenantName?.charAt(0) || "T"}
                        </div>
                        <div>
                          <span className="font-bold text-slate-800 block">
                            {item.tenantName}
                          </span>
                          <span className="text-[11px] text-slate-400 block">
                            Mã khách thuê #{item.tenantId}
                          </span>
                        </div>
                      </div>

                      {item.tenantPhone && (
                        <a
                          href={`tel:${item.tenantPhone}`}
                          className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-emerald-50 text-emerald-700 hover:bg-emerald-100 border border-emerald-200 text-xs font-semibold transition-colors"
                        >
                          <Phone className="w-3 h-3 text-emerald-600" />
                          <span>{item.tenantPhone}</span>
                        </a>
                      )}
                    </div>
                  </div>

                  {/* Cancellation Reason if Cancelled */}
                  {item.status === "CANCELLED" && (
                    <div className="p-3 rounded-xl bg-rose-50/70 border border-rose-200/80 text-xs text-rose-800 flex items-start gap-2">
                      <AlertCircle className="w-4 h-4 text-rose-600 shrink-0 mt-0.5" />
                      <div>
                        <span className="font-bold">
                          Đã hủy bởi {item.cancelledBy === "LANDLORD" ? "Bạn (Chủ nhà)" : "Khách thuê"}:
                        </span>{" "}
                        <span>{item.cancelledReason || "Không có lý do cụ thể."}</span>
                      </div>
                    </div>
                  )}
                </div>

                {/* Landlord Action Buttons */}
                <div className="pt-3.5 border-t border-slate-100 flex flex-wrap items-center justify-between gap-2">
                  <button
                    type="button"
                    onClick={() => router.push(`/messages?listingId=${item.listingId}`)}
                    className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-800 text-xs font-bold transition-colors"
                  >
                    <MessageSquare className="w-3.5 h-3.5 text-blue-600" />
                    <span>Nhắn tin với khách</span>
                  </button>

                  <div className="flex items-center gap-2">
                    {/* Approve when PENDING */}
                    {item.status === "PENDING" && (
                      <button
                        type="button"
                        disabled={isBusy}
                        onClick={() => handleConfirmViewing(item)}
                        className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold transition-colors disabled:opacity-50 shadow-xs"
                      >
                        <CheckCircle2 className="w-3.5 h-3.5" />
                        <span>Duyệt lịch hẹn</span>
                      </button>
                    )}

                    {/* Mark Completed when CONFIRMED */}
                    {item.status === "CONFIRMED" && (
                      <button
                        type="button"
                        disabled={isBusy}
                        onClick={() => handleCompleteViewing(item)}
                        className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold transition-colors disabled:opacity-50 shadow-xs"
                      >
                        <CheckCircle2 className="w-3.5 h-3.5" />
                        <span>Đã xem xong</span>
                      </button>
                    )}

                    {/* Reject / Cancel when PENDING or CONFIRMED */}
                    {(item.status === "PENDING" || item.status === "CONFIRMED") && (
                      <button
                        type="button"
                        disabled={isBusy}
                        onClick={() => {
                          setCancelTarget(item);
                          setIsRejecting(item.status === "PENDING");
                          setCancelReason("");
                        }}
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl border border-rose-200 text-rose-600 hover:bg-rose-50 text-xs font-bold transition-colors disabled:opacity-50"
                      >
                        <XCircle className="w-3.5 h-3.5" />
                        <span>{item.status === "PENDING" ? "Từ chối" : "Hủy lịch"}</span>
                      </button>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Reject / Cancel Modal */}
      {cancelTarget && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-md w-full p-6 shadow-2xl border border-slate-100 space-y-4 animate-in fade-in zoom-in-95">
            <div className="flex items-start justify-between">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-2xl bg-rose-50 text-rose-600 flex items-center justify-center shrink-0">
                  <XCircle className="w-5 h-5" />
                </div>
                <div>
                  <h3 className="text-base font-black text-slate-900">
                    {isRejecting ? "Từ Chối Lịch Hẹn Xem Phòng" : "Hủy Lịch Hẹn Xem Phòng"}
                  </h3>
                  <p className="text-xs text-slate-500">Mã lịch hẹn #{cancelTarget.id}</p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setCancelTarget(null)}
                className="text-slate-400 hover:text-slate-600 p-1"
              >
                ✕
              </button>
            </div>

            <p className="text-xs text-slate-600 leading-relaxed">
              Bạn có chắc chắn muốn {isRejecting ? "từ chối" : "hủy"} lịch hẹn của khách thuê{" "}
              <strong>{cancelTarget.tenantName}</strong> vào lúc{" "}
              <strong>
                {new Date(cancelTarget.appointmentTime).toLocaleTimeString("vi-VN", {
                  hour: "2-digit",
                  minute: "2-digit",
                })}{" "}
                ngày{" "}
                {new Date(cancelTarget.appointmentTime).toLocaleDateString("vi-VN")}
              </strong>{" "}
              không? Khách thuê sẽ nhận được thông báo ngay lập tức.
            </p>

            <form onSubmit={handleConfirmCancel} className="space-y-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">
                  Lý do {isRejecting ? "từ chối" : "hủy"} (tùy chọn nhưng nên có):
                </label>
                <textarea
                  rows={3}
                  value={cancelReason}
                  onChange={(e) => setCancelReason(e.target.value)}
                  placeholder={
                    isRejecting
                      ? "VD: Khung giờ này tôi bận việc đột xuất, xin vui lòng đổi sang 15h..."
                      : "VD: Phòng đã có khách cọc trước, xin phép hủy lịch hẹn..."
                  }
                  className="w-full text-xs rounded-xl border border-slate-200 p-3 focus:outline-none focus:ring-2 focus:ring-rose-500/20 focus:border-rose-500"
                />
              </div>

              {/* Quick Reason Suggestions */}
              <div className="flex flex-wrap gap-1.5">
                {[
                  "Bận việc đột xuất",
                  "Phòng đã có khách cọc giữ chỗ",
                  "Xin đổi sang khung giờ khác",
                ].map((reason, idx) => (
                  <button
                    key={idx}
                    type="button"
                    onClick={() => setCancelReason(reason)}
                    className="px-2.5 py-1 rounded-lg text-[11px] bg-slate-100 hover:bg-slate-200 text-slate-700 transition-colors"
                  >
                    + {reason}
                  </button>
                ))}
              </div>

              <div className="flex items-center justify-end gap-2.5 pt-2">
                <button
                  type="button"
                  onClick={() => setCancelTarget(null)}
                  className="px-4 py-2 rounded-xl border border-slate-200 text-xs font-bold text-slate-600 hover:bg-slate-50 transition-colors"
                >
                  Đóng
                </button>
                <button
                  type="submit"
                  disabled={actionLoadingId === cancelTarget.id}
                  className="px-5 py-2 rounded-xl bg-rose-600 hover:bg-rose-700 text-white text-xs font-bold shadow-md shadow-rose-500/20 transition-all disabled:opacity-50"
                >
                  {actionLoadingId === cancelTarget.id
                    ? "Đang xử lý..."
                    : isRejecting
                    ? "Xác nhận từ chối"
                    : "Xác nhận hủy lịch"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
