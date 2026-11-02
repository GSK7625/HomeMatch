"use client";

import React, { useState, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { listingService, viewingService } from "@/lib/services";
import { PropertyListing } from "@/types";
import { useAuth } from "@/features/auth/AuthContext";
import { useToast } from "@/components/Toast";
import { Modal } from "@/components/Modal";
import {
  MapPin,
  Calendar,
  Heart,
  Scale,
  Maximize2,
  DollarSign,
  Zap,
  Droplet,
  ShieldCheck,
  Phone,
  MessageSquare,
  User,
  Clock,
  CheckCircle2,
  ArrowLeft,
  Share2,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function ListingDetailPage() {
  const params = useParams();
  const router = useRouter();
  const id = params.id as string;
  const { user } = useAuth();
  const { showToast } = useToast();

  const [listing, setListing] = useState<PropertyListing | null>(null);
  const [activeImageIndex, setActiveImageIndex] = useState(0);
  const [isFav, setIsFav] = useState(false);

  // Booking Modal
  const [isBookingOpen, setIsBookingOpen] = useState(false);
  const [bookingDate, setBookingDate] = useState("");
  const [bookingTime, setBookingTime] = useState("10:00");
  const [tenantName, setTenantName] = useState(user?.displayName || "Đỗ Trung Kiên");
  const [tenantPhone, setTenantPhone] = useState(user?.phoneNumber || "0977 123 456");
  const [note, setNote] = useState("");
  const [bookingLoading, setBookingLoading] = useState(false);

  useEffect(() => {
    if (id) {
      const data = listingService.getById(id);
      if (data) {
        setListing(data);
        listingService.incrementView(id);
        setIsFav(listingService.isFavorite(id));
      }
    }
  }, [id]);

  if (!listing) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-16 text-center space-y-4">
        <h2 className="text-xl font-bold text-slate-800">Không tìm thấy thông tin phòng</h2>
        <p className="text-xs text-slate-500">Tin đăng có thể đã bị gỡ hoặc đường dẫn không đúng.</p>
        <Link
          href="/listings"
          className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-xl text-xs font-bold"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Về danh sách phòng</span>
        </Link>
      </div>
    );
  }

  const handleToggleFav = () => {
    const newState = listingService.toggleFavorite(listing.id);
    setIsFav(newState);
    showToast(
      newState ? "Đã thêm vào phòng yêu thích!" : "Đã xóa khỏi phòng yêu thích",
      newState ? "success" : "info"
    );
  };

  const handleAddToCompare = () => {
    const res = listingService.addToCompare(listing.id);
    showToast(res.message, res.success ? "success" : "error");
  };

  const handleBookViewing = (e: React.FormEvent) => {
    e.preventDefault();
    if (!bookingDate) {
      showToast("Vui lòng chọn ngày xem phòng!", "error");
      return;
    }

    setBookingLoading(true);
    const scheduledTime = new Date(`${bookingDate}T${bookingTime}:00Z`).toISOString();

    viewingService.book({
      listingId: listing.id,
      listingTitle: listing.title,
      listingAddress: listing.address,
      listingImage: listing.images[0] || "",
      tenantId: user?.accountId || "demo-tenant-id",
      tenantName,
      tenantPhone,
      landlordId: listing.landlordId,
      scheduledTime,
      note,
    });

    setBookingLoading(false);
    setIsBookingOpen(false);
    showToast(
      "Đặt lịch xem phòng thành công! Chủ nhà sẽ xác nhận lịch hẹn sớm nhất.",
      "success"
    );
  };

  const statusColors = {
    "Còn trống": "bg-emerald-50 text-emerald-700 border-emerald-200",
    "Đã thuê": "bg-slate-100 text-slate-600 border-slate-200",
    "Tạm ngừng": "bg-rose-50 text-rose-700 border-rose-200",
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-8">
      {/* Back button & Breadcrumb */}
      <div className="flex items-center justify-between text-xs text-slate-500">
        <Link
          href="/listings"
          className="flex items-center gap-1.5 font-semibold text-slate-700 hover:text-blue-600"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Quay lại danh sách phòng</span>
        </Link>

        <div className="flex items-center gap-2">
          <button
            onClick={handleAddToCompare}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-slate-200 hover:bg-slate-50 font-medium text-slate-700"
          >
            <Scale className="w-3.5 h-3.5 text-blue-600" />
            <span>So sánh</span>
          </button>
          <button
            onClick={handleToggleFav}
            className={cn(
              "flex items-center gap-1.5 px-3 py-1.5 rounded-lg border font-medium transition-colors",
              isFav
                ? "bg-rose-50 border-rose-200 text-rose-600 font-bold"
                : "border-slate-200 hover:bg-slate-50 text-slate-700"
            )}
          >
            <Heart className={cn("w-3.5 h-3.5", isFav && "fill-current")} />
            <span>{isFav ? "Đã lưu tin" : "Lưu tin"}</span>
          </button>
        </div>
      </div>

      {/* Main Grid: Gallery & Details */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Left 2 Cols: Images & Full Content */}
        <div className="lg:col-span-2 space-y-8">
          {/* Gallery */}
          <div className="space-y-3">
            <div className="aspect-[16/10] rounded-2xl overflow-hidden bg-slate-100 relative shadow-sm">
              <img
                src={listing.images[activeImageIndex] || listing.images[0]}
                alt={listing.title}
                className="w-full h-full object-cover"
              />
              <div className="absolute top-4 left-4 flex gap-2">
                <span className="px-3 py-1 rounded-full text-xs font-bold bg-slate-900/80 text-white backdrop-blur-md">
                  {listing.roomType}
                </span>
                <span
                  className={cn(
                    "px-3 py-1 rounded-full text-xs font-bold border backdrop-blur-md",
                    statusColors[listing.status]
                  )}
                >
                  {listing.status}
                </span>
              </div>
            </div>

            {/* Thumbnail selector */}
            {listing.images.length > 1 && (
              <div className="flex gap-3 overflow-x-auto pb-1">
                {listing.images.map((img, idx) => (
                  <button
                    key={idx}
                    onClick={() => setActiveImageIndex(idx)}
                    className={cn(
                      "w-20 h-14 rounded-xl overflow-hidden shrink-0 border-2 transition-all",
                      activeImageIndex === idx
                        ? "border-blue-600 shadow-md scale-105"
                        : "border-transparent opacity-70 hover:opacity-100"
                    )}
                  >
                    <img src={img} alt="Thumb" className="w-full h-full object-cover" />
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Title & Core Meta */}
          <div className="bg-white p-6 sm:p-8 rounded-2xl border border-slate-200/80 shadow-xs space-y-4">
            <h1 className="text-xl sm:text-2xl font-black text-slate-900 leading-snug">
              {listing.title}
            </h1>

            <div className="flex items-center gap-2 text-xs text-slate-500">
              <MapPin className="w-4 h-4 text-slate-400 shrink-0" />
              <span>{listing.address}, {listing.district}, {listing.city}</span>
            </div>

            {/* Key specs bar */}
            <div className="grid grid-cols-3 gap-4 p-4 rounded-xl bg-slate-50 border border-slate-200/60 text-center">
              <div>
                <span className="block text-[11px] font-bold text-slate-400 uppercase">Giá thuê</span>
                <span className="text-lg sm:text-xl font-black text-blue-600">
                  {(listing.price / 1000000).toLocaleString("vi-VN", { maximumFractionDigits: 1 })} tr
                </span>
                <span className="text-[10px] text-slate-500 block">/tháng</span>
              </div>
              <div>
                <span className="block text-[11px] font-bold text-slate-400 uppercase">Diện tích</span>
                <span className="text-lg sm:text-xl font-black text-slate-800">{listing.area}</span>
                <span className="text-[10px] text-slate-500 block">m² sàn</span>
              </div>
              <div>
                <span className="block text-[11px] font-bold text-slate-400 uppercase">Tiền đặt cọc</span>
                <span className="text-lg sm:text-xl font-black text-slate-800">
                  {(listing.deposit / 1000000).toFixed(1)} tr
                </span>
                <span className="text-[10px] text-slate-500 block">01 tháng</span>
              </div>
            </div>

            {/* Description */}
            <div className="pt-4 border-t border-slate-100 space-y-2">
              <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider">
                Mô tả chi tiết phòng
              </h3>
              <p className="text-xs sm:text-sm text-slate-600 leading-relaxed whitespace-pre-line">
                {listing.description}
              </p>
            </div>

            {/* Amenities Grid */}
            <div className="pt-4 border-t border-slate-100 space-y-3">
              <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider">
                Tiện ích & Dịch vụ có sẵn
              </h3>
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-2.5">
                {listing.amenities.map((item) => (
                  <div
                    key={item}
                    className="flex items-center gap-2 p-2.5 rounded-xl bg-blue-50/50 border border-blue-100/60 text-xs text-slate-700 font-medium"
                  >
                    <CheckCircle2 className="w-3.5 h-3.5 text-blue-600 shrink-0" />
                    <span>{item}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Utility Tariff Disclosure (Section 3.8 / 4.3) */}
            <div className="pt-4 border-t border-slate-100 space-y-3">
              <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider">
                Biểu phí điện, nước & Dịch vụ
              </h3>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                <div className="p-3 rounded-xl border border-amber-200 bg-amber-50/60 flex items-center gap-3">
                  <div className="w-8 h-8 rounded-lg bg-amber-500 text-white flex items-center justify-center shrink-0">
                    <Zap className="w-4 h-4" />
                  </div>
                  <div>
                    <span className="text-[10px] font-bold text-amber-900 uppercase block">Giá điện</span>
                    <span className="text-xs font-black text-amber-950">
                      {listing.electricityPrice.toLocaleString()} đ/kWh
                    </span>
                  </div>
                </div>

                <div className="p-3 rounded-xl border border-sky-200 bg-sky-50/60 flex items-center gap-3">
                  <div className="w-8 h-8 rounded-lg bg-sky-500 text-white flex items-center justify-center shrink-0">
                    <Droplet className="w-4 h-4" />
                  </div>
                  <div>
                    <span className="text-[10px] font-bold text-sky-900 uppercase block">Giá nước</span>
                    <span className="text-xs font-black text-sky-950">
                      {listing.waterPrice.toLocaleString()} đ/m³
                    </span>
                  </div>
                </div>

                <div className="p-3 rounded-xl border border-slate-200 bg-slate-50 flex items-center gap-3">
                  <div className="w-8 h-8 rounded-lg bg-slate-600 text-white flex items-center justify-center shrink-0">
                    <ShieldCheck className="w-4 h-4" />
                  </div>
                  <div>
                    <span className="text-[10px] font-bold text-slate-700 uppercase block">Phí dịch vụ</span>
                    <span className="text-xs font-black text-slate-900">
                      {listing.serviceFee.toLocaleString()} đ/tháng
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Right 1 Col: Landlord Card & Action Box */}
        <div className="space-y-6">
          <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs space-y-6 sticky top-20">
            {/* Action CTAs */}
            <div className="space-y-3">
              <button
                onClick={() => setIsBookingOpen(true)}
                disabled={listing.status !== "Còn trống"}
                className="w-full py-3.5 px-4 rounded-xl font-bold text-sm text-white bg-blue-600 hover:bg-blue-700 shadow-lg shadow-blue-500/25 transition-all flex items-center justify-center gap-2 disabled:opacity-50"
              >
                <Calendar className="w-4 h-4" />
                <span>
                  {listing.status === "Còn trống" ? "Đặt Lịch Xem Phòng Ngay" : "Phòng hiện không trống"}
                </span>
              </button>

              <Link
                href="/messages"
                className="w-full py-3 px-4 rounded-xl font-bold text-xs text-slate-800 bg-slate-100 hover:bg-slate-200 transition-all flex items-center justify-center gap-2"
              >
                <MessageSquare className="w-4 h-4 text-blue-600" />
                <span>Nhắn tin với chủ nhà</span>
              </Link>
            </div>

            {/* Landlord Info Card */}
            <div className="pt-6 border-t border-slate-100 space-y-4">
              <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                Thông tin chủ trọ
              </span>

              <div className="flex items-center gap-3">
                <div className="w-12 h-12 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold text-base shadow-sm">
                  {listing.landlordName.charAt(0)}
                </div>
                <div>
                  <h4 className="font-bold text-slate-900 text-sm">{listing.landlordName}</h4>
                  <div className="flex items-center gap-1 text-[11px] text-emerald-600 font-medium">
                    <ShieldCheck className="w-3.5 h-3.5" />
                    <span>Chủ nhà đã xác minh danh tính</span>
                  </div>
                </div>
              </div>

              <div className="p-3 bg-slate-50 rounded-xl space-y-2 text-xs">
                <div className="flex items-center justify-between text-slate-600">
                  <span className="flex items-center gap-1.5">
                    <Phone className="w-3.5 h-3.5 text-slate-400" />
                    <span>Số điện thoại:</span>
                  </span>
                  <span className="font-bold text-slate-900">{listing.landlordPhone}</span>
                </div>
                <div className="flex items-center justify-between text-slate-600">
                  <span className="flex items-center gap-1.5">
                    <Clock className="w-3.5 h-3.5 text-slate-400" />
                    <span>Thời gian phản hồi:</span>
                  </span>
                  <span className="font-semibold text-slate-800">Dưới 15 phút</span>
                </div>
              </div>
            </div>

            {/* Safety tips */}
            <div className="p-3.5 bg-blue-50/60 border border-blue-100 rounded-xl space-y-1.5 text-[11px] text-blue-900">
              <p className="font-bold flex items-center gap-1.5 text-blue-700">
                <ShieldCheck className="w-4 h-4" />
                <span>Thuê nhà an toàn với HomeMatch</span>
              </p>
              <p className="leading-relaxed text-blue-800/80">
                Luôn đi xem phòng trực tiếp trước khi đặt cọc. Mọi khoản cọc và hợp đồng nên được lập
                bằng văn bản trên hệ thống.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Booking Viewing Modal */}
      <Modal
        isOpen={isBookingOpen}
        onClose={() => setIsBookingOpen(false)}
        title="Đặt Lịch Hẹn Xem Phòng Trực Tiếp"
        description="Điền thông tin và thời gian mong muốn, chủ nhà sẽ phản hồi xác nhận qua hệ thống."
        maxWidth="md"
      >
        <form onSubmit={handleBookViewing} className="space-y-4">
          <div className="p-3 rounded-xl bg-slate-50 border border-slate-200/80 text-xs space-y-1">
            <span className="font-bold text-slate-800 block line-clamp-1">{listing.title}</span>
            <span className="text-slate-500 block">{listing.address}</span>
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
              Họ tên người xem
            </label>
            <input
              type="text"
              required
              value={tenantName}
              onChange={(e) => setTenantName(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
              Số điện thoại liên hệ
            </label>
            <input
              type="tel"
              required
              value={tenantPhone}
              onChange={(e) => setTenantPhone(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
                Ngày muốn xem
              </label>
              <input
                type="date"
                required
                value={bookingDate}
                onChange={(e) => setBookingDate(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
                Giờ xem
              </label>
              <input
                type="time"
                required
                value={bookingTime}
                onChange={(e) => setBookingTime(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
              Ghi chú thêm cho chủ nhà
            </label>
            <textarea
              rows={2}
              placeholder="Ví dụ: Cháu đi xe máy, có cần chuẩn bị giấy tờ gì khi đến xem không ạ..."
              value={note}
              onChange={(e) => setNote(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
            />
          </div>

          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={() => setIsBookingOpen(false)}
              className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={bookingLoading}
              className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 shadow-md shadow-blue-500/20 disabled:opacity-50"
            >
              {bookingLoading ? "Đang gửi yêu cầu..." : "Xác nhận gửi yêu cầu"}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
