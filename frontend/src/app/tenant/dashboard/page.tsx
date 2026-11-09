"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import {
  listingService,
  leasingService,
  billingService,
  viewingService,
  messagingService,
} from "@/lib/services";
import {
  getStoredToken,
  notificationsApi,
  parseListingNumericId,
  tenantOverviewApi,
  viewingsApi,
} from "@/lib/api-client";
import {
  LeaseContract,
  MonthlyBill,
  NotificationDto,
  ViewedListingContractItem,
  ViewingAppointmentDto,
} from "@/types";
import { useToast } from "@/components/Toast";
import {
  FileText,
  DollarSign,
  Heart,
  Calendar,
  Eye,
  MessageSquare,
  Clock,
  CreditCard,
  Bell,
  ArrowRight,
  MapPin,
  Maximize2,
  Check,
  RefreshCw,
  User,
  Phone,
  Mail,
  Search,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function TenantDashboardPage() {
  const { user, role } = useAuth();
  const router = useRouter();
  const { showToast } = useToast();

  const [activeTab, setActiveTab] = useState<
    "overview" | "history" | "contracts" | "bills"
  >("overview");
  const [isLoading, setIsLoading] = useState(true);

  const [contracts, setContracts] = useState<LeaseContract[]>([]);
  const [bills, setBills] = useState<MonthlyBill[]>([]);
  const [viewedListings, setViewedListings] = useState<ViewedListingContractItem[]>([]);
  const [upcomingViewings, setUpcomingViewings] = useState<ViewingAppointmentDto[]>([]);
  const [recentNotifications, setRecentNotifications] = useState<NotificationDto[]>([]);
  const [unreadNotificationsCount, setUnreadNotificationsCount] = useState(0);

  const [profileSummary, setProfileSummary] = useState({
    fullName: "",
    email: "",
    phoneNumber: "",
  });

  const [stats, setStats] = useState({
    viewedCount: 0,
    favCount: 0,
    viewingCount: 0,
    chatCount: 0,
  });

  useEffect(() => {
    if (role === "Landlord") {
      router.replace("/landlord/dashboard");
    }
  }, [role, router]);

  const loadData = useCallback(async () => {
    if (!user) return;
    setIsLoading(true);

    try {
      const token = getStoredToken();
      if (token) {
        const [dashRes, viewedRes, viewingsRes, notifRes] = await Promise.all([
          tenantOverviewApi.getDashboard(),
          tenantOverviewApi.getViewedListings(1, 6),
          viewingsApi.getAppointments({ roleView: "TENANT", page: 1, pageSize: 6 }),
          notificationsApi.getNotifications({ page: 1, pageSize: 5 }),
        ]);

        if (dashRes.isSuccess && dashRes.data) {
          setProfileSummary({
            fullName: dashRes.data.fullName,
            email: dashRes.data.email,
            phoneNumber: dashRes.data.phoneNumber || "",
          });
          const clientFavCount = typeof window !== "undefined" ? listingService.getFavorites().length : 0;
          setStats({
            viewedCount: dashRes.data.totalViewedListings,
            favCount: dashRes.data.totalSavedListings > 0 ? dashRes.data.totalSavedListings : clientFavCount,
            viewingCount: dashRes.data.totalAppointments,
            chatCount: dashRes.data.totalConversations,
          });
        } else {
          setProfileSummary({
            fullName: user.displayName || user.fullName || "Người thuê",
            email: user.email,
            phoneNumber: user.phoneNumber || "",
          });
          setStats({
            viewedCount: 0,
            favCount: 0,
            viewingCount: 0,
            chatCount: 0,
          });
        }

        setViewedListings(viewedRes.isSuccess && viewedRes.data ? viewedRes.data.items : []);
        setUpcomingViewings(viewingsRes.isSuccess && viewingsRes.data ? viewingsRes.data.items : []);
        setRecentNotifications(notifRes.isSuccess && notifRes.data ? notifRes.data.items : []);
        setUnreadNotificationsCount(notifRes.isSuccess && notifRes.data ? notifRes.data.unreadCount : 0);
      } else {
        // When no token: clean empty states, absolutely no mock listings or mock viewings
        setProfileSummary({
          fullName: user.displayName || user.fullName || "Người thuê",
          email: user.email,
          phoneNumber: user.phoneNumber || "",
        });
        setStats({
          viewedCount: 0,
          favCount: 0,
          viewingCount: 0,
          chatCount: 0,
        });
        setViewedListings([]);
        setUpcomingViewings([]);
        setRecentNotifications([]);
        setUnreadNotificationsCount(0);
      }

      // Real user contracts & bills only (filter strictly by current tenant name/phone)
      const myContracts = leasingService
        .getAll()
        .filter((c) => c.tenantName === user.displayName || c.tenantPhone === user.phoneNumber);
      setContracts(myContracts);
      const myContractIds = new Set(myContracts.map((c) => c.id));
      const myBills = billingService.getAll().filter((b) => myContractIds.has(b.contractId));
      setBills(myBills);
    } catch {
      showToast("Không thể tải thông tin Dashboard. Vui lòng tải lại trang.", "error");
    } finally {
      setIsLoading(false);
    }
  }, [user, showToast]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handlePayBill = (billId: string, amount: number) => {
    billingService.payBill(billId, amount);
    loadData();
    showToast("Thanh toán thành công! Công nợ kỳ này đã được cập nhật.", "success");
  };

  const handleMarkNotificationRead = async (notif: NotificationDto) => {
    if (notif.isRead) return;
    const token = getStoredToken();
    if (token) {
      await notificationsApi.markAsRead(notif.id);
    }
    setRecentNotifications((prev) =>
      prev.map((n) => (n.id === notif.id ? { ...n, isRead: true } : n))
    );
    setUnreadNotificationsCount((prev) => Math.max(0, prev - 1));
  };

  if (!user) {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <FileText className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Yêu Cầu Đăng Nhập</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập tài khoản Người thuê để xem Dashboard tìm phòng, lịch sử xem phòng, thông báo và hợp đồng thuê nhà.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/tenant/dashboard"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng nhập tài khoản Người thuê</span>
          </Link>
        </div>
      </div>
    );
  }

  if (role === "Landlord") {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <FileText className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Kênh Quản Lý Dành Cho Chủ Nhà</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Tài khoản của bạn mang vai trò <strong>Chủ nhà (Landlord)</strong>. Vui lòng chuyển sang trang Tổng quan quản lý tài chính và vận hành của chủ nhà.
        </p>
        <div className="pt-2">
          <Link
            href="/landlord/dashboard"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đến Dashboard Chủ Nhà</span>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-8">
      {/* Welcome Banner & Account Summary (Mục 3.4) */}
      <div className="bg-gradient-to-r from-blue-700 via-blue-800 to-indigo-900 rounded-2xl p-6 sm:p-8 text-white shadow-xl flex flex-col lg:flex-row items-start lg:items-center justify-between gap-6">
        <div className="space-y-2">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="inline-block px-3 py-1 rounded-full text-xs font-semibold bg-white/20 backdrop-blur-md">
              Tổng Quan Tài Khoản Người Thuê
            </span>
            {unreadNotificationsCount > 0 && (
              <Link
                href="/tenant/notifications"
                className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-rose-500 text-white hover:bg-rose-600 transition-colors"
              >
                <Bell className="w-3.5 h-3.5" />
                <span>{unreadNotificationsCount} thông báo mới</span>
              </Link>
            )}
          </div>

          <h1 className="text-2xl sm:text-3xl font-black">
            Xin chào, {profileSummary.fullName || user.displayName}!
          </h1>

          <div className="flex flex-wrap items-center gap-4 text-xs text-blue-100/90 pt-1">
            <span className="inline-flex items-center gap-1.5">
              <Mail className="w-3.5 h-3.5 text-blue-300" />
              <span>{profileSummary.email || user.email}</span>
            </span>
            {(profileSummary.phoneNumber || user.phoneNumber) && (
              <span className="inline-flex items-center gap-1.5">
                <Phone className="w-3.5 h-3.5 text-blue-300" />
                <span>{profileSummary.phoneNumber || user.phoneNumber}</span>
              </span>
            )}
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2.5 shrink-0">
          <button
            type="button"
            onClick={loadData}
            className="p-2.5 rounded-xl bg-white/15 hover:bg-white/25 text-white transition-colors"
            title="Làm mới số liệu Dashboard"
          >
            <RefreshCw className={cn("w-4 h-4", isLoading && "animate-spin")} />
          </button>

          <Link
            href="/tenant/viewings"
            className="px-4 py-2.5 rounded-xl bg-white/15 hover:bg-white/25 text-white font-bold text-xs transition-all flex items-center gap-1.5"
          >
            <Calendar className="w-3.5 h-3.5" />
            <span>Quản lý lịch xem</span>
          </Link>

          <Link
            href="/listings"
            className="px-5 py-2.5 rounded-xl bg-white text-blue-700 font-bold text-xs hover:bg-blue-50 shadow-md transition-all"
          >
            Khám phá thêm phòng →
          </Link>
        </div>
      </div>

      {/* 4 KPI Stat Cards (Mục 3.4.1) */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <Link
          href="/tenant/viewed-listings"
          className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md hover:border-blue-300 transition-all flex items-center gap-4 group"
        >
          <div className="w-12 h-12 rounded-xl bg-blue-50 text-blue-600 group-hover:bg-blue-600 group-hover:text-white transition-colors flex items-center justify-center shrink-0">
            <Eye className="w-6 h-6" />
          </div>
          <div>
            <span className="block text-2xl font-black text-slate-900">
              {stats.viewedCount}
            </span>
            <span className="text-xs text-slate-500 font-medium group-hover:text-blue-600">
              Phòng đã xem →
            </span>
          </div>
        </Link>

        <Link
          href="/tenant/favorites"
          className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md hover:border-rose-300 transition-all flex items-center gap-4 group"
        >
          <div className="w-12 h-12 rounded-xl bg-rose-50 text-rose-500 group-hover:bg-rose-500 group-hover:text-white transition-colors flex items-center justify-center shrink-0">
            <Heart className="w-6 h-6" />
          </div>
          <div>
            <span className="block text-2xl font-black text-slate-900">
              {stats.favCount}
            </span>
            <span className="text-xs text-slate-500 font-medium group-hover:text-rose-600">
              Tin đã lưu yêu thích →
            </span>
          </div>
        </Link>

        <Link
          href="/tenant/viewings"
          className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md hover:border-amber-300 transition-all flex items-center gap-4 group"
        >
          <div className="w-12 h-12 rounded-xl bg-amber-50 text-amber-600 group-hover:bg-amber-500 group-hover:text-white transition-colors flex items-center justify-center shrink-0">
            <Calendar className="w-6 h-6" />
          </div>
          <div>
            <span className="block text-2xl font-black text-slate-900">
              {stats.viewingCount}
            </span>
            <span className="text-xs text-slate-500 font-medium group-hover:text-amber-600">
              Lịch hẹn xem phòng →
            </span>
          </div>
        </Link>

        <Link
          href="/messages"
          className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md hover:border-emerald-300 transition-all flex items-center gap-4 group"
        >
          <div className="w-12 h-12 rounded-xl bg-emerald-50 text-emerald-600 group-hover:bg-emerald-600 group-hover:text-white transition-colors flex items-center justify-center shrink-0">
            <MessageSquare className="w-6 h-6" />
          </div>
          <div>
            <span className="block text-2xl font-black text-slate-900">
              {stats.chatCount}
            </span>
            <span className="text-xs text-slate-500 font-medium group-hover:text-emerald-600">
              Phiên chat chủ trọ →
            </span>
          </div>
        </Link>
      </div>

      {/* Navigation Tabs */}
      <div className="flex border-b border-slate-200 gap-5 overflow-x-auto">
        <button
          type="button"
          onClick={() => setActiveTab("overview")}
          className={cn(
            "pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2 whitespace-nowrap",
            activeTab === "overview"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          )}
        >
          <Calendar className="w-4 h-4" />
          <span>Tổng Quan Tìm Phòng & Thông Báo</span>
        </button>

        <button
          type="button"
          onClick={() => setActiveTab("history")}
          className={cn(
            "pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2 whitespace-nowrap",
            activeTab === "history"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          )}
        >
          <Eye className="w-4 h-4" />
          <span>Danh Sách Phòng Đã Xem ({viewedListings.length})</span>
        </button>

        <button
          type="button"
          onClick={() => setActiveTab("contracts")}
          className={cn(
            "pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2 whitespace-nowrap",
            activeTab === "contracts"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          )}
        >
          <FileText className="w-4 h-4" />
          <span>Hợp Đồng Thuê Nhà ({contracts.length})</span>
        </button>

        <button
          type="button"
          onClick={() => setActiveTab("bills")}
          className={cn(
            "pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2 whitespace-nowrap",
            activeTab === "bills"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          )}
        >
          <DollarSign className="w-4 h-4" />
          <span>Hóa Đơn & Điện Nước ({bills.length})</span>
        </button>
      </div>

      {/* TAB 1: Overview (Upcoming Viewings + Recent Notifications) */}
      {activeTab === "overview" && (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* Left 7 Cols: Lịch hẹn xem phòng gần đây (Mục 3.3.1) */}
          <div className="lg:col-span-7 bg-white rounded-2xl border border-slate-200/80 shadow-xs p-6 space-y-4">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <div>
                <h2 className="font-bold text-slate-900 text-base flex items-center gap-2">
                  <Calendar className="w-4 h-4 text-blue-600" />
                  <span>Lịch Hẹn Xem Phòng Của Bạn</span>
                </h2>
                <p className="text-[11px] text-slate-500 mt-0.5">
                  Theo dõi trạng thái xác nhận từ chủ nhà cho các buổi hẹn sắp tới
                </p>
              </div>
              <Link
                href="/tenant/viewings"
                className="text-xs font-bold text-blue-600 hover:text-blue-700 flex items-center gap-1"
              >
                <span>Quản lý tất cả</span>
                <ArrowRight className="w-3.5 h-3.5" />
              </Link>
            </div>

            {upcomingViewings.length === 0 ? (
              <div className="p-8 text-center space-y-3 bg-slate-50/60 rounded-xl border border-slate-100">
                <Calendar className="w-8 h-8 text-slate-300 mx-auto" />
                <p className="text-xs text-slate-500">
                  Bạn chưa có lịch hẹn xem phòng nào. Hãy đặt lịch hẹn để chủ nhà chuẩn bị đón tiếp!
                </p>
                <Link
                  href="/listings"
                  className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 transition-colors"
                >
                  <Search className="w-3.5 h-3.5" />
                  <span>Tìm phòng để đặt lịch xem</span>
                </Link>
              </div>
            ) : (
              <div className="space-y-3">
                {upcomingViewings.map((vw) => {
                  const statusColor =
                    vw.status === "CONFIRMED"
                      ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                      : vw.status === "CANCELLED"
                      ? "bg-rose-50 text-rose-700 border-rose-200"
                      : vw.status === "COMPLETED"
                      ? "bg-blue-50 text-blue-700 border-blue-200"
                      : "bg-amber-50 text-amber-700 border-amber-200";

                  const statusLabel =
                    vw.status === "CONFIRMED"
                      ? "Đã xác nhận"
                      : vw.status === "CANCELLED"
                      ? "Đã hủy"
                      : vw.status === "COMPLETED"
                      ? "Hoàn thành"
                      : "Chờ xác nhận";

                  return (
                    <div
                      key={vw.id}
                      className="p-4 rounded-xl border border-slate-200/80 hover:border-blue-200 transition-all flex flex-col sm:flex-row sm:items-center justify-between gap-3"
                    >
                      <div className="space-y-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <span
                            className={cn(
                              "px-2.5 py-0.5 rounded-full text-[10px] font-bold border",
                              statusColor
                            )}
                          >
                            {statusLabel}
                          </span>
                          <span className="text-xs font-bold text-slate-900 truncate">
                            {vw.listingTitle}
                          </span>
                        </div>

                        <div className="flex items-center gap-3 text-[11px] text-slate-500">
                          <span className="inline-flex items-center gap-1 font-semibold text-blue-700">
                            <Clock className="w-3.5 h-3.5" />
                            {new Date(vw.appointmentTime).toLocaleString("vi-VN")}
                          </span>
                          <span>• Chủ nhà: {vw.landlordName}</span>
                        </div>
                      </div>

                      <div className="flex items-center gap-2 shrink-0">
                        <button
                          type="button"
                          onClick={() => router.push(`/messages?listingId=${vw.listingId}`)}
                          className="px-3 py-1.5 rounded-lg bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold transition-colors"
                        >
                          Nhắn tin
                        </button>
                        <Link
                          href="/tenant/viewings"
                          className="px-3 py-1.5 rounded-lg bg-blue-50 hover:bg-blue-100 text-blue-700 text-xs font-bold transition-colors"
                        >
                          Chi tiết
                        </Link>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Right 5 Cols: Thông báo tài khoản mới nhất (Mục 3.4.3) */}
          <div className="lg:col-span-5 bg-white rounded-2xl border border-slate-200/80 shadow-xs p-6 space-y-4">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <div className="flex items-center gap-2">
                <Bell className="w-4 h-4 text-blue-600" />
                <h2 className="font-bold text-slate-900 text-base">
                  Thông Báo Mới Nhất
                </h2>
                {unreadNotificationsCount > 0 && (
                  <span className="px-2 py-0.5 rounded-full bg-rose-500 text-white text-[10px] font-black">
                    {unreadNotificationsCount}
                  </span>
                )}
              </div>

              <Link
                href="/tenant/notifications"
                className="text-xs font-bold text-blue-600 hover:text-blue-700 flex items-center gap-1"
              >
                <span>Xem tất cả</span>
                <ArrowRight className="w-3.5 h-3.5" />
              </Link>
            </div>

            {recentNotifications.length === 0 ? (
              <div className="p-8 text-center space-y-2 bg-slate-50/60 rounded-xl border border-slate-100">
                <Bell className="w-7 h-7 text-slate-300 mx-auto" />
                <p className="text-xs text-slate-500">
                  Hiện chưa có thông báo mới nào cho tài khoản của bạn.
                </p>
              </div>
            ) : (
              <div className="space-y-2.5">
                {recentNotifications.map((notif) => (
                  <div
                    key={notif.id}
                    className={cn(
                      "p-3.5 rounded-xl border transition-all flex items-start justify-between gap-3",
                      notif.isRead
                        ? "bg-white border-slate-200/70"
                        : "bg-blue-50/40 border-blue-200"
                    )}
                  >
                    <div className="space-y-1 min-w-0">
                      <div className="flex items-center gap-2">
                        {!notif.isRead && (
                          <span className="w-2 h-2 rounded-full bg-blue-600 shrink-0" />
                        )}
                        <h4 className="text-xs font-bold text-slate-900 truncate">
                          {notif.title}
                        </h4>
                      </div>
                      <p className="text-[11px] text-slate-600 line-clamp-2">
                        {notif.content}
                      </p>
                      <span className="text-[10px] text-slate-400 block">
                        {new Date(notif.createdAt).toLocaleString("vi-VN")}
                      </span>
                    </div>

                    {!notif.isRead && (
                      <button
                        type="button"
                        onClick={() => handleMarkNotificationRead(notif)}
                        className="p-1.5 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 text-emerald-600 shrink-0"
                        title="Đánh dấu đã đọc"
                      >
                        <Check className="w-3.5 h-3.5" />
                      </button>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* TAB 2: Viewed Listings History (Mục 3.4.2) */}
      {activeTab === "history" && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <p className="text-xs text-slate-500">
              Danh sách các phòng trọ bạn đã truy cập gần đây.
            </p>
            <Link
              href="/tenant/viewed-listings"
              className="text-xs font-bold text-blue-600 hover:text-blue-700 flex items-center gap-1"
            >
              <span>Mở trang Lịch sử xem phòng đầy đủ</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </Link>
          </div>

          {viewedListings.length === 0 ? (
            <div className="bg-white rounded-2xl border border-slate-200/80 p-12 text-center text-xs text-slate-500">
              Chưa có lịch sử xem phòng nào được ghi nhận.
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              {viewedListings.map((item) => {
                const listingSlug = `lst-00${item.listingId <= 3 ? item.listingId : 1}`;
                const defaultImg =
                  item.primaryImageUrl ||
                  "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=800&q=80";

                return (
                  <div
                    key={item.viewId}
                    className="bg-white rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-all overflow-hidden flex flex-col justify-between"
                  >
                    <div>
                      <div className="aspect-[16/10] bg-slate-100 relative overflow-hidden">
                        <img
                          src={defaultImg}
                          alt={item.title}
                          className="w-full h-full object-cover"
                        />
                        <div className="absolute bottom-3 right-3 px-3 py-1 rounded-xl bg-blue-600 text-white text-xs font-black shadow-md">
                          {item.price.toLocaleString("vi-VN")} đ/tháng
                        </div>
                      </div>

                      <div className="p-5 space-y-2.5">
                        <Link href={`/listings/${listingSlug}`}>
                          <h3 className="font-bold text-slate-900 text-sm hover:text-blue-600 line-clamp-1">
                            {item.title}
                          </h3>
                        </Link>

                        <div className="flex items-start gap-1.5 text-xs text-slate-500">
                          <MapPin className="w-3.5 h-3.5 text-slate-400 shrink-0 mt-0.5" />
                          <span className="line-clamp-1">
                            {item.address || "TP. Hồ Chí Minh"}
                          </span>
                        </div>

                        <div className="flex items-center justify-between pt-2 border-t border-slate-100 text-xs text-slate-600">
                          <span className="inline-flex items-center gap-1 font-semibold">
                            <Maximize2 className="w-3.5 h-3.5 text-blue-600" />
                            <span>{item.areaSqm} m²</span>
                          </span>
                          <span className="inline-flex items-center gap-1 text-slate-500">
                            <User className="w-3.5 h-3.5 text-slate-400" />
                            <span>{item.landlordName}</span>
                          </span>
                        </div>
                      </div>
                    </div>

                    <div className="px-5 pb-5 pt-1 flex items-center gap-2">
                      <button
                        type="button"
                        onClick={() =>
                          router.push(`/messages?listingId=${item.listingId}`)
                        }
                        className="flex-1 py-2 px-3 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-800 text-xs font-bold transition-colors flex items-center justify-center gap-1.5"
                      >
                        <MessageSquare className="w-3.5 h-3.5 text-blue-600" />
                        <span>Chat chủ nhà</span>
                      </button>

                      <Link
                        href={`/listings/${listingSlug}`}
                        className="flex-1 py-2 px-3 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold transition-colors flex items-center justify-center gap-1.5"
                      >
                        <Calendar className="w-3.5 h-3.5" />
                        <span>Đặt lịch</span>
                      </Link>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}

      {/* TAB 3: Contracts */}
      {activeTab === "contracts" && (
        <div className="space-y-4">
          {contracts.length === 0 ? (
            <div className="p-12 text-center space-y-3 bg-white rounded-2xl border border-slate-200/80 shadow-xs">
              <FileText className="w-10 h-10 text-slate-300 mx-auto" />
              <h4 className="text-sm font-bold text-slate-800">Chưa Có Hợp Đồng Thuê Nhà</h4>
              <p className="text-xs text-slate-500 max-w-md mx-auto leading-relaxed">
                Bạn hiện chưa có hợp đồng thuê nhà điện tử nào trên hệ thống. Khi bạn ký kết hợp đồng với chủ trọ, các thông tin hợp đồng, tiền cọc và thời hạn sẽ hiển thị tại đây.
              </p>
            </div>
          ) : (
            contracts.map((ctr) => (
              <div
                key={ctr.id}
                className="bg-white rounded-2xl border border-slate-200/80 shadow-xs p-6 space-y-4"
              >
                <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2 pb-3 border-b border-slate-100">
                  <div className="flex items-center gap-3">
                    <span className="font-mono text-xs font-bold bg-slate-100 px-2.5 py-1 rounded-md text-slate-800">
                      {ctr.contractNumber}
                    </span>
                    <h3 className="font-bold text-slate-900 text-base">{ctr.roomName}</h3>
                  </div>

                  <div className="flex items-center gap-2">
                    <span
                      className={cn(
                        "px-3 py-1 rounded-full text-xs font-bold border",
                        ctr.status === "Còn hiệu lực"
                          ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                          : "bg-amber-50 text-amber-700 border-amber-200"
                      )}
                    >
                      {ctr.status} ({ctr.daysRemaining} ngày còn lại)
                    </span>
                  </div>
                </div>

                <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs">
                  <div>
                    <span className="text-slate-400 block font-medium">Giá thuê hàng tháng</span>
                    <span className="font-black text-blue-600 text-sm">
                      {ctr.monthlyRent.toLocaleString()} đ/tháng
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block font-medium">Tiền cọc giữ chỗ</span>
                    <span className="font-bold text-slate-800 text-sm">
                      {ctr.depositAmount.toLocaleString()} đ
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block font-medium">Thời hạn hợp đồng</span>
                    <span className="font-medium text-slate-700">
                      {ctr.startDate} → {ctr.endDate}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block font-medium">Kỳ hạn thanh toán</span>
                    <span className="font-medium text-slate-700">
                      {ctr.paymentCycle} tháng / lần
                    </span>
                  </div>
                </div>

                {ctr.terms && (
                  <div className="p-3 bg-slate-50 rounded-xl text-xs text-slate-600 border border-slate-100">
                    <span className="font-bold text-slate-800 block mb-0.5">Điều khoản chính:</span>
                    {ctr.terms}
                  </div>
                )}
              </div>
            ))
          )}
        </div>
      )}

      {/* TAB 4: Monthly Bills */}
      {activeTab === "bills" && (
        <div className="space-y-4">
          {bills.length === 0 ? (
            <div className="p-12 text-center space-y-3 bg-white rounded-2xl border border-slate-200/80 shadow-xs">
              <CreditCard className="w-10 h-10 text-slate-300 mx-auto" />
              <h4 className="text-sm font-bold text-slate-800">Chưa Có Hóa Đơn Cần Thanh Toán</h4>
              <p className="text-xs text-slate-500 max-w-md mx-auto leading-relaxed">
                Bạn chưa có hóa đơn tiền phòng hoặc hóa đơn dịch vụ điện nước nào phát sinh trong kỳ này.
              </p>
            </div>
          ) : (
            bills.map((bill) => (
            <div
              key={bill.id}
              className="bg-white rounded-2xl border border-slate-200/80 shadow-xs p-6 space-y-4"
            >
              <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2 pb-3 border-b border-slate-100">
                <div>
                  <span className="text-xs font-bold text-blue-600 uppercase tracking-wider block">
                    Kỳ thanh toán: {bill.period}
                  </span>
                  <h3 className="font-bold text-slate-900 text-base">{bill.roomName}</h3>
                </div>

                <div className="flex items-center gap-3">
                  <span
                    className={cn(
                      "px-3 py-1 rounded-full text-xs font-bold border",
                      bill.status === "Đã thanh toán"
                        ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                        : "bg-rose-50 text-rose-700 border-rose-200"
                    )}
                  >
                    {bill.status}
                  </span>

                  {bill.remainingDebt > 0 && (
                    <button
                      type="button"
                      onClick={() => handlePayBill(bill.id, bill.remainingDebt)}
                      className="px-4 py-1.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-sm transition-colors flex items-center gap-1.5"
                    >
                      <CreditCard className="w-3.5 h-3.5" />
                      <span>Thanh toán ngay ({bill.remainingDebt.toLocaleString()} đ)</span>
                    </button>
                  )}
                </div>
              </div>

              <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs">
                <div className="p-3 rounded-xl bg-slate-50 border border-slate-100">
                  <span className="text-slate-500 block">Tiền phòng cơ bản</span>
                  <span className="font-bold text-slate-900 text-sm">
                    {bill.rentAmount.toLocaleString()} đ
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-amber-50/60 border border-amber-100">
                  <span className="text-amber-800 font-medium block">
                    Tiền điện ({bill.electricityUsage} kWh)
                  </span>
                  <span className="font-bold text-amber-950 text-sm">
                    {bill.electricityCost.toLocaleString()} đ
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-sky-50/60 border border-sky-100">
                  <span className="text-sky-800 font-medium block">
                    Tiền nước ({bill.waterUsage} m³)
                  </span>
                  <span className="font-bold text-sky-950 text-sm">
                    {bill.waterCost.toLocaleString()} đ
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-slate-50 border border-slate-100">
                  <span className="text-slate-500 block">Phí dịch vụ chung</span>
                  <span className="font-bold text-slate-900 text-sm">
                    {bill.serviceCost.toLocaleString()} đ
                  </span>
                </div>
              </div>

              <div className="p-4 rounded-xl bg-slate-900 text-white flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 text-xs">
                <div>
                  <span className="text-slate-400 block">
                    Công thức công nợ: Còn nợ = Tổng phải thu − Đã thanh toán
                  </span>
                  <span className="text-sm font-semibold text-slate-200">
                    Hạn thanh toán: trước ngày {bill.dueDate}
                  </span>
                </div>

                <div className="flex items-center gap-6">
                  <div>
                    <span className="text-slate-400 block text-[11px]">Tổng phải thu</span>
                    <span className="font-black text-white text-base">
                      {bill.totalDue.toLocaleString()} đ
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Đã thanh toán</span>
                    <span className="font-bold text-emerald-400 text-base">
                      {bill.paidAmount.toLocaleString()} đ
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Số tiền còn nợ</span>
                    <span className="font-black text-rose-400 text-base">
                      {bill.remainingDebt.toLocaleString()} đ
                    </span>
                  </div>
                </div>
              </div>
            </div>
            ))
          )}
        </div>
      )}
    </div>
  );
}
