"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { PropertyListing } from "@/types";
import { listingService } from "@/lib/services";
import { listingsApi, parseListingNumericId, getStoredToken } from "@/lib/api-client";
import { useToast } from "@/components/Toast";
import {
  Heart,
  Scale,
  MapPin,
  Maximize2,
  Zap,
  Droplet,
  Star,
  ShieldCheck,
  Tag,
  Phone,
  Flame,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface ListingCardProps {
  listing: PropertyListing;
  onFavoriteChange?: () => void;
  isFavorite?: boolean;
}

export function ListingCard({ listing, onFavoriteChange, isFavorite: isFavoriteProp }: ListingCardProps) {
  const { showToast } = useToast();
  const [isFav, setIsFav] = useState(() => {
    if (typeof isFavoriteProp === "boolean") return isFavoriteProp;
    return listingService.isFavorite(listing.id);
  });
  const [isSyncing, setIsSyncing] = useState(false);

  useEffect(() => {
    if (typeof isFavoriteProp === "boolean") {
      setIsFav(isFavoriteProp);
    } else {
      setIsFav(listingService.isFavorite(listing.id));
    }
  }, [isFavoriteProp, listing.id]);

  useEffect(() => {
    const handleFavUpdated = (e: Event) => {
      const customEvent = e as CustomEvent<{ id?: string; isFavorite?: boolean; ids?: string[] }>;
      if (customEvent.detail?.id === listing.id && typeof customEvent.detail?.isFavorite === "boolean") {
        setIsFav(customEvent.detail.isFavorite);
      } else if (Array.isArray(customEvent.detail?.ids)) {
        setIsFav(customEvent.detail.ids.includes(listing.id));
      } else {
        setIsFav(listingService.isFavorite(listing.id));
      }
    };
    window.addEventListener("homematch:favorites-updated", handleFavUpdated);
    return () => {
      window.removeEventListener("homematch:favorites-updated", handleFavUpdated);
    };
  }, [listing.id]);

  const handleFavoriteClick = async (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (isSyncing) return;

    const previousState = isFav;
    const nextState = !previousState;
    const numericId = parseListingNumericId(listing.id);
    const token = getStoredToken();

    // Optimistic update
    setIsFav(nextState);
    listingService.setFavorite(listing.id, nextState);

    if (numericId > 0 && token) {
      setIsSyncing(true);
      try {
        const res = await listingsApi.toggleSaveListing(numericId);
        if (res.isSuccess && typeof res.data === "boolean") {
          const serverState = res.data;
          setIsFav(serverState);
          listingService.setFavorite(listing.id, serverState);
          showToast(
            serverState ? "Đã lưu vào danh sách phòng yêu thích!" : "Đã xóa khỏi danh sách yêu thích",
            serverState ? "success" : "info"
          );
        } else {
          // Revert optimistic update on failure (e.g. 403 Landlord role or server error)
          setIsFav(previousState);
          listingService.setFavorite(listing.id, previousState);
          const errorMsg = res.error?.message || "Không thể đồng bộ trạng thái yêu thích với máy chủ";
          showToast(errorMsg, "error");
        }
      } catch (err) {
        console.warn("Could not sync favorite to backend", err);
        setIsFav(previousState);
        listingService.setFavorite(listing.id, previousState);
        showToast("Lỗi kết nối máy chủ khi lưu tin", "error");
      } finally {
        setIsSyncing(false);
      }
    } else {
      showToast(
        nextState ? "Đã lưu vào danh sách phòng yêu thích!" : "Đã xóa khỏi danh sách yêu thích",
        nextState ? "success" : "info"
      );
    }

    if (onFavoriteChange) onFavoriteChange();
  };

  const handleCompareClick = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    const res = listingService.addToCompare(listing.id);
    showToast(res.message, res.success ? "success" : "error");
  };

  const formattedPrice = (price: number) => {
    return (price / 1000000).toLocaleString("vi-VN", { maximumFractionDigits: 1 }) + " tr/tháng";
  };

  const statusColors = {
    "Còn trống": "bg-emerald-600 text-white",
    "Đã thuê": "bg-slate-700 text-slate-200",
    "Tạm ngừng": "bg-rose-600 text-white",
  };

  return (
    <div className="group bg-white rounded-2xl border border-slate-200/90 shadow-xs hover:shadow-xl hover:border-blue-300 transition-all duration-300 overflow-hidden flex flex-col relative">
      {/* Photo Container */}
      <Link
        href={`/listings/${listing.id}`}
        className="block relative aspect-[16/10] overflow-hidden bg-slate-100 cursor-pointer"
      >
        <img
          src={listing.images[0] || "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=800&q=80"}
          alt={listing.title}
          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
        />

        {/* Top Badges & Action Buttons */}
        <div className="absolute top-3 left-3 right-3 flex items-start justify-between gap-2 z-10 pointer-events-none">
          {/* Badges Left */}
          <div className="flex flex-wrap items-center gap-1.5 max-w-[calc(100%-76px)]">
            <span className="inline-flex items-center h-6 px-2.5 rounded-full text-[11px] font-bold bg-slate-900/85 text-white backdrop-blur-md shadow-xs whitespace-nowrap">
              {listing.roomType}
            </span>

            {listing.isVerifiedLandlord && (
              <span className="inline-flex items-center gap-1 h-6 px-2.5 rounded-full text-[11px] font-bold bg-emerald-500 text-white shadow-xs whitespace-nowrap">
                <ShieldCheck className="w-3.5 h-3.5 shrink-0" />
                <span>Xác thực 100%</span>
              </span>
            )}

            <span
              className={cn(
                "inline-flex items-center h-6 px-2.5 rounded-full text-[11px] font-bold shadow-xs whitespace-nowrap",
                statusColors[listing.status]
              )}
            >
              {listing.status}
            </span>
          </div>

          {/* Action Buttons Right */}
          <div className="flex items-center gap-1.5 shrink-0 z-20 pointer-events-auto">
            <button
              onClick={handleCompareClick}
              className="w-8 h-8 rounded-full bg-white/90 backdrop-blur-md text-slate-700 hover:text-blue-600 hover:bg-white flex items-center justify-center shadow-md transition-all cursor-pointer"
              title="Thêm vào bảng so sánh"
            >
              <Scale className="w-4 h-4" />
            </button>
            <button
              onClick={handleFavoriteClick}
              className={cn(
                "w-8 h-8 rounded-full bg-white/90 backdrop-blur-md flex items-center justify-center shadow-md transition-all cursor-pointer",
                isFav ? "text-rose-500 fill-rose-500" : "text-slate-700 hover:text-rose-500 hover:bg-white"
              )}
              title="Lưu tin yêu thích"
            >
              <Heart className={cn("w-4 h-4", isFav && "fill-current")} />
            </button>
          </div>
        </div>

        {/* Bottom Banner with Price & Area */}
        <div className="absolute bottom-0 inset-x-0 bg-gradient-to-t from-slate-950/90 via-slate-950/50 to-transparent p-3 pt-6 flex items-end justify-between text-white pointer-events-none">
          <div>
            <span className="text-xl font-black text-white tracking-tight drop-shadow-sm">
              {formattedPrice(listing.price)}
            </span>
            <span className="text-[11px] text-slate-300 block">
              Cọc {(listing.deposit / 1000000).toFixed(1)} tr • Đóng 1 tháng
            </span>
          </div>

          <div className="flex items-center gap-1 text-xs font-bold bg-white/25 backdrop-blur-md px-2.5 py-1 rounded-lg">
            <Maximize2 className="w-3.5 h-3.5" />
            <span>{listing.area} m²</span>
          </div>
        </div>
      </Link>

      {/* Content Area */}
      <div className="p-4 flex-1 flex flex-col justify-between space-y-3">
        <div>
          {/* Promo / Discount banner if available */}
          {listing.discountTag && (
            <div className="flex items-center gap-1 text-[11px] font-bold text-amber-700 bg-amber-50 px-2 py-0.5 rounded-md mb-2 border border-amber-200/80">
              <Flame className="w-3 h-3 text-amber-600 shrink-0" />
              <span className="truncate">{listing.discountTag}</span>
            </div>
          )}

          {/* Title */}
          <Link href={`/listings/${listing.id}`}>
            <h3 className="font-bold text-slate-900 text-sm line-clamp-2 group-hover:text-blue-600 transition-colors leading-snug">
              {listing.title}
            </h3>
          </Link>

          {/* Location */}
          <div className="flex items-center gap-1 text-xs text-slate-500 mt-1.5 mb-2.5">
            <MapPin className="w-3.5 h-3.5 text-rose-500 shrink-0" />
            <span className="line-clamp-1 font-medium">
              {[listing.district, listing.city]
                .map((p) => p?.trim())
                .filter((p, idx, arr): p is string => Boolean(p) && arr.findIndex((x) => x?.toLowerCase() === p?.toLowerCase()) === idx)
                .join(", ")}
            </span>
          </div>

          {/* Rating & Social proof */}
          {listing.rating && (
            <div className="flex items-center gap-2 mb-2 text-xs">
              <div className="flex items-center gap-1 text-amber-500 font-bold bg-amber-50 px-1.5 py-0.5 rounded">
                <Star className="w-3.5 h-3.5 fill-current" />
                <span>{listing.rating}</span>
              </div>
              <span className="text-slate-400 text-[11px]">({listing.reviewCount} đánh giá)</span>
            </div>
          )}

          {/* Commercial Tags (e.g. Không chung chủ, Giờ tự do) */}
          {listing.tags && listing.tags.length > 0 && (
            <div className="flex flex-wrap gap-1 mb-2">
              {listing.tags.slice(0, 3).map((tag) => (
                <span
                  key={tag}
                  className="px-2 py-0.5 text-[10px] font-semibold bg-blue-50 text-blue-700 rounded-md"
                >
                  {tag}
                </span>
              ))}
            </div>
          )}
        </div>

        {/* Footer: Tariff & Action */}
        <div className="pt-3 border-t border-slate-100 flex items-center justify-between text-[11px] text-slate-500">
          <div className="flex items-center gap-2">
            <span className="flex items-center gap-0.5 text-slate-700 font-medium">
              <Zap className="w-3 h-3 text-amber-500" />
              <span>{listing.electricityPrice.toLocaleString()}đ</span>
            </span>
            <span>•</span>
            <span className="flex items-center gap-0.5 text-slate-700 font-medium">
              <Droplet className="w-3 h-3 text-sky-500" />
              <span>{listing.waterPrice.toLocaleString()}đ</span>
            </span>
          </div>

          <Link
            href={`/listings/${listing.id}`}
            className="font-bold text-xs text-blue-600 hover:text-blue-700 flex items-center gap-1 px-2.5 py-1 -mr-1 rounded-lg hover:bg-blue-50 transition-colors"
          >
            <span>Chi tiết</span>
            <span className="text-sm font-bold">→</span>
          </Link>
        </div>
      </div>
    </div>
  );
}
