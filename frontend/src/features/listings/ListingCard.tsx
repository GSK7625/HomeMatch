"use client";

import React, { useState } from "react";
import Link from "next/link";
import { PropertyListing } from "@/types";
import { listingService } from "@/lib/services";
import { useToast } from "@/components/Toast";
import { Heart, Scale, MapPin, Maximize2, Zap, Droplet } from "lucide-react";
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
      newState ? "Đã thêm vào danh sách yêu thích!" : "Đã xóa khỏi danh sách yêu thích",
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
    "Còn trống": "bg-emerald-50 text-emerald-700 border-emerald-200",
    "Đã thuê": "bg-slate-100 text-slate-600 border-slate-200",
    "Tạm ngừng": "bg-rose-50 text-rose-700 border-rose-200",
  };

  return (
    <div className="group bg-white rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-xl hover:border-blue-200 transition-all duration-300 overflow-hidden flex flex-col">
      {/* Image & Badges */}
      <div className="relative aspect-[16/10] overflow-hidden bg-slate-100">
        <img
          src={listing.images[0] || "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=800&q=80"}
          alt={listing.title}
          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
        />

        {/* Top Badges */}
        <div className="absolute top-3 left-3 flex flex-wrap gap-1.5">
          <span className="px-2.5 py-1 rounded-full text-[11px] font-bold bg-slate-900/80 text-white backdrop-blur-md">
            {listing.roomType}
          </span>
          <span
            className={cn(
              "px-2.5 py-1 rounded-full text-[11px] font-bold border backdrop-blur-md",
              statusColors[listing.status]
            )}
          >
            {listing.status}
          </span>
        </div>

        {/* Top Action Buttons */}
        <div className="absolute top-3 right-3 flex items-center gap-1.5">
          <button
            onClick={handleCompareClick}
            className="w-8 h-8 rounded-full bg-white/90 backdrop-blur-md text-slate-700 hover:text-blue-600 hover:bg-white flex items-center justify-center shadow-sm transition-all"
            title="Thêm vào bảng so sánh"
          >
            <Scale className="w-4 h-4" />
          </button>
          <button
            onClick={handleFavoriteClick}
            className={cn(
              "w-8 h-8 rounded-full bg-white/90 backdrop-blur-md flex items-center justify-center shadow-sm transition-all",
              isFav ? "text-rose-500 fill-rose-500" : "text-slate-700 hover:text-rose-500 hover:bg-white"
            )}
            title="Lưu tin yêu thích"
          >
            <Heart className={cn("w-4 h-4", isFav && "fill-current")} />
          </button>
        </div>

        {/* Bottom Price Overlay */}
        <div className="absolute bottom-0 inset-x-0 bg-gradient-to-t from-slate-950/80 via-slate-950/40 to-transparent p-3 pt-6 flex items-end justify-between text-white">
          <div>
            <span className="text-lg font-black text-white">{formattedPrice(listing.price)}</span>
            <span className="text-[11px] text-slate-200 block">Cọc: {(listing.deposit / 1000000).toFixed(1)} tr</span>
          </div>
          <div className="flex items-center gap-1 text-xs font-semibold bg-white/20 backdrop-blur-md px-2 py-1 rounded-lg">
            <Maximize2 className="w-3.5 h-3.5" />
            <span>{listing.area} m²</span>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="p-4 flex-1 flex flex-col justify-between">
        <div>
          <Link href={`/listings/${listing.id}`}>
            <h3 className="font-bold text-slate-900 text-sm line-clamp-2 group-hover:text-blue-600 transition-colors mb-2">
              {listing.title}
            </h3>
          </Link>

          <div className="flex items-start gap-1.5 text-xs text-slate-500 mb-3">
            <MapPin className="w-3.5 h-3.5 text-slate-400 shrink-0 mt-0.5" />
            <span className="line-clamp-1">{listing.district}, {listing.city}</span>
          </div>

          {/* Amenities tags */}
          <div className="flex flex-wrap gap-1.5 mb-3">
            {listing.amenities.slice(0, 3).map((amenity) => (
              <span
                key={amenity}
                className="px-2 py-0.5 text-[10px] font-medium bg-slate-100 text-slate-600 rounded-md"
              >
                {amenity}
              </span>
            ))}
            {listing.amenities.length > 3 && (
              <span className="px-1.5 py-0.5 text-[10px] font-medium bg-slate-50 text-slate-400 rounded-md">
                +{listing.amenities.length - 3}
              </span>
            )}
          </div>
        </div>

        {/* Utility rates & Details CTA */}
        <div className="pt-3 border-t border-slate-100 flex items-center justify-between text-[11px] text-slate-500">
          <div className="flex items-center gap-2">
            <span className="flex items-center gap-0.5 text-amber-600">
              <Zap className="w-3 h-3" />
              <span>{listing.electricityPrice.toLocaleString()}đ</span>
            </span>
            <span>•</span>
            <span className="flex items-center gap-0.5 text-sky-600">
              <Droplet className="w-3 h-3" />
              <span>{listing.waterPrice.toLocaleString()}đ</span>
            </span>
          </div>

          <Link
            href={`/listings/${listing.id}`}
            className="font-bold text-blue-600 hover:text-blue-700 text-xs"
          >
            Chi tiết →
          </Link>
        </div>
      </div>
    </div>
  );
}
