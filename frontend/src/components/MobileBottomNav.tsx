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
  Calendar,
  MessageSquare,
  Building2,
  FileText,
  User,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface BottomTab {
  label: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
  badgeCount?: number;
  hasDot?: boolean;
}

export function MobileBottomNav() {
  const pathname = usePathname();
  const { user, role } = useAuth();
  const [favCount, setFavCount] = useState(0);

  useEffect(() => {
    const updateCounters = () => {
      setFavCount(listingService.getFavorites().length);
    };
    updateCounters();
    const interval = setInterval(updateCounters, 2000);
    return () => clearInterval(interval);
  }, []);

  // Hide BottomNav on listing detail pages so the dedicated sticky CTA bar takes full focus
  const isListingDetail = pathname.startsWith("/listings/") && pathname.split("/").filter(Boolean).length === 2;
  if (isListingDetail) {
    return null;
  }

  // Tenant / Guest navigation
  const tenantTabs: BottomTab[] = [
    {
      label: "Trang chủ",
      href: "/",
      icon: Home,
    },
    {
      label: "Tìm phòng",
      href: "/listings",
      icon: Search,
    },
    {
      label: "Lịch xem",
      href: user ? "/tenant/viewings" : "/auth/login",
      icon: Calendar,
    },
    {
      label: "Tin nhắn",
      href: "/messages",
      icon: MessageSquare,
      hasDot: true,
    },
    {
      label: user ? "Đã lưu" : "Tài khoản",
      href: user ? "/tenant/favorites" : "/auth/login",
      icon: user ? Heart : User,
      badgeCount: user ? favCount : undefined,
    },
  ];

  // Landlord navigation
  const landlordTabs: BottomTab[] = [
    {
      label: "Tổng quan",
      href: "/landlord/dashboard",
      icon: Building2,
    },
    {
      label: "Phòng trọ",
      href: "/landlord/properties",
      icon: Home,
    },
    {
      label: "Tin đăng",
      href: "/landlord/listings",
      icon: FileText,
    },
    {
      label: "Tin nhắn",
      href: "/messages",
      icon: MessageSquare,
      hasDot: true,
    },
    {
      label: "Tài khoản",
      href: "/settings",
      icon: User,
    },
  ];

  const currentTabs = role === "Landlord" ? landlordTabs : tenantTabs;

  return (
    <nav
      aria-label="Mobile Navigation Bar"
      className="lg:hidden fixed bottom-0 left-0 right-0 z-40 bg-white/95 backdrop-blur-lg border-t border-slate-200/90 shadow-[0_-4px_16px_rgba(0,0,0,0.06)] pb-[env(safe-area-inset-bottom,0px)]"
    >
      <div className="grid grid-cols-5 h-14 max-w-lg mx-auto px-1">
        {currentTabs.map((tab) => {
          const Icon = tab.icon;
          const isActive =
            tab.href === "/"
              ? pathname === "/"
              : pathname === tab.href || pathname.startsWith(tab.href + "/");

          return (
            <Link
              key={tab.href + tab.label}
              href={tab.href}
              className={cn(
                "relative flex flex-col items-center justify-center gap-1 transition-all py-1 select-none",
                isActive
                  ? "text-blue-600 font-bold"
                  : "text-slate-500 hover:text-slate-900 font-medium"
              )}
            >
              <div className="relative">
                <Icon
                  className={cn(
                    "w-5 h-5 transition-transform",
                    isActive ? "scale-110 text-blue-600" : "text-slate-500"
                  )}
                />

                {/* Badge count for Favorites */}
                {tab.badgeCount !== undefined && tab.badgeCount > 0 && (
                  <span className="absolute -top-1 -right-2 min-w-3.5 h-3.5 px-0.5 rounded-full bg-rose-500 text-white text-[9px] font-black flex items-center justify-center leading-none">
                    {tab.badgeCount > 9 ? "9+" : tab.badgeCount}
                  </span>
                )}

                {/* Dot indicator for Messages */}
                {tab.hasDot && (
                  <span className="absolute -top-0.5 -right-0.5 w-2 h-2 rounded-full bg-rose-500 ring-2 ring-white" />
                )}
              </div>

              <span
                className={cn(
                  "text-[10px] tracking-tight leading-none truncate max-w-full px-0.5",
                  isActive ? "text-blue-600 font-bold" : "text-slate-500"
                )}
              >
                {tab.label}
              </span>

              {/* Active pill dot */}
              {isActive && (
                <span className="absolute bottom-1 w-1 h-1 rounded-full bg-blue-600" />
              )}
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
