"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { listingService, viewingService } from "@/lib/services";
import { getStoredToken, parseListingNumericId, viewingsApi } from "@/lib/api-client";
import { BackendViewingStatus, ViewingAppointmentDto } from "@/types";
import { useToast } from "@/components/Toast";
import { Modal } from "@/components/Modal";
import {
  Calendar,
  Clock,
  MapPin,
  CheckCircle2,
  XCircle,
  ArrowRight,
  Phone,
  Video,
  MessageSquare,
  Plus,
  RefreshCw,
  User,
  ExternalLink,
  AlertCircle,
  Search,
} from "lucide-react";
import { cn } from "@/lib/utils";

const STATUS_CONFIG: Record<
  BackendViewingStatus,
  { label: string; badgeClass: string; dotClass: string }
> = {
  PENDING: {
    label: "Chờ chủ nhà xác nhận",
    badgeClass: "bg-amber-50 text-amber-700 border-amber-200",
    dotClass: "bg-amber-500",
  },
  CONFIRMED: {
    label: "Đã xác nhận lịch hẹn",
    badgeClass: "bg-emerald-50 text-emerald-700 border-emerald-200",
    dotClass: "bg-emerald-500",
  },
  COMPLETED: {
    label: "Đã hoàn thành xem phòng",
    badgeClass: "bg-blue-50 text-blue-700 border-blue-200",
    dotClass: "bg-blue-500",
  },
  CANCELLED: {
    label: "Đã hủy lịch hẹn",
    badgeClass: "bg-rose-50 text-rose-700 border-rose-200",
    dotClass: "bg-rose-500",
  },
};

function mapLocalMockToDto(): ViewingAppointmentDto[] {
  const localItems = viewingService.getAll();
  return localItems.map((item, idx) => {
    const statusMap: Record<string, BackendViewingStatus> = {
      "Chờ xác nhận": "PENDING",
      "Đã xác nhận": "CONFIRMED",
      "Đã hoàn thành": "COMPLETED",
      "Đã hủy": "CANCELLED",
    };
    return {
      id: idx + 1,
      tenantId: 3,
      tenantName: item.tenantName || "Trần Bình An",
      tenantPhone: item.tenantPhone || "0977123456",
      listingId: parseListingNumericId(item.listingId),
      listingTitle: item.listingTitle,
      listingPrice: 4500000,
      listingAddress: item.listingAddress,
      listingPrimaryImageUrl: item.listingImage,
      landlordId: 2,
      landlordName: "Nguyễn Văn Hoàng (Chủ Trọ)",
      landlordPhone: "0902000002",
      appointmentTime: item.scheduledTime,
      durationMinutes: 30,
      notes: item.note || null,
      status: statusMap[item.status] || "PENDING",
      cancelledBy: null,
      cancelledReason: null,
      remindAt: null,
      isReminded: false,
      virtualViewing: null,
      createdAt: item.createdAt,
      updatedAt: item.createdAt,
    };
  });
}

export default function TenantViewingsPage() {
  const { user, role } = useAuth();
  const router = useRouter();
  const { showToast } = useToast();

  const [appointments, setAppointments] = useState<ViewingAppointmentDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [statusFilter, setStatusFilter] = useState<string>("ALL");
  const [isLoading, setIsLoading] = useState(true);
  const [actionLoadingId, setActionLoadingId] = useState<number | null>(null);

  // Book New Viewing Modal
  const [isBookModalOpen, setIsBookModalOpen] = useState(false);
  const [selectedListingId, setSelectedListingId] = useState<number>(1);
  const [bookingDate, setBookingDate] = useState(() => {
    const tomorrow = new Date(Date.now() + 24 * 60 * 60 * 1000);
    return tomorrow.toISOString().split("T")[0];
  });
  const [bookingTime, setBookingTime] = useState("10:00");
  const [durationMinutes, setDurationMinutes] = useState<number>(30);
  const [isVirtualViewing, setIsVirtualViewing] = useState<boolean>(false);
  const [bookingNotes, setBookingNotes] = useState("");
  const [bookingSubmitting, setBookingSubmitting] = useState(false);

  // Cancel Viewing Modal
  const [cancelTarget, setCancelTarget] = useState<ViewingAppointmentDto | null>(null);
  const [cancelReason, setCancelReason] = useState("");

  const availableListings = listingService.getAll();

  useEffect(() => {
    if (role === "Landlord") {
      router.replace("/landlord/viewings");
    }
  }, [role, router]);

  const loadAppointments = useCallback(async () => {
    if (!user) return;
    setIsLoading(true);
    try {
      const token = getStoredToken();
      if (token) {
        const res = await viewingsApi.getAppointments({
          status: statusFilter,
          roleView: "TENANT",
          page: 1,
          pageSize: 30,
        });
        if (res.isSuccess && res.data) {
          setAppointments(res.data.items);
          setTotalCount(res.data.totalCount);
          return;
        }
        setAppointments([]);
        setTotalCount(0);
        return;
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

  const handleBookAppointment = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!bookingDate || !bookingTime) {
      showToast("Vui lòng chọn đầy đủ ngày và giờ hẹn xem phòng!", "error");
      return;
    }

    setBookingSubmitting(true);
    try {
      const scheduledIso = new Date(`${bookingDate}T${bookingTime}:00`).toISOString();
      const token = getStoredToken();

      if (token) {
        const res = await viewingsApi.bookAppointment({
          listingId: selectedListingId,
          appointmentTime: scheduledIso,
          durationMinutes,
          notes: bookingNotes.trim() || undefined,
          isVirtualViewing,
        });

        if (!res.isSuccess) {
          showToast(
            res.error?.message || "Đặt lịch xem phòng không thành công.",
            "error"
          );
          return;
        }

        showToast(
          "Đặt lịch xem phòng thành công! Đã gửi thông báo tới chủ nhà.",
          "success"
        );
        setIsBookModalOpen(false);
        setBookingNotes("");
        await loadAppointments();
        return;
      }

      // Demo fallback
      const matchedListing = availableListings.find(
        (l) => parseListingNumericId(l.id) === selectedListingId
      );
      viewingService.book({
        listingId: matchedListing?.id || `lst-00${selectedListingId}`,
        listingTitle: matchedListing?.title || `Phòng trọ mã #${selectedListingId}`,
        listingAddress: matchedListing?.address || "TP. Hồ Chí Minh",
        listingImage: matchedListing?.images?.[0] || "",
        tenantId: user?.accountId || "demo-tenant-id",
        tenantName: user?.displayName || "Người thuê",
        tenantPhone: user?.phoneNumber || "0977123456",
        landlordId: matchedListing?.landlordId || "landlord-1",
        scheduledTime: scheduledIso,
        note: bookingNotes,
      });
      showToast("Đặt lịch xem phòng thành công!", "success");
      setIsBookModalOpen(false);
      setBookingNotes("");
      await loadAppointments();
    } finally {
      setBookingSubmitting(false);
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
          showToast("Đã hủy lịch hẹn xem phòng thành công.", "info");
          setCancelTarget(null);
          setCancelReason("");
          await loadAppointments();
        } else {
          showToast(res.error?.message || "Không thể hủy lịch hẹn.", "error");
        }
      } else {
        viewingService.updateStatus(`vw-${cancelTarget.id}`, "Đã hủy");
        showToast("Đã hủy lịch hẹn xem phòng.", "info");
        setCancelTarget(null);
        setCancelReason("");
        await loadAppointments();
      }
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
        <h2 className="text-xl font-black text-slate-900">Yêu Cầu Đăng Nhập</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập để đặt và quản lý các lịch hẹn xem phòng trực tiếp hoặc qua Video Call.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/tenant/viewings"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng nhập ngay</span>
          </Link>
        </div>
      </div>
    );
  }

  if (role === "Landlord") {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <Calendar className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Kênh Lịch Hẹn Dành Cho Chủ Nhà</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Tài khoản của bạn mang vai trò <strong>Chủ nhà</strong>. Vui lòng chuyển sang trang Quản lý lịch xem phòng dành riêng cho Chủ nhà để theo dõi và duyệt lịch hẹn của khách thuê.
        </p>
        <div className="pt-2">
          <Link
            href="/landlord/viewings"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đến trang Quản lý lịch xem phòng chủ nhà</span>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Page Header */}
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4 pb-5 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Calendar className="w-4 h-4" />
            <span>Lịch hẹn xem phòng</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Quản Lý Lịch Hẹn Xem Phòng ({totalCount})
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Đặt lịch xem trực tiếp hoặc qua Video Call, theo dõi xác nhận từ chủ nhà và liên hệ nhanh.
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
            href="/listings"
            className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20 transition-all"
          >
            <Plus className="w-4 h-4" />
            <span>Đặt lịch xem phòng mới</span>
          </Link>
        </div>
      </div>

      {/* Status Filter Bar */}
      <div className="flex flex-wrap items-center gap-2">
        {[
          { key: "ALL", label: "Tất cả lịch hẹn" },
          { key: "PENDING", label: "Chờ xác nhận" },
          { key: "CONFIRMED", label: "Đã xác nhận" },
          { key: "COMPLETED", label: "Đã hoàn thành" },
          { key: "CANCELLED", label: "Đã hủy" },
        ].map((tab) => (
          <button
            key={tab.key}
            type="button"
            onClick={() => setStatusFilter(tab.key)}
            className={cn(
              "px-3.5 py-2 rounded-xl text-xs font-bold border transition-all",
              statusFilter === tab.key
                ? "bg-blue-600 text-white border-blue-600 shadow-xs"
                : "bg-white text-slate-600 border-slate-200 hover:bg-slate-50"
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Appointments List */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {[1, 2].map((skeleton) => (
            <div
              key={skeleton}
              className="bg-white rounded-2xl border border-slate-200/80 p-6 space-y-4 animate-pulse"
            >
              <div className="h-6 bg-slate-100 rounded-lg w-1/3" />
              <div className="h-5 bg-slate-100 rounded-lg w-3/4" />
              <div className="h-20 bg-slate-100 rounded-xl w-full" />
              <div className="h-8 bg-slate-100 rounded-lg w-1/2" />
            </div>
          ))}
        </div>
      ) : appointments.length === 0 ? (
        <div className="bg-white rounded-2xl border border-slate-200/80 p-14 text-center space-y-4">
          <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto">
            <Calendar className="w-8 h-8" />
          </div>
          <h2 className="text-lg font-bold text-slate-800">
            Chưa có lịch hẹn xem phòng nào ở trạng thái này
          </h2>
          <p className="text-xs text-slate-500 max-w-md mx-auto">
            Khám phá danh sách phòng trên sàn tin để lựa chọn căn phòng ưng ý và đặt lịch hẹn trực tiếp với chủ trọ.
          </p>
          <div className="pt-2 flex items-center justify-center gap-3">
            <Link
              href="/listings"
              className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20"
            >
              <Search className="w-4 h-4" />
              <span>Tìm phòng để đặt lịch xem</span>
            </Link>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {appointments.map((item) => {
            const statusMeta = STATUS_CONFIG[item.status] || STATUS_CONFIG.PENDING;
            const dateObj = new Date(item.appointmentTime);
            const dateStr = dateObj.toLocaleDateString("vi-VN", {
              weekday: "long",
              year: "numeric",
              month: "long",
              day: "numeric",
            });
            const timeStr = dateObj.toLocaleTimeString("vi-VN", {
              hour: "2-digit",
              minute: "2-digit",
            });
            const isBusy = actionLoadingId === item.id;

            return (
              <div
                key={item.id}
                className="bg-white rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-all p-6 flex flex-col justify-between space-y-4"
              >
                <div className="space-y-3.5">
                  {/* Top Row: Status Badge + ID & Created Date */}
                  <div className="flex items-center justify-between gap-2 flex-wrap">
                    <div className="flex items-center gap-2">
                      <span
                        className={cn(
                          "inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border",
                          statusMeta.badgeClass
                        )}
                      >
                        <span className={cn("w-2 h-2 rounded-full", statusMeta.dotClass)} />
                        <span>{statusMeta.label}</span>
                      </span>

                      {item.virtualViewing && (
                        <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-bold bg-indigo-50 text-indigo-700 border border-indigo-200">
                          <Video className="w-3 h-3" />
                          <span>Video Call</span>
                        </span>
                      )}
                    </div>

                    <span className="text-[11px] text-slate-400 font-medium">
                      Mã #{item.id} • {new Date(item.createdAt).toLocaleDateString("vi-VN")}
                    </span>
                  </div>

                  {/* Listing Title & Price */}
                  <div>
                    <Link href={`/listings/lst-00${item.listingId}`}>
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

                  {/* Scheduled Time & Notes Box */}
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
                        <span className="font-bold text-slate-800">Ghi chú:</span> {item.notes}
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
                          <span>Vào phòng</span>
                          <ExternalLink className="w-3 h-3" />
                        </a>
                      </div>
                    )}
                  </div>

                  {/* Contact Info Row */}
                  <div className="grid grid-cols-2 gap-3 p-3 rounded-xl bg-slate-50 border border-slate-100 text-xs">
                    <div>
                      <span className="text-[10px] uppercase font-bold text-slate-400 block">
                        Chủ nhà phụ trách
                      </span>
                      <span className="font-bold text-slate-800 block truncate">
                        {item.landlordName}
                      </span>
                      {item.landlordPhone && (
                        <span className="text-[11px] text-emerald-700 font-semibold flex items-center gap-1 mt-0.5">
                          <Phone className="w-3 h-3" />
                          {item.landlordPhone}
                        </span>
                      )}
                    </div>
                    <div>
                      <span className="text-[10px] uppercase font-bold text-slate-400 block">
                        Người thuê đặt lịch
                      </span>
                      <span className="font-bold text-slate-800 block truncate">
                        {item.tenantName}
                      </span>
                      {item.tenantPhone && (
                        <span className="text-[11px] text-slate-600 font-medium flex items-center gap-1 mt-0.5">
                          <User className="w-3 h-3" />
                          {item.tenantPhone}
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Cancellation Reason if Cancelled */}
                  {item.status === "CANCELLED" && (
                    <div className="p-3 rounded-xl bg-rose-50/70 border border-rose-200/80 text-xs text-rose-800 flex items-start gap-2">
                      <AlertCircle className="w-4 h-4 text-rose-600 shrink-0 mt-0.5" />
                      <div>
                        <span className="font-bold">
                          Đã hủy bởi {item.cancelledBy === "LANDLORD" ? "Chủ nhà" : "Người thuê"}:
                        </span>{" "}
                        <span>{item.cancelledReason || "Không có lý do cụ thể."}</span>
                      </div>
                    </div>
                  )}
                </div>

                {/* Action Footer */}
                <div className="pt-3.5 border-t border-slate-100 flex flex-wrap items-center justify-between gap-2">
                  <button
                    type="button"
                    onClick={() => router.push(`/messages?listingId=${item.listingId}`)}
                    className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-800 text-xs font-bold transition-colors"
                  >
                    <MessageSquare className="w-3.5 h-3.5 text-blue-600" />
                    <span>Nhắn tin trao đổi</span>
                  </button>

                  <div className="flex items-center gap-2">
                    {/* Cancel Button for Tenant when PENDING or CONFIRMED */}
                    {(item.status === "PENDING" || item.status === "CONFIRMED") && (
                      <button
                        type="button"
                        disabled={isBusy}
                        onClick={() => {
                          setCancelTarget(item);
                          setCancelReason("");
                        }}
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl border border-rose-200 text-rose-600 hover:bg-rose-50 text-xs font-bold transition-colors disabled:opacity-50"
                      >
                        <XCircle className="w-3.5 h-3.5" />
                        <span>Hủy lịch hẹn</span>
                      </button>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Modal: Đặt lịch xem phòng mới */}
      <Modal
        isOpen={isBookModalOpen}
        onClose={() => setIsBookModalOpen(false)}
        title="Đặt Lịch Hẹn Xem Phòng Mới"
        description="Chọn phòng trọ, ngày giờ và hình thức xem phòng (Trực tiếp hoặc Video Call)."
        maxWidth="md"
      >
        <form onSubmit={handleBookAppointment} className="space-y-4">
          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
              Chọn phòng muốn đặt lịch xem
            </label>
            <select
              value={selectedListingId}
              onChange={(e) => setSelectedListingId(Number(e.target.value))}
              className="w-full px-3 py-2.5 text-xs rounded-xl border border-slate-200 bg-white font-semibold text-slate-800"
            >
              <option value={1}>
                Phòng #1 — Studio Ban Công Quận 7 (4.500.000 đ/tháng)
              </option>
              <option value={2}>
                Phòng #2 — Căn Hộ Dịch Vụ Gần ĐH Bách Khoa (5.200.000 đ/tháng)
              </option>
              <option value={3}>
                Phòng #3 — Phòng Trọ Cao Cấp Bình Thạnh (3.800.000 đ/tháng)
              </option>
            </select>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
                Ngày hẹn xem
              </label>
              <input
                type="date"
                required
                value={bookingDate}
                onChange={(e) => setBookingDate(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
                Giờ hẹn xem
              </label>
              <input
                type="time"
                required
                value={bookingTime}
                onChange={(e) => setBookingTime(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
                Thời lượng buổi xem
              </label>
              <select
                value={durationMinutes}
                onChange={(e) => setDurationMinutes(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white"
              >
                <option value={30}>30 phút (Tiêu chuẩn)</option>
                <option value={45}>45 phút</option>
                <option value={60}>60 phút (Xem chi tiết)</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
                Hình thức xem
              </label>
              <select
                value={isVirtualViewing ? "virtual" : "inperson"}
                onChange={(e) => setIsVirtualViewing(e.target.value === "virtual")}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white"
              >
                <option value="inperson">Xem trực tiếp tại phòng</option>
                <option value="virtual">Xem qua Video Call (Online)</option>
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
              Ghi chú gửi chủ nhà
            </label>
            <textarea
              rows={3}
              placeholder="Ví dụ: Mình muốn xem kỹ khu vực bếp, chỗ để xe máy và hỏi thêm về giờ giấc ra vào..."
              value={bookingNotes}
              onChange={(e) => setBookingNotes(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
            />
          </div>

          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={() => setIsBookModalOpen(false)}
              className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
            >
              Đóng
            </button>
            <button
              type="submit"
              disabled={bookingSubmitting}
              className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 shadow-md shadow-blue-500/20 disabled:opacity-50"
            >
              {bookingSubmitting ? "Đang gửi yêu cầu..." : "Xác nhận đặt lịch"}
            </button>
          </div>
        </form>
      </Modal>

      {/* Modal: Xác nhận hủy lịch hẹn */}
      <Modal
        isOpen={Boolean(cancelTarget)}
        onClose={() => setCancelTarget(null)}
        title="Hủy Lịch Hẹn Xem Phòng"
        description="Vui lòng nhập lý do hủy lịch để hệ thống thông báo lịch sự cho đối phương."
        maxWidth="sm"
      >
        <form onSubmit={handleConfirmCancel} className="space-y-4">
          {cancelTarget && (
            <div className="p-3 rounded-xl bg-slate-50 border border-slate-200 text-xs">
              <span className="font-bold text-slate-900 block">
                {cancelTarget.listingTitle}
              </span>
              <span className="text-slate-500">
                Hẹn lúc:{" "}
                {new Date(cancelTarget.appointmentTime).toLocaleString("vi-VN")}
              </span>
            </div>
          )}

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
              Lý do hủy lịch hẹn
            </label>
            <textarea
              rows={3}
              placeholder="Ví dụ: Mình có lịch công tác đột xuất, xin phép hẹn lại vào cuối tuần..."
              value={cancelReason}
              onChange={(e) => setCancelReason(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
            />
          </div>

          <div className="flex gap-3 pt-1">
            <button
              type="button"
              onClick={() => setCancelTarget(null)}
              className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
            >
              Quay lại
            </button>
            <button
              type="submit"
              disabled={actionLoadingId !== null}
              className="flex-1 py-2.5 rounded-xl bg-rose-600 text-white text-xs font-bold hover:bg-rose-700 shadow-md shadow-rose-500/20 disabled:opacity-50"
            >
              {actionLoadingId !== null ? "Đang hủy..." : "Xác nhận hủy lịch"}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
