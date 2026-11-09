"use client";

import React, { useState, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { listingService, viewingService } from "@/lib/services";
import { parseListingNumericId, viewingsApi } from "@/lib/api-client";
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
  Star,
  GraduationCap,
  Bus,
  ShoppingBag,
  Building2,
  Calculator,
  Flame,
  Check,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function ListingDetailPage() {
  const params = useParams();
  const router = useRouter();
  const id = params.id as string;
  const { user, role } = useAuth();
  const isLandlord = role === "Landlord" || user?.role === "Landlord";
  const { showToast } = useToast();

  const [listing, setListing] = useState<PropertyListing | null>(null);
  const [activeImageIndex, setActiveImageIndex] = useState(0);
  const [isFav, setIsFav] = useState(false);
  const [showFullPhone, setShowFullPhone] = useState(false);

  // Booking Modal
  const [isBookingOpen, setIsBookingOpen] = useState(false);
  const [bookingDate, setBookingDate] = useState(() => {
    const tomorrow = new Date(Date.now() + 24 * 60 * 60 * 1000);
    return tomorrow.toISOString().split("T")[0];
  });
  const [bookingTime, setBookingTime] = useState("10:00");
  const [durationMinutes, setDurationMinutes] = useState<number>(30);
  const [isVirtualViewing, setIsVirtualViewing] = useState<boolean>(false);
  const [tenantName, setTenantName] = useState(user?.displayName || "");
  const [tenantPhone, setTenantPhone] = useState(user?.phoneNumber || "");
  const [note, setNote] = useState("");
  const [bookingLoading, setBookingLoading] = useState(false);

  useEffect(() => {
    if (user) {
      if (!tenantName) setTenantName(user.displayName || user.fullName || "");
      if (!tenantPhone) setTenantPhone(user.phoneNumber || "");
    }
  }, [user]);

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

  const handlePhoneClick = () => {
    setShowFullPhone(true);
    showToast(`Số điện thoại chủ nhà: ${listing.landlordPhone}`, "info");
  };

  const handleShare = () => {
    if (navigator.clipboard) {
      navigator.clipboard.writeText(window.location.href);
      showToast("Đã sao chép liên kết tin phòng vào bộ nhớ tạm!", "success");
    }
  };

  const handleBookViewing = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!bookingDate) {
      showToast("Vui lòng chọn ngày xem phòng!", "error");
      return;
    }

    setBookingLoading(true);
    const scheduledTime = new Date(`${bookingDate}T${bookingTime}:00`).toISOString();
    const numericListingId = parseListingNumericId(listing.id);

    try {
      const apiRes = await viewingsApi.bookAppointment({
        listingId: numericListingId,
        appointmentTime: scheduledTime,
        durationMinutes,
        notes: note.trim() || undefined,
        isVirtualViewing,
      });

      if (!apiRes.isSuccess && apiRes.error && apiRes.error.code !== "HTTP_401" && apiRes.error.code !== "NETWORK_ERROR") {
        showToast(apiRes.error.message || "Không thể đặt lịch xem phòng.", "error");
        setBookingLoading(false);
        return;
      }

      // Sync to local fallback for guest/demo mode
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

      setIsBookingOpen(false);
      showToast(
        "Đặt lịch xem phòng thành công! Chủ nhà sẽ xác nhận lịch hẹn sớm nhất.",
        "success"
      );
      router.push("/tenant/viewings");
    } finally {
      setBookingLoading(false);
    }
  };

  // First month total cost calculation widget
  const estElecUsage = 100; // 100 kWh
  const estWaterUsage = 6; // 6 m3
  const estElecCost = estElecUsage * listing.electricityPrice;
  const estWaterCost = estWaterUsage * listing.waterPrice;
  const firstMonthTotal = listing.price + listing.deposit + estElecCost + estWaterCost + listing.serviceFee;

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-8">
      {/* Top Action Bar */}
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
            onClick={handleShare}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl border border-slate-200 hover:bg-slate-50 font-medium text-slate-700"
            title="Chia sẻ tin"
          >
            <Share2 className="w-3.5 h-3.5" />
            <span>Chia sẻ</span>
          </button>
          <button
            onClick={handleAddToCompare}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl border border-slate-200 hover:bg-slate-50 font-medium text-slate-700"
          >
            <Scale className="w-3.5 h-3.5 text-blue-600" />
            <span>So sánh</span>
          </button>
          <button
            onClick={handleToggleFav}
            className={cn(
              "flex items-center gap-1.5 px-3 py-1.5 rounded-xl border font-medium transition-colors",
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

      {/* Main Grid: Gallery & Details + Sticky Widget */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
        {/* Left 8 Cols: Images, Specs, POIs, Reviews */}
        <div className="lg:col-span-8 space-y-8">
          {/* Gallery */}
          <div className="space-y-3">
            <div className="aspect-[16/10] rounded-3xl overflow-hidden bg-slate-100 relative shadow-md">
              <img
                src={listing.images[activeImageIndex] || listing.images[0]}
                alt={listing.title}
                className="w-full h-full object-cover"
              />
              <div className="absolute top-4 left-4 flex gap-2">
                <span className="px-3 py-1 rounded-full text-xs font-bold bg-slate-900/85 text-white backdrop-blur-md">
                  {listing.roomType}
                </span>
                {listing.isVerifiedLandlord && (
                  <span className="px-3 py-1 rounded-full text-xs font-bold bg-emerald-600 text-white flex items-center gap-1 shadow-md">
                    <ShieldCheck className="w-3.5 h-3.5" />
                    <span>Đã kiểm duyệt thực địa</span>
                  </span>
                )}
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
                      "w-24 h-16 rounded-2xl overflow-hidden shrink-0 border-2 transition-all",
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

          {/* Core Info Card */}
          <div className="bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/90 shadow-xs space-y-6">
            {/* Promo banner */}
            {listing.discountTag && (
              <div className="flex items-center gap-2 p-3 rounded-xl bg-amber-50 border border-amber-200 text-amber-900 text-xs font-bold">
                <Flame className="w-4 h-4 text-amber-600 shrink-0" />
                <span>{listing.discountTag}</span>
              </div>
            )}

            <div>
              <h1 className="text-xl sm:text-2xl font-black text-slate-900 leading-snug">
                {listing.title}
              </h1>

              <div className="flex items-center gap-2 text-xs text-slate-500 mt-2">
                <MapPin className="w-4 h-4 text-rose-500 shrink-0" />
                <span>{listing.address}, {listing.district}, {listing.city}</span>
              </div>

              {/* Rating & Review Summary */}
              {listing.rating && (
                <div className="flex items-center gap-3 mt-3 text-xs">
                  <div className="flex items-center gap-1 bg-amber-50 px-2 py-0.5 rounded-lg border border-amber-200/60 font-bold text-amber-700">
                    <Star className="w-3.5 h-3.5 fill-current" />
                    <span>{listing.rating} / 5.0</span>
                  </div>
                  <span className="text-slate-500">{listing.reviewCount} lượt đánh giá thực tế</span>
                  <span className="text-slate-300">•</span>
                  <span className="text-emerald-600 font-semibold flex items-center gap-1">
                    <CheckCircle2 className="w-3.5 h-3.5" />
                    <span>Phòng còn trống có thể vào ở ngay</span>
                  </span>
                </div>
              )}
            </div>

            {/* Key Specs Bar */}
            <div className="grid grid-cols-3 gap-4 p-4 rounded-2xl bg-slate-50 border border-slate-200/70 text-center">
              <div>
                <span className="block text-[11px] font-bold text-slate-400 uppercase">Giá thuê niêm yết</span>
                <span className="text-xl sm:text-2xl font-black text-blue-600">
                  {(listing.price / 1000000).toLocaleString("vi-VN", { maximumFractionDigits: 1 })} tr
                </span>
                <span className="text-[10px] text-slate-500 block">/tháng</span>
              </div>
              <div>
                <span className="block text-[11px] font-bold text-slate-400 uppercase">Diện tích sử dụng</span>
                <span className="text-xl sm:text-2xl font-black text-slate-800">{listing.area}</span>
                <span className="text-[10px] text-slate-500 block">m² sàn</span>
              </div>
              <div>
                <span className="block text-[11px] font-bold text-slate-400 uppercase">Tiền đặt cọc</span>
                <span className="text-xl sm:text-2xl font-black text-slate-800">
                  {(listing.deposit / 1000000).toFixed(1)} tr
                </span>
                <span className="text-[10px] text-slate-500 block">01 tháng tiền phòng</span>
              </div>
            </div>

            {/* Detailed Description */}
            <div className="space-y-2 pt-2">
              <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider">
                Mô tả chi tiết phòng trọ
              </h3>
              <p className="text-xs sm:text-sm text-slate-600 leading-relaxed whitespace-pre-line">
                {listing.description}
              </p>
            </div>

            {/* Amenities Grid */}
            <div className="space-y-3 pt-4 border-t border-slate-100">
              <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider">
                Tiện ích & Dịch vụ đi kèm
              </h3>
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-2.5">
                {listing.amenities.map((item) => (
                  <div
                    key={item}
                    className="flex items-center gap-2 p-3 rounded-xl bg-blue-50/60 border border-blue-100/70 text-xs text-slate-700 font-medium"
                  >
                    <CheckCircle2 className="w-4 h-4 text-blue-600 shrink-0" />
                    <span>{item}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Nearby POIs (Section 3.2: Tiện ích tra cứu liên quan) */}
            {listing.nearbyPOIs && listing.nearbyPOIs.length > 0 && (
              <div className="space-y-3 pt-4 border-t border-slate-100">
                <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider flex items-center gap-2">
                  <MapPin className="w-4 h-4 text-blue-600" />
                  <span>Tiện ích xung quanh trong bán kính 1km</span>
                </h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
                  {listing.nearbyPOIs.map((poi, idx) => (
                    <div
                      key={idx}
                      className="flex items-center justify-between p-3 rounded-xl bg-slate-50 border border-slate-200/70"
                    >
                      <div className="flex items-center gap-2.5">
                        {poi.type === "school" && <GraduationCap className="w-4 h-4 text-indigo-600" />}
                        {poi.type === "transit" && <Bus className="w-4 h-4 text-emerald-600" />}
                        {poi.type === "market" && <ShoppingBag className="w-4 h-4 text-amber-600" />}
                        {poi.type === "hospital" && <Building2 className="w-4 h-4 text-rose-600" />}
                        <span className="font-semibold text-slate-800">{poi.name}</span>
                      </div>
                      <span className="text-[11px] font-bold text-blue-600 bg-blue-50 px-2 py-0.5 rounded-md">
                        {poi.distance}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Transparent Utility Breakdown */}
            <div className="space-y-3 pt-4 border-t border-slate-100">
              <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider">
                Bảng biểu phí điện nước & Phí dịch vụ
              </h3>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                <div className="p-3.5 rounded-2xl border border-amber-200 bg-amber-50/60 flex items-center gap-3">
                  <div className="w-10 h-10 rounded-xl bg-amber-500 text-white flex items-center justify-center shrink-0">
                    <Zap className="w-5 h-5" />
                  </div>
                  <div>
                    <span className="text-[10px] font-bold text-amber-900 uppercase block">Giá điện</span>
                    <span className="text-xs font-black text-amber-950">
                      {listing.electricityPrice.toLocaleString()} đ/kWh
                    </span>
                  </div>
                </div>

                <div className="p-3.5 rounded-2xl border border-sky-200 bg-sky-50/60 flex items-center gap-3">
                  <div className="w-10 h-10 rounded-xl bg-sky-500 text-white flex items-center justify-center shrink-0">
                    <Droplet className="w-5 h-5" />
                  </div>
                  <div>
                    <span className="text-[10px] font-bold text-sky-900 uppercase block">Giá nước</span>
                    <span className="text-xs font-black text-sky-950">
                      {listing.waterPrice.toLocaleString()} đ/m³
                    </span>
                  </div>
                </div>

                <div className="p-3.5 rounded-2xl border border-slate-200 bg-slate-50 flex items-center gap-3">
                  <div className="w-10 h-10 rounded-xl bg-slate-700 text-white flex items-center justify-center shrink-0">
                    <ShieldCheck className="w-5 h-5" />
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

            {/* Real Reviews Section */}
            {listing.reviews && listing.reviews.length > 0 && (
              <div className="space-y-4 pt-4 border-t border-slate-100">
                <div className="flex items-center justify-between">
                  <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider">
                    Đánh giá từ khách thuê thực tế ({listing.reviews.length})
                  </h3>
                  <div className="flex items-center gap-1 text-amber-500 text-xs font-bold">
                    <Star className="w-4 h-4 fill-current" />
                    <span>{listing.rating} / 5.0</span>
                  </div>
                </div>

                <div className="space-y-3">
                  {listing.reviews.map((rev) => (
                    <div
                      key={rev.id}
                      className="p-4 rounded-2xl bg-slate-50/70 border border-slate-200/80 space-y-2 text-xs"
                    >
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <div className="w-8 h-8 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold text-xs">
                            {rev.author.charAt(0)}
                          </div>
                          <div>
                            <span className="font-bold text-slate-900 block">{rev.author}</span>
                            <span className="text-[10px] text-slate-500">{rev.role}</span>
                          </div>
                        </div>

                        <div className="flex items-center gap-1 text-amber-500">
                          {[1, 2, 3, 4, 5].map((i) => (
                            <Star key={i} className="w-3 h-3 fill-current" />
                          ))}
                        </div>
                      </div>
                      <p className="text-slate-600 leading-relaxed italic">{rev.content}</p>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Right 4 Cols: Sticky Commercial Contact & Estimated Cost Widget */}
        <div className="lg:col-span-4 space-y-6 sticky top-20">
          {/* Contact Box */}
          <div className="bg-white p-6 rounded-3xl border border-slate-200/90 shadow-xl space-y-5">
            <div className="space-y-3">
              {!isLandlord ? (
                <button
                  onClick={() => setIsBookingOpen(true)}
                  disabled={listing.status !== "Còn trống"}
                  className="w-full py-3.5 px-4 rounded-2xl font-bold text-sm text-white bg-blue-600 hover:bg-blue-700 shadow-lg shadow-blue-500/30 transition-all flex items-center justify-center gap-2 disabled:opacity-50"
                >
                  <Calendar className="w-4 h-4" />
                  <span>Đặt Lịch Xem Phòng Miễn Phí</span>
                </button>
              ) : (
                <div className="w-full py-3 px-3.5 rounded-2xl text-xs font-semibold text-slate-600 bg-slate-50 border border-slate-200/80 text-center flex items-center justify-center gap-2">
                  <ShieldCheck className="w-4 h-4 text-slate-400" />
                  <span>Tài khoản Chủ nhà không đặt lịch xem phòng</span>
                </div>
              )}

              <div className="grid grid-cols-2 gap-2">
                <Link
                  href={`/messages?listingId=${parseListingNumericId(listing.id)}`}
                  className="py-2.5 px-3 rounded-xl font-bold text-xs text-slate-800 bg-slate-100 hover:bg-slate-200 transition-all flex items-center justify-center gap-1.5"
                >
                  <MessageSquare className="w-3.5 h-3.5 text-blue-600" />
                  <span>Chat chủ trọ</span>
                </Link>

                <button
                  type="button"
                  onClick={handlePhoneClick}
                  className="py-2.5 px-3 rounded-xl font-bold text-xs text-emerald-700 bg-emerald-50 hover:bg-emerald-100 border border-emerald-200 transition-all flex items-center justify-center gap-1.5"
                >
                  <Phone className="w-3.5 h-3.5 text-emerald-600" />
                  <span>{showFullPhone ? listing.landlordPhone : "Hiện số ĐT"}</span>
                </button>
              </div>
            </div>

            {/* Landlord Trust Profile */}
            <div className="pt-4 border-t border-slate-100 space-y-3">
              <div className="flex items-center gap-3">
                <div className="w-12 h-12 rounded-full bg-blue-600 text-white flex items-center justify-center font-black text-lg shadow-sm">
                  {listing.landlordName.charAt(0)}
                </div>
                <div>
                  <h4 className="font-bold text-slate-900 text-sm">{listing.landlordName}</h4>
                  <div className="flex items-center gap-1 text-[11px] text-emerald-600 font-semibold">
                    <ShieldCheck className="w-3.5 h-3.5" />
                    <span>Chủ nhà đã xác minh giấy tờ</span>
                  </div>
                </div>
              </div>

              <div className="p-3 bg-slate-50 rounded-xl space-y-1.5 text-xs text-slate-600">
                <div className="flex justify-between">
                  <span>Tỷ lệ phản hồi:</span>
                  <span className="font-bold text-slate-900">98% (trong 15 phút)</span>
                </div>
                <div className="flex justify-between">
                  <span>Số tin đang quản lý:</span>
                  <span className="font-bold text-slate-900">3 bất động sản</span>
                </div>
              </div>
            </div>

            {/* First Month Budget Estimator Widget */}
            <div className="pt-4 border-t border-slate-100 space-y-3">
              <div className="flex items-center gap-2 text-slate-900 font-bold text-xs uppercase tracking-wider">
                <Calculator className="w-4 h-4 text-blue-600" />
                <span>Dự tính chi phí tháng đầu vào ở</span>
              </div>

              <div className="p-3.5 bg-blue-50/50 rounded-2xl border border-blue-100 space-y-2 text-xs">
                <div className="flex justify-between text-slate-600">
                  <span>Tiền thuê tháng đầu:</span>
                  <span className="font-semibold text-slate-900">{listing.price.toLocaleString()} đ</span>
                </div>
                <div className="flex justify-between text-slate-600">
                  <span>Tiền cọc giữ chỗ (1 tháng):</span>
                  <span className="font-semibold text-slate-900">{listing.deposit.toLocaleString()} đ</span>
                </div>
                <div className="flex justify-between text-slate-600">
                  <span>Dự kiến điện (100 kWh):</span>
                  <span className="font-semibold text-slate-900">{estElecCost.toLocaleString()} đ</span>
                </div>
                <div className="flex justify-between text-slate-600">
                  <span>Dự kiến nước (6 m³):</span>
                  <span className="font-semibold text-slate-900">{estWaterCost.toLocaleString()} đ</span>
                </div>
                <div className="flex justify-between text-slate-600">
                  <span>Phí dịch vụ:</span>
                  <span className="font-semibold text-slate-900">{listing.serviceFee.toLocaleString()} đ</span>
                </div>

                <div className="pt-2 border-t border-blue-200/80 flex justify-between font-black text-sm text-blue-900">
                  <span>Tổng chuẩn bị ban đầu:</span>
                  <span className="text-blue-700">{firstMonthTotal.toLocaleString()} VNĐ</span>
                </div>
              </div>

              <p className="text-[10px] text-slate-400 italic text-center">
                * Tiền cọc được hoàn lại 100% khi kết thúc hợp đồng theo điều khoản.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Booking Modal */}
      <Modal
        isOpen={isBookingOpen}
        onClose={() => setIsBookingOpen(false)}
        title="Đặt Lịch Hẹn Xem Phòng Trực Tiếp (Miễn Phí)"
        description="Điền thông tin và thời gian mong muốn, chủ nhà sẽ liên hệ xác nhận trong vòng 15 phút."
        maxWidth="md"
      >
        <form onSubmit={handleBookViewing} className="space-y-4">
          <div className="p-3 rounded-xl bg-slate-50 border border-slate-200 text-xs space-y-1">
            <span className="font-bold text-slate-900 block line-clamp-1">{listing.title}</span>
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

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
                Thời lượng xem
              </label>
              <select
                value={durationMinutes}
                onChange={(e) => setDurationMinutes(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white"
              >
                <option value={30}>30 phút (Tiêu chuẩn)</option>
                <option value={45}>45 phút</option>
                <option value={60}>60 phút (Xem kỹ)</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
                Hình thức xem phòng
              </label>
              <select
                value={isVirtualViewing ? "virtual" : "inperson"}
                onChange={(e) => setIsVirtualViewing(e.target.value === "virtual")}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white"
              >
                <option value="inperson">Xem trực tiếp tại phòng</option>
                <option value="virtual">Xem online (Video Call)</option>
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1">
              Ghi chú thêm cho chủ nhà
            </label>
            <textarea
              rows={2}
              placeholder="Ví dụ: Cháu đi xe máy, có cần mang CMND/CCCD khi đến xem không ạ..."
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
              {bookingLoading ? "Đang gửi yêu cầu..." : "Xác nhận gửi lịch hẹn"}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
