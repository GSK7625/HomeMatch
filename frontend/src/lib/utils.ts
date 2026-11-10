import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";
import { ListingSummaryDto, PropertyListing } from "@/types";

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

export function mapListingSummaryToCard(item: ListingSummaryDto): PropertyListing {
  return {
    id: String(item.id),
    title: item.title,
    description: item.title,
    address: item.addressDetail || item.districtName || item.provinceName || "Việt Nam",
    ward: item.addressDetail || "",
    district: item.districtName || item.addressDetail || "Trung tâm",
    city: item.provinceName || "Việt Nam",
    price: item.price,
    deposit: item.depositAmount && item.depositAmount > 0 ? item.depositAmount : item.price,
    area: item.areaSqm ? Number(item.areaSqm) : 25,
    roomType: formatRoomTypeLabel(item.roomType),
    status: (() => {
      const norm = (item.status || "").toUpperCase();
      if (norm === "PUBLISHED" || norm === "AVAILABLE" || norm === "CÒN TRỐNG") {
        return "Còn trống";
      }
      if (norm === "RENTED" || norm === "ĐÃ THUÊ") {
        return "Đã thuê";
      }
      return "Tạm ngừng";
    })(),
    images: item.coverImageUrl
      ? [item.coverImageUrl]
      : ["https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&q=80"],
    amenities:
      item.amenities && item.amenities.length > 0
        ? item.amenities
        : ["Wifi", "Điều hòa", "Nóng lạnh"],
    tags: item.amenities && item.amenities.length > 0 ? item.amenities.slice(0, 3) : undefined,
    landlordId: "ll-1",
    landlordName: "Chủ nhà xác thực",
    landlordPhone: "0901234567",
    isVerifiedLandlord: true,
    electricityPrice: item.electricityPrice ?? 3500,
    waterPrice: item.waterPrice ?? 25000,
    serviceFee: 150000,
    latitude: item.latitude ?? 21.0285,
    longitude: item.longitude ?? 105.8542,
    createdAt: new Date().toISOString(),
    viewCount: 0,
    isFeatured: item.isFeatured ?? true,
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
