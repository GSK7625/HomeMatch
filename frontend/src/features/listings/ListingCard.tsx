"use client";

import React, { useState } from "react";
import Link from "next/link";
import { PropertyListing } from "@/types";
import { listingService } from "@/lib/services";
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
}

export function ListingCard({ listing, onFavoriteChange }: ListingCardProps) {
  const { showToast } = useToast();
  const [isFav, setIsFav] = useState(() => listingService.isFavorite(listing.id));

  const handleFavoriteClick = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    const newState = listingService.toggleFavorite(listing.id);
    setIsFav(newState);
    showToast(
      newState ? "Đã lưu vào danh sách phòng yêu thích!" : "Đã xóa khỏi danh sách yêu thích",
      newState ? "success" : "info"
    );
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
      <div className="relative aspect-[16/10] overflow-hidden bg-slate-100">
        <img
          src={listing.images[0] || "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=800&q=80"}
          alt={listing.title}
          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
        />

        {/* Top Badges */}
        <div className="absolute top-3 left-3 flex flex-wrap gap-1.5 z-10">
          <span className="px-2.5 py-1 rounded-full text-[11px] font-bold bg-slate-900/85 text-white backdrop-blur-md shadow-xs">
            {listing.roomType}
          </span>

          {listing.isVerifiedLandlord && (
            <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-500 text-white flex items-center gap-1 shadow-xs">
              <ShieldCheck className="w-3 h-3" />
              <span>Xác thực 100%</span>
            </span>
          )}

          <span
            className={cn(
              "px-2 py-0.5 rounded-full text-[10px] font-bold shadow-xs",
              statusColors[listing.status]
            )}
          >
            {listing.status}
          </span>
        </div>

        {/* Action Buttons */}
        <div className="absolute top-3 right-3 flex items-center gap-1.5 z-10">
          <button
            onClick={handleCompareClick}
            className="w-8 h-8 rounded-full bg-white/90 backdrop-blur-md text-slate-700 hover:text-blue-600 hover:bg-white flex items-center justify-center shadow-md transition-all"
            title="Thêm vào bảng so sánh"
          >
            <Scale className="w-4 h-4" />
          </button>
          <button
            onClick={handleFavoriteClick}
            className={cn(
              "w-8 h-8 rounded-full bg-white/90 backdrop-blur-md flex items-center justify-center shadow-md transition-all",
              isFav ? "text-rose-500 fill-rose-500" : "text-slate-700 hover:text-rose-500 hover:bg-white"
            )}
            title="Lưu tin yêu thích"
          >
            <Heart className={cn("w-4 h-4", isFav && "fill-current")} />
          </button>
        </div>

        {/* Bottom Banner with Price & Area */}
        <div className="absolute bottom-0 inset-x-0 bg-gradient-to-t from-slate-950/90 via-slate-950/50 to-transparent p-3 pt-6 flex items-end justify-between text-white">
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
      </div>

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
            <span className="line-clamp-1 font-medium">{listing.district}, {listing.city}</span>
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
            className="font-bold text-xs text-blue-600 hover:text-blue-700 flex items-center gap-0.5"
          >
            <span>Chi tiết</span>
            <span>→</span>
          </Link>
        </div>
      </div>
    </div>
  );
}
