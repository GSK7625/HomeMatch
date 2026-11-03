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
  PlusCircle,
  HelpCircle,
  Sparkles,
  Settings,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface NavItem {
  label: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
}

export function Navbar() {
  const pathname = usePathname();
  const { user, role, logout } = useAuth();
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

  // Guest navigation
  const guestNav: NavItem[] = [
    { label: "Tìm phòng", href: "/listings", icon: Search },
    { label: "Kênh Chủ Nhà", href: "/landlord/dashboard", icon: Building2 },
  ];

  // Tenant / Public navigation
  const tenantNav: NavItem[] = [
    { label: "Tìm phòng", href: "/listings", icon: Search },
    { label: "Lịch xem", href: "/tenant/viewings", icon: Calendar },
    { label: "Hợp đồng", href: "/tenant/dashboard", icon: FileText },
  ];

  // Landlord dedicated navigation
  const landlordNav: NavItem[] = [
    { label: "Tổng quan", href: "/landlord/dashboard", icon: Building2 },
    { label: "Phòng trọ", href: "/landlord/properties", icon: Home },
    { label: "Tin đăng", href: "/landlord/listings", icon: FileText },
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
      <div className="max-w-[1440px] mx-auto px-3 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-15 sm:h-16 gap-2 sm:gap-4">
          {/* Logo */}
          <Link href="/" className="flex items-center gap-2 shrink-0 group">
            <div className="w-8.5 h-8.5 sm:w-9 sm:h-9 rounded-xl bg-blue-600 flex items-center justify-center text-white font-black text-lg shadow-md shadow-blue-500/25 group-hover:scale-105 transition-transform">
              <Home className="w-4.5 h-4.5" />
            </div>
            <div className="flex flex-col">
              <span className="font-black text-base sm:text-lg tracking-tight text-slate-900 group-hover:text-blue-600 transition-colors whitespace-nowrap">
                HomeMatch
              </span>
              <span className="text-[9px] text-blue-600 font-bold tracking-wider uppercase -mt-1 hidden sm:inline whitespace-nowrap">
                Sàn Thuê & Quản Lý Trọ
              </span>
            </div>
          </Link>

          {/* Right Action Cluster */}
          <div className="flex items-center gap-1 sm:gap-2">
            {/* Desktop Navigation Links */}
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

            {/* Compare Quick Button (Desktop / Tablet) */}
            {role !== "Landlord" && role !== "Admin" && (
              <Link
                href="/compare"
                className={cn(
                  "hidden md:flex items-center justify-center w-8 h-8 rounded-xl text-xs font-bold transition-all relative shrink-0",
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

            {/* Favorites Quick Button (Desktop / Tablet) */}
            {role !== "Landlord" && role !== "Admin" && (
              <Link
                href="/tenant/favorites"
                className={cn(
                  "hidden md:flex items-center justify-center w-8 h-8 rounded-xl text-xs font-bold transition-all relative shrink-0",
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

            {/* Messages Quick Button (Desktop / Tablet) */}
            <Link
              href="/messages"
              className={cn(
                "hidden sm:flex items-center justify-center w-8 h-8 rounded-xl text-xs font-bold transition-all relative shrink-0",
                pathname === "/messages"
                  ? "text-blue-600 bg-blue-50/80 shadow-xs"
                  : "text-slate-600 hover:text-blue-600 hover:bg-slate-100/80"
              )}
              title="Hộp thư tin nhắn"
            >
              <MessageSquare className="w-4 h-4 shrink-0" />
              <span className="absolute top-1 right-1 w-2 h-2 rounded-full bg-rose-500 ring-2 ring-white" />
            </Link>

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
              <div className="relative shrink-0">
                <button
                  onClick={() => setUserDropdownOpen(!userDropdownOpen)}
                  className="flex items-center gap-1.5 p-1 rounded-xl hover:bg-slate-100 text-slate-700 transition-colors shrink-0"
                  title={user.displayName}
                >
                  <div className="w-8 h-8 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold text-xs shadow-xs shrink-0">
                    {user.displayName.charAt(0).toUpperCase()}
                  </div>
                  <ChevronDown className="w-3.5 h-3.5 text-slate-400 shrink-0" />
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
                      className="w-full flex items-center gap-2.5 px-4 py-2 text-xs font-medium text-rose-600 hover:bg-rose-50 text-left"
                    >
                      <LogOut className="w-4 h-4 text-rose-500" />
                      <span>Đăng xuất</span>
                    </button>
                  </div>
                )}
              </div>
            ) : (
              <div className="hidden lg:flex items-center gap-1.5">
                <Link
                  href="/auth/login"
                  className="px-3.5 py-2 text-xs font-bold text-slate-700 bg-slate-100 hover:bg-slate-200 hover:text-slate-900 border border-slate-200/90 rounded-xl transition-all whitespace-nowrap shadow-2xs"
                >
                  Đăng nhập
                </Link>
                <Link
                  href="/auth/register"
                  className="px-3.5 py-2 text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 rounded-xl shadow-sm shadow-blue-500/25 transition-all whitespace-nowrap"
                >
                  Đăng ký
                </Link>
              </div>
            )}

            {/* Mobile Hamburger Toggle Button */}
            <button
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              className="lg:hidden p-2 text-slate-600 hover:text-slate-900 rounded-xl hover:bg-slate-100 transition-colors"
              aria-label="Toggle Navigation Menu"
            >
              {mobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
            </button>
          </div>
        </div>

        {/* Mobile Slide-down Drawer */}
        {mobileMenuOpen && (
          <div className="lg:hidden border-t border-slate-100 py-3 space-y-1 animate-in fade-in">
            {user ? (
              <div className="px-3.5 py-2.5 bg-slate-50 rounded-2xl mb-2 flex items-center justify-between border border-slate-100">
                <div className="min-w-0 pr-2">
                  <p className="text-xs font-bold text-slate-900 truncate">{user.displayName}</p>
                  <p className="text-[10px] text-slate-500 truncate">{user.email}</p>
                </div>
                <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-blue-50 text-blue-700 shrink-0">
                  {role === "Landlord" ? "Chủ nhà" : role === "Admin" ? "Admin" : "Người thuê"}
                </span>
              </div>
            ) : (
              <div className="grid grid-cols-2 gap-2 p-1 mb-2">
                <Link
                  href="/auth/login"
                  onClick={() => setMobileMenuOpen(false)}
                  className="py-2.5 text-center text-xs font-bold text-slate-700 bg-slate-100 rounded-xl hover:bg-slate-200 transition-colors"
                >
                  Đăng nhập
                </Link>
                <Link
                  href="/auth/register"
                  onClick={() => setMobileMenuOpen(false)}
                  className="py-2.5 text-center text-xs font-bold text-white bg-blue-600 rounded-xl hover:bg-blue-700 transition-colors shadow-xs"
                >
                  Đăng ký
                </Link>
              </div>
            )}

            {currentNav.map((item) => {
              const Icon = item.icon;
              const isActive = pathname === item.href;
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  onClick={() => setMobileMenuOpen(false)}
                  className={cn(
                    "flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-xs font-bold transition-colors",
                    isActive
                      ? "text-blue-600 bg-blue-50/80"
                      : "text-slate-700 hover:bg-slate-100"
                  )}
                >
                  <Icon className="w-4 h-4 text-slate-500" />
                  <span>{item.label}</span>
                </Link>
              );
            })}

            {/* Quick compare link on mobile drawer */}
            <Link
              href="/compare"
              onClick={() => setMobileMenuOpen(false)}
              className="flex items-center justify-between px-3.5 py-2.5 rounded-xl text-xs font-bold text-slate-700 hover:bg-slate-100"
            >
              <div className="flex items-center gap-3">
                <Scale className="w-4 h-4 text-slate-500" />
                <span>So sánh phòng</span>
              </div>
              {compareCount > 0 && (
                <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-blue-100 text-blue-700">
                  {compareCount} phòng
                </span>
              )}
            </Link>

            {/* Settings link on mobile drawer */}
            {user && (
              <Link
                href="/settings"
                onClick={() => setMobileMenuOpen(false)}
                className="flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-xs font-bold text-slate-700 hover:bg-slate-100"
              >
                <Settings className="w-4 h-4 text-slate-500" />
                <span>Cài đặt tài khoản & Bảo mật</span>
              </Link>
            )}

            {user && (
              <div className="pt-2 border-t border-slate-100">
                <button
                  onClick={() => {
                    setMobileMenuOpen(false);
                    logout();
                  }}
                  className="flex items-center justify-center gap-2 w-full py-2.5 px-4 rounded-xl text-xs font-bold text-rose-600 hover:bg-rose-50 border border-rose-200 transition-colors"
                >
                  <LogOut className="w-4 h-4" />
                  <span>Đăng xuất</span>
                </button>
              </div>
            )}
          </div>
        )}
      </div>
    </header>
  );
}
