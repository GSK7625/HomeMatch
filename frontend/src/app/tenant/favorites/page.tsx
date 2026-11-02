"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { listingService } from "@/lib/services";
import { PropertyListing } from "@/types";
import { ListingCard } from "@/features/listings/ListingCard";
import { Heart, Search, ArrowRight } from "lucide-react";

export default function FavoritesPage() {
  const [favListings, setFavListings] = useState<PropertyListing[]>([]);

  const loadFavorites = () => {
    const favIds = listingService.getFavorites();
    const all = listingService.getAll();
    setFavListings(all.filter((x) => favIds.includes(x.id)));
  };

  useEffect(() => {
    loadFavorites();
  }, []);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      <div className="flex items-center justify-between pb-4 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-rose-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Heart className="w-4 h-4 fill-current" />
            <span>Bộ sưu tập cá nhân</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Danh Sách Phòng Đã Lưu Yêu Thích
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Theo dõi nhanh các tin phòng bạn quan tâm để dễ dàng so sánh hoặc đặt lịch xem.
          </p>
        </div>

        <span className="text-xs font-bold text-slate-600 bg-slate-100 px-3 py-1.5 rounded-xl">
          {favListings.length} phòng đã lưu
        </span>
      </div>

      {favListings.length === 0 ? (
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
              onFavoriteChange={loadFavorites}
            />
          ))}
        </div>
      )}
    </div>
  );
}
