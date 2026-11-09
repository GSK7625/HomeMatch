"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import { listingService } from "@/lib/services";
import { propertiesApi, catalogsApi, getStoredToken } from "@/lib/api-client";
import {
  PropertyDto,
  RoomDto,
  ProvinceDto,
  WardDto,
  AmenityDto,
  PropertyListing,
  RoomStatus,
} from "@/types";
import { useToast } from "@/components/Toast";
import { Modal } from "@/components/Modal";
import {
  Home,
  Plus,
  Edit2,
  Trash2,
  MapPin,
  Maximize2,
  Zap,
  Droplet,
  CheckCircle2,
  Eye,
  ArrowRight,
  Building2,
  Layers,
  Sparkles,
  RefreshCw,
  SlidersHorizontal,
  DollarSign,
  AlertCircle,
} from "lucide-react";
import { cn } from "@/lib/utils";

const PROPERTY_TYPES = [
  { value: "BOARDING_HOUSE", label: "Dãy nhà trọ / Khu phòng trọ" },
  { value: "APARTMENT", label: "Chung cư mini" },
  { value: "SERVICED_APARTMENT", label: "Căn hộ dịch vụ" },
  { value: "HOMESTAY", label: "Homestay / Nhà nguyên căn" },
];

const ROOM_TYPES = [
  { value: "SINGLE", label: "Phòng đơn khép kín" },
  { value: "STUDIO", label: "Studio" },
  { value: "1B1B", label: "1 Phòng ngủ + 1 Khách" },
  { value: "2B1B", label: "2 Phòng ngủ" },
  { value: "DORMITORY", label: "Ký túc xá / Ở ghép" },
];

export default function LandlordPropertiesPage() {
  const { user, role } = useAuth();
  const { showToast } = useToast();

  // Backend state
  const [properties, setProperties] = useState<PropertyDto[]>([]);
  const [selectedPropertyId, setSelectedPropertyId] = useState<number | null>(null);
  const [rooms, setRooms] = useState<RoomDto[]>([]);
  const [amenities, setAmenities] = useState<AmenityDto[]>([]);
  const [provinces, setProvinces] = useState<ProvinceDto[]>([]);
  const [wards, setWards] = useState<WardDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [isBackendConnected, setIsBackendConnected] = useState(false);

  // Properties & Rooms state

  // Modals
  const [isCreatePropOpen, setIsCreatePropOpen] = useState(false);
  const [isCreateRoomOpen, setIsCreateRoomOpen] = useState(false);

  // Form: Create Property
  const [newPropName, setNewPropName] = useState("");
  const [newPropType, setNewPropType] = useState("BOARDING_HOUSE");
  const [newPropProvince, setNewPropProvince] = useState("01"); // Hà Nội
  const [newPropWard, setNewPropWard] = useState("");
  const [newPropAddress, setNewPropAddress] = useState("");
  const [savingProp, setSavingProp] = useState(false);

  // Form: Create Room
  const [newRoomNumber, setNewRoomNumber] = useState("");
  const [newRoomFloor, setNewRoomFloor] = useState(1);
  const [newRoomArea, setNewRoomArea] = useState(25);
  const [newRoomType, setNewRoomType] = useState("STUDIO");
  const [newRoomMaxOccupants, setNewRoomMaxOccupants] = useState(2);
  const [newRoomPrice, setNewRoomPrice] = useState(4500000);
  const [newRoomAmenities, setNewRoomAmenities] = useState<number[]>([]);
  const [savingRoom, setSavingRoom] = useState(false);

  // Load catalogs (Provinces & Amenities)
  useEffect(() => {
    async function loadCatalogs() {
      try {
        const [provRes, amenRes] = await Promise.all([
          catalogsApi.getProvinces(),
          catalogsApi.getAmenities(),
        ]);
        if (provRes.isSuccess && provRes.data) {
          setProvinces(provRes.data);
        }
        if (amenRes.isSuccess && amenRes.data) {
          setAmenities(amenRes.data);
        }
      } catch (err) {
        console.warn("Could not load catalogs from backend", err);
      }
    }
    loadCatalogs();
  }, []);

  // Load wards when province changes
  useEffect(() => {
    async function loadWards() {
      if (!newPropProvince) return;
      try {
        const wardRes = await catalogsApi.getWards(newPropProvince);
        if (wardRes.isSuccess && wardRes.data) {
          setWards(wardRes.data);
          if (wardRes.data.length > 0 && !newPropWard) {
            setNewPropWard(wardRes.data[0].code);
          }
        }
      } catch (err) {
        console.warn("Could not load wards", err);
      }
    }
    loadWards();
  }, [newPropProvince, newPropWard]);

  // Load Properties from backend
  const loadProperties = useCallback(async () => {
    setLoading(true);
    const token = getStoredToken();
    if (!token) {
      setProperties([]);
      setRooms([]);
      setIsBackendConnected(false);
      setLoading(false);
      return;
    }

    try {
      const res = await propertiesApi.getMyProperties();
      if (res.isSuccess && res.data) {
        setProperties(res.data);
        setIsBackendConnected(true);
        if (res.data.length > 0) {
          setSelectedPropertyId((prev) => (prev && res.data!.some((p) => p.id === prev) ? prev : res.data![0].id));
        } else {
          setSelectedPropertyId(null);
          setRooms([]);
        }
      } else {
        setProperties([]);
        setSelectedPropertyId(null);
        setRooms([]);
      }
    } catch {
      setProperties([]);
      setSelectedPropertyId(null);
      setRooms([]);
    } finally {
      setLoading(false);
    }
  }, []);

  // Load rooms for selected property
  const loadRooms = useCallback(async (propertyId: number) => {
    setLoading(true);
    try {
      const res = await propertiesApi.getRooms(propertyId);
      if (res.isSuccess && res.data) {
        setRooms(res.data);
      } else {
        setRooms([]);
      }
    } catch {
      setRooms([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadProperties();
  }, [loadProperties]);

  useEffect(() => {
    if (selectedPropertyId) {
      loadRooms(selectedPropertyId);
    }
  }, [selectedPropertyId, loadRooms]);

  if (!user || role === "Tenant") {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <Home className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Quản Lý Căn & Phòng Cho Thuê</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập với tài khoản <strong>Chủ nhà (Landlord)</strong> để quản lý danh sách phòng và trạng thái cho thuê.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/landlord/properties"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng nhập tài khoản Chủ nhà</span>
          </Link>
        </div>
      </div>
    );
  }

  // Handle Create Property
  const handleCreateProperty = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newPropName || !newPropAddress) {
      showToast("Vui lòng điền đầy đủ tên và địa chỉ tòa nhà!", "error");
      return;
    }
    setSavingProp(true);
    try {
      const res = await propertiesApi.createProperty({
        name: newPropName,
        propertyType: newPropType,
        provinceCode: newPropProvince,
        wardCode: newPropWard || "00001",
        addressDetail: newPropAddress,
      });

      if (res.isSuccess && res.data) {
        showToast("Tạo mới bất động sản thành công!", "success");
        setIsCreatePropOpen(false);
        setNewPropName("");
        setNewPropAddress("");
        await loadProperties();
        setSelectedPropertyId(res.data.id);
      } else {
        showToast(res.error?.message || "Tạo bất động sản thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối máy chủ", "error");
    } finally {
      setSavingProp(false);
    }
  };

  // Handle Create Room
  const handleCreateRoom = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPropertyId) {
      showToast("Vui lòng chọn bất động sản trước khi tạo phòng!", "error");
      return;
    }
    if (!newRoomNumber) {
      showToast("Vui lòng nhập số hiệu phòng (ví dụ: P.201)!", "error");
      return;
    }
    setSavingRoom(true);
    try {
      const res = await propertiesApi.createRoom(selectedPropertyId, {
        roomNumber: newRoomNumber,
        floor: Number(newRoomFloor),
        areaSqm: Number(newRoomArea),
        roomType: newRoomType,
        maxOccupants: Number(newRoomMaxOccupants),
        basePrice: Number(newRoomPrice),
        amenityIds: newRoomAmenities,
      });

      if (res.isSuccess && res.data) {
        showToast(`Tạo phòng ${newRoomNumber} thành công!`, "success");
        setIsCreateRoomOpen(false);
        setNewRoomNumber("");
        loadRooms(selectedPropertyId);
      } else {
        showToast(res.error?.message || "Tạo phòng thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối máy chủ", "error");
    } finally {
      setSavingRoom(false);
    }
  };

  // Handle Update Room Status on Backend
  const handleUpdateStatus = async (roomId: number, newStatus: "AVAILABLE" | "RENTED" | "SUSPENDED") => {
    if (!selectedPropertyId) return;
    try {
      const res = await propertiesApi.setRoomStatus(selectedPropertyId, roomId, newStatus);
      if (res.isSuccess) {
        showToast("Đã cập nhật trạng thái phòng thành công!", "success");
        loadRooms(selectedPropertyId);
      } else {
        showToast(res.error?.message || "Không thể cập nhật trạng thái", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    }
  };


  const selectedProperty = properties.find((p) => p.id === selectedPropertyId);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Building2 className="w-4 h-4" />
            <span>Quản Lý Bất Động Sản & Phòng Cho Thuê</span>
            {isBackendConnected && (
              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-100 text-emerald-800">
                <CheckCircle2 className="w-3 h-3" /> Live Backend API
              </span>
            )}
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Danh Sách Tòa Nhà & Phòng Trọ
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Quản lý cấu trúc tòa nhà, phân chia phòng, thiết lập biểu giá và liên kết tin đăng.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <button
            onClick={() => setIsCreatePropOpen(true)}
            className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-800 text-xs font-bold transition-colors"
          >
            <Plus className="w-4 h-4" />
            <span>Thêm Tòa Nhà / Dãy Trọ</span>
          </button>

          {selectedPropertyId && (
            <button
              onClick={() => setIsCreateRoomOpen(true)}
              className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20 transition-all"
            >
              <Plus className="w-4 h-4" />
              <span>Thêm Phòng Mới</span>
            </button>
          )}

          <Link
            href="/landlord/listings"
            className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-indigo-50 hover:bg-indigo-100 text-indigo-700 text-xs font-bold transition-colors"
          >
            <Sparkles className="w-4 h-4" />
            <span>Tạo tin đăng phòng</span>
          </Link>
        </div>
      </div>

      {/* Properties Selector & Summary */}
      {properties.length > 0 && (
        <div className="bg-white rounded-2xl border border-slate-200/80 p-4 shadow-xs flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
          <div className="flex items-center gap-3 w-full md:w-auto">
            <div className="w-10 h-10 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center shrink-0">
              <Building2 className="w-5 h-5" />
            </div>
            <div className="flex-1 min-w-0">
              <label className="text-[11px] font-bold text-slate-400 uppercase tracking-wider block">
                Đang xem bất động sản:
              </label>
              <select
                value={selectedPropertyId || ""}
                onChange={(e) => setSelectedPropertyId(Number(e.target.value))}
                className="font-bold text-slate-900 text-sm bg-transparent border-none p-0 focus:ring-0 cursor-pointer"
              >
                {properties.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name} ({p.addressDetail})
                  </option>
                ))}
              </select>
            </div>
          </div>

          {selectedProperty && (
            <div className="flex items-center gap-3 text-xs text-slate-500 flex-wrap">
              <span className="flex items-center gap-1">
                <MapPin className="w-3.5 h-3.5 text-slate-400" />
                {selectedProperty.addressDetail}
              </span>
              <span>•</span>
              <span className="font-bold text-slate-700">
                {rooms.length} phòng trực thuộc
              </span>
              <span>•</span>
              <Link
                href={`/landlord/utilities?propertyId=${selectedProperty.id}`}
                className="text-blue-600 font-bold hover:underline flex items-center gap-1"
              >
                <SlidersHorizontal className="w-3.5 h-3.5" />
                <span>Cấu hình biểu phí điện nước</span>
              </Link>
            </div>
          )}
        </div>
      )}

      {/* Backend Rooms Table */}
      {isBackendConnected && selectedPropertyId ? (
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-x-auto">
          <div className="p-4 border-b border-slate-100 flex items-center justify-between">
            <h3 className="font-black text-slate-900 text-sm flex items-center gap-2">
              <Layers className="w-4 h-4 text-blue-600" />
              <span>Danh Sách Phòng Tại {selectedProperty?.name}</span>
            </h3>
            <button
              onClick={() => selectedPropertyId && loadRooms(selectedPropertyId)}
              className="text-xs font-bold text-slate-500 hover:text-blue-600 flex items-center gap-1"
            >
              <RefreshCw className={cn("w-3.5 h-3.5", loading && "animate-spin")} />
              <span>Làm mới</span>
            </button>
          </div>

          <table className="w-full text-xs text-left border-collapse min-w-[800px]">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50/70 text-slate-500 uppercase tracking-wider">
                <th className="p-4">Số phòng & Tầng</th>
                <th className="p-4">Loại phòng & Diện tích</th>
                <th className="p-4">Số người tối đa</th>
                <th className="p-4">Giá thuê niêm yết</th>
                <th className="p-4">Trạng thái</th>
                <th className="p-4 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {rooms.length === 0 ? (
                <tr>
                  <td colSpan={6} className="p-12 text-center text-slate-400">
                    Chưa có phòng nào trong bất động sản này. Bấm &quot;Thêm Phòng Mới&quot; để bắt đầu!
                  </td>
                </tr>
              ) : (
                rooms.map((room) => (
                  <tr key={room.id} className="hover:bg-slate-50/60 transition-colors">
                    <td className="p-4">
                      <div className="font-bold text-slate-900 text-sm">Phòng {room.roomNumber}</div>
                      <div className="text-[11px] text-slate-400">Tầng {room.floor}</div>
                    </td>
                    <td className="p-4">
                      <div className="font-bold text-slate-800">{room.roomType}</div>
                      <div className="text-[11px] text-slate-500">{room.areaSqm} m²</div>
                    </td>
                    <td className="p-4 font-medium text-slate-700">{room.maxOccupants} người</td>
                    <td className="p-4 font-black text-blue-600 text-sm">
                      {room.basePrice.toLocaleString()} đ/tháng
                    </td>
                    <td className="p-4">
                      <select
                        value={room.status}
                        onChange={(e) =>
                          handleUpdateStatus(
                            room.id,
                            e.target.value as "AVAILABLE" | "RENTED" | "SUSPENDED"
                          )
                        }
                        className={cn(
                          "px-3 py-1.5 rounded-xl text-xs font-bold border focus:outline-none cursor-pointer",
                          room.status === "AVAILABLE" &&
                            "bg-emerald-50 text-emerald-700 border-emerald-300",
                          room.status === "RENTED" &&
                            "bg-slate-100 text-slate-700 border-slate-300",
                          room.status === "SUSPENDED" &&
                            "bg-rose-50 text-rose-700 border-rose-300"
                        )}
                      >
                        <option value="AVAILABLE">Còn trống</option>
                        <option value="RENTED">Đã thuê</option>
                        <option value="SUSPENDED">Tạm ngừng</option>
                      </select>
                    </td>
                    <td className="p-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Link
                          href={`/landlord/listings?roomId=${room.id}&price=${room.basePrice}`}
                          className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg bg-blue-50 text-blue-700 hover:bg-blue-100 font-bold text-[11px] transition-colors"
                        >
                          <Sparkles className="w-3.5 h-3.5" />
                          <span>Đăng tin</span>
                        </Link>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="bg-white rounded-2xl border border-dashed border-slate-300 p-12 text-center max-w-2xl mx-auto space-y-4">
          <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
            <Building2 className="w-8 h-8" />
          </div>
          <div>
            <h3 className="text-lg font-bold text-slate-900">Bạn chưa có tòa nhà / dãy trọ nào</h3>
            <p className="text-xs text-slate-500 mt-1 max-w-md mx-auto leading-relaxed">
              Bắt đầu số hóa khu trọ của bạn bằng cách thêm tòa nhà đầu tiên, phân chia từng phòng và thiết lập biểu giá điện nước.
            </p>
          </div>
          <button
            onClick={() => setIsCreatePropOpen(true)}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs shadow-md shadow-blue-500/20"
          >
            <Plus className="w-4 h-4" />
            <span>Thêm Tòa Nhà / Dãy Trọ Mới</span>
          </button>
        </div>
      )}

      {/* Modal: Create Property */}
      {isCreatePropOpen && (
        <Modal
          isOpen={true}
          onClose={() => setIsCreatePropOpen(false)}
          title="Tạo Mới Tòa Nhà / Khu Bất Động Sản"
          maxWidth="md"
        >
          <form onSubmit={handleCreateProperty} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Tên Tòa Nhà / Dãy Trọ *
              </label>
              <input
                type="text"
                required
                placeholder="VD: Chung Cư Mini Cầu Giấy 1"
                value={newPropName}
                onChange={(e) => setNewPropName(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Mô hình bất động sản
                </label>
                <select
                  value={newPropType}
                  onChange={(e) => setNewPropType(e.target.value)}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
                >
                  {PROPERTY_TYPES.map((t) => (
                    <option key={t.value} value={t.value}>
                      {t.label}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Tỉnh / Thành Phố
                </label>
                <select
                  value={newPropProvince}
                  onChange={(e) => setNewPropProvince(e.target.value)}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
                >
                  {provinces.length > 0 ? (
                    provinces.map((p) => (
                      <option key={p.code} value={p.code}>
                        {p.fullName || p.name}
                      </option>
                    ))
                  ) : (
                    <option value="01">Hà Nội</option>
                  )}
                </select>
              </div>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Địa chỉ chi tiết (Số nhà, ngõ, tên đường) *
              </label>
              <input
                type="text"
                required
                placeholder="VD: Số 18 Ngõ 68 Cầu Giấy, Quan Hoa"
                value={newPropAddress}
                onChange={(e) => setNewPropAddress(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>

            <div className="flex gap-3 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setIsCreatePropOpen(false)}
                className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={savingProp}
                className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 disabled:opacity-50"
              >
                {savingProp ? "Đang lưu..." : "Xác nhận tạo"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Modal: Create Room */}
      {isCreateRoomOpen && (
        <Modal
          isOpen={true}
          onClose={() => setIsCreateRoomOpen(false)}
          title={`Thêm Phòng Trọ Mới vào ${selectedProperty?.name}`}
          maxWidth="lg"
        >
          <form onSubmit={handleCreateRoom} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Số hiệu phòng *
                </label>
                <input
                  type="text"
                  required
                  placeholder="VD: P.202"
                  value={newRoomNumber}
                  onChange={(e) => setNewRoomNumber(e.target.value)}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Tầng số
                </label>
                <input
                  type="number"
                  min={0}
                  value={newRoomFloor}
                  onChange={(e) => setNewRoomFloor(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
                />
              </div>
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Loại phòng
                </label>
                <select
                  value={newRoomType}
                  onChange={(e) => setNewRoomType(e.target.value)}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
                >
                  {ROOM_TYPES.map((t) => (
                    <option key={t.value} value={t.value}>
                      {t.label}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Diện tích (m²)
                </label>
                <input
                  type="number"
                  min={5}
                  value={newRoomArea}
                  onChange={(e) => setNewRoomArea(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Số người tối đa
                </label>
                <input
                  type="number"
                  min={1}
                  value={newRoomMaxOccupants}
                  onChange={(e) => setNewRoomMaxOccupants(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Giá thuê niêm yết (VNĐ/tháng) *
              </label>
              <input
                type="number"
                step={100000}
                required
                value={newRoomPrice}
                onChange={(e) => setNewRoomPrice(Number(e.target.value))}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>

            <div className="flex gap-3 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setIsCreateRoomOpen(false)}
                className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={savingRoom}
                className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 disabled:opacity-50"
              >
                {savingRoom ? "Đang lưu..." : "Xác nhận thêm"}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
