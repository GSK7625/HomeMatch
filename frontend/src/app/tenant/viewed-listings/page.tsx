"use client";

import React, { useState, useEffect, useCallback, useMemo } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import {
  getStoredToken,
  parseListingNumericId,
  tenantOverviewApi,
} from "@/lib/api-client";
import { ViewedListingContractItem } from "@/types";
import {
  Eye,
  MapPin,
  Maximize2,
  Calendar,
  MessageSquare,
  Search,
  ArrowRight,
  RefreshCw,
  User,
  Clock,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function TenantViewedListingsPage() {
  const { user } = useAuth();
  const router = useRouter();

  const [items, setItems] = useState<ViewedListingContractItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const [sortBy, setSortBy] = useState<"recent" | "price_asc" | "price_desc" | "area_desc">("recent");

  const loadViewedListings = useCallback(async () => {
    if (!user) return;
    setIsLoading(true);
    try {
      const token = getStoredToken();
      if (token) {
        const res = await tenantOverviewApi.getViewedListings(1, 30);
        if (res.isSuccess && res.data) {
          setItems(res.data.items);
          setTotalCount(res.data.totalCount);
          return;
        }
        setItems([]);
        setTotalCount(0);
        return;
      }
      setItems([]);
      setTotalCount(0);
    } finally {
      setIsLoading(false);
    }
  }, [user]);

  useEffect(() => {
    loadViewedListings();
  }, [loadViewedListings]);

  const filteredAndSorted = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    const list = items.filter((item) => {
      if (!q) return true;
      return (
        item.title.toLowerCase().includes(q) ||
        (item.address || "").toLowerCase().includes(q) ||
        item.landlordName.toLowerCase().includes(q)
      );
    });

    return [...list].sort((a, b) => {
      if (sortBy === "price_asc") return a.price - b.price;
      if (sortBy === "price_desc") return b.price - a.price;
      if (sortBy === "area_desc") return b.areaSqm - a.areaSqm;
      return new Date(b.viewedAt).getTime() - new Date(a.viewedAt).getTime();
    });
  }, [items, searchQuery, sortBy]);

  if (!user) {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <Eye className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Danh Sách Phòng Đã Xem</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập để xem lại lịch sử các phòng trọ bạn đã tìm hiểu và đối chiếu.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/tenant/viewed-listings"
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
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-5 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Eye className="w-4 h-4" />
            <span>Mục 3.4.2 • Xem danh sách phòng đã xem</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Lịch Sử Phòng Đã Xem ({totalCount})
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Tra cứu nhanh các phòng bạn đã từng truy cập, đặt lịch hẹn xem trực tiếp hoặc nhắn tin cho chủ nhà.
          </p>
        </div>

        <div className="flex items-center gap-2.5">
          <button
            type="button"
            onClick={loadViewedListings}
            className="p-2 rounded-xl border border-slate-200 text-slate-600 hover:text-blue-600 hover:bg-slate-50 transition-colors"
            title="Làm mới danh sách"
          >
            <RefreshCw className={cn("w-4 h-4", isLoading && "animate-spin")} />
          </button>

          <Link
            href="/tenant/dashboard"
            className="px-4 py-2 rounded-xl border border-slate-200 bg-white hover:bg-slate-50 text-slate-700 text-xs font-bold transition-colors"
          >
            Về Dashboard
          </Link>

          <Link
            href="/listings"
            className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-sm transition-colors"
          >
            <span>Khám phá thêm phòng</span>
            <ArrowRight className="w-3.5 h-3.5" />
          </Link>
        </div>
      </div>

      {/* Search & Sort Bar */}
      <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-2xs flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
        <div className="relative flex-1">
          <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            placeholder="Tìm theo tên phòng, địa chỉ hoặc tên chủ nhà..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-10 pr-4 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500/20"
          />
        </div>

        <div className="flex items-center gap-2">
          <span className="text-xs font-bold text-slate-500 whitespace-nowrap">
            Sắp xếp:
          </span>
          <select
            value={sortBy}
            onChange={(e) =>
              setSortBy(
                e.target.value as "recent" | "price_asc" | "price_desc" | "area_desc"
              )
            }
            className="px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white font-semibold text-slate-800"
          >
            <option value="recent">Mới xem gần nhất</option>
            <option value="price_asc">Giá thuê: Thấp đến Cao</option>
            <option value="price_desc">Giá thuê: Cao đến Thấp</option>
            <option value="area_desc">Diện tích: Lớn đến Nhỏ</option>
          </select>
        </div>
      </div>

      {/* Viewed Listings Grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[1, 2, 3].map((s) => (
            <div
              key={s}
              className="bg-white rounded-2xl border border-slate-200 overflow-hidden animate-pulse"
            >
              <div className="h-44 bg-slate-100" />
              <div className="p-5 space-y-3">
                <div className="h-5 bg-slate-100 rounded w-3/4" />
                <div className="h-4 bg-slate-100 rounded w-1/2" />
              </div>
            </div>
          ))}
        </div>
      ) : filteredAndSorted.length === 0 ? (
        <div className="bg-white rounded-2xl border border-slate-200/80 p-14 text-center space-y-4">
          <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto">
            <Eye className="w-8 h-8" />
          </div>
          <h2 className="text-lg font-bold text-slate-800">
            Không tìm thấy phòng đã xem phù hợp
          </h2>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            Hãy khám phá các phòng trọ trên sàn để hệ thống lưu lại lịch sử tìm kiếm cho bạn.
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredAndSorted.map((item) => {
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
                      className="w-full h-full object-cover hover:scale-105 transition-transform duration-300"
                    />
                    <div className="absolute top-3 left-3 flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-slate-900/80 text-white backdrop-blur-md text-[11px] font-bold">
                      <Clock className="w-3 h-3 text-blue-400" />
                      <span>
                        Đã xem:{" "}
                        {new Date(item.viewedAt).toLocaleDateString("vi-VN", {
                          day: "2-digit",
                          month: "2-digit",
                          hour: "2-digit",
                          minute: "2-digit",
                        })}
                      </span>
                    </div>

                    <div className="absolute bottom-3 right-3 px-3 py-1 rounded-xl bg-blue-600 text-white text-xs font-black shadow-md">
                      {item.price.toLocaleString("vi-VN")} đ/tháng
                    </div>
                  </div>

                  <div className="p-5 space-y-3">
                    <Link href={`/listings/${listingSlug}`}>
                      <h3 className="font-bold text-slate-900 text-sm hover:text-blue-600 transition-colors line-clamp-2">
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

                      <span className="inline-flex items-center gap-1 text-slate-500 truncate max-w-[170px]">
                        <User className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                        <span className="truncate">{item.landlordName}</span>
                      </span>
                    </div>
                  </div>
                </div>

                <div className="px-5 pb-5 pt-2 flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => router.push(`/messages?listingId=${item.listingId}`)}
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
                    <span>Đặt lịch xem</span>
                  </Link>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
