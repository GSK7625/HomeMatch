"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import { viewingService } from "@/lib/services";
import { ViewingAppointment } from "@/types";
import { useToast } from "@/components/Toast";
import {
  Calendar,
  Clock,
  MapPin,
  CheckCircle,
  XCircle,
  AlertCircle,
  ArrowRight,
  Phone,
  Building,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function TenantViewingsPage() {
  const { user } = useAuth();
  const { showToast } = useToast();
  const [appointments, setAppointments] = useState<ViewingAppointment[]>([]);

  const loadData = () => {
    setAppointments(viewingService.getAll());
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleCancelViewing = (id: string) => {
    if (confirm("Bạn có chắc chắn muốn hủy lịch hẹn xem phòng này?")) {
      viewingService.updateStatus(id, "Đã hủy");
      loadData();
      showToast("Đã hủy lịch xem phòng thành công.", "info");
    }
  };

  const statusBadges = {
    "Chờ xác nhận": "bg-amber-50 text-amber-700 border-amber-200",
    "Đã xác nhận": "bg-emerald-50 text-emerald-700 border-emerald-200",
    "Đã hoàn thành": "bg-blue-50 text-blue-700 border-blue-200",
    "Đã hủy": "bg-rose-50 text-rose-700 border-rose-200",
  };

  if (!user) {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <Calendar className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Yêu Cầu Đăng Nhập</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập để xem và quản lý các lịch hẹn xem phòng trực tiếp của bạn.
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

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Calendar className="w-4 h-4" />
            <span>Lịch hẹn trực tiếp</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Quản Lý Lịch Hẹn Xem Phòng
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Theo dõi trạng thái xác nhận từ chủ nhà và thời gian hẹn xem phòng trực tiếp.
          </p>
        </div>

        <Link
          href="/listings"
          className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20"
        >
          <span>Tìm & Đặt lịch thêm phòng</span>
          <ArrowRight className="w-3.5 h-3.5" />
        </Link>
      </div>

      {appointments.length === 0 ? (
        <div className="bg-white rounded-2xl border border-slate-200/80 p-16 text-center space-y-4">
          <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto">
            <Calendar className="w-8 h-8" />
          </div>
          <h2 className="text-lg font-bold text-slate-800">Bạn chưa có lịch hẹn xem phòng nào</h2>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            Chọn phòng bạn ưng ý và nhấn "Đặt Lịch Xem Phòng" để kết nối với chủ nhà.
          </p>
          <div className="pt-2">
            <Link
              href="/listings"
              className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20"
            >
              <span>Xem danh sách phòng</span>
              <ArrowRight className="w-4 h-4" />
            </Link>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {appointments.map((item) => {
            const dateObj = new Date(item.scheduledTime);
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

            return (
              <div
                key={item.id}
                className="bg-white rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-shadow p-6 flex flex-col justify-between space-y-4"
              >
                <div>
                  <div className="flex items-start justify-between gap-4 mb-3">
                    <span
                      className={cn(
                        "px-3 py-1 rounded-full text-xs font-bold border",
                        statusBadges[item.status]
                      )}
                    >
                      {item.status}
                    </span>

                    <span className="text-[11px] text-slate-400">
                      Tạo ngày {new Date(item.createdAt).toLocaleDateString("vi-VN")}
                    </span>
                  </div>

                  <Link href={`/listings/${item.listingId}`}>
                    <h3 className="font-bold text-slate-900 text-sm hover:text-blue-600 line-clamp-1 mb-2">
                      {item.listingTitle}
                    </h3>
                  </Link>

                  <div className="flex items-start gap-1.5 text-xs text-slate-500 mb-4">
                    <MapPin className="w-3.5 h-3.5 text-slate-400 shrink-0 mt-0.5" />
                    <span className="line-clamp-1">{item.listingAddress}</span>
                  </div>

                  {/* Scheduled info box */}
                  <div className="p-3.5 bg-blue-50/50 border border-blue-100 rounded-xl space-y-2 text-xs">
                    <div className="flex items-center gap-2 text-blue-900 font-semibold">
                      <Clock className="w-4 h-4 text-blue-600 shrink-0" />
                      <span>{timeStr} — {dateStr}</span>
                    </div>

                    {item.note && (
                      <p className="text-[11px] text-slate-600 bg-white/80 p-2 rounded-lg border border-slate-200/60">
                        <span className="font-bold text-slate-700">Ghi chú:</span> {item.note}
                      </p>
                    )}
                  </div>
                </div>

                <div className="pt-3 border-t border-slate-100 flex items-center justify-between">
                  <Link
                    href={`/listings/${item.listingId}`}
                    className="text-xs font-bold text-blue-600 hover:text-blue-700"
                  >
                    Xem chi tiết tin đăng →
                  </Link>

                  {item.status === "Chờ xác nhận" && (
                    <button
                      onClick={() => handleCancelViewing(item.id)}
                      className="px-3 py-1.5 rounded-lg border border-rose-200 text-rose-600 hover:bg-rose-50 text-xs font-bold transition-colors"
                    >
                      Hủy lịch hẹn
                    </button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
