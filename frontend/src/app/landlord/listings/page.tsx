"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { listingsApi, propertiesApi, getStoredToken } from "@/lib/api-client";
import {
  PropertyListing,
  ListingSummaryDto,
  PropertyDto,
  RoomDto,
} from "@/types";
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
  RefreshCw,
  Send,
  AlertCircle,
  Building2,
  Upload,
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

const STATUS_BADGES: Record<
  string,
  { label: string; bg: string; text: string; border: string }
> = {
  Published: {
    label: "Đang công khai",
    bg: "bg-emerald-50",
    text: "text-emerald-700",
    border: "border-emerald-200",
  },
  PendingApproval: {
    label: "Chờ Admin duyệt",
    bg: "bg-amber-50",
    text: "text-amber-700",
    border: "border-amber-200",
  },
  Draft: {
    label: "Bản nháp",
    bg: "bg-slate-100",
    text: "text-slate-700",
    border: "border-slate-200",
  },
  Hidden: {
    label: "Đã ẩn tin",
    bg: "bg-rose-50",
    text: "text-rose-700",
    border: "border-rose-200",
  },
  Rejected: {
    label: "Bị từ chối",
    bg: "bg-red-50",
    text: "text-red-700",
    border: "border-red-200",
  },
  Banned: {
    label: "Bị khóa",
    bg: "bg-gray-100",
    text: "text-gray-800",
    border: "border-gray-300",
  },
};

function generateListingSlug(title: string, roomId?: number | null): string {
  const normalized = title
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[đĐ]/g, "d")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");

  const base = normalized || "phong-tro";
  const roomPart = roomId ? `r${roomId}-` : "";
  const randomSuffix = Math.random().toString(36).substring(2, 7);
  const candidate = `${base}-${roomPart}${Date.now().toString(36)}-${randomSuffix}`.replace(/--+/g, "-");

  return candidate.length > 300 ? candidate.slice(0, 300).replace(/-+$/, "") : candidate;
}

export default function LandlordListingsPage() {
  const { user } = useAuth();
  const { showToast } = useToast();

  // Backend state
  const [backendListings, setBackendListings] = useState<ListingSummaryDto[]>([]);
  const [isBackendConnected, setIsBackendConnected] = useState(false);
  const [loading, setLoading] = useState(false);


  // Properties & Rooms for dropdown in Create Listing
  const [properties, setProperties] = useState<PropertyDto[]>([]);
  const [selectedPropId, setSelectedPropId] = useState<number | null>(null);
  const [rooms, setRooms] = useState<RoomDto[]>([]);
  const [selectedRoomId, setSelectedRoomId] = useState<number | null>(null);

  // Modals
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [isAddImageOpen, setIsAddImageOpen] = useState(false);
  const [selectedListingForImage, setSelectedListingForImage] = useState<number | null>(null);
  const [newImageUrl, setNewImageUrl] = useState("");
  const [addingImage, setAddingImage] = useState(false);

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
  const [submitting, setSubmitting] = useState(false);

  // Load listings from backend
  const loadData = useCallback(async () => {
    setLoading(true);
    const token = getStoredToken();
    if (!token) {
      setBackendListings([]);
      setIsBackendConnected(false);
      setLoading(false);
      return;
    }

    try {
      const res = await listingsApi.getMyListings();
      if (res.isSuccess && res.data) {
        setBackendListings(res.data);
        setIsBackendConnected(true);
      } else {
        setBackendListings([]);
      }
    } catch {
      setBackendListings([]);
    } finally {
      setLoading(false);
    }
  }, []);

  // Load properties for create form
  useEffect(() => {
    async function fetchProps() {
      const token = getStoredToken();
      if (!token) return;
      try {
        const res = await propertiesApi.getMyProperties();
        if (res.isSuccess && res.data && res.data.length > 0) {
          setProperties(res.data);
          setSelectedPropId(res.data[0].id);
        }
      } catch (err) {
        console.warn("Could not fetch properties", err);
      }
    }
    fetchProps();
  }, []);

  // When selectedPropId changes, load rooms
  useEffect(() => {
    async function fetchRooms() {
      if (!selectedPropId) return;
      try {
        const res = await propertiesApi.getRooms(selectedPropId);
        if (res.isSuccess && res.data && res.data.length > 0) {
          setRooms(res.data);
          setSelectedRoomId(res.data[0].id);
          setPrice(res.data[0].basePrice);
          setDeposit(res.data[0].basePrice);
          setArea(res.data[0].areaSqm);
        } else {
          setRooms([]);
          setSelectedRoomId(null);
        }
      } catch (err) {
        console.warn("Could not fetch rooms", err);
      }
    }
    fetchRooms();
  }, [selectedPropId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const toggleAmenity = (name: string) => {
    setSelectedAmenities((prev) =>
      prev.includes(name) ? prev.filter((x) => x !== name) : [...prev, name]
    );
  };

  // Submit listing to Admin for Approval
  const handleSubmitApproval = async (listingId: number) => {
    try {
      const res = await listingsApi.submitListing(listingId);
      if (res.isSuccess) {
        showToast("Đã gửi tin đăng đến hàng đợi kiểm duyệt của Admin!", "success");
        loadData();
      } else {
        showToast(res.error?.message || "Không thể gửi duyệt", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    }
  };

  // Hide or unhide listing
  const handleToggleHideBackend = async (item: ListingSummaryDto) => {
    try {
      if (item.status === "Published") {
        const res = await listingsApi.hideListing(item.id);
        if (res.isSuccess) {
          showToast("Đã ẩn tin đăng khỏi danh sách công khai.", "info");
          loadData();
        }
      } else {
        const res = await listingsApi.submitListing(item.id);
        if (res.isSuccess) {
          showToast("Đã gửi yêu cầu kích hoạt lại tin đăng!", "success");
          loadData();
        }
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    }
  };

  // Add image to listing
  const handleAddImage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedListingForImage || !newImageUrl) return;
    setAddingImage(true);
    try {
      const res = await listingsApi.addImage(selectedListingForImage, {
        imageUrl: newImageUrl,
        isPrimary: true,
        sortOrder: 1,
      });
      if (res.isSuccess) {
        showToast("Thêm ảnh đại diện thành công!", "success");
        setIsAddImageOpen(false);
        setNewImageUrl("");
        loadData();
      } else {
        showToast(res.error?.message || "Thêm ảnh thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    } finally {
      setAddingImage(false);
    }
  };



  // Create Listing
  const handleCreateListing = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);

    if (selectedRoomId) {
      // Create via backend API
      try {
        const finalTitle = title.trim() || `Phòng cho thuê - ${selectedPropId}`;
        const finalSlug = generateListingSlug(finalTitle, selectedRoomId);

        const res = await listingsApi.createListing({
          roomId: selectedRoomId,
          title: finalTitle,
          slug: finalSlug,
          description: description.trim() || "Phòng đầy đủ tiện nghi, an ninh tốt, giờ giấc tự do.",
          price: Number(price),
          depositAmount: Number(deposit),
          minLeaseMonths: 6,
          allowPets: selectedAmenities.includes("Nuôi thú cưng"),
          hasCurfew: false,
          curfewTime: null,
        });

        if (res.isSuccess && res.data) {
          if (imageUrl) {
            await listingsApi.addImage(res.data.id, {
              imageUrl,
              isPrimary: true,
              sortOrder: 1,
            });
          }
          await listingsApi.submitListing(res.data.id);
          showToast("Đã tạo tin và gửi kiểm duyệt lên Admin thành công!", "success");
          setIsCreateOpen(false);
          setTitle("");
          setDescription("");
          await loadData();
          setSubmitting(false);
          return;
        } else {
          const detailMsg = res.error?.details
            ?.map((d) => (d.field ? `${d.field}: ${d.message}` : d.message))
            .join(", ");
          showToast(
            detailMsg
              ? `${res.error?.message || "Không thể tạo tin đăng."} (${detailMsg})`
              : res.error?.message || "Không thể tạo tin đăng.",
            "error"
          );
          setSubmitting(false);
          return;
        }
      } catch (err) {
        showToast("Không thể tạo tin đăng. Vui lòng kiểm tra lại kết nối.", "error");
        setSubmitting(false);
        return;
      }
    } else {
      showToast("Vui lòng chọn phòng cần tạo tin đăng!", "error");
      setSubmitting(false);
    }
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
            <span>Nghiệp vụ tin cho thuê (Module Listings)</span>
            {isBackendConnected && (
              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-100 text-emerald-800">
                <CheckCircle2 className="w-3 h-3" /> Live Backend API
              </span>
            )}
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Quản Lý & Đăng Tin Cho Thuê
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Tạo tin từ phòng có sẵn, gửi duyệt lên Quản trị viên và quản lý hiển thị công khai.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => loadData()}
            className="p-2.5 rounded-xl border border-slate-200 text-slate-600 hover:bg-slate-50"
            title="Làm mới"
          >
            <RefreshCw className={cn("w-4 h-4", loading && "animate-spin")} />
          </button>
          <button
            onClick={() => setIsCreateOpen(true)}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20 transition-all"
          >
            <Plus className="w-4 h-4" />
            <span>Đăng tin cho thuê mới</span>
          </button>
        </div>
      </div>

      {/* Backend Listings Grid */}
      {isBackendConnected && backendListings.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {backendListings.map((item) => {
            const badge = STATUS_BADGES[item.status] || {
              label: item.status,
              bg: "bg-slate-100",
              text: "text-slate-700",
              border: "border-slate-200",
            };

            return (
              <div
                key={item.id}
                className="bg-white rounded-2xl border border-slate-200/80 shadow-xs hover:shadow-md transition-shadow overflow-hidden flex flex-col justify-between"
              >
                <div>
                  <div className="relative aspect-[16/10] bg-slate-100">
                    <img
                      src={
                        item.coverImageUrl ||
                        "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=800&q=80"
                      }
                      alt={item.title}
                      className="w-full h-full object-cover"
                    />
                    <div className="absolute top-3 left-3 flex gap-2">
                      <span
                        className={cn(
                          "px-2.5 py-1 rounded-full text-[11px] font-bold border",
                          badge.bg,
                          badge.text,
                          badge.border
                        )}
                      >
                        {badge.label}
                      </span>
                    </div>

                    <button
                      onClick={() => {
                        setSelectedListingForImage(item.id);
                        setIsAddImageOpen(true);
                      }}
                      className="absolute bottom-3 right-3 px-2 py-1 bg-black/60 hover:bg-black/80 text-white text-[11px] font-bold rounded-lg flex items-center gap-1 backdrop-blur-xs transition-colors"
                    >
                      <Upload className="w-3 h-3" />
                      <span>Đổi ảnh</span>
                    </button>
                  </div>

                  <div className="p-4 space-y-2">
                    <h3 className="font-bold text-slate-900 text-sm line-clamp-2">{item.title}</h3>
                    <div className="flex items-center justify-between pt-2 text-xs font-semibold">
                      <span className="text-blue-600 font-black text-sm">
                        {item.price.toLocaleString()} đ/tháng
                      </span>
                      <span className="text-slate-400 text-[11px]">ID: #{item.id}</span>
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
                    {item.status === "Draft" && (
                      <button
                        onClick={() => handleSubmitApproval(item.id)}
                        className="px-3 py-1.5 rounded-lg text-xs font-bold bg-amber-500 hover:bg-amber-600 text-white transition-colors flex items-center gap-1"
                      >
                        <Send className="w-3 h-3" />
                        <span>Gửi duyệt</span>
                      </button>
                    )}

                    {item.status === "Published" && (
                      <button
                        onClick={() => handleToggleHideBackend(item)}
                        className="px-3 py-1.5 rounded-lg text-xs font-bold border border-slate-200 bg-slate-100 text-slate-700 hover:bg-slate-200 transition-colors flex items-center gap-1"
                      >
                        <EyeOff className="w-3.5 h-3.5" />
                        <span>Ẩn tin</span>
                      </button>
                    )}

                    {item.status === "Hidden" && (
                      <button
                        onClick={() => handleToggleHideBackend(item)}
                        className="px-3 py-1.5 rounded-lg text-xs font-bold border border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100 transition-colors flex items-center gap-1"
                      >
                        <Eye className="w-3.5 h-3.5" />
                        <span>Hiện lại</span>
                      </button>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <div className="bg-white rounded-2xl border border-dashed border-slate-300 p-12 text-center max-w-2xl mx-auto space-y-4">
          <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
            <FileText className="w-8 h-8" />
          </div>
          <div>
            <h3 className="text-lg font-bold text-slate-900">Bạn chưa đăng tin cho thuê nào</h3>
            <p className="text-xs text-slate-500 mt-1 max-w-md mx-auto leading-relaxed">
              Tạo tin đăng cho thuê phòng để tiếp cận hàng ngàn người thuê tiềm năng trên HomeMatch.
            </p>
          </div>
          <button
            onClick={() => setIsCreateOpen(true)}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs shadow-md shadow-blue-500/20"
          >
            <Plus className="w-4 h-4" />
            <span>Đăng Tin Cho Thuê Mới</span>
          </button>
        </div>
      )}

      {/* Modal: Add Image */}
      {isAddImageOpen && (
        <Modal
          isOpen={true}
          onClose={() => setIsAddImageOpen(false)}
          title="Thêm Ảnh Cho Tin Đăng"
          maxWidth="md"
        >
          <form onSubmit={handleAddImage} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Đường dẫn hình ảnh (URL)
              </label>
              <input
                type="url"
                required
                placeholder="https://images.unsplash.com/..."
                value={newImageUrl}
                onChange={(e) => setNewImageUrl(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            {newImageUrl && (
              <div className="aspect-[16/10] rounded-xl overflow-hidden bg-slate-100">
                <img src={newImageUrl} alt="Xem trước" className="w-full h-full object-cover" />
              </div>
            )}
            <div className="flex gap-3 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setIsAddImageOpen(false)}
                className="flex-1 py-2 rounded-xl border border-slate-200 text-xs font-bold text-slate-700"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={addingImage}
                className="flex-1 py-2 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 disabled:opacity-50"
              >
                {addingImage ? "Đang lưu..." : "Xác nhận thêm"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Modal: Create Listing */}
      {isCreateOpen && (
        <Modal
          isOpen={true}
          onClose={() => setIsCreateOpen(false)}
          title="Đăng Tin Cho Thuê Phòng Mới"
          maxWidth="2xl"
        >
          <form onSubmit={handleCreateListing} className="space-y-4">
            {/* If landlord has backend properties and rooms */}
            {properties.length > 0 && (
              <div className="p-3.5 rounded-xl bg-blue-50/60 border border-blue-200 space-y-3">
                <div className="flex items-center gap-2 text-xs font-bold text-blue-900">
                  <Building2 className="w-4 h-4 text-blue-600" />
                  <span>Chọn Phòng Trong Tòa Nhà Của Bạn:</span>
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-[10px] font-bold text-slate-500 uppercase mb-1">
                      Tòa nhà
                    </label>
                    <select
                      value={selectedPropId || ""}
                      onChange={(e) => setSelectedPropId(Number(e.target.value))}
                      className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white font-bold"
                    >
                      {properties.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.name}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block text-[10px] font-bold text-slate-500 uppercase mb-1">
                      Phòng cụ thể
                    </label>
                    <select
                      value={selectedRoomId || ""}
                      onChange={(e) => setSelectedRoomId(Number(e.target.value))}
                      className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white font-bold"
                    >
                      {rooms.length > 0 ? (
                        rooms.map((r) => (
                          <option key={r.id} value={r.id}>
                            Phòng {r.roomNumber} (Tầng {r.floor} - {r.areaSqm} m²)
                          </option>
                        ))
                      ) : (
                        <option value="">Chưa có phòng trong tòa nhà này</option>
                      )}
                    </select>
                  </div>
                </div>
              </div>
            )}

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Tiêu đề tin đăng *
              </label>
              <input
                type="text"
                required
                placeholder="VD: Phòng Studio Full Đồ Ban Công Thoáng Mát - Cầu Giấy"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-semibold"
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Giá thuê tháng (VNĐ) *
                </label>
                <input
                  type="number"
                  required
                  step={100000}
                  value={price}
                  onChange={(e) => setPrice(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-bold text-blue-600"
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Tiền đặt cọc (VNĐ)
                </label>
                <input
                  type="number"
                  required
                  step={100000}
                  value={deposit}
                  onChange={(e) => setDeposit(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-bold"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Mô tả chi tiết phòng
              </label>
              <textarea
                rows={3}
                placeholder="Mô tả tiện ích, vị trí gần các trường đại học, quy định giờ giấc..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Ảnh đại diện (URL)
              </label>
              <input
                type="url"
                value={imageUrl}
                onChange={(e) => setImageUrl(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-2">
                Tiện ích phòng
              </label>
              <div className="flex flex-wrap gap-2">
                {DEFAULT_AMENITIES_OPTIONS.map((amenity) => (
                  <button
                    type="button"
                    key={amenity}
                    onClick={() => toggleAmenity(amenity)}
                    className={cn(
                      "px-3 py-1.5 rounded-lg text-xs font-bold border transition-colors",
                      selectedAmenities.includes(amenity)
                        ? "bg-blue-600 text-white border-blue-600 shadow-xs"
                        : "bg-slate-50 text-slate-700 border-slate-200 hover:bg-slate-100"
                    )}
                  >
                    {amenity}
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
                disabled={submitting}
                className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 shadow-md shadow-blue-500/20 disabled:opacity-50"
              >
                {submitting ? "Đang gửi duyệt..." : "Xác nhận & Gửi kiểm duyệt"}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
