"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { listingService } from "@/lib/services";
import { getStoredToken, notificationsApi } from "@/lib/api-client";
import { NotificationDto } from "@/types";
import {
  Home,
  Search,
  Heart,
  Scale,
  Calendar,
  MessageSquare,
  Building2,
  FileText,
  Zap,
  ShieldCheck,
  User,
  LogOut,
  ChevronDown,
  Menu,
  X,
  PlusCircle,
  Loader2,
  Bell,
  Eye,
  CheckCheck,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface NavItem {
  label: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
}

export function Navbar() {
  const rawPathname = usePathname();
  const pathname = rawPathname || "";
  const router = useRouter();
  const { user, role, logout } = useAuth();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [userDropdownOpen, setUserDropdownOpen] = useState(false);
  const [notifDropdownOpen, setNotifDropdownOpen] = useState(false);
  const [loggingOut, setLoggingOut] = useState(false);
  const [favCount, setFavCount] = useState(0);
  const [compareCount, setCompareCount] = useState(0);
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [unreadNotifCount, setUnreadNotifCount] = useState(0);

  const fetchNotificationsSummary = useCallback(async () => {
    if (!user || !getStoredToken()) {
      setNotifications([]);
      setUnreadNotifCount(0);
      return;
    }
    const res = await notificationsApi.getNotifications({ page: 1, pageSize: 6 });
    if (res.isSuccess && res.data) {
      setNotifications(res.data.items);
      setUnreadNotifCount(res.data.unreadCount);
    }
  }, [user]);

  useEffect(() => {
    const updateCounters = () => {
      setFavCount(listingService.getFavorites().length);
      setCompareCount(listingService.getCompareList().length);
    };
    updateCounters();
    const interval = setInterval(updateCounters, 1500);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    fetchNotificationsSummary();
    const notifInterval = setInterval(fetchNotificationsSummary, 15000);
    return () => clearInterval(notifInterval);
  }, [fetchNotificationsSummary, pathname]);

  // Guest navigation
  const guestNav: NavItem[] = [
    { label: "Tìm phòng", href: "/listings", icon: Search },
  ];

  // Tenant / Public navigation (Mục 3.3 & 3.4)
  const tenantNav: NavItem[] = [
    { label: "Tìm phòng", href: "/listings", icon: Search },
    { label: "Lịch xem", href: "/tenant/viewings", icon: Calendar },
    { label: "Phòng đã xem", href: "/tenant/viewed-listings", icon: Eye },
    { label: "Tổng quan", href: "/tenant/dashboard", icon: FileText },
  ];

  // Landlord dedicated navigation
  const landlordNav: NavItem[] = [
    { label: "Tổng quan", href: "/landlord/dashboard", icon: Building2 },
    { label: "Phòng trọ", href: "/landlord/properties", icon: Home },
    { label: "Tin đăng", href: "/landlord/listings", icon: FileText },
    { label: "Lịch xem phòng", href: "/landlord/viewings", icon: Calendar },
    { label: "Hợp đồng", href: "/landlord/contracts", icon: FileText },
    { label: "Điện nước", href: "/landlord/utilities", icon: Zap },
  ];

  // Admin navigation
  const adminNav: NavItem[] = [
    { label: "Quản trị", href: "/admin/dashboard", icon: ShieldCheck },
    { label: "Sàn tin", href: "/listings", icon: Search },
  ];

  const currentNav =
    role === "Landlord"
      ? landlordNav
      : role === "Admin"
      ? adminNav
      : role === "Tenant"
      ? tenantNav
      : guestNav;

  return (
    <header className="sticky top-0 z-40 border-b border-slate-200/90 bg-white/95 backdrop-blur-md shadow-xs">
      <div className="max-w-[1440px] mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16 gap-3 sm:gap-4">
          {/* Logo */}
          <Link
            href={role === "Landlord" ? "/landlord/dashboard" : "/"}
            className="flex items-center gap-2 shrink-0 group"
          >
            <div className="w-9 h-9 rounded-xl bg-blue-600 flex items-center justify-center text-white font-black text-lg shadow-md shadow-blue-500/25 group-hover:scale-105 transition-transform">
              <Home className="w-4.5 h-4.5" />
            </div>
            <div className="flex flex-col">
              <span className="font-black text-lg tracking-tight text-slate-900 group-hover:text-blue-600 transition-colors whitespace-nowrap">
                HomeMatch
              </span>
              <span className="text-[9px] text-blue-600 font-bold tracking-wider uppercase -mt-1 hidden sm:inline whitespace-nowrap">
                Sàn Thuê & Quản Lý Trọ
              </span>
            </div>
          </Link>

          {/* Right Action Cluster: Nav Links + Quick Action Buttons + CTA + Profile */}
          <div className="flex items-center gap-1 sm:gap-2">
            {/* Main Navigation Links */}
            <nav className="hidden lg:flex items-center gap-1">
              {currentNav.map((item) => {
                const Icon = item.icon;
                const isActive = pathname === item.href || (item.href !== "/" && pathname.startsWith(item.href));
                return (
                  <Link
                    key={item.href}
                    href={item.href}
                    className={cn(
                      "flex items-center gap-1.5 px-2.5 py-1.5 rounded-xl text-xs font-bold transition-all whitespace-nowrap",
                      isActive
                        ? "text-blue-600 bg-blue-50/80 shadow-xs"
                        : "text-slate-600 hover:text-slate-900 hover:bg-slate-100/80"
                    )}
                  >
                    <Icon className="w-4 h-4 shrink-0" />
                    <span className="whitespace-nowrap">{item.label}</span>
                  </Link>
                );
              })}
            </nav>

            {/* Compare Quick Button - Only for Tenant / Guest */}
            {role !== "Landlord" && role !== "Admin" && (
              <Link
                href="/compare"
                className={cn(
                  "hidden lg:flex items-center justify-center w-8 h-8 rounded-xl text-xs font-bold transition-all relative shrink-0",
                  pathname === "/compare"
                    ? "text-blue-600 bg-blue-50/80 shadow-xs"
                    : "text-slate-600 hover:text-blue-600 hover:bg-slate-100/80"
                )}
                title="So sánh phòng đối chiếu"
              >
                <Scale className="w-4 h-4 shrink-0" />
                {compareCount > 0 && (
                  <span className="absolute -top-1 -right-1 w-4 h-4 rounded-full bg-blue-600 text-white text-[9px] font-black flex items-center justify-center shadow-xs">
                    {compareCount}
                  </span>
                )}
              </Link>
            )}

            {/* Favorites Quick Button - Only for Tenant / Guest */}
            {role !== "Landlord" && role !== "Admin" && (
              <Link
                href="/tenant/favorites"
                className={cn(
                  "hidden lg:flex items-center justify-center w-8 h-8 rounded-xl text-xs font-bold transition-all relative shrink-0",
                  pathname === "/tenant/favorites"
                    ? "text-rose-600 bg-rose-50/80 shadow-xs"
                    : "text-slate-600 hover:text-rose-600 hover:bg-slate-100/80"
                )}
                title="Danh sách phòng đã lưu"
              >
                <Heart className="w-4 h-4 shrink-0" />
                {favCount > 0 && (
                  <span className="absolute -top-1 -right-1 w-4 h-4 rounded-full bg-rose-500 text-white text-[9px] font-black flex items-center justify-center shadow-xs">
                    {favCount}
                  </span>
                )}
              </Link>
            )}

            {/* Messages Quick Button (Mục 3.3.2) */}
            <Link
              href="/messages"
              className={cn(
                "hidden lg:flex items-center justify-center w-8 h-8 rounded-xl text-xs font-bold transition-all relative shrink-0",
                pathname === "/messages"
                  ? "text-blue-600 bg-blue-50/80 shadow-xs"
                  : "text-slate-600 hover:text-blue-600 hover:bg-slate-100/80"
              )}
              title="Hộp thư tin nhắn trực tiếp"
            >
              <MessageSquare className="w-4 h-4 shrink-0" />
              <span className="absolute top-1 right-1 w-2 h-2 rounded-full bg-rose-500 ring-2 ring-white" />
            </Link>

            {/* Notifications Bell Dropdown (Mục 3.4.3) */}
            {user && (
              <div className="relative shrink-0">
                <button
                  type="button"
                  onClick={() => {
                    setNotifDropdownOpen(!notifDropdownOpen);
                    if (!notifDropdownOpen) fetchNotificationsSummary();
                  }}
                  className={cn(
                    "flex items-center justify-center w-8 h-8 rounded-xl text-xs font-bold transition-all relative shrink-0",
                    pathname === "/tenant/notifications" || notifDropdownOpen
                      ? "text-blue-600 bg-blue-50/80 shadow-xs"
                      : "text-slate-600 hover:text-blue-600 hover:bg-slate-100/80"
                  )}
                  title="Thông báo tài khoản"
                >
                  <Bell className="w-4 h-4 shrink-0" />
                  {unreadNotifCount > 0 && (
                    <span className="absolute -top-1 -right-1 min-w-4 h-4 px-1 rounded-full bg-rose-500 text-white text-[9px] font-black flex items-center justify-center shadow-xs">
                      {unreadNotifCount > 9 ? "9+" : unreadNotifCount}
                    </span>
                  )}
                </button>

                {notifDropdownOpen && (
                  <>
                    <div
                      className="fixed inset-0 z-40"
                      onClick={() => setNotifDropdownOpen(false)}
                    />
                    <div className="fixed inset-x-3 top-16 sm:inset-x-auto sm:right-0 sm:top-auto sm:mt-2 w-auto sm:w-96 bg-white rounded-2xl shadow-2xl border border-slate-100 py-2 z-50 animate-in fade-in zoom-in-95 max-h-[80vh] flex flex-col">
                      <div className="px-4 py-2.5 border-b border-slate-100 flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <span className="text-xs font-black text-slate-900">
                            Thông báo của bạn
                          </span>
                          {unreadNotifCount > 0 && (
                            <span className="px-2 py-0.5 rounded-full bg-rose-50 text-rose-600 text-[10px] font-bold">
                              {unreadNotifCount} mới
                            </span>
                          )}
                        </div>
                        {unreadNotifCount > 0 && (
                          <button
                            type="button"
                            onClick={async () => {
                              await notificationsApi.markAllAsRead();
                              setUnreadNotifCount(0);
                              setNotifications((prev) =>
                                prev.map((n) => ({ ...n, isRead: true }))
                              );
                            }}
                            className="text-[11px] font-bold text-blue-600 hover:text-blue-700 inline-flex items-center gap-1"
                          >
                            <CheckCheck className="w-3 h-3" />
                            <span>Đọc tất cả</span>
                          </button>
                        )}
                      </div>

                      <div className="max-h-80 overflow-y-auto divide-y divide-slate-100">
                        {notifications.length === 0 ? (
                          <div className="p-6 text-center text-xs text-slate-400">
                            Bạn chưa có thông báo mới nào.
                          </div>
                        ) : (
                          notifications.map((n) => (
                            <button
                              key={n.id}
                              type="button"
                              onClick={async () => {
                                if (!n.isRead) {
                                  await notificationsApi.markAsRead(n.id);
                                  setUnreadNotifCount((prev) => Math.max(0, prev - 1));
                                }
                                setNotifDropdownOpen(false);
                                const t = (n.type || "").toUpperCase();
                                if (t.includes("VIEWING")) {
                                  router.push(role === "Landlord" ? "/landlord/viewings" : "/tenant/viewings");
                                } else if (t.includes("MESSAGE")) {
                                  router.push(
                                    n.referenceId
                                      ? `/messages?conversationId=${n.referenceId}`
                                      : "/messages"
                                  );
                                } else {
                                  router.push("/tenant/notifications");
                                }
                              }}
                              className={cn(
                                "w-full px-4 py-3 text-left hover:bg-slate-50 transition-colors flex items-start gap-2.5",
                                !n.isRead ? "bg-blue-50/40" : ""
                              )}
                            >
                              <span
                                className={cn(
                                  "w-2 h-2 rounded-full mt-1.5 shrink-0",
                                  !n.isRead ? "bg-blue-600" : "bg-slate-200"
                                )}
                              />
                              <div className="min-w-0 flex-1">
                                <p
                                  className={cn(
                                    "text-xs truncate",
                                    !n.isRead
                                      ? "font-bold text-slate-900"
                                      : "font-medium text-slate-700"
                                  )}
                                >
                                  {n.title}
                                </p>
                                <p className="text-[11px] text-slate-500 line-clamp-2 mt-0.5">
                                  {n.content}
                                </p>
                                <span className="text-[10px] text-slate-400 block mt-1">
                                  {new Date(n.createdAt).toLocaleString("vi-VN")}
                                </span>
                              </div>
                            </button>
                          ))
                        )}
                      </div>

                      <div className="px-4 pt-2 pb-1 border-t border-slate-100 text-center">
                        <Link
                          href="/tenant/notifications"
                          onClick={() => setNotifDropdownOpen(false)}
                          className="text-xs font-bold text-blue-600 hover:text-blue-700 block py-1"
                        >
                          Xem tất cả thông báo →
                        </Link>
                      </div>
                    </div>
                  </>
                )}
              </div>
            )}

            <div className="h-5 w-px bg-slate-200 hidden lg:block mx-0.5" />

            {/* Standout Commercial CTA Button */}
            {role === "Landlord" ? (
              <Link
                href="/landlord/listings"
                className="hidden lg:inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 shadow-sm transition-all whitespace-nowrap shrink-0"
              >
                <PlusCircle className="w-3.5 h-3.5 shrink-0" />
                <span>Đăng tin</span>
              </Link>
            ) : !user ? (
              <Link
                href="/landlord/dashboard"
                className="hidden lg:inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-bold text-blue-700 bg-blue-50/90 hover:bg-blue-100 border border-blue-200/90 shadow-2xs transition-all whitespace-nowrap shrink-0"
              >
                <Building2 className="w-3.5 h-3.5 shrink-0 text-blue-600" />
                <span>Kênh Chủ Nhà</span>
              </Link>
            ) : null}

            {/* User Profile Menu or High-Contrast Auth Buttons */}
            {user ? (
              <div className="hidden lg:block relative shrink-0">
                <button
                  onClick={() => setUserDropdownOpen(!userDropdownOpen)}
                  className="flex items-center gap-1.5 p-1 rounded-xl hover:bg-slate-100 text-slate-700 transition-colors shrink-0"
                  title={user.displayName}
                >
                  <div className="w-8 h-8 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold text-xs shadow-xs shrink-0 overflow-hidden">
                    {user.avatarUrl ? (
                      <img src={user.avatarUrl} alt={user.displayName} className="w-full h-full object-cover" />
                    ) : (
                      user.displayName.charAt(0).toUpperCase()
                    )}
                  </div>
                  <ChevronDown className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                </button>

                {userDropdownOpen && (
                  <>
                    <div
                      className="fixed inset-0 z-40"
                      onClick={() => setUserDropdownOpen(false)}
                    />
                    <div className="fixed inset-x-3 top-16 sm:inset-x-auto sm:right-0 sm:top-auto sm:mt-2 w-auto sm:w-72 bg-white rounded-2xl shadow-2xl border border-slate-100 py-2 z-50 animate-in fade-in zoom-in-95 max-h-[80vh] overflow-y-auto">
                      <div className="px-4 py-3 border-b border-slate-100">
                        <div className="flex items-center gap-2">
                          <p className="text-xs font-bold text-slate-900 truncate">{user.displayName}</p>
                          {user.emailConfirmed && (
                            <span title="Email đã xác thực" className="inline-flex text-emerald-600">
                              <ShieldCheck className="w-3.5 h-3.5" />
                            </span>
                          )}
                        </div>
                        <p className="text-[11px] text-slate-500 truncate">{user.email}</p>
                        <div className="mt-1.5 flex items-center gap-1.5">
                          <span
                            className={cn(
                              "px-2 py-0.5 rounded-md text-[10px] font-bold",
                              role === "Landlord"
                                ? "bg-emerald-50 text-emerald-700 border border-emerald-200"
                                : role === "Admin"
                                ? "bg-purple-50 text-purple-700 border border-purple-200"
                                : "bg-blue-50 text-blue-700 border border-blue-200"
                            )}
                          >
                            {role === "Landlord"
                              ? "Tài khoản Chủ nhà"
                              : role === "Admin"
                              ? "Quản trị viên"
                              : "Tài khoản Người thuê"}
                          </span>
                        </div>
                      </div>

                      {/* Direct role dashboard links */}
                      {role === "Landlord" && (
                        <>
                          <Link
                            href="/landlord/dashboard"
                            onClick={() => setUserDropdownOpen(false)}
                            className="flex items-center gap-2.5 px-4 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 hover:text-blue-600"
                          >
                            <Building2 className="w-4 h-4 text-slate-400" />
                            <span>Kênh quản lý Chủ nhà</span>
                          </Link>
                          <Link
                            href="/landlord/viewings"
                            onClick={() => setUserDropdownOpen(false)}
                            className="flex items-center gap-2.5 px-4 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 hover:text-blue-600"
                          >
                            <Calendar className="w-4 h-4 text-slate-400" />
                            <span>Lịch hẹn xem phòng của khách</span>
                          </Link>
                          <Link
                            href="/landlord/listings"
                            onClick={() => setUserDropdownOpen(false)}
                            className="flex items-center gap-2.5 px-4 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 hover:text-blue-600"
                          >
                            <FileText className="w-4 h-4 text-slate-400" />
                            <span>Quản lý tin đăng cho thuê</span>
                          </Link>
                          <Link
                            href="/landlord/properties"
                            onClick={() => setUserDropdownOpen(false)}
                            className="flex items-center gap-2.5 px-4 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 hover:text-blue-600"
                          >
                            <Home className="w-4 h-4 text-slate-400" />
                            <span>Quản lý phòng trọ & căn hộ</span>
                          </Link>
                          <Link
                            href="/landlord/contracts"
                            onClick={() => setUserDropdownOpen(false)}
                            className="flex items-center gap-2.5 px-4 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 hover:text-blue-600"
                          >
                            <FileText className="w-4 h-4 text-slate-400" />
                            <span>Hợp đồng & Điện nước</span>
                          </Link>
                        </>
                      )}
                      {role === "Tenant" && (
                        <>
                          <Link
                            href="/tenant/dashboard"
                            onClick={() => setUserDropdownOpen(false)}
                            className="flex items-center gap-2.5 px-4 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 hover:text-blue-600"
                          >
                            <FileText className="w-4 h-4 text-slate-400" />
                            <span>Dashboard tìm phòng & Hợp đồng</span>
                          </Link>
                          <Link
                            href="/tenant/viewings"
                            onClick={() => setUserDropdownOpen(false)}
                            className="flex items-center gap-2.5 px-4 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 hover:text-blue-600"
                          >
                            <Calendar className="w-4 h-4 text-slate-400" />
                            <span>Quản lý lịch xem phòng</span>
                          </Link>
                          <Link
                            href="/tenant/viewed-listings"
                            onClick={() => setUserDropdownOpen(false)}
                            className="flex items-center gap-2.5 px-4 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 hover:text-blue-600"
                          >
                            <Eye className="w-4 h-4 text-slate-400" />
                            <span>Danh sách phòng đã xem</span>
                          </Link>
                        </>
                      )}

                      <Link
                        href="/tenant/notifications"
                        onClick={() => setUserDropdownOpen(false)}
                        className="flex items-center justify-between px-4 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 hover:text-blue-600"
                      >
                        <span className="flex items-center gap-2.5">
                          <Bell className="w-4 h-4 text-slate-400" />
                          <span>Trung tâm thông báo</span>
                        </span>
                        {unreadNotifCount > 0 && (
                          <span className="px-1.5 py-0.5 rounded-full bg-rose-500 text-white text-[10px] font-bold">
                            {unreadNotifCount}
                          </span>
                        )}
                      </Link>

                      <Link
                        href="/settings"
                        onClick={() => setUserDropdownOpen(false)}
                        className="flex items-center gap-2.5 px-4 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 hover:text-blue-600"
                      >
                        <User className="w-4 h-4 text-slate-400" />
                        <span>Cài đặt tài khoản & Bảo mật</span>
                      </Link>

                      <div className="my-1 border-t border-slate-100" />

                      {/* Normal logout */}
                      <button
                        disabled={loggingOut}
                        onClick={async () => {
                          setLoggingOut(true);
                          await logout();
                          setUserDropdownOpen(false);
                          setLoggingOut(false);
                        }}
                        className="w-full flex items-center gap-2.5 px-4 py-2 text-xs font-medium text-rose-600 hover:bg-rose-50 disabled:opacity-50 cursor-pointer"
                      >
                        {loggingOut ? (
                          <Loader2 className="w-4 h-4 text-rose-500 animate-spin" />
                        ) : (
                          <LogOut className="w-4 h-4 text-rose-500" />
                        )}
                        <span>{loggingOut ? "Đang đăng xuất..." : "Đăng xuất"}</span>
                      </button>
                    </div>
                  </>
                )}
              </div>
            ) : (
              <div className="hidden lg:flex items-center gap-2 ml-1">
                <Link
                  href="/auth/login"
                  className="px-3.5 py-2 text-xs font-bold text-slate-700 bg-slate-100 hover:bg-slate-200 hover:text-slate-900 border border-slate-200/90 rounded-xl transition-all whitespace-nowrap shadow-2xs"
                >
                  Đăng nhập
                </Link>
                <Link
                  href="/auth/register"
                  className="px-4 py-2 text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 rounded-xl shadow-sm shadow-blue-500/25 transition-all whitespace-nowrap"
                >
                  Đăng ký
                </Link>
              </div>
            )}

            {/* Mobile Menu Toggle Button */}
            <button
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              className="lg:hidden p-2 text-slate-600 hover:text-slate-900 rounded-xl hover:bg-slate-100"
            >
              {mobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
            </button>
          </div>
        </div>

          {/* Mobile Drawer */}
          {mobileMenuOpen && (
            <div className="lg:hidden border-t border-slate-100 py-3 space-y-1 animate-in fade-in">
              {user ? (
                <div className="px-3 py-2.5 bg-slate-50 rounded-xl mb-2 flex items-center justify-between">
                  <div>
                    <p className="text-xs font-bold text-slate-900">{user.displayName}</p>
                    <p className="text-[10px] text-slate-500">{user.email}</p>
                  </div>
                  <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-blue-50 text-blue-700">
                    {role === "Landlord" ? "Chủ nhà" : role === "Admin" ? "Admin" : "Người thuê"}
                  </span>
                </div>
              ) : (
                <div className="grid grid-cols-2 gap-2 p-1 mb-2">
                  <Link
                    href="/auth/login"
                    onClick={() => setMobileMenuOpen(false)}
                    className="py-2 text-center text-xs font-bold text-slate-700 bg-slate-100 rounded-xl"
                  >
                    Đăng nhập
                  </Link>
                  <Link
                    href="/auth/register"
                    onClick={() => setMobileMenuOpen(false)}
                    className="py-2 text-center text-xs font-bold text-white bg-blue-600 rounded-xl"
                  >
                    Đăng ký
                  </Link>
                </div>
              )}

              {/* Mobile Extended Drawer Utilities (Không trùng lặp với Bottom Nav) */}
              <div className="py-1 space-y-0.5">
                {role === "Landlord" ? (
                  <>
                    <Link
                      href="/landlord/contracts"
                      onClick={() => setMobileMenuOpen(false)}
                      className="flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-xs font-bold text-slate-700 hover:bg-slate-100"
                    >
                      <FileText className="w-4 h-4 text-slate-500" />
                      <span>Hợp đồng thuê & Cọc</span>
                    </Link>
                    <Link
                      href="/landlord/utilities"
                      onClick={() => setMobileMenuOpen(false)}
                      className="flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-xs font-bold text-slate-700 hover:bg-slate-100"
                    >
                      <Zap className="w-4 h-4 text-slate-500" />
                      <span>Quản lý điện nước</span>
                    </Link>
                    <Link
                      href="/listings"
                      onClick={() => setMobileMenuOpen(false)}
                      className="flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-xs font-bold text-slate-700 hover:bg-slate-100"
                    >
                      <Search className="w-4 h-4 text-slate-500" />
                      <span>Khám phá sàn tin thuê</span>
                    </Link>
                  </>
                ) : role === "Admin" ? (
                  <>
                    <Link
                      href="/admin/dashboard"
                      onClick={() => setMobileMenuOpen(false)}
                      className="flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-xs font-bold text-slate-700 hover:bg-slate-100"
                    >
                      <ShieldCheck className="w-4 h-4 text-slate-500" />
                      <span>Quản trị hệ thống</span>
                    </Link>
                    <Link
                      href="/listings"
                      onClick={() => setMobileMenuOpen(false)}
                      className="flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-xs font-bold text-slate-700 hover:bg-slate-100"
                    >
                      <Search className="w-4 h-4 text-slate-500" />
                      <span>Sàn tin phòng</span>
                    </Link>
                  </>
                ) : (
                  <>
                    <Link
                      href="/tenant/viewings"
                      onClick={() => setMobileMenuOpen(false)}
                      className="flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-xs font-bold text-slate-700 hover:bg-slate-100"
                    >
                      <Calendar className="w-4 h-4 text-slate-500" />
                      <span>Lịch hẹn xem phòng</span>
                    </Link>
                    <Link
                      href="/tenant/viewed-listings"
                      onClick={() => setMobileMenuOpen(false)}
                      className="flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-xs font-bold text-slate-700 hover:bg-slate-100"
                    >
                      <Eye className="w-4 h-4 text-slate-500" />
                      <span>Phòng đã xem gần đây</span>
                    </Link>
                    <Link
                      href="/compare"
                      onClick={() => setMobileMenuOpen(false)}
                      className="flex items-center justify-between px-3.5 py-2.5 rounded-xl text-xs font-bold text-slate-700 hover:bg-slate-100"
                    >
                      <div className="flex items-center gap-3">
                        <Scale className="w-4 h-4 text-blue-600" />
                        <span>So sánh đối chiếu</span>
                      </div>
                      {compareCount > 0 && (
                        <span className="px-2 py-0.5 rounded-full bg-blue-600 text-white text-[10px] font-bold">
                          {compareCount}
                        </span>
                      )}
                    </Link>
                    {!user && (
                      <Link
                        href="/landlord/dashboard"
                        onClick={() => setMobileMenuOpen(false)}
                        className="flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-xs font-bold text-slate-700 hover:bg-slate-100"
                      >
                        <Building2 className="w-4 h-4 text-slate-500" />
                        <span>Kênh dành cho Chủ nhà</span>
                      </Link>
                    )}
                  </>
                )}

                {user && (
                  <Link
                    href="/settings"
                    onClick={() => setMobileMenuOpen(false)}
                    className="flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-xs font-bold text-slate-700 hover:bg-slate-100"
                  >
                    <User className="w-4 h-4 text-slate-500" />
                    <span>Cài đặt tài khoản</span>
                  </Link>
                )}
              </div>

              {user && (
                <div className="pt-2 border-t border-slate-100 space-y-1.5">
                  <button
                    disabled={loggingOut}
                    onClick={async () => {
                      setLoggingOut(true);
                      await logout();
                      setMobileMenuOpen(false);
                      setLoggingOut(false);
                    }}
                    className="flex items-center justify-center gap-2 w-full py-2.5 px-4 rounded-xl text-xs font-bold text-rose-600 hover:bg-rose-50 border border-rose-200 disabled:opacity-50"
                  >
                    {loggingOut ? (
                      <Loader2 className="w-4 h-4 text-rose-500 animate-spin" />
                    ) : (
                      <LogOut className="w-4 h-4" />
                    )}
                    <span>{loggingOut ? "Đang đăng xuất..." : "Đăng xuất"}</span>
                  </button>
                </div>
              )}
            </div>
          )}
        </div>
      </header>
  );
}
