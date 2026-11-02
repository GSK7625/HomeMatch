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
  Sparkles,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface NavItem {
  label: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
  badge?: number;
}

export function Navbar() {
  const pathname = usePathname();
  const { user, role, logout, switchDemoRole } = useAuth();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [userDropdownOpen, setUserDropdownOpen] = useState(false);
  const [roleDropdownOpen, setRoleDropdownOpen] = useState(false);
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

  const tenantNav: NavItem[] = [
    { label: "Khám phá phòng", href: "/listings", icon: Search },
    {
      label: "So sánh",
      href: "/compare",
      icon: Scale,
      badge: compareCount > 0 ? compareCount : undefined,
    },
    {
      label: "Yêu thích",
      href: "/tenant/favorites",
      icon: Heart,
      badge: favCount > 0 ? favCount : undefined,
    },
    { label: "Lịch xem phòng", href: "/tenant/viewings", icon: Calendar },
    { label: "Hợp đồng & Tiền phòng", href: "/tenant/dashboard", icon: FileText },
  ];

  const landlordNav: NavItem[] = [
    { label: "Tổng quan tài chính", href: "/landlord/dashboard", icon: Building2 },
    { label: "Quản lý phòng", href: "/landlord/properties", icon: Home },
    { label: "Quản lý tin đăng", href: "/landlord/listings", icon: FileText },
    { label: "Hợp đồng & Khách", href: "/landlord/contracts", icon: FileText },
    { label: "Điện nước & Thu nợ", href: "/landlord/utilities", icon: Zap },
  ];

  const adminNav: NavItem[] = [
    { label: "Dashboard Quản trị", href: "/admin/dashboard", icon: ShieldCheck },
    { label: "Khám phá phòng", href: "/listings", icon: Search },
  ];

  const currentNav = role === "Landlord" ? landlordNav : role === "Admin" ? adminNav : tenantNav;

  return (
    <header className="sticky top-0 z-40 bg-white/95 backdrop-blur-md border-b border-slate-200/80 shadow-xs">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Logo & Brand */}
          <div className="flex items-center gap-6">
            <Link href="/" className="flex items-center gap-2.5 group">
              <div className="w-10 h-10 rounded-xl bg-blue-600 flex items-center justify-center text-white font-black text-xl shadow-md shadow-blue-500/20 group-hover:scale-105 transition-transform">
                <Home className="w-5 h-5" />
              </div>
              <div className="flex flex-col">
                <span className="font-extrabold text-xl tracking-tight text-slate-900 group-hover:text-blue-600 transition-colors">
                  HomeMatch
                </span>
                <span className="text-[10px] text-blue-600 font-semibold tracking-wider uppercase -mt-1">
                  Thuê & Quản Lý Trọ
                </span>
              </div>
            </Link>

            {/* Role Switcher Pill (Quick Tester) */}
            <div className="relative hidden lg:block">
              <button
                onClick={() => setRoleDropdownOpen(!roleDropdownOpen)}
                className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold bg-slate-100 hover:bg-slate-200 text-slate-700 transition-colors border border-slate-200"
              >
                <Sparkles className="w-3.5 h-3.5 text-blue-600" />
                <span>Chế độ: </span>
                <span className="text-blue-700 font-bold">
                  {role === "Landlord" ? "Chủ nhà" : role === "Admin" ? "Quản trị viên" : "Người thuê"}
                </span>
                <ChevronDown className="w-3 h-3 text-slate-400" />
              </button>

              {roleDropdownOpen && (
                <div className="absolute left-0 mt-2 w-48 bg-white rounded-xl shadow-xl border border-slate-100 py-1 z-50 animate-in fade-in zoom-in-95">
                  <div className="px-3 py-1.5 text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
                    Đổi góc nhìn vai trò
                  </div>
                  <button
                    onClick={() => {
                      switchDemoRole("Tenant");
                      setRoleDropdownOpen(false);
                    }}
                    className={cn(
                      "w-full text-left px-3 py-2 text-xs font-medium hover:bg-slate-50 flex items-center justify-between",
                      role === "Tenant" && "text-blue-600 font-bold bg-blue-50/50"
                    )}
                  >
                    <span>Người thuê (Tenant)</span>
                    {role === "Tenant" && <div className="w-1.5 h-1.5 rounded-full bg-blue-600" />}
                  </button>
                  <button
                    onClick={() => {
                      switchDemoRole("Landlord");
                      setRoleDropdownOpen(false);
                    }}
                    className={cn(
                      "w-full text-left px-3 py-2 text-xs font-medium hover:bg-slate-50 flex items-center justify-between",
                      role === "Landlord" && "text-blue-600 font-bold bg-blue-50/50"
                    )}
                  >
                    <span>Chủ nhà (Landlord)</span>
                    {role === "Landlord" && <div className="w-1.5 h-1.5 rounded-full bg-blue-600" />}
                  </button>
                  <button
                    onClick={() => {
                      switchDemoRole("Admin");
                      setRoleDropdownOpen(false);
                    }}
                    className={cn(
                      "w-full text-left px-3 py-2 text-xs font-medium hover:bg-slate-50 flex items-center justify-between",
                      role === "Admin" && "text-blue-600 font-bold bg-blue-50/50"
                    )}
                  >
                    <span>Quản trị (Admin)</span>
                    {role === "Admin" && <div className="w-1.5 h-1.5 rounded-full bg-blue-600" />}
                  </button>
                </div>
              )}
            </div>
          </div>

          {/* Desktop Navigation */}
          <nav className="hidden md:flex items-center gap-1">
            {currentNav.map((item) => {
              const Icon = item.icon;
              const isActive = pathname === item.href || (item.href !== "/" && pathname.startsWith(item.href));
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={cn(
                    "relative flex items-center gap-2 px-3.5 py-2 rounded-lg text-sm font-medium transition-colors",
                    isActive
                      ? "text-blue-600 bg-blue-50/80 font-semibold"
                      : "text-slate-600 hover:text-slate-900 hover:bg-slate-100/70"
                  )}
                >
                  <Icon className="w-4 h-4" />
                  <span>{item.label}</span>
                  {item.badge !== undefined && (
                    <span className="inline-flex items-center justify-center px-1.5 py-0.2 text-[11px] font-bold leading-none text-white bg-blue-600 rounded-full">
                      {item.badge}
                    </span>
                  )}
                </Link>
              );
            })}
          </nav>

          {/* Right Actions */}
          <div className="flex items-center gap-3">
            {/* Messages Chat Icon */}
            <Link
              href="/messages"
              className={cn(
                "relative p-2 text-slate-600 hover:text-blue-600 hover:bg-slate-100 rounded-lg transition-colors",
                pathname === "/messages" && "text-blue-600 bg-blue-50"
              )}
              title="Tin nhắn trực tiếp"
            >
              <MessageSquare className="w-5 h-5" />
              <span className="absolute top-1.5 right-1.5 w-2 h-2 rounded-full bg-rose-500" />
            </Link>

            {/* User Profile Dropdown */}
            {user ? (
              <div className="relative">
                <button
                  onClick={() => setUserDropdownOpen(!userDropdownOpen)}
                  className="flex items-center gap-2 p-1.5 rounded-xl hover:bg-slate-100 text-slate-700 transition-colors"
                >
                  <div className="w-8 h-8 rounded-full bg-gradient-to-tr from-blue-600 to-indigo-500 text-white flex items-center justify-center font-bold text-xs shadow-xs">
                    {user.displayName.charAt(0).toUpperCase()}
                  </div>
                  <div className="hidden lg:flex flex-col text-left">
                    <span className="text-xs font-semibold text-slate-800 line-clamp-1 max-w-[130px]">
                      {user.displayName}
                    </span>
                    <span className="text-[10px] text-slate-500">
                      {role === "Landlord" ? "Chủ nhà" : role === "Admin" ? "Quản trị viên" : "Người thuê"}
                    </span>
                  </div>
                  <ChevronDown className="w-3.5 h-3.5 text-slate-400" />
                </button>

                {userDropdownOpen && (
                  <div className="absolute right-0 mt-2 w-56 bg-white rounded-xl shadow-xl border border-slate-100 py-1.5 z-50 animate-in fade-in zoom-in-95">
                    <div className="px-4 py-2 border-b border-slate-100">
                      <p className="text-xs font-bold text-slate-900">{user.displayName}</p>
                      <p className="text-[11px] text-slate-500 truncate">{user.email}</p>
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
                  className="px-3.5 py-1.5 text-xs font-semibold text-slate-700 hover:text-blue-600 hover:bg-slate-100 rounded-lg transition-colors"
                >
                  Đăng nhập
                </Link>
                <Link
                  href="/auth/register"
                  className="px-3.5 py-1.5 text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-sm transition-colors"
                >
                  Đăng ký
                </Link>
              </div>
            )}

            {/* Mobile Menu Button */}
            <button
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              className="md:hidden p-2 text-slate-600 hover:text-slate-900 rounded-lg hover:bg-slate-100"
            >
              {mobileMenuOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
            </button>
          </div>
        </div>

        {/* Mobile Navigation Dropdown */}
        {mobileMenuOpen && (
          <div className="md:hidden border-t border-slate-100 py-3 space-y-1 animate-in fade-in">
            {/* Quick Role Switch in Mobile */}
            <div className="px-3 py-2 flex items-center justify-between bg-slate-50 rounded-lg mb-2">
              <span className="text-xs font-semibold text-slate-600">Đổi vai trò:</span>
              <div className="flex gap-1">
                {(["Tenant", "Landlord", "Admin"] as const).map((r) => (
                  <button
                    key={r}
                    onClick={() => switchDemoRole(r)}
                    className={cn(
                      "px-2 py-1 text-[11px] rounded font-medium",
                      role === r ? "bg-blue-600 text-white" : "bg-white text-slate-600 border border-slate-200"
                    )}
                  >
                    {r === "Tenant" ? "Thuê" : r === "Landlord" ? "Chủ" : "Admin"}
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
                  className="flex items-center justify-between px-3 py-2 rounded-lg text-sm font-medium text-slate-700 hover:bg-slate-100"
                >
                  <div className="flex items-center gap-2.5">
                    <Icon className="w-4 h-4 text-slate-500" />
                    <span>{item.label}</span>
                  </div>
                  {item.badge !== undefined && (
                    <span className="px-2 py-0.5 text-xs font-bold text-white bg-blue-600 rounded-full">
                      {item.badge}
                    </span>
                  )}
                </Link>
              );
            })}
          </div>
        )}
      </div>
    </header>
  );
}
