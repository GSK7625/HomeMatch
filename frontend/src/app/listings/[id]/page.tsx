"use client";

import React, { useState, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { listingService, viewingService } from "@/lib/services";
import { parseListingNumericId, viewingsApi, listingsApi, adminListingsApi, propertiesApi, getStoredToken } from "@/lib/api-client";
import { PropertyListing, PublicListingDetailDto } from "@/types";
import { useAuth } from "@/features/auth/AuthContext";
import { useToast } from "@/components/Toast";
import { Modal } from "@/components/Modal";
import { MapLocationViewer } from "@/components/MapLocationViewer";
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
  AlertTriangle,
} from "lucide-react";
import { cn, formatRoomTypeLabel } from "@/lib/utils";

export default function ListingDetailPage() {
  const params = useParams();
  const router = useRouter();
  const id = params.id as string;
  const { user, role } = useAuth();
  const isLandlord = role === "Landlord" || user?.role === "Landlord";
  const { showToast } = useToast();

  const [listing, setListing] = useState<PropertyListing | null>(null);
  const [rawDetail, setRawDetail] = useState<PublicListingDetailDto | null>(null);
  const [isPreview, setIsPreview] = useState(false);
  const [activeImageIndex, setActiveImageIndex] = useState(0);
  const [isFav, setIsFav] = useState(false);
  const [showFullPhone, setShowFullPhone] = useState(false);
  const [loading, setLoading] = useState(true);

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
    window.scrollTo({ top: 0, behavior: "instant" });
  }, [id]);

  useEffect(() => {
    let isMounted = true;
    async function loadDetail() {
      if (!id) return;
      setLoading(true);
      const numericId = parseListingNumericId(id);
      if (numericId > 0) {
        try {
          let d: any = null;
          let preview = false;

          // 1. Try public endpoint
          const res = await listingsApi.getPublicListingById(numericId);
          if (res.isSuccess && res.data) {
            d = res.data;
          } else if (getStoredToken()) {
            // 2. If public fails (e.g. 404 because DRAFT or PENDING_APPROVAL), try landlord endpoint
            const mineRes = await listingsApi.getMyListingById(numericId);
            if (mineRes.isSuccess && mineRes.data) {
              d = mineRes.data;
              preview = true;
            } else {
              // 3. Try admin endpoint
              const adminRes = await adminListingsApi.getReviewListing(numericId);
              if (adminRes.isSuccess && adminRes.data) {
                d = adminRes.data;
                preview = true;
              }
            }
          }

          if (isMounted && d) {
            // In preview mode (d comes from getMyListingById or admin review, without property/room objects)
            if (preview && d.roomId && (!d.property || !d.room)) {
              try {
                const propsRes = await propertiesApi.getMyProperties();
                if (propsRes.isSuccess && propsRes.data) {
                  for (const p of propsRes.data) {
                    const roomsRes = await propertiesApi.getRooms(p.id);
                    if (roomsRes.isSuccess && roomsRes.data) {
                      const matchedRoom = roomsRes.data.find((r) => r.id === d.roomId);
                      if (matchedRoom) {
                        d.property = {
                          id: p.id,
                          name: p.name,
                          propertyType: p.propertyType,
                          addressDetail: p.addressDetail,
                          wardName: p.wardCode,
                          provinceName: p.provinceCode,
                        };
                        d.room = {
                          id: matchedRoom.id,
                          roomNumber: matchedRoom.roomNumber,
                          floor: matchedRoom.floor,
                          areaSqm: matchedRoom.areaSqm,
                          roomType: matchedRoom.roomType,
                          maxOccupants: matchedRoom.maxOccupants,
                          amenities: matchedRoom.amenityIds?.map((id: number) => ({ id, code: String(id), name: `Tiện ích #${id}` })) || [],
                        };
                        break;
                      }
                    }
                  }
                }
              } catch {
                // Ignore enrichment error
              }
            }

            const mapped: PropertyListing = {
              id: String(d.id),
              title: d.title,
              description: d.description || "",
              address: d.property
                ? [d.property.addressDetail, d.property.wardName, d.property.provinceName]
                    .filter(Boolean)
                    .join(", ")
                : "Hà Nội, Việt Nam",
              ward: d.property?.wardName || "Phường / Xã",
              district: d.property?.wardName || "",
              city: d.property?.provinceName || "Hà Nội",
              price: d.price,
              deposit: d.depositAmount,
              area: Number(d.room?.areaSqm || 25),
              roomType: formatRoomTypeLabel(d.room?.roomType),
              electricityPrice:
                d.tariffs?.find((t: any) => t.tariffType === "ELECTRICITY")?.unitPrice || 3500,
              waterPrice: d.tariffs?.find((t: any) => t.tariffType === "WATER")?.unitPrice || 25000,
              serviceFee: d.tariffs?.find((t: any) => t.tariffType === "SERVICE")?.unitPrice || 150000,
              amenities:
                d.room?.amenities && d.room.amenities.length > 0
                  ? d.room.amenities.map((a: any) => a.name)
                  : ["Điều hòa", "Nóng lạnh", "Máy giặt"],
              images:
                d.images && d.images.length > 0
                  ? d.images.map((img: any) => img.imageUrl)
                  : [
                      "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=800&q=80",
                    ],
              status:
                (d.status || "").toUpperCase() === "PUBLISHED"
                  ? "Còn trống"
                  : (d.status || "").toUpperCase() === "HIDDEN"
                  ? "Tạm ngừng"
                  : "Đã thuê",
              landlordId: String(d.landlordId),
              landlordName: "Chủ Nhà",
              landlordPhone: "0902000002",
              latitude: d.latitude || 21.034,
              longitude: d.longitude || 105.793,
              createdAt: new Date().toISOString(),
              viewCount: Number(d.viewCount) || 1,
              isFeatured: d.isFeatured,
            };
            setRawDetail(d);
            setIsPreview(preview);
            setListing(mapped);
            setIsFav(listingService.isFavorite(id));
            if (getStoredToken()) {
              listingsApi.getSavedListings().then((sRes) => {
                if (isMounted && sRes.isSuccess && Array.isArray(sRes.data)) {
                  const savedIds = sRes.data.map((s) => String(s.listingId));
                  listingService.syncFavorites(savedIds);
                  const savedOnServer = sRes.data.some((s) => s.listingId === d.id);
                  setIsFav(savedOnServer);
                }
              }).catch(() => null);
            }
            if (isMounted) setLoading(false);
            return;
          }
        } catch (err) {
          console.warn("Failed to fetch listing detail from backend", err);
        }
      }

      // If not found in backend
      if (isMounted) {
        setListing(null);
        setLoading(false);
      }
    }
    loadDetail();
    return () => {
      isMounted = false;
    };
  }, [id]);

  if (loading) {
    return (
      <div className="max-w-6xl mx-auto px-4 py-8 space-y-6 animate-pulse">
        <div className="flex items-center gap-2">
          <div className="h-4 w-24 bg-slate-200 rounded" />
          <span className="text-slate-300">/</span>
          <div className="h-4 w-32 bg-slate-200 rounded" />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <div className="lg:col-span-2 space-y-6">
            <div className="aspect-[16/10] bg-slate-200 rounded-2xl" />
            <div className="grid grid-cols-4 gap-2">
              <div className="aspect-[4/3] bg-slate-200 rounded-xl" />
              <div className="aspect-[4/3] bg-slate-200 rounded-xl" />
              <div className="aspect-[4/3] bg-slate-200 rounded-xl" />
              <div className="aspect-[4/3] bg-slate-200 rounded-xl" />
            </div>
            <div className="space-y-3 pt-4">
              <div className="h-8 w-3/4 bg-slate-200 rounded-lg" />
              <div className="h-4 w-1/2 bg-slate-200 rounded" />
              <div className="h-24 bg-slate-100 rounded-xl border border-slate-200/60" />
            </div>
          </div>
          <div className="space-y-4">
            <div className="h-72 bg-slate-100 rounded-2xl border border-slate-200 p-6 space-y-4">
              <div className="h-8 w-2/3 bg-slate-200 rounded" />
              <div className="h-12 bg-slate-200 rounded-xl" />
              <div className="h-12 bg-slate-200 rounded-xl" />
            </div>
          </div>
        </div>
      </div>
    );
  }

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

  const handleToggleFav = async () => {
    if (!listing) return;
    const previousState = isFav;
    const nextState = !previousState;
    const numericId = parseListingNumericId(listing.id);
    const token = getStoredToken();

    // Optimistic update
    setIsFav(nextState);
    listingService.setFavorite(listing.id, nextState);

    if (numericId > 0 && token) {
      try {
        const res = await listingsApi.toggleSaveListing(numericId);
        if (res.isSuccess && typeof res.data === "boolean") {
          const serverState = res.data;
          setIsFav(serverState);
          listingService.setFavorite(listing.id, serverState);
          showToast(
            serverState ? "Đã thêm vào phòng yêu thích!" : "Đã xóa khỏi phòng yêu thích",
            serverState ? "success" : "info"
          );
        } else {
          // Revert on server error / forbidden role
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
      }
    } else {
      showToast(
        nextState ? "Đã thêm vào phòng yêu thích!" : "Đã xóa khỏi phòng yêu thích",
        nextState ? "success" : "info"
      );
    }
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

      if (!apiRes.isSuccess) {
        showToast(apiRes.error?.message || "Không thể đặt lịch xem phòng.", "error");
        setBookingLoading(false);
        return;
      }

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
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-8 w-full flex-1 space-y-6 sm:space-y-8 pb-24 lg:pb-8">
      {/* Preview Banner for Landlord / Admin */}
      {isPreview && (
        <div className="bg-amber-500/10 border border-amber-300 text-amber-900 px-4 py-3 rounded-2xl flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 text-xs font-semibold shadow-xs">
          <div className="flex items-center gap-2">
            <AlertTriangle className="w-4 h-4 text-amber-600 shrink-0" />
            <span>
              <strong>Chế độ xem trước tin đăng</strong> — Tin đăng này đang ở trạng thái <em>{rawDetail?.status || "Bản nháp"}</em> và chưa được công khai cho khách thuê trên trang tìm kiếm.
            </span>
          </div>
          <div className="flex items-center gap-2 shrink-0">
            <span className="px-2.5 py-1 rounded-full bg-amber-100 text-amber-800 text-[11px] font-bold">
              Trạng thái: {rawDetail?.status || "DRAFT"}
            </span>
            <Link
              href="/landlord/listings"
              className="px-2.5 py-1 rounded-lg bg-amber-600 text-white text-[11px] font-bold hover:bg-amber-700 transition-colors"
            >
              Về Quản lý tin
            </Link>
          </div>
        </div>
      )}

      {/* Top Action Bar */}
      <div className="flex items-center justify-between text-xs text-slate-500 gap-2">
        <Link
          href="/listings"
          className="flex items-center gap-1.5 font-semibold text-slate-700 hover:text-blue-600 truncate"
        >
          <ArrowLeft className="w-4 h-4 shrink-0" />
          <span className="hidden sm:inline">Quay lại danh sách phòng</span>
          <span className="sm:hidden">Quay lại</span>
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
                <span>{listing.address}</span>
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

            {/* Room & Property Specifications from Backend API */}
            {rawDetail && (
              <div className="space-y-3 pt-4 border-t border-slate-100">
                <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider">
                  Thông số kỹ thuật phòng & Tòa nhà
                </h3>
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs">
                  <div className="p-3 rounded-xl bg-slate-50 border border-slate-200/70">
                    <span className="text-[10px] font-bold text-slate-400 uppercase block">
                      Tòa nhà / Khu trọ
                    </span>
                    <span className="font-bold text-slate-800 block mt-0.5 truncate" title={rawDetail.property?.name || listing.title}>
                      {rawDetail.property?.name || listing.title}
                    </span>
                  </div>
                  <div className="p-3 rounded-xl bg-slate-50 border border-slate-200/70">
                    <span className="text-[10px] font-bold text-slate-400 uppercase block">
                      Số phòng & Tầng
                    </span>
                    <span className="font-bold text-slate-800 block mt-0.5">
                      {rawDetail.room
                        ? `P.${rawDetail.room.roomNumber} • Tầng ${rawDetail.room.floor}`
                        : `Phòng #${rawDetail.roomId || listing.id}`}
                    </span>
                  </div>
                  <div className="p-3 rounded-xl bg-slate-50 border border-slate-200/70">
                    <span className="text-[10px] font-bold text-slate-400 uppercase block">
                      Số người ở tối đa
                    </span>
                    <span className="font-bold text-slate-800 block mt-0.5">
                      {rawDetail.room
                        ? `Tối đa ${rawDetail.room.maxOccupants} người`
                        : "2 - 4 người"}
                    </span>
                  </div>
                  <div className="p-3 rounded-xl bg-slate-50 border border-slate-200/70">
                    <span className="text-[10px] font-bold text-slate-400 uppercase block">
                      Hợp đồng tối thiểu
                    </span>
                    <span className="font-bold text-blue-600 block mt-0.5">
                      {rawDetail.minLeaseMonths || 6} tháng
                    </span>
                  </div>
                </div>

                {rawDetail.houseRules && (
                  <div className="p-3.5 rounded-xl bg-amber-50/60 border border-amber-200/80 text-xs space-y-1">
                    <span className="font-bold text-amber-900 uppercase text-[11px] block">
                      Nội quy phòng & Tòa nhà (House Rules):
                    </span>
                    <p className="text-slate-700 leading-relaxed whitespace-pre-line">
                      {rawDetail.houseRules}
                    </p>
                  </div>
                )}
              </div>
            )}

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

            {/* Map Location Section */}
            <div className="space-y-3 pt-4 border-t border-slate-100">
              <div className="flex items-center justify-between flex-wrap gap-2">
                <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider flex items-center gap-2">
                  <MapPin className="w-4 h-4 text-blue-600" />
                  <span>Vị trí phòng trọ trên bản đồ & Chỉ đường</span>
                </h3>
                <span className="text-xs text-slate-500 font-medium">
                  {rawDetail?.property?.addressDetail || listing.address}
                </span>
              </div>
              <MapLocationViewer
                latitude={rawDetail?.latitude ?? (listing.latitude || null)}
                longitude={rawDetail?.longitude ?? (listing.longitude || null)}
                title={rawDetail?.property?.name || listing.title}
                address={
                  rawDetail?.property
                    ? `${rawDetail.property.addressDetail}, ${rawDetail.property.wardName || ""}, ${rawDetail.property.provinceName || ""}`
                    : listing.address
                }
                heightClassName="h-[280px] sm:h-[340px]"
              />
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

      {/* Mobile Fixed Bottom CTA Bar */}
      <div className="lg:hidden fixed bottom-0 left-0 right-0 z-40 bg-white/95 backdrop-blur-lg border-t border-slate-200/90 shadow-[0_-4px_20px_rgba(0,0,0,0.08)] px-4 py-2.5 flex items-center justify-between gap-3 safe-bottom">
        <div>
          <span className="text-[10px] font-bold text-slate-400 uppercase block">Giá thuê</span>
          <div className="flex items-baseline gap-1">
            <span className="text-base sm:text-lg font-black text-blue-600">
              {(listing.price / 1000000).toLocaleString("vi-VN", { maximumFractionDigits: 1 })} tr
            </span>
            <span className="text-[10px] text-slate-500 font-medium">/tháng</span>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Link
            href={`/messages?listingId=${parseListingNumericId(listing.id)}`}
            className="p-2.5 rounded-xl font-bold text-xs text-slate-700 bg-slate-100 hover:bg-slate-200 transition-all flex items-center justify-center shrink-0"
            title="Chat với chủ trọ"
          >
            <MessageSquare className="w-4 h-4 text-blue-600" />
          </Link>

          {!isLandlord ? (
            <button
              onClick={() => setIsBookingOpen(true)}
              disabled={listing.status !== "Còn trống"}
              className="py-2.5 px-4 rounded-xl font-bold text-xs text-white bg-blue-600 hover:bg-blue-700 shadow-md shadow-blue-500/25 transition-all flex items-center gap-1.5 disabled:opacity-50 whitespace-nowrap"
            >
              <Calendar className="w-3.5 h-3.5" />
              <span>Đặt lịch xem</span>
            </button>
          ) : (
            <span className="text-[11px] text-slate-500 font-medium px-2">Kênh chủ nhà</span>
          )}
        </div>
      </div>
    </div>
  );
}
