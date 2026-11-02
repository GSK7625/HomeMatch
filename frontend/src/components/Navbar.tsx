"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { listingService } from "@/lib/services";
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
  Phone,
  PlusCircle,
  HelpCircle,
  Smartphone,
  Sparkles,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface NavItem {
  label: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
}

export function Navbar() {
  const pathname = usePathname();
  const { user, role, logout, switchDemoRole } = useAuth();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [userDropdownOpen, setUserDropdownOpen] = useState(false);
  const [favCount, setFavCount] = useState(0);
  const [compareCount, setCompareCount] = useState(0);

  useEffect(() => {
    const updateCounters = () => {
      setFavCount(listingService.getFavorites().length);
      setCompareCount(listingService.getCompareList().length);
    };
    updateCounters();
    const interval = setInterval(updateCounters, 1500);
    return () => clearInterval(interval);
  }, []);

  // Tenant / Public navigation
  const tenantNav: NavItem[] = [
    { label: "Khám phá phòng", href: "/listings", icon: Search },
    { label: "Lịch xem phòng", href: "/tenant/viewings", icon: Calendar },
    { label: "Hợp đồng & Hóa đơn", href: "/tenant/dashboard", icon: FileText },
  ];

  // Landlord dedicated navigation
  const landlordNav: NavItem[] = [
    { label: "Tổng quan tài chính", href: "/landlord/dashboard", icon: Building2 },
    { label: "Quản lý phòng", href: "/landlord/properties", icon: Home },
    { label: "Quản lý tin đăng", href: "/landlord/listings", icon: FileText },
    { label: "Hợp đồng & Khách", href: "/landlord/contracts", icon: FileText },
    { label: "Điện nước & Thu nợ", href: "/landlord/utilities", icon: Zap },
  ];

  // Admin navigation
  const adminNav: NavItem[] = [
    { label: "Dashboard Quản trị", href: "/admin/dashboard", icon: ShieldCheck },
    { label: "Khám phá sàn tin", href: "/listings", icon: Search },
  ];

  const currentNav = role === "Landlord" ? landlordNav : role === "Admin" ? adminNav : tenantNav;

  return (
    <header className="sticky top-0 z-40 bg-white shadow-xs">
      {/* 1. Commercial Topbar (Hotline, Trust Seal, Mobile App) */}
      <div className="bg-slate-900 text-slate-300 text-[11px] py-1.5 px-4 sm:px-6 lg:px-8 border-b border-slate-800">
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          <div className="flex items-center gap-4 sm:gap-6">
            <a
              href="tel:19008899"
              className="flex items-center gap-1.5 text-slate-300 hover:text-white transition-colors font-medium whitespace-nowrap"
            >
              <Phone className="w-3 h-3 text-blue-400" />
              <span>Hotline 24/7: <strong className="text-white">1900 8899</strong></span>
            </a>

            <div className="hidden md:flex items-center gap-1.5 text-emerald-400 font-medium whitespace-nowrap">
              <ShieldCheck className="w-3.5 h-3.5" />
              <span>100% Tin đăng kiểm duyệt thực địa • 0đ Phí môi giới</span>
            </div>
          </div>

          <div className="flex items-center gap-4 text-slate-400">
            <Link
              href="/listings?city=Hà%20Nội"
              className="hover:text-slate-200 transition-colors hidden sm:inline whitespace-nowrap"
            >
              Khu vực Hà Nội
            </Link>
            <span className="hidden sm:inline text-slate-700">•</span>
            <Link
              href="/listings?city=Hồ%20Chí%20Minh"
              className="hover:text-slate-200 transition-colors hidden sm:inline whitespace-nowrap"
            >
              Khu vực TP.HCM
            </Link>
            <span className="hidden sm:inline text-slate-700">•</span>
            <span className="text-slate-400 flex items-center gap-1 whitespace-nowrap">
              <Smartphone className="w-3 h-3 text-blue-400" />
              <span>Tải App HomeMatch</span>
            </span>
          </div>
        </div>
      </div>

      {/* 2. Main Navbar */}
      <div className="border-b border-slate-200/90 bg-white/95 backdrop-blur-md">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16 gap-4">
            {/* Logo */}
            <Link href="/" className="flex items-center gap-2.5 shrink-0 group">
              <div className="w-10 h-10 rounded-xl bg-blue-600 flex items-center justify-center text-white font-black text-xl shadow-md shadow-blue-500/25 group-hover:scale-105 transition-transform">
                <Home className="w-5 h-5" />
              </div>
              <div className="flex flex-col">
                <span className="font-black text-xl tracking-tight text-slate-900 group-hover:text-blue-600 transition-colors whitespace-nowrap">
                  HomeMatch
                </span>
                <span className="text-[9px] text-blue-600 font-bold tracking-wider uppercase -mt-1 whitespace-nowrap">
                  Sàn Thuê Nhà & Quản Lý Trọ
                </span>
              </div>
            </Link>

            {/* Main Navigation Links (Clean, No-Wrap, Well-Spaced) */}
            <nav className="hidden lg:flex items-center gap-1.5 shrink-0">
              {currentNav.map((item) => {
                const Icon = item.icon;
                const isActive = pathname === item.href || (item.href !== "/" && pathname.startsWith(item.href));
                return (
                  <Link
                    key={item.href}
                    href={item.href}
                    className={cn(
                      "flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-bold transition-all whitespace-nowrap",
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

            {/* Right Action Icons & CTA */}
            <div className="flex items-center gap-2 sm:gap-3 shrink-0">
              {/* Compare Quick Icon Badge */}
              <Link
                href="/compare"
                className={cn(
                  "relative p-2 rounded-xl text-slate-600 hover:text-blue-600 hover:bg-slate-100 transition-colors",
                  pathname === "/compare" && "bg-blue-50 text-blue-600"
                )}
                title="So sánh phòng đối chiếu"
              >
                <Scale className="w-4 h-4" />
                {compareCount > 0 && (
                  <span className="absolute -top-1 -right-1 w-4 h-4 rounded-full bg-blue-600 text-white text-[10px] font-black flex items-center justify-center shadow-xs">
                    {compareCount}
                  </span>
                )}
              </Link>

              {/* Favorites Quick Icon Badge */}
              <Link
                href="/tenant/favorites"
                className={cn(
                  "relative p-2 rounded-xl text-slate-600 hover:text-rose-600 hover:bg-slate-100 transition-colors",
                  pathname === "/tenant/favorites" && "bg-rose-50 text-rose-600"
                )}
                title="Danh sách phòng đã lưu"
              >
                <Heart className="w-4 h-4" />
                {favCount > 0 && (
                  <span className="absolute -top-1 -right-1 w-4 h-4 rounded-full bg-rose-500 text-white text-[10px] font-black flex items-center justify-center shadow-xs">
                    {favCount}
                  </span>
                )}
              </Link>

              {/* Messages Quick Icon */}
              <Link
                href="/messages"
                className={cn(
                  "relative p-2 rounded-xl text-slate-600 hover:text-blue-600 hover:bg-slate-100 transition-colors",
                  pathname === "/messages" && "bg-blue-50 text-blue-600"
                )}
                title="Hộp thư tin nhắn"
              >
                <MessageSquare className="w-4 h-4" />
                <span className="absolute top-1.5 right-1.5 w-2 h-2 rounded-full bg-rose-500 ring-2 ring-white" />
              </Link>

              <div className="h-6 w-px bg-slate-200 hidden sm:block" />

              {/* Standout Commercial CTA Button */}
              {role === "Landlord" ? (
                <Link
                  href="/landlord/listings"
                  className="hidden sm:inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all whitespace-nowrap"
                >
                  <PlusCircle className="w-4 h-4 shrink-0" />
                  <span className="whitespace-nowrap">Đăng tin mới</span>
                </Link>
              ) : (
                <Link
                  href="/landlord/dashboard"
                  className="hidden sm:inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-bold text-white bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 shadow-md shadow-blue-500/20 transition-all whitespace-nowrap"
                >
                  <Building2 className="w-4 h-4 shrink-0" />
                  <span className="whitespace-nowrap">Kênh Chủ Nhà</span>
                </Link>
              )}

              {/* User Profile Menu */}
              {user ? (
                <div className="relative">
                  <button
                    onClick={() => setUserDropdownOpen(!userDropdownOpen)}
                    className="flex items-center gap-2 p-1.5 rounded-xl hover:bg-slate-100 text-slate-700 transition-colors"
                  >
                    <div className="w-8 h-8 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold text-xs shadow-xs shrink-0">
                      {user.displayName.charAt(0).toUpperCase()}
                    </div>
                    <span className="text-xs font-bold text-slate-800 hidden xl:inline max-w-[120px] truncate whitespace-nowrap">
                      {user.displayName}
                    </span>
                    <ChevronDown className="w-3.5 h-3.5 text-slate-400" />
                  </button>

                  {userDropdownOpen && (
                    <div className="absolute right-0 mt-2 w-64 bg-white rounded-2xl shadow-2xl border border-slate-100 py-2 z-50 animate-in fade-in zoom-in-95">
                      <div className="px-4 py-2.5 border-b border-slate-100">
                        <p className="text-xs font-bold text-slate-900 truncate">{user.displayName}</p>
                        <p className="text-[11px] text-slate-500 truncate">{user.email}</p>
                        <span className="inline-block mt-1 px-2 py-0.5 rounded-md text-[10px] font-bold bg-blue-50 text-blue-700">
                          {role === "Landlord" ? "Tài khoản Chủ nhà" : role === "Admin" ? "Quản trị viên" : "Tài khoản Người thuê"}
                        </span>
                      </div>

                      {/* Subtle Demo Role Switcher tucked inside menu for testing without cluttering UI */}
                      <div className="p-2 border-b border-slate-100 bg-slate-50/60">
                        <span className="text-[10px] font-bold text-slate-400 uppercase tracking-wider block px-2 mb-1">
                          Chuyển vai trò thử nghiệm:
                        </span>
                        <div className="grid grid-cols-3 gap-1">
                          {(["Tenant", "Landlord", "Admin"] as const).map((r) => (
                            <button
                              key={r}
                              type="button"
                              onClick={() => {
                                switchDemoRole(r);
                                setUserDropdownOpen(false);
                              }}
                              className={cn(
                                "py-1 px-1.5 text-[10px] rounded-lg font-bold transition-colors text-center",
                                role === r
                                  ? "bg-blue-600 text-white shadow-xs"
                                  : "bg-white text-slate-600 border border-slate-200 hover:bg-slate-100"
                              )}
                            >
                              {r === "Tenant" ? "Người thuê" : r === "Landlord" ? "Chủ nhà" : "Admin"}
                            </button>
                          ))}
                        </div>
                      </div>

                      <Link
                        href="/settings"
                        onClick={() => setUserDropdownOpen(false)}
                        className="flex items-center gap-2.5 px-4 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 hover:text-blue-600"
                      >
                        <User className="w-4 h-4 text-slate-400" />
                        <span>Cài đặt tài khoản & Bảo mật</span>
                      </Link>

                      <button
                        onClick={() => {
                          setUserDropdownOpen(false);
                          logout();
                        }}
                        className="w-full flex items-center gap-2.5 px-4 py-2 text-xs font-medium text-rose-600 hover:bg-rose-50"
                      >
                        <LogOut className="w-4 h-4 text-rose-500" />
                        <span>Đăng xuất</span>
                      </button>
                    </div>
                  )}
                </div>
              ) : (
                <div className="flex items-center gap-2">
                  <Link
                    href="/auth/login"
                    className="px-3 py-1.5 text-xs font-bold text-slate-700 hover:text-blue-600 hover:bg-slate-100 rounded-xl transition-colors whitespace-nowrap"
                  >
                    Đăng nhập
                  </Link>
                  <Link
                    href="/auth/register"
                    className="px-3.5 py-1.5 text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 rounded-xl shadow-xs transition-colors whitespace-nowrap"
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
              <div className="px-3 py-2 flex items-center justify-between bg-slate-50 rounded-xl mb-2">
                <span className="text-xs font-bold text-slate-600">Đổi vai trò:</span>
                <div className="flex gap-1">
                  {(["Tenant", "Landlord", "Admin"] as const).map((r) => (
                    <button
                      key={r}
                      onClick={() => switchDemoRole(r)}
                      className={cn(
                        "px-2.5 py-1 text-[11px] rounded-lg font-bold",
                        role === r ? "bg-blue-600 text-white" : "bg-white text-slate-600 border border-slate-200"
                      )}
                    >
                      {r === "Tenant" ? "Người thuê" : r === "Landlord" ? "Chủ nhà" : "Admin"}
                    </button>
                  ))}
                </div>
              </div>

              {currentNav.map((item) => {
                const Icon = item.icon;
                return (
                  <Link
                    key={item.href}
                    href={item.href}
                    onClick={() => setMobileMenuOpen(false)}
                    className="flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-xs font-bold text-slate-700 hover:bg-slate-100"
                  >
                    <Icon className="w-4 h-4 text-slate-500" />
                    <span>{item.label}</span>
                  </Link>
                );
              })}

              <div className="pt-2 border-t border-slate-100">
                <Link
                  href="/landlord/listings"
                  onClick={() => setMobileMenuOpen(false)}
                  className="flex items-center justify-center gap-2 w-full py-2.5 px-4 rounded-xl text-xs font-bold text-white bg-blue-600 shadow-sm"
                >
                  <PlusCircle className="w-4 h-4" />
                  <span>Đăng tin cho thuê miễn phí</span>
                </Link>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
