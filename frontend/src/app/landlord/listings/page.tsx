"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { listingService } from "@/lib/services";
import { PropertyListing } from "@/types";
import { useAuth } from "@/features/auth/AuthContext";
import { useToast } from "@/components/Toast";
import { Modal } from "@/components/Modal";
import {
  FileText,
  Plus,
  Eye,
  EyeOff,
  Trash2,
  CheckCircle2,
  Image as ImageIcon,
  DollarSign,
  Maximize2,
  MapPin,
  Sparkles,
} from "lucide-react";
import { cn } from "@/lib/utils";

const DEFAULT_AMENITIES_OPTIONS = [
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
  "Gác lửng",
  "Nuôi thú cưng",
];

export default function LandlordListingsPage() {
  const { user } = useAuth();
  const { showToast } = useToast();
  const [listings, setListings] = useState<PropertyListing[]>([]);
  const [isCreateOpen, setIsCreateOpen] = useState(false);

  // Form State
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [address, setAddress] = useState("");
  const [ward, setWard] = useState("");
  const [district, setDistrict] = useState("Cầu Giấy");
  const [city, setCity] = useState("Hà Nội");
  const [price, setPrice] = useState(4500000);
  const [deposit, setDeposit] = useState(4500000);
  const [area, setArea] = useState(25);
  const [roomType, setRoomType] = useState<PropertyListing["roomType"]>("Chung cư mini");
  const [electricityPrice, setElectricityPrice] = useState(3800);
  const [waterPrice, setWaterPrice] = useState(28000);
  const [serviceFee, setServiceFee] = useState(150000);
  const [selectedAmenities, setSelectedAmenities] = useState<string[]>([
    "Điều hòa",
    "Nóng lạnh",
    "Khóa vân tay",
    "Giờ tự do",
  ]);
  const [imageUrl, setImageUrl] = useState(
    "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=800&q=80"
  );

  const loadData = () => {
    setListings(listingService.getAll());
  };

  useEffect(() => {
    loadData();
  }, []);

  const toggleAmenity = (name: string) => {
    setSelectedAmenities((prev) =>
      prev.includes(name) ? prev.filter((x) => x !== name) : [...prev, name]
    );
  };

  const handleToggleHide = (listing: PropertyListing) => {
    const newStatus = listing.status === "Tạm ngừng" ? "Còn trống" : "Tạm ngừng";
    listingService.update(listing.id, { status: newStatus });
    loadData();
    showToast(
      newStatus === "Tạm ngừng" ? "Đã ẩn tin đăng khỏi trang tìm kiếm." : "Đã kích hoạt hiển thị tin đăng!",
      "info"
    );
  };

  const handleDelete = (id: string) => {
    if (confirm("Bạn có chắc chắn muốn xóa vĩnh viễn tin đăng này?")) {
      listingService.delete(id);
      loadData();
      showToast("Đã xóa tin đăng thành công.", "success");
    }
  };

  const handleCreateListing = (e: React.FormEvent) => {
    e.preventDefault();

    listingService.create({
      title,
      description,
      address,
      ward: ward || "Dịch Vọng",
      district,
      city,
      price: Number(price),
      deposit: Number(deposit),
      area: Number(area),
      roomType,
      status: "Còn trống",
      images: [imageUrl],
      amenities: selectedAmenities,
      landlordId: user?.accountId || "landlord-1",
      landlordName: user?.displayName || "Nguyễn Văn Hoàng (Chủ Trọ)",
      landlordPhone: user?.phoneNumber || "0902000002",
      electricityPrice: Number(electricityPrice),
      waterPrice: Number(waterPrice),
      serviceFee: Number(serviceFee),
      latitude: 21.034,
      longitude: 105.793,
    });

    setIsCreateOpen(false);
    loadData();
    showToast("Tạo tin đăng cho thuê mới thành công!", "success");
  };

  if (!user || user.role === "Tenant") {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <FileText className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Quản Lý & Đăng Tin Cho Thuê</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập với tài khoản <strong>Chủ nhà (Landlord)</strong> để quản lý và đăng tin phòng cho thuê.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/landlord/listings"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng nhập tài khoản Chủ nhà</span>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <FileText className="w-4 h-4" />
            <span>Nghiệp vụ tin cho thuê (Module 3.6)</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Quản Lý & Đăng Tin Cho Thuê
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Tạo, xem, chỉnh sửa và ẩn/hiện tin cho thuê của bạn trên nền tảng HomeMatch.
          </p>
        </div>

        <button
          onClick={() => setIsCreateOpen(true)}
          className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20 transition-all"
        >
          <Plus className="w-4 h-4" />
          <span>Đăng tin cho thuê mới</span>
        </button>
      </div>

      {/* Listings Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {listings.map((item) => (
          <div
            key={item.id}
            className="bg-white rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-shadow overflow-hidden flex flex-col justify-between"
          >
            <div>
              <div className="relative aspect-[16/10] bg-slate-100">
                <img src={item.images[0]} alt={item.title} className="w-full h-full object-cover" />
                <div className="absolute top-3 left-3 flex gap-2">
                  <span className="px-2.5 py-1 rounded-full text-[11px] font-bold bg-slate-900/80 text-white">
                    {item.roomType}
                  </span>
                  <span
                    className={cn(
                      "px-2.5 py-1 rounded-full text-[11px] font-bold border",
                      item.status === "Tạm ngừng"
                        ? "bg-rose-50 text-rose-700 border-rose-200"
                        : "bg-emerald-50 text-emerald-700 border-emerald-200"
                    )}
                  >
                    {item.status === "Tạm ngừng" ? "Đã ẩn" : "Đang công khai"}
                  </span>
                </div>
              </div>

              <div className="p-4 space-y-2">
                <h3 className="font-bold text-slate-900 text-sm line-clamp-2">{item.title}</h3>
                <p className="text-xs text-slate-500 flex items-center gap-1">
                  <MapPin className="w-3.5 h-3.5" />
                  <span>{item.address}</span>
                </p>

                <div className="flex items-center justify-between pt-2 text-xs font-semibold">
                  <span className="text-blue-600 font-black text-sm">
                    {item.price.toLocaleString()} đ/tháng
                  </span>
                  <span className="text-slate-500">{item.area} m²</span>
                </div>
              </div>
            </div>

            <div className="p-4 border-t border-slate-100 flex items-center justify-between gap-2">
              <Link
                href={`/listings/${item.id}`}
                className="text-xs font-bold text-blue-600 hover:text-blue-700 flex items-center gap-1"
              >
                <Eye className="w-3.5 h-3.5" />
                <span>Xem tin</span>
              </Link>

              <div className="flex items-center gap-1.5">
                <button
                  onClick={() => handleToggleHide(item)}
                  className={cn(
                    "px-3 py-1.5 rounded-lg text-xs font-bold border transition-colors flex items-center gap-1",
                    item.status === "Tạm ngừng"
                      ? "bg-emerald-50 text-emerald-700 border-emerald-200 hover:bg-emerald-100"
                      : "bg-slate-100 text-slate-700 border-slate-200 hover:bg-slate-200"
                  )}
                >
                  {item.status === "Tạm ngừng" ? (
                    <>
                      <Eye className="w-3.5 h-3.5" />
                      <span>Hiện tin</span>
                    </>
                  ) : (
                    <>
                      <EyeOff className="w-3.5 h-3.5" />
                      <span>Ẩn tin</span>
                    </>
                  )}
                </button>

                <button
                  onClick={() => handleDelete(item.id)}
                  className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors"
                  title="Xóa tin đăng"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Create Listing Modal */}
      <Modal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        title="Tạo Tin Đăng Cho Thuê Phòng Mới"
        maxWidth="xl"
      >
        <form onSubmit={handleCreateListing} className="space-y-4">
          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
              Tiêu đề tin đăng
            </label>
            <input
              type="text"
              required
              placeholder="Ví dụ: Phòng Studio full đồ cao cấp gần ĐH Ngoại Thương..."
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Loại phòng
              </label>
              <select
                value={roomType}
                onChange={(e) => setRoomType(e.target.value as any)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-semibold"
              >
                <option value="Phòng trọ">Phòng trọ</option>
                <option value="Chung cư mini">Chung cư mini</option>
                <option value="Căn hộ dịch vụ">Căn hộ dịch vụ</option>
                <option value="Nhà nguyên căn">Nhà nguyên căn</option>
                <option value="Ở ghép">Ở ghép</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Tỉnh / Thành phố
              </label>
              <select
                value={city}
                onChange={(e) => setCity(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-semibold"
              >
                <option value="Hà Nội">Hà Nội</option>
                <option value="Hồ Chí Minh">TP. Hồ Chí Minh</option>
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Quận / Huyện
              </label>
              <input
                type="text"
                required
                value={district}
                onChange={(e) => setDistrict(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Địa chỉ chi tiết (Số nhà, ngõ, tên đường)
              </label>
              <input
                type="text"
                required
                placeholder="Số 20 Ngõ 165 Cầu Giấy"
                value={address}
                onChange={(e) => setAddress(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Giá thuê (đ/tháng)
              </label>
              <input
                type="number"
                required
                value={price}
                onChange={(e) => setPrice(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Tiền đặt cọc (đ)
              </label>
              <input
                type="number"
                required
                value={deposit}
                onChange={(e) => setDeposit(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Diện tích (m²)
              </label>
              <input
                type="number"
                required
                value={area}
                onChange={(e) => setArea(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Giá điện (đ/kWh)
              </label>
              <input
                type="number"
                required
                value={electricityPrice}
                onChange={(e) => setElectricityPrice(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Giá nước (đ/m³)
              </label>
              <input
                type="number"
                required
                value={waterPrice}
                onChange={(e) => setWaterPrice(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Phí dịch vụ (đ/tháng)
              </label>
              <input
                type="number"
                required
                value={serviceFee}
                onChange={(e) => setServiceFee(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
              Link ảnh phòng mẫu (Cloudflare R2 / S3 compliant)
            </label>
            <input
              type="text"
              required
              value={imageUrl}
              onChange={(e) => setImageUrl(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-mono"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
              Mô tả chi tiết
            </label>
            <textarea
              rows={3}
              required
              placeholder="Mô tả nội thất, không gian sống, an ninh..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase mb-2">
              Tiện ích đi kèm
            </label>
            <div className="flex flex-wrap gap-1.5">
              {DEFAULT_AMENITIES_OPTIONS.map((am) => (
                <button
                  type="button"
                  key={am}
                  onClick={() => toggleAmenity(am)}
                  className={cn(
                    "px-2.5 py-1 text-xs rounded-lg border font-medium transition-colors",
                    selectedAmenities.includes(am)
                      ? "bg-blue-600 text-white border-blue-600"
                      : "bg-slate-50 text-slate-600 border-slate-200 hover:bg-slate-100"
                  )}
                >
                  {am}
                </button>
              ))}
            </div>
          </div>

          <div className="flex gap-3 pt-3 border-t border-slate-100">
            <button
              type="button"
              onClick={() => setIsCreateOpen(false)}
              className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
            >
              Hủy
            </button>
            <button
              type="submit"
              className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 shadow-md shadow-blue-500/20"
            >
              Đăng tin công khai
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
