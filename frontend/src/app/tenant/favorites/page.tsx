"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { listingsApi, getStoredToken } from "@/lib/api-client";
import { useAuth } from "@/features/auth/AuthContext";
import { PropertyListing } from "@/types";
import { ListingCard } from "@/features/listings/ListingCard";
import { Heart, ArrowRight, RefreshCw, LogIn } from "lucide-react";
import { cn, mapListingSummaryToCard } from "@/lib/utils";

export default function FavoritesPage() {
  const { user, role, isLoading: authLoading } = useAuth();
  const [favListings, setFavListings] = useState<PropertyListing[]>([]);
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const isTenant = Boolean(user && role === "Tenant" && getStoredToken());

  const loadFavorites = useCallback(async () => {
    if (!isTenant) {
      window.dispatchEvent(new CustomEvent("homematch:favorites-updated", { detail: { ids: [] } }));
      setFavListings([]);
      return;
    }

    setLoading(true);
    setErrorMsg(null);
    try {
      const res = await listingsApi.getSavedListings();
      if (res.isSuccess && Array.isArray(res.data)) {
        const savedIds = res.data.map((s) => String(s.listingId));
        window.dispatchEvent(
          new CustomEvent("homematch:favorites-updated", { detail: { ids: savedIds } })
        );
        setFavListings(
          res.data.map((s) => ({
            ...mapListingSummaryToCard(s.listing),
            id: String(s.listingId),
            createdAt: s.createdAt,
          }))
        );
      } else {
        setFavListings([]);
        setErrorMsg(res.error?.message || "Không thể tải danh sách tin đã lưu.");
      }
    } catch {
      setFavListings([]);
      setErrorMsg("Lỗi kết nối máy chủ khi tải danh sách tin đã lưu.");
    } finally {
      setLoading(false);
    }
  }, [isTenant]);

  useEffect(() => {
    if (!authLoading) {
      loadFavorites();
    }
  }, [authLoading, loadFavorites]);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-rose-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Heart className="w-4 h-4 fill-current" />
            <span>Bộ sưu tập cá nhân (Saved Listings)</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Danh Sách Phòng Đã Lưu Yêu Thích
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Theo dõi nhanh các tin phòng bạn quan tâm để dễ dàng so sánh hoặc đặt lịch xem.
          </p>
        </div>

        {isTenant && (
          <div className="flex items-center gap-2">
            <button
              onClick={() => loadFavorites()}
              className="p-2 rounded-xl border border-slate-200 text-slate-600 hover:bg-slate-50"
              title="Làm mới"
            >
              <RefreshCw className={cn("w-4 h-4", loading && "animate-spin")} />
            </button>
            <span className="text-xs font-bold text-slate-600 bg-slate-100 px-3 py-1.5 rounded-xl">
              {favListings.length} phòng đã lưu
            </span>
          </div>
        )}
      </div>

      {!authLoading && !isTenant ? (
        <div className="bg-white rounded-2xl border border-slate-200/80 p-16 text-center space-y-4">
          <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto">
            <LogIn className="w-8 h-8" />
          </div>
          <h2 className="text-lg font-bold text-slate-800">
            {user ? "Chức năng dành riêng cho tài khoản Người thuê" : "Vui lòng đăng nhập để xem tin đã lưu"}
          </h2>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            {user
              ? "Tài khoản hiện tại không phải vai trò Người thuê (Tenant) nên không có danh sách tin đã lưu."
              : "Đăng nhập tài khoản Người thuê để lưu và đồng bộ danh sách phòng yêu thích."}
          </p>
          {!user && (
            <div className="pt-2">
              <Link
                href="/auth/login?redirect=/tenant/favorites"
                className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20"
              >
                <span>Đăng nhập ngay</span>
                <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
          )}
        </div>
      ) : errorMsg ? (
        <div className="bg-rose-50 rounded-2xl border border-rose-200 p-8 text-center space-y-3">
          <p className="text-sm font-bold text-rose-700">{errorMsg}</p>
          <button
            onClick={() => loadFavorites()}
            className="px-4 py-2 rounded-xl bg-rose-600 text-white text-xs font-bold hover:bg-rose-700"
          >
            Thử lại
          </button>
        </div>
      ) : favListings.length === 0 && !loading ? (
        <div className="bg-white rounded-2xl border border-slate-200/80 p-16 text-center space-y-4">
          <div className="w-16 h-16 rounded-2xl bg-rose-50 text-rose-500 flex items-center justify-center mx-auto">
            <Heart className="w-8 h-8" />
          </div>
          <h2 className="text-lg font-bold text-slate-800">Bạn chưa lưu tin phòng nào</h2>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            Khám phá danh sách phòng trọ và bấm vào biểu tượng trái tim để lưu lại phòng bạn yêu
            thích.
          </p>
          <div className="pt-2">
            <Link
              href="/listings"
              className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20"
            >
              <span>Khám phá phòng ngay</span>
              <ArrowRight className="w-4 h-4" />
            </Link>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {favListings.map((listing) => (
            <ListingCard
              key={listing.id}
              listing={listing}
              isFavorite={true}
              onFavoriteChange={loadFavorites}
            />
          ))}
        </div>
      )}
    </div>
  );
}
