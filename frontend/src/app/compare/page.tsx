"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { listingService } from "@/lib/services";
import { PropertyListing } from "@/types";
import { useToast } from "@/components/Toast";
import {
  Scale,
  X,
  Trash2,
  CheckCircle2,
  XCircle,
  ArrowRight,
  Maximize2,
  Zap,
  Droplet,
  ShieldCheck,
  Calendar,
} from "lucide-react";
import { cn } from "@/lib/utils";

const COMPARISON_AMENITIES = [
  "Điều hòa",
  "Nóng lạnh",
  "Máy giặt riêng",
  "Tủ lạnh",
  "Ban công",
  "Thang máy",
  "Giờ tự do",
  "Chỗ để xe",
  "Bảo vệ 24/7",
  "Khóa vân tay",
  "Vệ sinh riêng",
];

export default function ComparePage() {
  const { showToast } = useToast();
  const [comparedListings, setComparedListings] = useState<PropertyListing[]>([]);

  const loadData = () => {
    const ids = listingService.getCompareList();
    const all = listingService.getAll();
    const matches = ids.map((id) => all.find((x) => x.id === id)).filter(Boolean) as PropertyListing[];
    setComparedListings(matches);
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleRemove = (id: string) => {
    listingService.removeFromCompare(id);
    loadData();
    showToast("Đã bỏ phòng khỏi danh sách so sánh.", "info");
  };

  const handleClearAll = () => {
    listingService.clearCompare();
    setComparedListings([]);
    showToast("Đã xóa toàn bộ danh sách so sánh.", "info");
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Scale className="w-4 h-4" />
            <span>Đối chiếu trực quan</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            So Sánh Đối Chiếu Phòng Trọ
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Hỗ trợ so sánh tối đa 3 phòng cùng lúc theo tiêu chuẩn hệ thống HomeMatch.
          </p>
        </div>

        {comparedListings.length > 0 && (
          <button
            onClick={handleClearAll}
            className="flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs font-bold text-rose-600 hover:bg-rose-50 border border-rose-200 transition-colors"
          >
            <Trash2 className="w-3.5 h-3.5" />
            <span>Xóa danh sách so sánh</span>
          </button>
        )}
      </div>

      {comparedListings.length === 0 ? (
        <div className="bg-white rounded-2xl border border-slate-200/80 p-16 text-center space-y-4">
          <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto">
            <Scale className="w-8 h-8" />
          </div>
          <h2 className="text-lg font-bold text-slate-800">Chưa có phòng nào trong danh sách so sánh</h2>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            Nhấn vào biểu tượng cân (So sánh) ở góc ảnh của bất kỳ phòng nào tại trang khám phá để
            thêm vào đối chiếu.
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
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-x-auto">
          <table className="w-full text-xs text-left border-collapse min-w-[700px]">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50/70">
                <th className="p-4 w-48 font-bold text-slate-500 uppercase tracking-wider">
                  Tiêu chí đối chiếu
                </th>
                {comparedListings.map((room) => (
                  <th key={room.id} className="p-4 w-72 align-top">
                    <div className="relative space-y-3">
                      <button
                        onClick={() => handleRemove(room.id)}
                        className="absolute -top-1 -right-1 p-1 rounded-full bg-slate-200 hover:bg-rose-100 hover:text-rose-600 transition-colors"
                        title="Bỏ phòng này"
                      >
                        <X className="w-3.5 h-3.5" />
                      </button>

                      <div className="aspect-[16/10] rounded-xl overflow-hidden bg-slate-100">
                        <img
                          src={room.images[0]}
                          alt={room.title}
                          className="w-full h-full object-cover"
                        />
                      </div>

                      <Link href={`/listings/${room.id}`}>
                        <h3 className="font-bold text-slate-900 line-clamp-2 hover:text-blue-600">
                          {room.title}
                        </h3>
                      </Link>

                      <Link
                        href={`/listings/${room.id}`}
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-blue-600 text-white font-bold text-[11px] hover:bg-blue-700 shadow-xs"
                      >
                        <Calendar className="w-3 h-3" />
                        <span>Xem & Đặt lịch</span>
                      </Link>
                    </div>
                  </th>
                ))}
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100">
              {/* Row: Giá thuê */}
              <tr className="hover:bg-slate-50/50">
                <td className="p-4 font-bold text-slate-700 bg-slate-50/40">Giá thuê / tháng</td>
                {comparedListings.map((r) => (
                  <td key={r.id} className="p-4 font-black text-blue-600 text-sm">
                    {(r.price / 1000000).toLocaleString("vi-VN", { maximumFractionDigits: 1 })} triệu đ
                  </td>
                ))}
              </tr>

              {/* Row: Tiền cọc */}
              <tr className="hover:bg-slate-50/50">
                <td className="p-4 font-bold text-slate-700 bg-slate-50/40">Tiền đặt cọc</td>
                {comparedListings.map((r) => (
                  <td key={r.id} className="p-4 font-semibold text-slate-800">
                    {(r.deposit / 1000000).toFixed(1)} triệu đ
                  </td>
                ))}
              </tr>

              {/* Row: Diện tích */}
              <tr className="hover:bg-slate-50/50">
                <td className="p-4 font-bold text-slate-700 bg-slate-50/40">Diện tích sử dụng</td>
                {comparedListings.map((r) => (
                  <td key={r.id} className="p-4 font-bold text-slate-800">
                    {r.area} m²
                  </td>
                ))}
              </tr>

              {/* Row: Loại hình */}
              <tr className="hover:bg-slate-50/50">
                <td className="p-4 font-bold text-slate-700 bg-slate-50/40">Loại hình phòng</td>
                {comparedListings.map((r) => (
                  <td key={r.id} className="p-4">
                    <span className="px-2 py-0.5 rounded-md bg-slate-100 font-bold text-slate-700">
                      {r.roomType}
                    </span>
                  </td>
                ))}
              </tr>

              {/* Row: Vị trí */}
              <tr className="hover:bg-slate-50/50">
                <td className="p-4 font-bold text-slate-700 bg-slate-50/40">Khu vực / Quận huyện</td>
                {comparedListings.map((r) => (
                  <td key={r.id} className="p-4 font-medium text-slate-700">
                    {r.district}, {r.city}
                  </td>
                ))}
              </tr>

              {/* Row: Giá điện */}
              <tr className="hover:bg-slate-50/50">
                <td className="p-4 font-bold text-slate-700 bg-slate-50/40">Giá điện (đ/kWh)</td>
                {comparedListings.map((r) => (
                  <td key={r.id} className="p-4 font-semibold text-amber-700">
                    {r.electricityPrice.toLocaleString()} đ
                  </td>
                ))}
              </tr>

              {/* Row: Giá nước */}
              <tr className="hover:bg-slate-50/50">
                <td className="p-4 font-bold text-slate-700 bg-slate-50/40">Giá nước (đ/m³)</td>
                {comparedListings.map((r) => (
                  <td key={r.id} className="p-4 font-semibold text-sky-700">
                    {r.waterPrice.toLocaleString()} đ
                  </td>
                ))}
              </tr>

              {/* Row: Phí dịch vụ */}
              <tr className="hover:bg-slate-50/50">
                <td className="p-4 font-bold text-slate-700 bg-slate-50/40">Phí dịch vụ / tháng</td>
                {comparedListings.map((r) => (
                  <td key={r.id} className="p-4 font-semibold text-slate-800">
                    {r.serviceFee.toLocaleString()} đ
                  </td>
                ))}
              </tr>

              {/* Section Header: Tiện ích */}
              <tr className="bg-slate-100/70 font-bold text-slate-800">
                <td colSpan={comparedListings.length + 1} className="p-3 text-[11px] uppercase tracking-wider">
                  Bảng đối chiếu tiện ích đi kèm
                </td>
              </tr>

              {COMPARISON_AMENITIES.map((amenity) => (
                <tr key={amenity} className="hover:bg-slate-50/50">
                  <td className="p-3.5 font-medium text-slate-600 bg-slate-50/40">{amenity}</td>
                  {comparedListings.map((r) => {
                    const hasAmenity = r.amenities.includes(amenity);
                    return (
                      <td key={r.id} className="p-3.5">
                        {hasAmenity ? (
                          <div className="flex items-center gap-1.5 text-emerald-600 font-bold">
                            <CheckCircle2 className="w-4 h-4" />
                            <span>Có</span>
                          </div>
                        ) : (
                          <div className="flex items-center gap-1.5 text-slate-300 font-normal">
                            <XCircle className="w-4 h-4" />
                            <span>Không</span>
                          </div>
                        )}
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
