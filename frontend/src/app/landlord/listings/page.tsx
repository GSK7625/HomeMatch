"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { listingsApi, propertiesApi, catalogsApi, getStoredToken } from "@/lib/api-client";
import {
  PropertyListing,
  ListingSummaryDto,
  ListingDetailDto,
  ListingImageDto,
  PropertyDto,
  RoomDto,
  AmenityDto,
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
  Edit2,
  Star,
  Check,
} from "lucide-react";
import { cn, formatAmenityName } from "@/lib/utils";

const DEFAULT_AMENITIES_OPTIONS = [
  "Điều hòa",
  "Bếp",
  "Tủ quần áo",
  "Ban công",
  "An ninh 24/7",
  "Giường",
  "Chỗ để xe",
  "Tủ lạnh",
  "Thang máy",
  "Máy giặt",
  "Wifi",
  "Camera",
  "Hồ bơi",
  "Cửa sổ",
  "Phòng gym",
  "Nóng lạnh",
  "Nội thất đầy đủ",
  "Gác lửng",
  "Khép kín",
];

const STATUS_BADGES: Record<
  string,
  { label: string; bg: string; text: string; border: string }
> = {
  PUBLISHED: {
    label: "Đang công khai",
    bg: "bg-emerald-50",
    text: "text-emerald-700",
    border: "border-emerald-200",
  },
  Published: {
    label: "Đang công khai",
    bg: "bg-emerald-50",
    text: "text-emerald-700",
    border: "border-emerald-200",
  },
  PENDING_APPROVAL: {
    label: "Chờ Admin duyệt",
    bg: "bg-amber-50",
    text: "text-amber-700",
    border: "border-amber-200",
  },
  PendingApproval: {
    label: "Chờ Admin duyệt",
    bg: "bg-amber-50",
    text: "text-amber-700",
    border: "border-amber-200",
  },
  DRAFT: {
    label: "Bản nháp",
    bg: "bg-slate-100",
    text: "text-slate-700",
    border: "border-slate-200",
  },
  Draft: {
    label: "Bản nháp",
    bg: "bg-slate-100",
    text: "text-slate-700",
    border: "border-slate-200",
  },
  HIDDEN: {
    label: "Đã ẩn tin",
    bg: "bg-rose-50",
    text: "text-rose-700",
    border: "border-rose-200",
  },
  Hidden: {
    label: "Đã ẩn tin",
    bg: "bg-rose-50",
    text: "text-rose-700",
    border: "border-rose-200",
  },
  REJECTED: {
    label: "Bị từ chối",
    bg: "bg-red-50",
    text: "text-red-700",
    border: "border-red-200",
  },
  Rejected: {
    label: "Bị từ chối",
    bg: "bg-red-50",
    text: "text-red-700",
    border: "border-red-200",
  },
  BANNED: {
    label: "Bị khóa",
    bg: "bg-gray-100",
    text: "text-gray-800",
    border: "border-gray-300",
  },
  Banned: {
    label: "Bị khóa",
    bg: "bg-gray-100",
    text: "text-gray-800",
    border: "border-gray-300",
  },
};

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
  const [catalogAmenities, setCatalogAmenities] = useState<AmenityDto[]>([]);

  // Modals
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [isAddImageOpen, setIsAddImageOpen] = useState(false);
  const [selectedListingForImage, setSelectedListingForImage] = useState<number | null>(null);
  const [listingImages, setListingImages] = useState<ListingImageDto[]>([]);
  const [loadingImages, setLoadingImages] = useState(false);
  const [newImageUrl, setNewImageUrl] = useState("");
  const [newMediaType, setNewMediaType] = useState("IMAGE");
  const [newIsPrimary, setNewIsPrimary] = useState(true);
  const [newSortOrder, setNewSortOrder] = useState(1);
  const [addingImage, setAddingImage] = useState(false);

  // Edit / Detail Listing Modal
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [editingListing, setEditingListing] = useState<ListingDetailDto | null>(null);
  const [editTitle, setEditTitle] = useState("");
  const [editDescription, setEditDescription] = useState("");
  const [editPrice, setEditPrice] = useState(4500000);
  const [editDeposit, setEditDeposit] = useState(4500000);
  const [editMinLeaseMonths, setEditMinLeaseMonths] = useState(6);
  const [editHouseRules, setEditHouseRules] = useState("");
  const [updatingListing, setUpdatingListing] = useState(false);

  // Form State
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [address, setAddress] = useState("");
  const [ward, setWard] = useState("");
  const [district, setDistrict] = useState("Cầu Giấy");
  const [city, setCity] = useState("Hà Nội");
  const [price, setPrice] = useState(4500000);
  const [deposit, setDeposit] = useState(4500000);
  const [minLeaseMonths, setMinLeaseMonths] = useState(6);
  const [houseRules, setHouseRules] = useState(
    "Giờ giấc tự do, không gây ồn sau 23h, giữ gìn vệ sinh khu vực chung."
  );
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
          if (res.data[0].basePrice) {
            setPrice(res.data[0].basePrice);
            setDeposit(res.data[0].basePrice);
          }
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

  // Load official amenities from backend catalog
  useEffect(() => {
    async function loadCatalogAmenities() {
      try {
        const res = await catalogsApi.getAmenities();
        if (res.isSuccess && res.data && res.data.length > 0) {
          setCatalogAmenities(res.data);
        }
      } catch (err) {
        console.warn("Could not load amenities catalog", err);
      }
    }
    loadCatalogAmenities();
  }, []);

  // When selected room or catalog amenities change, auto-sync selectedAmenities with the room's configured amenities
  useEffect(() => {
    if (!selectedRoomId || rooms.length === 0) return;
    const room = rooms.find((r) => r.id === selectedRoomId);
    if (room && room.amenityIds && room.amenityIds.length > 0 && catalogAmenities.length > 0) {
      const roomNames = catalogAmenities
        .filter((a) => room.amenityIds!.includes(a.id))
        .map((a) => formatAmenityName(a.name));
      if (roomNames.length > 0) {
        setSelectedAmenities(roomNames);
      }
    }
  }, [selectedRoomId, rooms, catalogAmenities]);

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
        showToast("Đã gửi tin đăng đến hàng đợi kiểm duyệt của Admin thành công!", "success");
        loadData();
      } else {
        const errorMsg = res.error?.message || "Không thể gửi duyệt tin đăng.";
        showToast(`Chưa thể gửi duyệt: ${errorMsg}`, "error");
      }
    } catch {
      showToast("Lỗi kết nối khi gửi duyệt tin đăng", "error");
    }
  };

  // Hide or unhide listing
  const handleToggleHideBackend = async (item: ListingSummaryDto) => {
    try {
      const isPublished = (item.status || "").toUpperCase() === "PUBLISHED";
      if (isPublished) {
        const res = await listingsApi.hideListing(item.id);
        if (res.isSuccess) {
          showToast("Đã ẩn tin đăng khỏi danh sách công khai.", "info");
          loadData();
        } else {
          showToast(res.error?.message || "Không thể ẩn tin", "error");
        }
      } else {
        const res = await listingsApi.submitListing(item.id);
        if (res.isSuccess) {
          showToast("Đã gửi yêu cầu kích hoạt lại tin đăng!", "success");
          loadData();
        } else {
          showToast(res.error?.message || "Không thể gửi yêu cầu kích hoạt", "error");
        }
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    }
  };

  // Open Image Management Modal (loads existing images via GET /api/v1/listings/mine/{id})
  const handleOpenImageModal = async (listingId: number) => {
    setSelectedListingForImage(listingId);
    setIsAddImageOpen(true);
    setLoadingImages(true);
    try {
      const res = await listingsApi.getMyListingById(listingId);
      if (res.isSuccess && res.data) {
        const imgs = res.data.images || [];
        setListingImages(imgs);
        setNewSortOrder(imgs.length + 1);
        setNewIsPrimary(imgs.length === 0);
      } else {
        setListingImages([]);
      }
    } catch {
      setListingImages([]);
    } finally {
      setLoadingImages(false);
    }
  };

  // Add image to listing (POST /api/v1/listings/{id}/images)
  const handleAddImage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedListingForImage || !newImageUrl.trim()) return;
    setAddingImage(true);
    try {
      const res = await listingsApi.addImage(selectedListingForImage, {
        imageUrl: newImageUrl.trim(),
        isPrimary: newIsPrimary,
        sortOrder: Number(newSortOrder) || 1,
        mediaType: newMediaType,
      });
      if (res.isSuccess) {
        showToast("Đã thêm media vào tin đăng thành công!", "success");
        setNewImageUrl("");
        await handleOpenImageModal(selectedListingForImage);
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

  // Set Primary Image (PATCH /api/v1/listings/{id}/images/{imageId})
  const handleSetPrimaryImage = async (img: ListingImageDto) => {
    if (!selectedListingForImage) return;
    try {
      const res = await listingsApi.updateImage(selectedListingForImage, img.id, {
        isPrimary: true,
        sortOrder: img.sortOrder,
        mediaType: img.mediaType || "IMAGE",
      });
      if (res.isSuccess) {
        showToast("Đã đặt làm ảnh bìa chính!", "success");
        await handleOpenImageModal(selectedListingForImage);
        loadData();
      } else {
        showToast(res.error?.message || "Cập nhật ảnh thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    }
  };

  // Delete Image (DELETE /api/v1/listings/{id}/images/{imageId})
  const handleDeleteImage = async (imageId: number) => {
    if (!selectedListingForImage) return;
    try {
      const res = await listingsApi.deleteImage(selectedListingForImage, imageId);
      if (res.isSuccess) {
        showToast("Đã xóa hình ảnh khỏi tin đăng!", "info");
        await handleOpenImageModal(selectedListingForImage);
        loadData();
      } else {
        showToast(res.error?.message || "Xóa ảnh thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    }
  };

  // Open Edit / Detail Listing Modal (GET /api/v1/listings/mine/{id})
  const handleOpenEditListing = async (listingId: number) => {
    try {
      const res = await listingsApi.getMyListingById(listingId);
      if (res.isSuccess && res.data) {
        const d = res.data;
        setEditingListing(d);
        setEditTitle(d.title);
        setEditDescription(d.description);
        setEditPrice(d.price);
        setEditDeposit(d.depositAmount);
        setEditMinLeaseMonths(d.minLeaseMonths || 6);
        setEditHouseRules(d.houseRules || "");
        setIsEditOpen(true);
      } else {
        showToast(res.error?.message || "Không thể tải chi tiết tin đăng", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    }
  };

  // Update Listing (PATCH /api/v1/listings/{id})
  const handleUpdateListing = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingListing) return;
    setUpdatingListing(true);
    try {
      const res = await listingsApi.updateListing(editingListing.id, {
        title: editTitle.trim(),
        description: editDescription.trim(),
        price: Number(editPrice),
        depositAmount: Number(editDeposit),
        minLeaseMonths: Number(editMinLeaseMonths),
        houseRules: editHouseRules.trim() || null,
      });
      if (res.isSuccess) {
        showToast("Đã cập nhật thông tin tin đăng thành công!", "success");
        setIsEditOpen(false);
        setEditingListing(null);
        loadData();
      } else {
        showToast(res.error?.message || "Cập nhật tin đăng thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    } finally {
      setUpdatingListing(false);
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

        const res = await listingsApi.createListing({
          roomId: selectedRoomId,
          title: finalTitle,
          description: description.trim() || "Phòng đầy đủ tiện nghi, an ninh tốt, giờ giấc tự do.",
          price: Number(price),
          depositAmount: Number(deposit),
          minLeaseMonths: Number(minLeaseMonths) || 6,
          houseRules: houseRules.trim() || null,
        });

        if (res.isSuccess && res.data) {
          if (imageUrl && imageUrl.trim()) {
            await listingsApi.addImage(res.data.id, {
              imageUrl: imageUrl.trim(),
              isPrimary: true,
              sortOrder: 1,
            });
          }
          const submitRes = await listingsApi.submitListing(res.data.id);
          if (submitRes.isSuccess) {
            showToast("Đã tạo tin và gửi kiểm duyệt lên Admin thành công!", "success");
          } else {
            showToast(
              `Đã tạo tin dạng Bản nháp. ${submitRes.error?.message ? "Lưu ý: " + submitRes.error.message : ""}`,
              "info"
            );
          }
          setIsCreateOpen(false);
          await loadData();
          setSubmitting(false);
          return;
        } else {
          const detailMsg = res.error?.details && res.error.details.length > 0
            ? `: ${res.error.details.map((d) => d.message || d.field).join(", ")}`
            : "";
          showToast(`${res.error?.message || "Không thể tạo tin đăng"}${detailMsg}`, "error");
          setSubmitting(false);
          return;
        }
      } catch (err) {
        showToast("Không thể tạo tin đăng. Vui lòng kiểm tra lại kết nối.", "error");
        setSubmitting(false);
        return;
      }
    } else {
      showToast("Vui lòng chọn hoặc tạo phòng trước khi đăng tin!", "error");
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
                      onClick={() => handleOpenImageModal(item.id)}
                      className="absolute bottom-3 right-3 px-2.5 py-1 bg-black/65 hover:bg-black/85 text-white text-[11px] font-bold rounded-lg flex items-center gap-1 backdrop-blur-xs transition-colors"
                    >
                      <ImageIcon className="w-3 h-3" />
                      <span>Quản lý ảnh</span>
                    </button>
                  </div>

                  <div className="p-4 space-y-2">
                    <h3 className="font-bold text-slate-900 text-sm line-clamp-2">{item.title}</h3>
                    {(item.districtName || item.provinceName) && (
                      <div className="flex items-center gap-1 text-[11px] text-slate-500">
                        <MapPin className="w-3 h-3 text-rose-500 shrink-0" />
                        <span className="truncate">
                          {[item.addressDetail, item.districtName, item.provinceName]
                            .filter(Boolean)
                            .join(", ")}
                        </span>
                      </div>
                    )}
                    <div className="flex items-center justify-between pt-2 text-xs font-semibold">
                      <span className="text-blue-600 font-black text-sm">
                        {item.price.toLocaleString()} đ/tháng
                      </span>
                      <span className="text-slate-400 text-[11px]">
                        ID: #{item.id} • Phòng #{item.roomId}
                      </span>
                    </div>
                  </div>
                </div>

                <div className="p-4 border-t border-slate-100 flex items-center justify-between gap-2">
                  <div className="flex items-center gap-3">
                    <Link
                      href={`/listings/${item.id}`}
                      className="text-xs font-bold text-blue-600 hover:text-blue-700 flex items-center gap-1"
                    >
                      <Eye className="w-3.5 h-3.5" />
                      <span>Xem</span>
                    </Link>
                    <button
                      type="button"
                      onClick={() => handleOpenEditListing(item.id)}
                      className="text-xs font-bold text-slate-700 hover:text-blue-600 flex items-center gap-1"
                    >
                      <Edit2 className="w-3.5 h-3.5" />
                      <span>Sửa / Chi tiết</span>
                    </button>
                  </div>

                  <div className="flex items-center gap-1.5">
                    {((item.status || "").toUpperCase() === "DRAFT" ||
                      (item.status || "").toUpperCase() === "REJECTED") && (
                      <button
                        onClick={() => handleSubmitApproval(item.id)}
                        className="px-3 py-1.5 rounded-lg text-xs font-bold bg-amber-500 hover:bg-amber-600 text-white transition-colors flex items-center gap-1 shadow-xs"
                      >
                        <Send className="w-3 h-3" />
                        <span>Gửi duyệt</span>
                      </button>
                    )}

                    {(item.status || "").toUpperCase() === "PUBLISHED" && (
                      <button
                        onClick={() => handleToggleHideBackend(item)}
                        className="px-3 py-1.5 rounded-lg text-xs font-bold border border-slate-200 bg-slate-100 text-slate-700 hover:bg-slate-200 transition-colors flex items-center gap-1"
                      >
                        <EyeOff className="w-3.5 h-3.5" />
                        <span>Ẩn tin</span>
                      </button>
                    )}

                    {(item.status || "").toUpperCase() === "HIDDEN" && (
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

      {/* Modal: Manage Images (POST / PATCH / DELETE /api/v1/listings/{id}/images) */}
      {isAddImageOpen && (
        <Modal
          isOpen={true}
          onClose={() => setIsAddImageOpen(false)}
          title={`Quản Lý Hình Ảnh & Media Tin Đăng #${selectedListingForImage}`}
          maxWidth="lg"
        >
          <div className="space-y-4">
            <div>
              <span className="block text-xs font-bold text-slate-700 uppercase mb-2">
                Danh sách ảnh hiện tại ({listingImages.length}/20)
              </span>
              {loadingImages ? (
                <p className="text-xs text-slate-400 py-4 text-center">Đang tải danh sách hình ảnh...</p>
              ) : listingImages.length === 0 ? (
                <p className="text-xs text-slate-400 py-4 text-center bg-slate-50 rounded-xl border border-dashed border-slate-200">
                  Tin đăng này chưa có hình ảnh nào. Hãy thêm ảnh bên dưới!
                </p>
              ) : (
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 max-h-56 overflow-y-auto p-1">
                  {listingImages.map((img) => (
                    <div
                      key={img.id}
                      className={cn(
                        "relative rounded-xl overflow-hidden border group bg-slate-100 aspect-[16/10]",
                        img.isPrimary ? "border-blue-500 ring-2 ring-blue-500/30" : "border-slate-200"
                      )}
                    >
                      <img src={img.imageUrl} alt="" className="w-full h-full object-cover" />
                      <div className="absolute top-1.5 left-1.5 flex gap-1">
                        {img.isPrimary && (
                          <span className="px-1.5 py-0.5 rounded bg-blue-600 text-white text-[9px] font-bold">
                            Ảnh bìa
                          </span>
                        )}
                        <span className="px-1.5 py-0.5 rounded bg-black/60 text-white text-[9px] font-semibold">
                          #{img.sortOrder} • {img.mediaType || "IMAGE"}
                        </span>
                      </div>
                      <div className="absolute inset-x-0 bottom-0 p-1.5 bg-gradient-to-t from-black/80 to-transparent flex items-center justify-between gap-1">
                        {!img.isPrimary ? (
                          <button
                            type="button"
                            onClick={() => handleSetPrimaryImage(img)}
                            className="px-2 py-0.5 rounded bg-white/90 hover:bg-white text-slate-800 text-[10px] font-bold flex items-center gap-0.5"
                          >
                            <Star className="w-2.5 h-2.5 text-amber-500" />
                            <span>Đặt ảnh bìa</span>
                          </button>
                        ) : (
                          <span className="text-[10px] text-emerald-300 font-bold pl-1">Đang là ảnh chính</span>
                        )}
                        <button
                          type="button"
                          onClick={() => handleDeleteImage(img.id)}
                          className="p-1 rounded bg-rose-600 hover:bg-rose-700 text-white"
                          title="Xóa ảnh"
                        >
                          <Trash2 className="w-3 h-3" />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <form onSubmit={handleAddImage} className="space-y-3 pt-3 border-t border-slate-200">
              <span className="block text-xs font-bold text-slate-800 uppercase">
                Thêm Ảnh / Video / Tour 360 Mới
              </span>
              <div>
                <label className="block text-[11px] font-bold text-slate-600 mb-1">
                  Đường dẫn Media (URL https://) *
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
              <div className="grid grid-cols-3 gap-2">
                <div>
                  <label className="block text-[11px] font-bold text-slate-600 mb-1">
                    Loại Media
                  </label>
                  <select
                    value={newMediaType}
                    onChange={(e) => setNewMediaType(e.target.value)}
                    className="w-full px-2.5 py-2 text-xs rounded-xl border border-slate-200 bg-white"
                  >
                    <option value="IMAGE">Hình ảnh (IMAGE)</option>
                    <option value="VIDEO">Video (VIDEO)</option>
                    <option value="VIRTUAL_TOUR_360">Ảnh 360° (VIRTUAL_TOUR_360)</option>
                  </select>
                </div>
                <div>
                  <label className="block text-[11px] font-bold text-slate-600 mb-1">
                    Thứ tự hiển thị
                  </label>
                  <input
                    type="number"
                    min={0}
                    value={newSortOrder}
                    onChange={(e) => setNewSortOrder(Number(e.target.value))}
                    className="w-full px-2.5 py-2 text-xs rounded-xl border border-slate-200"
                  />
                </div>
                <div className="flex items-end pb-2">
                  <label className="flex items-center gap-1.5 text-xs font-bold text-slate-700 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={newIsPrimary}
                      onChange={(e) => setNewIsPrimary(e.target.checked)}
                      className="rounded text-blue-600"
                    />
                    <span>Đặt làm ảnh bìa</span>
                  </label>
                </div>
              </div>
              <div className="flex gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setIsAddImageOpen(false)}
                  className="flex-1 py-2 rounded-xl border border-slate-200 text-xs font-bold text-slate-700"
                >
                  Đóng
                </button>
                <button
                  type="submit"
                  disabled={addingImage}
                  className="flex-1 py-2 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 disabled:opacity-50"
                >
                  {addingImage ? "Đang tải lên..." : "Thêm Media"}
                </button>
              </div>
            </form>
          </div>
        </Modal>
      )}

      {/* Modal: Edit & View Listing Detail (GET /api/v1/listings/mine/{id}, PATCH /api/v1/listings/{id}) */}
      {isEditOpen && editingListing && (
        <Modal
          isOpen={true}
          onClose={() => setIsEditOpen(false)}
          title={`Chi Tiết & Chỉnh Sửa Tin Đăng #${editingListing.id}`}
          maxWidth="2xl"
        >
          <form onSubmit={handleUpdateListing} className="space-y-4">
            {editingListing.review?.rejectionReason && (
              <div className="p-3.5 rounded-xl bg-rose-50 border border-rose-200 text-xs text-rose-800 space-y-1">
                <div className="font-bold flex items-center gap-1.5 text-rose-900">
                  <AlertCircle className="w-4 h-4 text-rose-600" />
                  <span>Lý do bị Admin từ chối (Lần duyệt #{editingListing.review.revision}):</span>
                </div>
                <p className="font-medium">{editingListing.review.rejectionReason}</p>
              </div>
            )}

            <div className="grid grid-cols-3 gap-3 p-3 rounded-xl bg-slate-50 border border-slate-200/80 text-xs">
              <div>
                <span className="text-slate-400 block text-[10px] uppercase font-bold">Mã phòng</span>
                <span className="font-bold text-slate-800">Phòng #{editingListing.roomId}</span>
              </div>
              <div>
                <span className="text-slate-400 block text-[10px] uppercase font-bold">Lượt xem</span>
                <span className="font-bold text-slate-800">{editingListing.viewCount || 0} lượt</span>
              </div>
              <div>
                <span className="text-slate-400 block text-[10px] uppercase font-bold">Trạng thái</span>
                <span className="font-bold text-blue-600">{editingListing.status}</span>
              </div>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Tiêu đề tin đăng *
              </label>
              <input
                type="text"
                required
                value={editTitle}
                onChange={(e) => setEditTitle(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-semibold"
              />
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Giá thuê tháng (VNĐ) *
                </label>
                <input
                  type="number"
                  required
                  step={100000}
                  value={editPrice}
                  onChange={(e) => setEditPrice(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-bold text-blue-600"
                />
              </div>
              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Tiền đặt cọc (VNĐ) *
                </label>
                <input
                  type="number"
                  required
                  step={100000}
                  value={editDeposit}
                  onChange={(e) => setEditDeposit(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-bold"
                />
              </div>
              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Hợp đồng tối thiểu (tháng)
                </label>
                <input
                  type="number"
                  min={1}
                  max={60}
                  required
                  value={editMinLeaseMonths}
                  onChange={(e) => setEditMinLeaseMonths(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 font-bold"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Mô tả chi tiết *
              </label>
              <textarea
                rows={3}
                required
                value={editDescription}
                onChange={(e) => setEditDescription(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Nội quy tòa nhà / phòng (House Rules)
              </label>
              <textarea
                rows={2}
                placeholder="VD: Giờ giấc tự do, không hút thuốc trong phòng, giữ vệ sinh chung..."
                value={editHouseRules}
                onChange={(e) => setEditHouseRules(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>

            <div className="flex gap-3 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setIsEditOpen(false)}
                className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
              >
                Đóng
              </button>
              <button
                type="submit"
                disabled={updatingListing}
                className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 shadow-md shadow-blue-500/20 disabled:opacity-50"
              >
                {updatingListing ? "Đang lưu..." : "Lưu thay đổi tin đăng"}
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
            {properties.length === 0 ? (
              <div className="p-4 rounded-xl bg-amber-50 border border-amber-200 text-amber-900 text-xs space-y-2">
                <div className="font-bold flex items-center gap-1.5 text-amber-800">
                  <Building2 className="w-4 h-4 text-amber-600" />
                  <span>Chưa có bất động sản hoặc phòng</span>
                </div>
                <p className="text-slate-600 text-[11px] leading-relaxed">
                  Để tạo tin đăng, bạn cần tạo thông tin Bất động sản và Phòng trước trong hệ thống.
                </p>
                <div>
                  <Link
                    href="/landlord/properties"
                    className="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg bg-amber-600 hover:bg-amber-700 text-white font-bold text-[11px] transition-colors"
                  >
                    <span>Đến trang Quản lý Bất động sản</span>
                  </Link>
                </div>
              </div>
            ) : (
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
                      Phòng cụ thể *
                    </label>
                    <select
                      value={selectedRoomId || ""}
                      onChange={(e) => {
                        const rId = Number(e.target.value);
                        setSelectedRoomId(rId);
                        const r = rooms.find((rm) => rm.id === rId);
                        if (r) {
                          setPrice(r.basePrice ?? 0);
                          setDeposit(r.basePrice ?? 0);
                          setArea(r.areaSqm);
                        }
                      }}
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

            <div className="grid grid-cols-3 gap-3">
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

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Thuê tối thiểu (tháng)
                </label>
                <input
                  type="number"
                  min={1}
                  max={60}
                  required
                  value={minLeaseMonths}
                  onChange={(e) => setMinLeaseMonths(Number(e.target.value))}
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
                Nội quy phòng / tòa nhà (House Rules)
              </label>
              <textarea
                rows={2}
                placeholder="VD: Giờ giấc tự do, không gây ồn sau 23h, giữ vệ sinh khu vực chung..."
                value={houseRules}
                onChange={(e) => setHouseRules(e.target.value)}
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
              <div className="flex items-center justify-between mb-2">
                <label className="block text-xs font-bold text-slate-700 uppercase">
                  Tiện ích phòng
                </label>
                <span className="text-[11px] font-bold text-blue-700 bg-blue-50 px-2.5 py-0.5 rounded-full border border-blue-200">
                  {selectedAmenities.length} đã chọn
                </span>
              </div>
              <div className="flex flex-wrap gap-2 max-h-48 overflow-y-auto p-3 rounded-2xl border border-slate-200/90 bg-slate-50/50">
                {(catalogAmenities.length > 0
                  ? catalogAmenities.map((a) => formatAmenityName(a.name))
                  : DEFAULT_AMENITIES_OPTIONS
                ).map((amenity) => {
                  const checked = selectedAmenities.includes(amenity);
                  return (
                    <button
                      type="button"
                      key={amenity}
                      onClick={() => toggleAmenity(amenity)}
                      className={cn(
                        "px-3 py-1.5 rounded-xl text-xs font-bold border transition-all flex items-center gap-1.5 shadow-2xs",
                        checked
                          ? "bg-blue-600 text-white border-blue-600 shadow-sm shadow-blue-500/20"
                          : "bg-white text-slate-700 border-slate-200/90 hover:border-slate-300 hover:bg-slate-100/60"
                      )}
                    >
                      {checked && <Check className="w-3.5 h-3.5 shrink-0" />}
                      <span>{amenity}</span>
                    </button>
                  );
                })}
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
                disabled={submitting || !selectedRoomId}
                className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 shadow-md shadow-blue-500/20 disabled:opacity-50"
              >
                {submitting ? "Đang gửi duyệt..." : !selectedRoomId ? "Cần chọn phòng trước" : "Xác nhận & Gửi kiểm duyệt"}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
