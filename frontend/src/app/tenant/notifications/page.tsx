"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { getStoredToken, notificationsApi } from "@/lib/api-client";
import { NotificationDto } from "@/types";
import { useToast } from "@/components/Toast";
import {
  Bell,
  Calendar,
  MessageSquare,
  CheckCheck,
  Check,
  ArrowRight,
  RefreshCw,
  Sparkles,
  Home,
} from "lucide-react";
import { cn } from "@/lib/utils";


function getNotificationMeta(type: string) {
  const upper = (type || "").toUpperCase();
  if (upper.includes("VIEWING")) {
    return {
      icon: Calendar,
      badgeText: "Lịch xem phòng",
      iconWrapClass: "bg-blue-50 text-blue-600",
      badgeClass: "bg-blue-50 text-blue-700 border-blue-200",
    };
  }
  if (upper.includes("MESSAGE")) {
    return {
      icon: MessageSquare,
      badgeText: "Tin nhắn mới",
      iconWrapClass: "bg-emerald-50 text-emerald-600",
      badgeClass: "bg-emerald-50 text-emerald-700 border-emerald-200",
    };
  }
  if (upper.includes("LISTING")) {
    return {
      icon: Home,
      badgeText: "Tin phòng trọ",
      iconWrapClass: "bg-amber-50 text-amber-600",
      badgeClass: "bg-amber-50 text-amber-700 border-amber-200",
    };
  }
  return {
    icon: Sparkles,
    badgeText: "Hệ thống",
    iconWrapClass: "bg-purple-50 text-purple-600",
    badgeClass: "bg-purple-50 text-purple-700 border-purple-200",
  };
}

export default function TenantNotificationsPage() {
  const { user, role } = useAuth();
  const router = useRouter();
  const { showToast } = useToast();

  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [filter, setFilter] = useState<"ALL" | "UNREAD" | "READ">("ALL");
  const [isLoading, setIsLoading] = useState(true);
  const [markingAll, setMarkingAll] = useState(false);

  const loadNotifications = useCallback(async () => {
    if (!user) return;
    setIsLoading(true);
    try {
      const token = getStoredToken();
      if (token) {
        const isReadParam =
          filter === "UNREAD" ? false : filter === "READ" ? true : undefined;
        const res = await notificationsApi.getNotifications({
          isRead: isReadParam,
          page: 1,
          pageSize: 30,
        });
        if (res.isSuccess && res.data) {
          setNotifications(res.data.items);
          setUnreadCount(res.data.unreadCount);
          setTotalCount(res.data.totalCount);
          return;
        }
        setNotifications([]);
        setUnreadCount(0);
        setTotalCount(0);
        return;
      }
      setNotifications([]);
      setUnreadCount(0);
      setTotalCount(0);
    } finally {
      setIsLoading(false);
    }
  }, [user, filter]);

  useEffect(() => {
    loadNotifications();
  }, [loadNotifications]);

  const handleMarkAsRead = async (item: NotificationDto) => {
    if (item.isRead) return;
    const token = getStoredToken();
    if (token) {
      const res = await notificationsApi.markAsRead(item.id);
      if (res.isSuccess) {
        setNotifications((prev) =>
          prev.map((n) => (n.id === item.id ? { ...n, isRead: true } : n))
        );
        setUnreadCount((prev) => Math.max(0, prev - 1));
      }
    } else {
      setNotifications((prev) =>
        prev.map((n) => (n.id === item.id ? { ...n, isRead: true } : n))
      );
      setUnreadCount((prev) => Math.max(0, prev - 1));
    }
  };

  const handleMarkAllAsRead = async () => {
    if (unreadCount === 0) return;
    setMarkingAll(true);
    try {
      const token = getStoredToken();
      if (token) {
        const res = await notificationsApi.markAllAsRead();
        if (res.isSuccess) {
          showToast(
            `Đã đánh dấu ${res.data?.markedReadCount ?? unreadCount} thông báo là đã đọc.`,
            "success"
          );
          await loadNotifications();
          return;
        }
      }
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
      setUnreadCount(0);
      showToast("Đã đánh dấu tất cả thông báo là đã đọc!", "success");
    } finally {
      setMarkingAll(false);
    }
  };

  const handleOpenNotificationTarget = async (item: NotificationDto) => {
    await handleMarkAsRead(item);
    const refType = (item.referenceType || "").toUpperCase();
    const typeUpper = (item.type || "").toUpperCase();

    if (refType.includes("VIEWING") || typeUpper.includes("VIEWING")) {
      router.push(role === "Landlord" ? "/landlord/viewings" : "/tenant/viewings");
      return;
    }
    if (refType.includes("CONVERSATION") || typeUpper.includes("MESSAGE")) {
      router.push(
        item.referenceId
          ? `/messages?conversationId=${item.referenceId}`
          : "/messages"
      );
      return;
    }
    router.push("/tenant/dashboard");
  };

  if (!user) {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <Bell className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Trung Tâm Thông Báo</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập để xem thông báo xác nhận lịch hẹn xem phòng và tin nhắn từ chủ nhà.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/tenant/notifications"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng nhập ngay</span>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-5 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Bell className="w-4 h-4" />
            <span>Mục 3.4.3 • Xem thông báo tài khoản</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight flex items-center gap-2.5">
            <span>Trung Tâm Thông Báo</span>
            {unreadCount > 0 && (
              <span className="px-2.5 py-0.5 rounded-full bg-rose-500 text-white text-xs font-black">
                {unreadCount} chưa đọc
              </span>
            )}
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Cập nhật tức thời mọi phản hồi lịch xem phòng, tin nhắn từ chủ trọ và cảnh báo tài khoản.
          </p>
        </div>

        <div className="flex items-center gap-2.5">
          <button
            type="button"
            onClick={loadNotifications}
            className="p-2 rounded-xl border border-slate-200 text-slate-600 hover:text-blue-600 hover:bg-slate-50 transition-colors"
            title="Làm mới thông báo"
          >
            <RefreshCw className={cn("w-4 h-4", isLoading && "animate-spin")} />
          </button>

          <button
            type="button"
            disabled={unreadCount === 0 || markingAll}
            onClick={handleMarkAllAsRead}
            className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-sm transition-all disabled:opacity-50"
          >
            <CheckCheck className="w-4 h-4" />
            <span>{markingAll ? "Đang xử lý..." : "Đánh dấu tất cả đã đọc"}</span>
          </button>
        </div>
      </div>

      {/* Filter Bar */}
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <div className="flex items-center gap-2">
          {[
            { key: "ALL", label: `Tất cả (${totalCount})` },
            { key: "UNREAD", label: `Chưa đọc (${unreadCount})` },
            { key: "READ", label: "Đã đọc" },
          ].map((tab) => (
            <button
              key={tab.key}
              type="button"
              onClick={() => setFilter(tab.key as "ALL" | "UNREAD" | "READ")}
              className={cn(
                "px-3.5 py-2 rounded-xl text-xs font-bold border transition-all",
                filter === tab.key
                  ? "bg-blue-600 text-white border-blue-600 shadow-xs"
                  : "bg-white text-slate-600 border-slate-200 hover:bg-slate-50"
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <Link
          href="/tenant/dashboard"
          className="text-xs font-bold text-blue-600 hover:text-blue-700 flex items-center gap-1"
        >
          <span>Về Dashboard người thuê</span>
          <ArrowRight className="w-3.5 h-3.5" />
        </Link>
      </div>

      {/* Notifications List */}
      {isLoading ? (
        <div className="space-y-3">
          {[1, 2, 3].map((s) => (
            <div
              key={s}
              className="bg-white rounded-2xl border border-slate-200 p-5 animate-pulse space-y-2"
            >
              <div className="h-4 bg-slate-100 rounded w-1/4" />
              <div className="h-4 bg-slate-100 rounded w-3/4" />
            </div>
          ))}
        </div>
      ) : notifications.length === 0 ? (
        <div className="bg-white rounded-2xl border border-slate-200/80 p-14 text-center space-y-3">
          <div className="w-14 h-14 rounded-2xl bg-slate-100 text-slate-400 flex items-center justify-center mx-auto">
            <Bell className="w-7 h-7" />
          </div>
          <h3 className="text-base font-bold text-slate-800">
            Không có thông báo nào trong mục này
          </h3>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            Khi bạn đặt lịch xem phòng hoặc nhắn tin với chủ nhà, mọi thông báo mới sẽ xuất hiện tại đây.
          </p>
        </div>
      ) : (
        <div className="space-y-3">
          {notifications.map((item) => {
            const meta = getNotificationMeta(item.type);
            const Icon = meta.icon;

            return (
              <div
                key={item.id}
                className={cn(
                  "rounded-2xl border p-5 transition-all flex flex-col sm:flex-row sm:items-center justify-between gap-4",
                  item.isRead
                    ? "bg-white border-slate-200/80"
                    : "bg-blue-50/40 border-blue-200 shadow-xs"
                )}
              >
                <div className="flex items-start gap-3.5 min-w-0">
                  <div
                    className={cn(
                      "w-11 h-11 rounded-xl flex items-center justify-center shrink-0 mt-0.5",
                      meta.iconWrapClass
                    )}
                  >
                    <Icon className="w-5 h-5" />
                  </div>

                  <div className="space-y-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span
                        className={cn(
                          "px-2.5 py-0.5 rounded-full text-[10px] font-bold border",
                          meta.badgeClass
                        )}
                      >
                        {meta.badgeText}
                      </span>
                      <span className="text-[11px] text-slate-400">
                        {new Date(item.createdAt).toLocaleString("vi-VN")}
                      </span>
                      {!item.isRead && (
                        <span className="inline-flex items-center gap-1 text-[10px] font-black text-blue-600">
                          <span className="w-2 h-2 rounded-full bg-blue-600" />
                          MỚI
                        </span>
                      )}
                    </div>

                    <h3
                      className={cn(
                        "text-sm",
                        item.isRead
                          ? "font-bold text-slate-800"
                          : "font-black text-slate-900"
                      )}
                    >
                      {item.title}
                    </h3>

                    <p className="text-xs text-slate-600 leading-relaxed">
                      {item.content}
                    </p>
                  </div>
                </div>

                <div className="flex items-center gap-2 shrink-0 self-end sm:self-center">
                  {!item.isRead && (
                    <button
                      type="button"
                      onClick={() => handleMarkAsRead(item)}
                      className="inline-flex items-center gap-1 px-3 py-1.5 rounded-xl border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 text-xs font-bold transition-colors"
                    >
                      <Check className="w-3.5 h-3.5 text-emerald-600" />
                      <span>Đã đọc</span>
                    </button>
                  )}

                  <button
                    type="button"
                    onClick={() => handleOpenNotificationTarget(item)}
                    className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold transition-colors"
                  >
                    <span>Mở chi tiết</span>
                    <ArrowRight className="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
