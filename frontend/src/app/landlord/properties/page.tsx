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
import { MapLocationPickerModal } from "@/components/MapLocationPickerModal";
import { MapLocationViewer } from "@/components/MapLocationViewer";
import { SearchableSelect } from "@/components/SearchableSelect";
import { AddressAutocompleteInput } from "@/components/AddressAutocompleteInput";
import {
  Home,
  Plus,
  Edit2,
  Trash2,
  MapPin,
  Maximize2,
  Zap,
  Droplet,
  Eye,
  ArrowRight,
  Building2,
  Layers,
  Sparkles,
  RefreshCw,
  SlidersHorizontal,
  DollarSign,
  AlertCircle,
  Check,
  Search,
  Compass,
  ChevronDown,
} from "lucide-react";
import { cn, formatAmenityName } from "@/lib/utils";

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

const FALLBACK_PROVINCES: ProvinceDto[] = [
  { code: "01", name: "Hà Nội", fullName: "Thành phố Hà Nội" },
  { code: "79", name: "Hồ Chí Minh", fullName: "Thành phố Hồ Chí Minh" },
  { code: "48", name: "Đà Nẵng", fullName: "Thành phố Đà Nẵng" },
  { code: "31", name: "Hải Phòng", fullName: "Thành phố Hải Phòng" },
  { code: "92", name: "Cần Thơ", fullName: "Thành phố Cần Thơ" },
  { code: "74", name: "Bình Dương", fullName: "Tỉnh Bình Dương" },
  { code: "75", name: "Đồng Nai", fullName: "Tỉnh Đồng Nai" },
  { code: "77", name: "Bà Rịa - Vũng Tàu", fullName: "Tỉnh Bà Rịa - Vũng Tàu" },
  { code: "56", name: "Khánh Hòa", fullName: "Tỉnh Khánh Hòa" },
  { code: "49", name: "Quảng Nam", fullName: "Tỉnh Quảng Nam" },
  { code: "46", name: "Thừa Thiên Huế", fullName: "Tỉnh Thừa Thiên Huế" },
  { code: "30", name: "Hải Dương", fullName: "Tỉnh Hải Dương" },
  { code: "27", name: "Bắc Ninh", fullName: "Tỉnh Bắc Ninh" },
];

const FALLBACK_WARDS_HN: WardDto[] = [
  // Các phường mới sáp nhập theo Nghị quyết của Quốc hội
  { code: "00181", name: "Văn Miếu - Quốc Tử Giám", fullName: "Phường Văn Miếu - Quốc Tử Giám (Đống Đa)", provinceCode: "01" },
  { code: "00226", name: "Phương Liên - Trung Tự", fullName: "Phường Phương Liên - Trung Tự (Đống Đa)", provinceCode: "01" },
  { code: "00202", name: "Khâm Thiên", fullName: "Phường Khâm Thiên (Đống Đa)", provinceCode: "01" },
  { code: "00187", name: "Láng Thượng", fullName: "Phường Láng Thượng (Đống Đa)", provinceCode: "01" },
  { code: "00199", name: "Láng Hạ", fullName: "Phường Láng Hạ (Đống Đa)", provinceCode: "01" },
  { code: "00190", name: "Ô Chợ Dừa", fullName: "Phường Ô Chợ Dừa (Đống Đa)", provinceCode: "01" },
  // Cầu Giấy
  { code: "00167", name: "Dịch Vọng Hậu", fullName: "Phường Dịch Vọng Hậu (Cầu Giấy)", provinceCode: "01" },
  { code: "00169", name: "Quan Hoa", fullName: "Phường Quan Hoa (Cầu Giấy)", provinceCode: "01" },
  { code: "00166", name: "Dịch Vọng", fullName: "Phường Dịch Vọng (Cầu Giấy)", provinceCode: "01" },
  { code: "00172", name: "Yên Hoà", fullName: "Phường Yên Hoà (Cầu Giấy)", provinceCode: "01" },
  { code: "00175", name: "Trung Hoà", fullName: "Phường Trung Hoà (Cầu Giấy)", provinceCode: "01" },
  { code: "00163", name: "Mai Dịch", fullName: "Phường Mai Dịch (Cầu Giấy)", provinceCode: "01" },
  { code: "00157", name: "Nghĩa Đô", fullName: "Phường Nghĩa Đô (Cầu Giấy)", provinceCode: "01" },
  { code: "00160", name: "Nghĩa Tân", fullName: "Phường Nghĩa Tân (Cầu Giấy)", provinceCode: "01" },
  // Nam Từ Liêm & Bắc Từ Liêm
  { code: "00625", name: "Mỹ Đình 1", fullName: "Phường Mỹ Đình 1 (Nam Từ Liêm)", provinceCode: "01" },
  { code: "00626", name: "Mỹ Đình 2", fullName: "Phường Mỹ Đình 2 (Nam Từ Liêm)", provinceCode: "01" },
  { code: "00631", name: "Mễ Trì", fullName: "Phường Mễ Trì (Nam Từ Liêm)", provinceCode: "01" },
  { code: "00632", name: "Phú Đô", fullName: "Phường Phú Đô (Nam Từ Liêm)", provinceCode: "01" },
  { code: "00637", name: "Trung Văn", fullName: "Phường Trung Văn (Nam Từ Liêm)", provinceCode: "01" },
  { code: "00616", name: "Cổ Nhuế 1", fullName: "Phường Cổ Nhuế 1 (Bắc Từ Liêm)", provinceCode: "01" },
  { code: "00617", name: "Cổ Nhuế 2", fullName: "Phường Cổ Nhuế 2 (Bắc Từ Liêm)", provinceCode: "01" },
  { code: "00619", name: "Phú Diễn", fullName: "Phường Phú Diễn (Bắc Từ Liêm)", provinceCode: "01" },
  { code: "00610", name: "Xuân Đỉnh", fullName: "Phường Xuân Đỉnh (Bắc Từ Liêm)", provinceCode: "01" },
  // Thanh Xuân & Hai Bà Trưng
  { code: "00343", name: "Nhân Chính", fullName: "Phường Nhân Chính (Thanh Xuân)", provinceCode: "01" },
  { code: "00355", name: "Thanh Xuân Trung", fullName: "Phường Thanh Xuân Trung (Thanh Xuân)", provinceCode: "01" },
  { code: "00346", name: "Thượng Đình", fullName: "Phường Thượng Đình (Thanh Xuân)", provinceCode: "01" },
  { code: "00277", name: "Bách Khoa", fullName: "Phường Bách Khoa (Hai Bà Trưng)", provinceCode: "01" },
  { code: "00280", name: "Đồng Tâm", fullName: "Phường Đồng Tâm (Hai Bà Trưng)", provinceCode: "01" },
  { code: "00244", name: "Bạch Đằng", fullName: "Phường Bạch Đằng (Hai Bà Trưng)", provinceCode: "01" },
  { code: "00271", name: "Thanh Nhàn", fullName: "Phường Thanh Nhàn (Hai Bà Trưng)", provinceCode: "01" },
  { code: "00283", name: "Vĩnh Tuy", fullName: "Phường Vĩnh Tuy (Hai Bà Trưng)", provinceCode: "01" },
  // Ba Đình, Hoàn Kiếm, Tây Hồ
  { code: "00004", name: "Trúc Bạch", fullName: "Phường Trúc Bạch (Ba Đình)", provinceCode: "01" },
  { code: "00007", name: "Cống Vị", fullName: "Phường Cống Vị (Ba Đình)", provinceCode: "01" },
  { code: "00028", name: "Kim Mã", fullName: "Phường Kim Mã (Ba Đình)", provinceCode: "01" },
  { code: "00031", name: "Giảng Võ", fullName: "Phường Giảng Võ (Ba Đình)", provinceCode: "01" },
  { code: "00073", name: "Cửa Nam", fullName: "Phường Cửa Nam (Hoàn Kiếm)", provinceCode: "01" },
  { code: "00079", name: "Tràng Tiền", fullName: "Phường Tràng Tiền (Hoàn Kiếm)", provinceCode: "01" },
  { code: "00100", name: "Quảng An", fullName: "Phường Quảng An (Tây Hồ)", provinceCode: "01" },
  // Hà Đông & Hoàng Mai
  { code: "09541", name: "Mộ Lao", fullName: "Phường Mộ Lao (Hà Đông)", provinceCode: "01" },
  { code: "09544", name: "Vạn Phúc", fullName: "Phường Vạn Phúc (Hà Đông)", provinceCode: "01" },
  { code: "09542", name: "Văn Quán", fullName: "Phường Văn Quán (Hà Đông)", provinceCode: "01" },
  { code: "00337", name: "Hoàng Liệt", fullName: "Phường Hoàng Liệt (Hoàng Mai)", provinceCode: "01" },
  { code: "00307", name: "Định Công", fullName: "Phường Định Công (Hoàng Mai)", provinceCode: "01" },
];

const FALLBACK_WARDS_HCM: WardDto[] = [
  // Quận 1, 3 & TP. Thủ Đức mới
  { code: "26734", name: "Bến Nghé", fullName: "Phường Bến Nghé (Quận 1)", provinceCode: "79" },
  { code: "26737", name: "Bến Thành", fullName: "Phường Bến Thành (Quận 1)", provinceCode: "79" },
  { code: "26740", name: "Nguyễn Thái Bình", fullName: "Phường Nguyễn Thái Bình (Quận 1)", provinceCode: "79" },
  { code: "26830", name: "Thảo Điền", fullName: "Phường Thảo Điền (TP. Thủ Đức)", provinceCode: "79" },
  { code: "26833", name: "An Phú", fullName: "Phường An Phú (TP. Thủ Đức)", provinceCode: "79" },
  { code: "26836", name: "Hiệp Phú", fullName: "Phường Hiệp Phú (TP. Thủ Đức)", provinceCode: "79" },
  { code: "26839", name: "Tăng Nhơn Phú A", fullName: "Phường Tăng Nhơn Phú A (TP. Thủ Đức)", provinceCode: "79" },
  // Bình Thạnh, Gò Vấp, Tân Bình
  { code: "27145", name: "Phường 25", fullName: "Phường 25 (Bình Thạnh)", provinceCode: "79" },
  { code: "27148", name: "Phường 26", fullName: "Phường 26 (Bình Thạnh)", provinceCode: "79" },
  { code: "27139", name: "Phường 19", fullName: "Phường 19 (Bình Thạnh)", provinceCode: "79" },
  { code: "27202", name: "Phường 01", fullName: "Phường 01 (Gò Vấp)", provinceCode: "79" },
  { code: "27205", name: "Phường 03", fullName: "Phường 03 (Gò Vấp)", provinceCode: "79" },
  { code: "27118", name: "Phường 02", fullName: "Phường 02 (Tân Bình)", provinceCode: "79" },
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
  const [isEditPropOpen, setIsEditPropOpen] = useState(false);
  const [isCreateRoomOpen, setIsCreateRoomOpen] = useState(false);
  const [isEditRoomOpen, setIsEditRoomOpen] = useState(false);
  const [isPickerOpenForCreate, setIsPickerOpenForCreate] = useState(false);
  const [isPickerOpenForEdit, setIsPickerOpenForEdit] = useState(false);
  const [isShowPropertyMap, setIsShowPropertyMap] = useState(false);

  // Form: Create Property
  const [newPropName, setNewPropName] = useState("");
  const [newPropType, setNewPropType] = useState("BOARDING_HOUSE");
  const [newPropProvince, setNewPropProvince] = useState("01"); // Hà Nội
  const [newPropWard, setNewPropWard] = useState("");
  const [newPropAddress, setNewPropAddress] = useState("");
  const [newPropLat, setNewPropLat] = useState<number | null>(null);
  const [newPropLng, setNewPropLng] = useState<number | null>(null);
  const [savingProp, setSavingProp] = useState(false);

  // Form: Edit Property
  const [editPropName, setEditPropName] = useState("");
  const [editPropType, setEditPropType] = useState("BOARDING_HOUSE");
  const [editPropProvince, setEditPropProvince] = useState("01");
  const [editPropWards, setEditPropWards] = useState<WardDto[]>([]);
  const [editPropWard, setEditPropWard] = useState("");
  const [editPropAddress, setEditPropAddress] = useState("");
  const [editPropLat, setEditPropLat] = useState<number | null>(null);
  const [editPropLng, setEditPropLng] = useState<number | null>(null);
  const [updatingProp, setUpdatingProp] = useState(false);

  // Form: Create Room
  const [newRoomNumber, setNewRoomNumber] = useState("");
  const [newRoomFloor, setNewRoomFloor] = useState(1);
  const [newRoomArea, setNewRoomArea] = useState(25);
  const [newRoomType, setNewRoomType] = useState("STUDIO");
  const [newRoomMaxOccupants, setNewRoomMaxOccupants] = useState(2);
  const [newRoomAvailableFrom, setNewRoomAvailableFrom] = useState(
    () => new Date().toISOString().split("T")[0]
  );
  const [newRoomAmenities, setNewRoomAmenities] = useState<number[]>([]);
  const [savingRoom, setSavingRoom] = useState(false);

  // Form: Edit Room
  const [editingRoomId, setEditingRoomId] = useState<number | null>(null);
  const [editRoomNumber, setEditRoomNumber] = useState("");
  const [editRoomFloor, setEditRoomFloor] = useState(1);
  const [editRoomArea, setEditRoomArea] = useState(25);
  const [editRoomType, setEditRoomType] = useState("STUDIO");
  const [editRoomMaxOccupants, setEditRoomMaxOccupants] = useState(2);
  const [editRoomAvailableFrom, setEditRoomAvailableFrom] = useState("");
  const [editRoomAmenities, setEditRoomAmenities] = useState<number[]>([]);
  const [updatingRoom, setUpdatingRoom] = useState(false);

  // Load catalogs (Provinces & Amenities)
  const [loadingCatalogs, setLoadingCatalogs] = useState(false);
  const [loadingWards, setLoadingWards] = useState(false);
  const [loadingEditWards, setLoadingEditWards] = useState(false);

  const loadCatalogs = useCallback(async () => {
    setLoadingCatalogs(true);
    try {
      const [provRes, amenRes] = await Promise.all([
        catalogsApi.getProvinces(),
        catalogsApi.getAmenities(),
      ]);
      if (provRes.isSuccess && provRes.data && provRes.data.length > 0) {
        setProvinces(provRes.data);
      }
      if (amenRes.isSuccess && amenRes.data && amenRes.data.length > 0) {
        setAmenities(amenRes.data);
      }
    } catch (err) {
      console.warn("Could not load catalogs from backend", err);
    } finally {
      setLoadingCatalogs(false);
    }
  }, []);

  useEffect(() => {
    loadCatalogs();
  }, [loadCatalogs]);

  // Load wards for Create Property Form
  const loadWardsForProvince = useCallback(async (provinceCode: string) => {
    if (!provinceCode) {
      setWards([]);
      setNewPropWard("");
      return;
    }
    setLoadingWards(true);
    try {
      const wardRes = await catalogsApi.getWards(provinceCode);
      if (wardRes.isSuccess && wardRes.data && wardRes.data.length > 0) {
        const wardList = wardRes.data;
        setWards(wardList);
        setNewPropWard((prev) => {
          const exists = wardList.some((w) => w.code === prev);
          return exists ? prev : wardList[0].code;
        });
        return;
      }
    } catch (err) {
      console.warn("Could not load wards from backend", err);
    } finally {
      setLoadingWards(false);
    }

    // Fallback if backend returned empty or network lagged
    const fallbackList =
      provinceCode === "01"
        ? FALLBACK_WARDS_HN
        : provinceCode === "79"
        ? FALLBACK_WARDS_HCM
        : [];
    setWards(fallbackList);
    setNewPropWard((prev) => {
      const exists = fallbackList.some((w) => w.code === prev);
      return exists ? prev : fallbackList.length > 0 ? fallbackList[0].code : "";
    });
  }, []);

  useEffect(() => {
    loadWardsForProvince(newPropProvince);
  }, [newPropProvince, loadWardsForProvince]);

  // Load wards for Edit Property Form
  const loadEditWardsForProvince = useCallback(async (provinceCode: string) => {
    if (!provinceCode) {
      setEditPropWards([]);
      return;
    }
    setLoadingEditWards(true);
    try {
      const wardRes = await catalogsApi.getWards(provinceCode);
      if (wardRes.isSuccess && wardRes.data && wardRes.data.length > 0) {
        const wardList = wardRes.data;
        setEditPropWards(wardList);
        setEditPropWard((prev) => {
          const exists = wardList.some((w) => w.code === prev);
          return exists ? prev : wardList[0].code;
        });
        return;
      }
    } catch (err) {
      console.warn("Could not load edit wards from backend", err);
    } finally {
      setLoadingEditWards(false);
    }

    const fallbackList =
      provinceCode === "01"
        ? FALLBACK_WARDS_HN
        : provinceCode === "79"
        ? FALLBACK_WARDS_HCM
        : [];
    setEditPropWards(fallbackList);
  }, []);

  useEffect(() => {
    if (isEditPropOpen && editPropProvince) {
      loadEditWardsForProvince(editPropProvince);
    }
  }, [editPropProvince, isEditPropOpen, loadEditWardsForProvince]);

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
    if (!newPropName.trim() || !newPropAddress.trim()) {
      showToast("Vui lòng điền đầy đủ tên và địa chỉ tòa nhà!", "error");
      return;
    }
    const finalWard = newPropWard || (wards.length > 0 ? wards[0].code : "00001");
    if (!finalWard) {
      showToast("Vui lòng chọn Phường / Xã!", "error");
      return;
    }
    setSavingProp(true);
    try {
      const res = await propertiesApi.createProperty({
        name: newPropName.trim(),
        propertyType: newPropType,
        provinceCode: newPropProvince,
        wardCode: finalWard,
        addressDetail: newPropAddress.trim(),
        latitude: newPropLat,
        longitude: newPropLng,
      });

      if (res.isSuccess && res.data) {
        showToast("Tạo mới bất động sản thành công!", "success");
        setIsCreatePropOpen(false);
        setNewPropName("");
        setNewPropAddress("");
        setNewPropLat(null);
        setNewPropLng(null);
        await loadProperties();
        setSelectedPropertyId(res.data.id);
      } else {
        const detailMsg = res.error?.details
          ?.map((d) => (d.field ? `${d.field}: ${d.message}` : d.message))
          .join(", ");
        showToast(
          detailMsg
            ? `${res.error?.message || "Tạo bất động sản thất bại"} (${detailMsg})`
            : res.error?.message || "Tạo bất động sản thất bại",
          "error"
        );
      }
    } catch {
      showToast("Lỗi kết nối máy chủ", "error");
    } finally {
      setSavingProp(false);
    }
  };


  // Open Edit Property Modal
  const handleOpenEditProperty = async (prop: PropertyDto) => {
    setEditPropName(prop.name);
    setEditPropType(prop.propertyType || "BOARDING_HOUSE");
    setEditPropProvince(prop.provinceCode || "01");
    setEditPropWard(prop.wardCode || "");
    setEditPropAddress(prop.addressDetail || "");
    setEditPropLat(prop.latitude ?? null);
    setEditPropLng(prop.longitude ?? null);
    setIsEditPropOpen(true);
    try {
      const res = await propertiesApi.getPropertyById(prop.id);
      if (res.isSuccess && res.data) {
        setEditPropName(res.data.name);
        setEditPropType(res.data.propertyType);
        setEditPropProvince(res.data.provinceCode);
        setEditPropWard(res.data.wardCode);
        setEditPropAddress(res.data.addressDetail);
        setEditPropLat(res.data.latitude ?? null);
        setEditPropLng(res.data.longitude ?? null);
      }
    } catch {
      // fallback to local prop
    }
  };

  // Handle Update Property (PATCH /api/v1/properties/{id})
  const handleUpdateProperty = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPropertyId) return;
    if (!editPropName.trim() || !editPropAddress.trim()) {
      showToast("Vui lòng nhập đầy đủ tên và địa chỉ tòa nhà!", "error");
      return;
    }
    setUpdatingProp(true);
    try {
      const res = await propertiesApi.updateProperty(selectedPropertyId, {
        name: editPropName.trim(),
        propertyType: editPropType,
        provinceCode: editPropProvince,
        wardCode: editPropWard,
        addressDetail: editPropAddress.trim(),
        latitude: editPropLat,
        longitude: editPropLng,
      });
      if (res.isSuccess && res.data) {
        showToast("Đã cập nhật thông tin tòa nhà thành công!", "success");
        setIsEditPropOpen(false);
        await loadProperties();
      } else {
        showToast(res.error?.message || "Cập nhật tòa nhà thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối máy chủ", "error");
    } finally {
      setUpdatingProp(false);
    }
  };

  // Handle Create Room
  const handleCreateRoom = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPropertyId) {
      showToast("Vui lòng chọn bất động sản trước khi tạo phòng!", "error");
      return;
    }
    if (!newRoomNumber.trim()) {
      showToast("Vui lòng nhập số hiệu phòng (ví dụ: P.201)!", "error");
      return;
    }
    setSavingRoom(true);
    try {
      const res = await propertiesApi.createRoom(selectedPropertyId, {
        roomNumber: newRoomNumber.trim(),
        floor: Number(newRoomFloor),
        areaSqm: Number(newRoomArea),
        roomType: newRoomType,
        maxOccupants: Number(newRoomMaxOccupants),
        availableFrom: newRoomAvailableFrom || null,
        amenityIds: newRoomAmenities,
      });

      if (res.isSuccess && res.data) {
        showToast(`Tạo phòng ${newRoomNumber} thành công!`, "success");
        setIsCreateRoomOpen(false);
        setNewRoomNumber("");
        setNewRoomAmenities([]);
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

  // Open Edit Room Modal (GET /api/v1/properties/{propertyId}/rooms/{roomId})
  const handleOpenEditRoom = async (room: RoomDto) => {
    if (!selectedPropertyId) return;
    setEditingRoomId(room.id);
    setEditRoomNumber(room.roomNumber);
    setEditRoomFloor(room.floor);
    setEditRoomArea(room.areaSqm);
    setEditRoomType(room.roomType);
    setEditRoomMaxOccupants(room.maxOccupants);
    setEditRoomAvailableFrom(
      room.availableFrom ? room.availableFrom.split("T")[0] : new Date().toISOString().split("T")[0]
    );
    setEditRoomAmenities(room.amenityIds || []);
    setIsEditRoomOpen(true);

    try {
      const res = await propertiesApi.getRoomById(selectedPropertyId, room.id);
      if (res.isSuccess && res.data) {
        setEditRoomNumber(res.data.roomNumber);
        setEditRoomFloor(res.data.floor);
        setEditRoomArea(res.data.areaSqm);
        setEditRoomType(res.data.roomType);
        setEditRoomMaxOccupants(res.data.maxOccupants);
        setEditRoomAvailableFrom(
          res.data.availableFrom
            ? res.data.availableFrom.split("T")[0]
            : new Date().toISOString().split("T")[0]
        );
        setEditRoomAmenities(res.data.amenityIds || []);
      }
    } catch {
      // use local room state
    }
  };

  // Handle Update Room (PATCH /api/v1/properties/{propertyId}/rooms/{roomId})
  const handleUpdateRoom = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPropertyId || !editingRoomId) return;
    if (!editRoomNumber.trim()) {
      showToast("Vui lòng nhập số hiệu phòng!", "error");
      return;
    }
    setUpdatingRoom(true);
    try {
      const res = await propertiesApi.updateRoom(selectedPropertyId, editingRoomId, {
        roomNumber: editRoomNumber.trim(),
        floor: Number(editRoomFloor),
        areaSqm: Number(editRoomArea),
        roomType: editRoomType,
        maxOccupants: Number(editRoomMaxOccupants),
        availableFrom: editRoomAvailableFrom || null,
        amenityIds: editRoomAmenities,
      });
      if (res.isSuccess && res.data) {
        showToast(`Đã cập nhật thông tin phòng ${editRoomNumber} thành công!`, "success");
        setIsEditRoomOpen(false);
        setEditingRoomId(null);
        loadRooms(selectedPropertyId);
      } else {
        showToast(res.error?.message || "Cập nhật phòng thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối máy chủ", "error");
    } finally {
      setUpdatingRoom(false);
    }
  };

  const toggleAmenitySelection = (id: number, isEdit = false) => {
    if (isEdit) {
      setEditRoomAmenities((prev) =>
        prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]
      );
    } else {
      setNewRoomAmenities((prev) =>
        prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]
      );
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

  const availableProvinces = provinces.length > 0 ? provinces : FALLBACK_PROVINCES;
  const availableWards =
    wards.length > 0
      ? wards
      : newPropProvince === "01"
      ? FALLBACK_WARDS_HN
      : newPropProvince === "79"
      ? FALLBACK_WARDS_HCM
      : [];
  const availableEditWards =
    editPropWards.length > 0
      ? editPropWards
      : editPropProvince === "01"
      ? FALLBACK_WARDS_HN
      : editPropProvince === "79"
      ? FALLBACK_WARDS_HCM
      : [];

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Building2 className="w-4 h-4" />
            <span>Quản Lý Bất Động Sản & Phòng Cho Thuê</span>
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
              <label className="text-[11px] font-bold text-slate-500 uppercase tracking-wider block mb-1">
                Đang xem bất động sản:
              </label>
              <div className="relative inline-flex items-center w-full max-w-sm sm:max-w-md">
                <select
                  value={selectedPropertyId || ""}
                  onChange={(e) => setSelectedPropertyId(Number(e.target.value))}
                  className="w-full appearance-none bg-slate-50 hover:bg-slate-100/80 active:bg-slate-100 text-slate-900 font-bold text-sm pl-3 pr-9 py-2 rounded-xl border border-slate-200/90 shadow-2xs focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 cursor-pointer select-none transition-all truncate"
                >
                  {properties.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name} ({p.addressDetail})
                    </option>
                  ))}
                </select>
                <ChevronDown className="w-4 h-4 text-slate-500 absolute right-3 pointer-events-none" />
              </div>
            </div>
          </div>

          {selectedProperty && (
            <div className="space-y-3">
              <div className="flex items-center gap-3 text-xs text-slate-500 flex-wrap">
                <span className="flex items-center gap-1">
                  <MapPin className="w-3.5 h-3.5 text-slate-400" />
                  {selectedProperty.addressDetail}
                  {selectedProperty.latitude != null && selectedProperty.longitude != null ? (
                    <button
                      type="button"
                      onClick={() => setIsShowPropertyMap((prev) => !prev)}
                      className="text-[10px] text-emerald-700 bg-emerald-50 hover:bg-emerald-100 px-2 py-0.5 rounded-md font-semibold ml-1 inline-flex items-center gap-1 transition-colors border border-emerald-200/60"
                      title="Bấm để xem/ẩn bản đồ vị trí"
                    >
                      <Compass className="w-3 h-3 text-emerald-600" />
                      <span>GPS: {selectedProperty.latitude.toFixed(4)}, {selectedProperty.longitude.toFixed(4)} ({isShowPropertyMap ? "Ẩn bản đồ" : "Xem bản đồ"})</span>
                    </button>
                  ) : (
                    <button
                      type="button"
                      onClick={() => handleOpenEditProperty(selectedProperty)}
                      className="text-[10px] text-amber-700 bg-amber-50 hover:bg-amber-100 px-2 py-0.5 rounded-md font-semibold ml-1 inline-flex items-center gap-1 transition-colors border border-amber-200/60"
                      title="Tòa nhà này chưa có tọa độ GPS, bấm để ghim vị trí"
                    >
                      <Compass className="w-3 h-3 text-amber-600" />
                      <span>Chưa có GPS (Bấm ghim ngay)</span>
                    </button>
                  )}
                </span>
                <span>•</span>
                <span className="font-bold text-slate-700">
                  {rooms.length} phòng trực thuộc
                </span>
                <span>•</span>
                <button
                  type="button"
                  onClick={() => handleOpenEditProperty(selectedProperty)}
                  className="text-slate-700 hover:text-blue-600 font-bold flex items-center gap-1 transition-colors"
                >
                  <Edit2 className="w-3.5 h-3.5" />
                  <span>Sửa tòa nhà</span>
                </button>
                <span>•</span>
                <Link
                  href={`/landlord/utilities?propertyId=${selectedProperty.id}`}
                  className="text-blue-600 font-bold hover:underline flex items-center gap-1"
                >
                  <SlidersHorizontal className="w-3.5 h-3.5" />
                  <span>Cấu hình biểu phí điện nước</span>
                </Link>
              </div>

              {/* Collapsible Property Map Viewer */}
              {isShowPropertyMap && selectedProperty.latitude != null && selectedProperty.longitude != null && (
                <div className="p-3 bg-slate-50 border border-slate-200/80 rounded-2xl">
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-xs font-bold text-slate-800 flex items-center gap-1.5">
                      <MapPin className="w-3.5 h-3.5 text-blue-600" />
                      <span>Vị trí ghim tòa nhà {selectedProperty.name}</span>
                    </span>
                    <button
                      type="button"
                      onClick={() => setIsShowPropertyMap(false)}
                      className="text-[11px] text-slate-500 hover:text-slate-700"
                    >
                      Đóng bản đồ
                    </button>
                  </div>
                  <MapLocationViewer
                    latitude={selectedProperty.latitude}
                    longitude={selectedProperty.longitude}
                    title={selectedProperty.name}
                    address={selectedProperty.addressDetail}
                    height={260}
                    heightClassName="h-[260px]"
                  />
                </div>
              )}
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
                <th className="p-4">Ngày trống & Tiện ích</th>
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
                rooms.map((room) => {
                  const roomTypeLabel =
                    ROOM_TYPES.find((t) => t.value === room.roomType)?.label || room.roomType;
                  const roomAmenityNames = (room.amenityIds || [])
                    .map((id) => amenities.find((a) => a.id === id)?.name)
                    .filter(Boolean) as string[];
                  return (
                    <tr key={room.id} className="hover:bg-slate-50/60 transition-colors">
                      <td className="p-4">
                        <div className="font-bold text-slate-900 text-sm">Phòng {room.roomNumber}</div>
                        <div className="text-[11px] text-slate-400">Tầng {room.floor} • Mã #{room.id}</div>
                      </td>
                      <td className="p-4">
                        <div className="font-bold text-slate-800">{roomTypeLabel}</div>
                        <div className="text-[11px] text-slate-500">{room.areaSqm} m²</div>
                      </td>
                      <td className="p-4 font-medium text-slate-700">{room.maxOccupants} người</td>
                      <td className="p-4">
                        <div className="text-xs font-bold text-slate-700">
                          Trống từ:{" "}
                          <span className="text-blue-600">
                            {room.availableFrom
                              ? new Date(room.availableFrom).toLocaleDateString("vi-VN")
                              : "Sẵn sàng"}
                          </span>
                        </div>
                        {roomAmenityNames.length > 0 ? (
                          <div className="flex flex-wrap gap-1 mt-1">
                            {roomAmenityNames.slice(0, 3).map((name) => (
                              <span
                                key={name}
                                className="px-1.5 py-0.5 rounded bg-slate-100 text-slate-600 text-[10px] font-semibold"
                              >
                                {name}
                              </span>
                            ))}
                            {roomAmenityNames.length > 3 && (
                              <span className="px-1.5 py-0.5 rounded bg-blue-50 text-blue-600 text-[10px] font-bold">
                                +{roomAmenityNames.length - 3}
                              </span>
                            )}
                          </div>
                        ) : (
                          <span className="text-[11px] text-slate-400">
                            {(room.amenityIds || []).length > 0
                              ? `${room.amenityIds.length} tiện ích`
                              : "Chưa chọn tiện ích"}
                          </span>
                        )}
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
                          <button
                            type="button"
                            onClick={() => handleOpenEditRoom(room)}
                            className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg bg-slate-100 text-slate-700 hover:bg-slate-200 font-bold text-[11px] transition-colors"
                          >
                            <Edit2 className="w-3.5 h-3.5" />
                            <span>Sửa phòng</span>
                          </button>
                          <Link
                            href={`/landlord/listings?roomId=${room.id}`}
                            className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg bg-blue-50 text-blue-700 hover:bg-blue-100 font-bold text-[11px] transition-colors"
                          >
                            <Sparkles className="w-3.5 h-3.5" />
                            <span>Đăng tin</span>
                          </Link>
                        </div>
                      </td>
                    </tr>
                  );
                })
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
                <div className="flex items-center justify-between mb-1">
                  <label className="block text-xs font-bold text-slate-700 uppercase">
                    Tỉnh / Thành Phố *
                  </label>
                  {provinces.length === 0 && (
                    <button
                      type="button"
                      onClick={() => loadCatalogs()}
                      disabled={loadingCatalogs}
                      className="text-[10px] text-blue-600 hover:underline inline-flex items-center gap-0.5"
                    >
                      <RefreshCw className={cn("w-2.5 h-2.5", loadingCatalogs && "animate-spin")} />
                      <span>{loadingCatalogs ? "Đang tải..." : "Tải lại"}</span>
                    </button>
                  )}
                </div>
                <select
                  value={newPropProvince}
                  onChange={(e) => setNewPropProvince(e.target.value)}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  {availableProvinces.map((p) => (
                    <option key={p.code} value={p.code}>
                      {p.fullName || p.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div>
              <div className="flex items-center justify-between mb-1">
                <label className="block text-xs font-bold text-slate-700 uppercase">
                  Phường / Xã *
                </label>
                {loadingWards && (
                  <span className="text-[10px] text-blue-600 font-medium inline-flex items-center gap-1">
                    <RefreshCw className="w-2.5 h-2.5 animate-spin" />
                    <span>Đang tải...</span>
                  </span>
                )}
              </div>
              <SearchableSelect
                value={newPropWard}
                onChange={(code) => setNewPropWard(code)}
                options={availableWards.map((w) => ({
                  value: w.code,
                  label: w.fullName || w.name,
                }))}
                placeholder={loadingWards ? "Đang tải danh sách phường/xã..." : "Chọn phường / xã..."}
                searchPlaceholder="Tìm nhanh phường / xã (VD: Văn Miếu, Dịch Vọng...)"
                loading={loadingWards}
                disabled={loadingWards || availableWards.length === 0}
                required
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Địa chỉ chi tiết (Số nhà, ngõ, tên đường) *
              </label>
              <AddressAutocompleteInput
                value={newPropAddress}
                onChange={(val) => setNewPropAddress(val)}
                onSelectCoords={(lat, lng) => {
                  setNewPropLat(lat);
                  setNewPropLng(lng);
                }}
                provinceName={
                  availableProvinces.find((p) => p.code === newPropProvince)?.name || "Hà Nội"
                }
                wardName={
                  availableWards.find((w) => w.code === newPropWard)?.name || ""
                }
                placeholder="VD: Số 18 Ngõ 68 Cầu Giấy hoặc gõ tên đường..."
                required
              />
            </div>

            {/* GPS Location Picker Block */}
            <div className="p-3 bg-slate-50 border border-slate-200/80 rounded-xl space-y-2">
              <div className="flex items-center justify-between">
                <label className="text-[11px] font-bold text-slate-700 uppercase flex items-center gap-1.5">
                  <MapPin className="w-3.5 h-3.5 text-blue-600" />
                  <span>Tọa độ vị trí GPS (Khuyến nghị)</span>
                </label>
                <button
                  type="button"
                  onClick={() => setIsPickerOpenForCreate(true)}
                  className="text-xs text-blue-600 font-bold hover:underline inline-flex items-center gap-1"
                >
                  <Compass className="w-3.5 h-3.5" />
                  <span>{newPropLat && newPropLng ? "Chỉnh sửa ghim" : "Ghim trên bản đồ"}</span>
                </button>
              </div>

              {newPropLat && newPropLng ? (
                <div className="flex items-center justify-between bg-white px-3 py-2 rounded-lg border border-slate-200 text-xs">
                  <div className="font-mono text-slate-700">
                    <span className="text-emerald-600 font-bold mr-1.5">Đã ghim:</span>
                    {newPropLat.toFixed(6)}, {newPropLng.toFixed(6)}
                  </div>
                  <button
                    type="button"
                    onClick={() => {
                      setNewPropLat(null);
                      setNewPropLng(null);
                    }}
                    className="text-[11px] text-red-500 hover:underline font-medium"
                  >
                    Xóa tọa độ
                  </button>
                </div>
              ) : (
                <p className="text-[11px] text-slate-500 leading-relaxed">
                  Nếu chưa ghim, hệ thống sẽ tự động định vị theo tên đường & phường/xã. Bạn có thể bấm <strong>&quot;Ghim trên bản đồ&quot;</strong> để chọn chính xác số nhà / ngõ trọ.
                </p>
              )}
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
                Ngày bắt đầu trống (Available From) *
              </label>
              <input
                type="date"
                required
                value={newRoomAvailableFrom}
                onChange={(e) => setNewRoomAvailableFrom(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>

            {amenities.length > 0 && (
              <div>
                <div className="flex items-center justify-between mb-2">
                  <label className="block text-xs font-bold text-slate-700 uppercase">
                    Tiện ích trong phòng
                  </label>
                  <span className="text-[11px] font-bold text-blue-700 bg-blue-50 px-2.5 py-0.5 rounded-full border border-blue-200">
                    {newRoomAmenities.length} đã chọn
                  </span>
                </div>
                <div className="flex flex-wrap gap-2 max-h-48 overflow-y-auto p-3 rounded-2xl border border-slate-200/90 bg-slate-50/50">
                  {amenities.map((amenity) => {
                    const checked = newRoomAmenities.includes(amenity.id);
                    return (
                      <button
                        type="button"
                        key={amenity.id}
                        onClick={() => toggleAmenitySelection(amenity.id, false)}
                        className={cn(
                          "px-3 py-1.5 rounded-xl text-xs font-bold border transition-all flex items-center gap-1.5 shadow-2xs",
                          checked
                            ? "bg-blue-600 text-white border-blue-600 shadow-sm shadow-blue-500/20"
                            : "bg-white text-slate-700 border-slate-200/90 hover:border-slate-300 hover:bg-slate-100/60"
                        )}
                      >
                        {checked && <Check className="w-3.5 h-3.5 shrink-0" />}
                        <span>{formatAmenityName(amenity.name)}</span>
                      </button>
                    );
                  })}
                </div>
              </div>
            )}

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

      {/* Modal: Edit Property (PATCH /api/v1/properties/{id}) */}
      {isEditPropOpen && (
        <Modal
          isOpen={true}
          onClose={() => setIsEditPropOpen(false)}
          title="Cập Nhật Thông Tin Tòa Nhà / Dãy Trọ"
          maxWidth="md"
        >
          <form onSubmit={handleUpdateProperty} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Tên Tòa Nhà / Dãy Trọ *
              </label>
              <input
                type="text"
                required
                value={editPropName}
                onChange={(e) => setEditPropName(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Mô hình bất động sản
                </label>
                <select
                  value={editPropType}
                  onChange={(e) => setEditPropType(e.target.value)}
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
                <div className="flex items-center justify-between mb-1">
                  <label className="block text-xs font-bold text-slate-700 uppercase">
                    Tỉnh / Thành Phố *
                  </label>
                  {provinces.length === 0 && (
                    <button
                      type="button"
                      onClick={() => loadCatalogs()}
                      disabled={loadingCatalogs}
                      className="text-[10px] text-blue-600 hover:underline inline-flex items-center gap-0.5"
                    >
                      <RefreshCw className={cn("w-2.5 h-2.5", loadingCatalogs && "animate-spin")} />
                      <span>{loadingCatalogs ? "Đang tải..." : "Tải lại"}</span>
                    </button>
                  )}
                </div>
                <select
                  value={editPropProvince}
                  onChange={(e) => setEditPropProvince(e.target.value)}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  {availableProvinces.map((p) => (
                    <option key={p.code} value={p.code}>
                      {p.fullName || p.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div>
              <div className="flex items-center justify-between mb-1">
                <label className="block text-xs font-bold text-slate-700 uppercase">
                  Phường / Xã *
                </label>
                {loadingEditWards && (
                  <span className="text-[10px] text-blue-600 font-medium inline-flex items-center gap-1">
                    <RefreshCw className="w-2.5 h-2.5 animate-spin" />
                    <span>Đang tải...</span>
                  </span>
                )}
              </div>
              <SearchableSelect
                value={editPropWard}
                onChange={(code) => setEditPropWard(code)}
                options={availableEditWards.map((w) => ({
                  value: w.code,
                  label: w.fullName || w.name,
                }))}
                placeholder={loadingEditWards ? "Đang tải danh sách phường/xã..." : "Chọn phường / xã..."}
                searchPlaceholder="Tìm nhanh phường / xã (VD: Bến Nghé, Thảo Điền...)"
                loading={loadingEditWards}
                disabled={loadingEditWards || availableEditWards.length === 0}
                required
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Địa chỉ chi tiết (Số nhà, ngõ, tên đường) *
              </label>
              <AddressAutocompleteInput
                value={editPropAddress}
                onChange={(val) => setEditPropAddress(val)}
                onSelectCoords={(lat, lng) => {
                  setEditPropLat(lat);
                  setEditPropLng(lng);
                }}
                provinceName={
                  availableProvinces.find((p) => p.code === editPropProvince)?.name || "Hà Nội"
                }
                wardName={
                  availableEditWards.find((w) => w.code === editPropWard)?.name || ""
                }
                placeholder="VD: Số 18 Ngõ 68 Cầu Giấy hoặc gõ tên đường..."
                required
              />
            </div>

            {/* GPS Location Picker Block for Edit */}
            <div className="p-3 bg-slate-50 border border-slate-200/80 rounded-xl space-y-2">
              <div className="flex items-center justify-between">
                <label className="text-[11px] font-bold text-slate-700 uppercase flex items-center gap-1.5">
                  <MapPin className="w-3.5 h-3.5 text-blue-600" />
                  <span>Tọa độ vị trí GPS (Khuyến nghị)</span>
                </label>
                <button
                  type="button"
                  onClick={() => setIsPickerOpenForEdit(true)}
                  className="text-xs text-blue-600 font-bold hover:underline inline-flex items-center gap-1"
                >
                  <Compass className="w-3.5 h-3.5" />
                  <span>{editPropLat && editPropLng ? "Chỉnh sửa ghim" : "Ghim trên bản đồ"}</span>
                </button>
              </div>

              {editPropLat && editPropLng ? (
                <div className="flex items-center justify-between bg-white px-3 py-2 rounded-lg border border-slate-200 text-xs">
                  <div className="font-mono text-slate-700">
                    <span className="text-emerald-600 font-bold mr-1.5">Đã ghim:</span>
                    {editPropLat.toFixed(6)}, {editPropLng.toFixed(6)}
                  </div>
                  <button
                    type="button"
                    onClick={() => {
                      setEditPropLat(null);
                      setEditPropLng(null);
                    }}
                    className="text-[11px] text-red-500 hover:underline font-medium"
                  >
                    Xóa tọa độ
                  </button>
                </div>
              ) : (
                <p className="text-[11px] text-slate-500 leading-relaxed">
                  Tòa nhà hiện chưa có tọa độ ghim tay. Bấm <strong>&quot;Ghim trên bản đồ&quot;</strong> để chọn vị trí chính xác trên bản đồ.
                </p>
              )}
            </div>

            <div className="flex gap-3 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setIsEditPropOpen(false)}
                className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={updatingProp}
                className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 disabled:opacity-50"
              >
                {updatingProp ? "Đang cập nhật..." : "Lưu thay đổi"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Modal: Edit Room (PATCH /api/v1/properties/{propertyId}/rooms/{roomId}) */}
      {isEditRoomOpen && (
        <Modal
          isOpen={true}
          onClose={() => setIsEditRoomOpen(false)}
          title={`Chỉnh Sửa Thông Tin Phòng ${editRoomNumber}`}
          maxWidth="lg"
        >
          <form onSubmit={handleUpdateRoom} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Số hiệu phòng *
                </label>
                <input
                  type="text"
                  required
                  value={editRoomNumber}
                  onChange={(e) => setEditRoomNumber(e.target.value)}
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
                  value={editRoomFloor}
                  onChange={(e) => setEditRoomFloor(Number(e.target.value))}
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
                  value={editRoomType}
                  onChange={(e) => setEditRoomType(e.target.value)}
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
                  value={editRoomArea}
                  onChange={(e) => setEditRoomArea(Number(e.target.value))}
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
                  value={editRoomMaxOccupants}
                  onChange={(e) => setEditRoomMaxOccupants(Number(e.target.value))}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Ngày bắt đầu trống (Available From) *
              </label>
              <input
                type="date"
                required
                value={editRoomAvailableFrom}
                onChange={(e) => setEditRoomAvailableFrom(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>

            {amenities.length > 0 && (
              <div>
                <div className="flex items-center justify-between mb-2">
                  <label className="block text-xs font-bold text-slate-700 uppercase">
                    Tiện ích trong phòng
                  </label>
                  <span className="text-[11px] font-bold text-blue-700 bg-blue-50 px-2.5 py-0.5 rounded-full border border-blue-200">
                    {editRoomAmenities.length} đã chọn
                  </span>
                </div>
                <div className="flex flex-wrap gap-2 max-h-48 overflow-y-auto p-3 rounded-2xl border border-slate-200/90 bg-slate-50/50">
                  {amenities.map((amenity) => {
                    const checked = editRoomAmenities.includes(amenity.id);
                    return (
                      <button
                        type="button"
                        key={amenity.id}
                        onClick={() => toggleAmenitySelection(amenity.id, true)}
                        className={cn(
                          "px-3 py-1.5 rounded-xl text-xs font-bold border transition-all flex items-center gap-1.5 shadow-2xs",
                          checked
                            ? "bg-blue-600 text-white border-blue-600 shadow-sm shadow-blue-500/20"
                            : "bg-white text-slate-700 border-slate-200/90 hover:border-slate-300 hover:bg-slate-100/60"
                        )}
                      >
                        {checked && <Check className="w-3.5 h-3.5 shrink-0" />}
                        <span>{formatAmenityName(amenity.name)}</span>
                      </button>
                    );
                  })}
                </div>
              </div>
            )}

            <div className="flex gap-3 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setIsEditRoomOpen(false)}
                className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={updatingRoom}
                className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 disabled:opacity-50"
              >
                {updatingRoom ? "Đang cập nhật..." : "Lưu thay đổi phòng"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Map Location Picker Modal for Create Property */}
      {isPickerOpenForCreate && (
        <MapLocationPickerModal
          isOpen={true}
          onClose={() => setIsPickerOpenForCreate(false)}
          initialLatitude={newPropLat}
          initialLongitude={newPropLng}
          addressContext={[
            newPropAddress,
            availableWards.find((w) => w.code === newPropWard)?.name,
            availableProvinces.find((p) => p.code === newPropProvince)?.name,
            "Việt Nam",
          ]
            .filter(Boolean)
            .join(", ")}
          onConfirm={({ latitude, longitude }) => {
            setNewPropLat(latitude);
            setNewPropLng(longitude);
            showToast(`Đã ghim tọa độ GPS: ${latitude}, ${longitude}`, "success");
          }}
        />
      )}

      {/* Map Location Picker Modal for Edit Property */}
      {isPickerOpenForEdit && (
        <MapLocationPickerModal
          isOpen={true}
          onClose={() => setIsPickerOpenForEdit(false)}
          initialLatitude={editPropLat}
          initialLongitude={editPropLng}
          addressContext={[
            editPropAddress,
            availableEditWards.find((w) => w.code === editPropWard)?.name,
            availableProvinces.find((p) => p.code === editPropProvince)?.name,
            "Việt Nam",
          ]
            .filter(Boolean)
            .join(", ")}
          onConfirm={({ latitude, longitude }) => {
            setEditPropLat(latitude);
            setEditPropLng(longitude);
            showToast(`Đã cập nhật tọa độ GPS: ${latitude}, ${longitude}`, "success");
          }}
        />
      )}
    </div>
  );
}
