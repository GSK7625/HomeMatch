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
  MessageSquare,
  User,
  Building2,
  Calendar,
  FileText,
} from "lucide-react";
import { cn } from "@/lib/utils";

export function BottomNav() {
  const rawPathname = usePathname();
  const pathname = rawPathname || "";
  const { user, role } = useAuth();
  const [favCount, setFavCount] = useState(0);
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (!mounted) return;
    const updateFavs = () => {
      setFavCount(listingService.getFavorites().length);
    };
    updateFavs();
    const interval = setInterval(updateFavs, 2000);
    return () => clearInterval(interval);
  }, [mounted]);

  // Prevent SSR/Hydration mismatch
  if (!mounted) {
    return null;
  }

  // Hide bottom nav on auth screens and listing detail view
  if (pathname.startsWith("/auth") || /^\/listings\/[^\/]+$/.test(pathname)) {
    return null;
  }

  // For Landlords: Bottom nav tailored for landlord workflows
  if (role === "Landlord") {
    const landlordItems = [
      {
        label: "Tổng quan",
        href: "/landlord/dashboard",
        icon: Building2,
        isActive: pathname === "/landlord/dashboard",
      },
      {
        label: "Phòng trọ",
        href: "/landlord/properties",
        icon: Home,
        isActive: pathname.startsWith("/landlord/properties"),
      },
      {
        label: "Tin đăng",
        href: "/landlord/listings",
        icon: FileText,
        isActive: pathname.startsWith("/landlord/listings"),
      },
      {
        label: "Tin nhắn",
        href: "/messages",
        icon: MessageSquare,
        isActive: pathname.startsWith("/messages"),
      },
      {
        label: "Lịch hẹn",
        href: "/landlord/viewings",
        icon: Calendar,
        isActive: pathname.startsWith("/landlord/viewings"),
      },
    ];

    return (
      <nav
        aria-label="Thanh điều hướng chủ nhà"
        className="lg:hidden fixed bottom-0 left-0 right-0 z-40 bg-white/95 backdrop-blur-lg border-t border-slate-200/90 shadow-[0_-4px_20px_rgba(0,0,0,0.06)] px-2 py-1 safe-bottom"
      >
        <div className="grid grid-cols-5 items-center justify-around max-w-lg mx-auto">
          {landlordItems.map((item) => {
            const Icon = item.icon;
            return (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  "flex flex-col items-center justify-center py-1.5 px-1 rounded-xl transition-all relative",
                  item.isActive
                    ? "text-blue-600 font-bold scale-105"
                    : "text-slate-500 hover:text-slate-800 font-medium"
                )}
              >
                <Icon className={cn("w-5 h-5", item.isActive ? "stroke-[2.5]" : "stroke-[1.75]")} />
                <span className="text-[10px] mt-1 tracking-tight truncate max-w-full">
                  {item.label}
                </span>
                {item.isActive && (
                  <span className="absolute bottom-0 w-5 h-0.5 bg-blue-600 rounded-full" />
                )}
              </Link>
            );
          })}
        </div>
      </nav>
    );
  }

  // Tenant / Guest navigation
  const items = [
    {
      label: "Trang chủ",
      href: "/",
      icon: Home,
      isActive: pathname === "/",
    },
    {
      label: "Tìm phòng",
      href: "/listings",
      icon: Search,
      isActive: pathname.startsWith("/listings"),
    },
    {
      label: "Yêu thích",
      href: "/tenant/favorites",
      icon: Heart,
      isActive: pathname === "/tenant/favorites",
      badge: favCount > 0 ? favCount : undefined,
    },
    {
      label: "Tin nhắn",
      href: "/messages",
      icon: MessageSquare,
      isActive: pathname.startsWith("/messages"),
    },
    {
      label: user ? "Cá nhân" : "Tài khoản",
      href: user ? "/tenant/dashboard" : "/auth/login",
      icon: User,
      isActive:
        pathname.startsWith("/tenant") && pathname !== "/tenant/favorites"
          ? true
          : pathname.startsWith("/auth"),
    },
  ];

  return (
    <nav
      aria-label="Thanh điều hướng di động"
      className="lg:hidden fixed bottom-0 left-0 right-0 z-40 bg-white/95 backdrop-blur-lg border-t border-slate-200/90 shadow-[0_-4px_20px_rgba(0,0,0,0.06)] px-2 py-1 safe-bottom"
    >
      <div className="grid grid-cols-5 items-center justify-around max-w-lg mx-auto">
        {items.map((item) => {
          const Icon = item.icon;
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex flex-col items-center justify-center py-1.5 px-1 rounded-xl transition-all relative",
                item.isActive
                  ? "text-blue-600 font-bold scale-105"
                  : "text-slate-500 hover:text-slate-800 font-medium"
              )}
            >
              <div className="relative">
                <Icon className={cn("w-5 h-5", item.isActive ? "stroke-[2.5]" : "stroke-[1.75]")} />
                {item.badge !== undefined && (
                  <span className="absolute -top-1.5 -right-2 min-w-4 h-4 px-1 rounded-full bg-rose-500 text-white text-[9px] font-black flex items-center justify-center shadow-xs">
                    {item.badge > 9 ? "9+" : item.badge}
                  </span>
                )}
              </div>
              <span className="text-[10px] mt-1 tracking-tight truncate max-w-full">
                {item.label}
              </span>
              {item.isActive && (
                <span className="absolute bottom-0 w-5 h-0.5 bg-blue-600 rounded-full" />
              )}
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
