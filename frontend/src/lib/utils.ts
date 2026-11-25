import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";
import {
  ListingSummaryDto,
  NotificationDto,
  PropertyListing,
  PublicListingDetailDto,
} from "@/types";

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
  if (code === "1B1B" || code === "2B1B") return code;
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
      joinUniqueAddressParts([item.addressDetail, item.wardName, item.districtName, item.provinceName]) ||
      "Chưa cập nhật địa chỉ",
    ward: item.wardName ?? "",
    district: item.districtName || "",
    city: item.provinceName || "",
    price: item.price,
    deposit: item.depositAmount ?? 0,
    area: item.areaSqm != null ? Number(item.areaSqm) : null,
    roomType: item.roomTypeName || item.roomType || "Chưa cập nhật",
    propertyType: item.propertyType,
    distanceMeters: item.distanceMeters,
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
    roomType: d.room?.roomTypeName || formatRoomTypeLabel(d.room?.roomType),
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

export const NOTIFICATION_OPENED_EVENT = "homematch:notification-opened";
export const NOTIFICATION_READ_EVENT = "homematch:notification-read";

export function dispatchNotificationOpened(notification: NotificationDto): void {
  if (typeof window === "undefined") return;
  window.dispatchEvent(
    new CustomEvent<NotificationDto>(NOTIFICATION_OPENED_EVENT, { detail: notification })
  );
}

export function dispatchNotificationRead(notificationId?: number): void {
  if (typeof window === "undefined") return;
  window.dispatchEvent(
    new CustomEvent<{ id?: number }>(NOTIFICATION_READ_EVENT, {
      detail: { id: notificationId },
    })
  );
}

/**
 * Chuẩn hóa đường dẫn đích khi người dùng bấm vào một thông báo ở bất kỳ đâu
 * (Toast realtime, chuông Navbar, Trang Thông báo, Dashboard).
 */
export function resolveNotificationTarget(
  notification: Pick<NotificationDto, "type" | "referenceType" | "referenceId">,
  role?: string | null
): string {
  const refType = (notification.referenceType || "").toUpperCase();
  const typeUpper = (notification.type || "").toUpperCase();
  const normalizedRole = (role || "").toLowerCase();

  if (refType.includes("VIEWING") || typeUpper.includes("VIEWING")) {
    const base = normalizedRole === "landlord" ? "/landlord/viewings" : "/tenant/viewings";
    return notification.referenceId ? `${base}?appointmentId=${notification.referenceId}` : base;
  }

  if (refType.includes("CONVERSATION") || typeUpper.includes("MESSAGE")) {
    return notification.referenceId
      ? `/messages?conversationId=${notification.referenceId}`
      : "/messages";
  }

  if (refType.includes("CONTRACT") || typeUpper.includes("CONTRACT")) {
    return normalizedRole === "landlord" ? "/landlord/contracts" : "/tenant/dashboard";
  }

  if (refType.includes("LISTING") || typeUpper.includes("LISTING")) {
    if (normalizedRole === "landlord") return "/landlord/listings";
    if (normalizedRole === "admin") return "/admin/dashboard";
    return notification.referenceId ? `/listings/${notification.referenceId}` : "/listings";
  }

  if (normalizedRole === "landlord") return "/landlord/dashboard";
  if (normalizedRole === "admin") return "/admin/dashboard";
  return "/tenant/notifications";
}

export function normalizeLocationName(value?: string | null): string {
  if (!value) return "";
  return value
    .trim()
    .toLowerCase()
    .replace(/^(thành phố|tp\.|tp|tỉnh|quận|huyện|thị xã|phường|xã|thị trấn)\s+/i, "")
    .trim();
}

export function matchesLocationFilter(
  candidate?: string | null,
  filterValue?: string | null
): boolean {
  if (!filterValue || filterValue === "Tất cả") return true;
  if (!candidate) return false;
  const normCandidate = normalizeLocationName(candidate);
  const normFilter = normalizeLocationName(filterValue);
  if (!normFilter) return true;
  return (
    normCandidate === normFilter ||
    normCandidate.includes(normFilter) ||
    normFilter.includes(normCandidate)
  );
}
