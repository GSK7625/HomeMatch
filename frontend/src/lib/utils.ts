import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";
import { ListingSummaryDto, PropertyListing, PublicListingDetailDto } from "@/types";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatDateTime(isoString: string): string {
  try {
    return new Date(isoString).toLocaleString("vi-VN");
  } catch {
    return isoString;
  }
}

export function formatRoomTypeLabel(
  roomType?: string | null
): PropertyListing["roomType"] {
  const code = (roomType || "").toUpperCase();
  if (code === "STUDIO") return "Studio";
  if (code === "1B1B" || code === "2B1B") return "Căn hộ dịch vụ";
  if (code === "DORMITORY") return "Ở ghép";
  if (code === "SINGLE") return "Phòng trọ";
  if (
    roomType === "Phòng trọ" ||
    roomType === "Chung cư mini" ||
    roomType === "Căn hộ dịch vụ" ||
    roomType === "Nhà nguyên căn" ||
    roomType === "Ở ghép" ||
    roomType === "Studio"
  ) {
    return roomType;
  }
  return "Phòng trọ";
}

function mapListingStatus(status?: string | null): PropertyListing["status"] {
  const norm = (status || "").toUpperCase();
  if (norm === "PUBLISHED" || norm === "AVAILABLE" || norm === "CÒN TRỐNG") {
    return "Còn trống";
  }
  if (norm === "RENTED" || norm === "ĐÃ THUÊ") {
    return "Đã thuê";
  }
  return "Tạm ngừng";
}

function joinUniqueAddressParts(parts: Array<string | null | undefined>): string {
  return parts
    .map((p) => p?.trim())
    .filter(
      (p, idx, arr): p is string =>
        Boolean(p) && arr.findIndex((x) => x?.toLowerCase() === p?.toLowerCase()) === idx
    )
    .join(", ");
}

export function mapListingSummaryToCard(item: ListingSummaryDto): PropertyListing {
  return {
    id: String(item.id),
    roomId: item.roomId,
    title: item.title,
    description: "",
    address:
      joinUniqueAddressParts([item.addressDetail, item.districtName, item.provinceName]) ||
      "Chưa cập nhật địa chỉ",
    ward: "",
    district: item.districtName || "",
    city: item.provinceName || "",
    price: item.price,
    deposit: item.depositAmount ?? 0,
    area: item.areaSqm != null ? Number(item.areaSqm) : null,
    roomType: formatRoomTypeLabel(item.roomType),
    status: mapListingStatus(item.status),
    images: item.coverImageUrl ? [item.coverImageUrl] : [],
    amenities: item.amenities ?? [],
    tags: item.amenities && item.amenities.length > 0 ? item.amenities.slice(0, 3) : undefined,
    landlordId: "",
    landlordName: null,
    landlordPhone: null,
    isVerifiedLandlord: false,
    electricityPrice: item.electricityPrice ?? null,
    waterPrice: item.waterPrice ?? null,
    serviceFee: null,
    latitude: item.latitude ?? null,
    longitude: item.longitude ?? null,
    createdAt: "",
    viewCount: 0,
    isFeatured: item.isFeatured ?? false,
  };
}

export function mapPublicDetailToCard(d: PublicListingDetailDto): PropertyListing {
  const electricity = d.tariffs?.find((t) => t.tariffType === "ELECTRICITY")?.unitPrice ?? null;
  const water = d.tariffs?.find((t) => t.tariffType === "WATER")?.unitPrice ?? null;
  const service = d.tariffs?.find((t) => t.tariffType === "SERVICE")?.unitPrice ?? null;

  return {
    id: String(d.id),
    roomId: d.roomId,
    title: d.title,
    description: d.description || "",
    address:
      joinUniqueAddressParts([
        d.property?.addressDetail,
        d.property?.wardName,
        d.property?.districtName,
        d.property?.provinceName,
      ]) || "Chưa cập nhật địa chỉ",
    ward: d.property?.wardName || "",
    district: d.property?.districtName || d.property?.wardName || "",
    city: d.property?.provinceName || "",
    price: d.price,
    deposit: d.depositAmount ?? 0,
    area: d.room?.areaSqm != null ? Number(d.room.areaSqm) : null,
    roomType: formatRoomTypeLabel(d.room?.roomType),
    status: mapListingStatus(d.status),
    images: d.images ? d.images.map((img) => img.imageUrl) : [],
    amenities: d.room?.amenities ? d.room.amenities.map((a) => a.name) : [],
    landlordId: String(d.landlordId),
    landlordName: null,
    landlordPhone: null,
    isVerifiedLandlord: false,
    electricityPrice: electricity,
    waterPrice: water,
    serviceFee: service,
    latitude: d.latitude ?? null,
    longitude: d.longitude ?? null,
    createdAt: "",
    viewCount: Number(d.viewCount ?? 0),
    isFeatured: d.isFeatured ?? false,
  };
}

/**
 * Trích xuất 1-2 ký tự viết tắt từ tên người dùng để hiển thị Avatar chữ.
 * Tự động loại bỏ phần chú thích trong ngoặc đơn như (Người Thuê), (Chủ Nhà).
 * Ví dụ:
 *  - "Trần Bình An (Người Thuê)" -> "TA"
 *  - "Nguyễn Văn Hoàng" -> "NH"
 *  - "Admin" -> "AD"
 */
export function getInitials(name?: string): string {
  if (!name || !name.trim()) return "U";
  const cleanName = name.replace(/\(.*?\)/g, "").trim();
  const words = cleanName.split(/\s+/).filter(Boolean);
  if (words.length === 0) return "U";
  if (words.length === 1) {
    return words[0].slice(0, Math.min(2, words[0].length)).toUpperCase();
  }
  const first = words[0][0];
  const last = words[words.length - 1][0];
  return (first + last).toUpperCase();
}

/**
 * Chuẩn hóa tên tiện ích: Viết hoa chữ cái đầu và xử lý các từ viết tắt chuyên ngành (Wifi, 24/7).
 * Ví dụ:
 *  - "điều hòa" -> "Điều hòa"
 *  - "an ninh 24/7" -> "An ninh 24/7"
 *  - "wifi" -> "Wifi"
 */
export function formatAmenityName(name?: string | null): string {
  if (!name) return "";
  const trimmed = name.trim();
  if (!trimmed) return "";
  if (trimmed.toLowerCase() === "wifi") return "Wifi";
  return trimmed.charAt(0).toUpperCase() + trimmed.slice(1);
}
