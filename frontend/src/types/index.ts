// Common API response matching ASP.NET Core ApiResponse<T>
export interface ApiResponse<T> {
  isSuccess: boolean;
  data?: T;
  error?: {
    code: string;
    message: string;
    details?: Array<{ field: string; message: string }>;
  };
  timestamp: string;
}

export interface SystemStatus {
  application: string;
  version: string;
  modules: string[];
  status: string;
  serverTime: string;
}

export interface HealthCheck {
  status: string;
  timestamp: string;
  environment: string;
}

// User & Auth types
export type UserRole = "Tenant" | "Landlord" | "Admin";

export interface CurrentUserProfile {
  id?: number | string;
  accountId?: string;
  email: string;
  fullName?: string;
  firstName?: string;
  lastName?: string;
  displayName: string;
  role: UserRole;
  phoneNumber?: string | null;
  avatarUrl?: string;
  emailConfirmed?: boolean;
  emailVerified?: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface AuthTokens {
  accessToken: string;
  expiresInSeconds?: number;
}

export interface LoginResponse {
  token: string;
  user?: CurrentUserProfile;
}

// Listing / Property types
export type RoomStatus = "Còn trống" | "Đã thuê" | "Tạm ngừng";

export interface NearbyPOI {
  name: string;
  distance: string;
  type: "school" | "transit" | "market" | "hospital";
}

export interface TenantReview {
  id: string;
  author: string;
  role: string;
  rating: number;
  date: string;
  content: string;
  avatar?: string;
}

export interface PropertyListing {
  id: string;
  title: string;
  description: string;
  address: string;
  ward: string;
  district: string;
  city: string;
  price: number; // VND/month
  deposit: number; // VND
  area: number; // m2
  roomType: "Phòng trọ" | "Chung cư mini" | "Căn hộ dịch vụ" | "Nhà nguyên căn" | "Ở ghép";
  status: RoomStatus;
  images: string[];
  amenities: string[];
  landlordId: string;
  landlordName: string;
  landlordPhone: string;
  landlordAvatar?: string;
  isVerifiedLandlord?: boolean;
  rating?: number;
  reviewCount?: number;
  discountTag?: string;
  tags?: string[];
  nearbyPOIs?: NearbyPOI[];
  reviews?: TenantReview[];
  electricityPrice: number; // VND/kWh
  waterPrice: number; // VND/m3
  serviceFee: number; // VND/month
  latitude: number;
  longitude: number;
  createdAt: string;
  viewCount: number;
  isFeatured?: boolean;
}

// Viewing Appointment types
export type ViewingStatus = "Chờ xác nhận" | "Đã xác nhận" | "Đã hủy" | "Đã hoàn thành";

export interface ViewingAppointment {
  id: string;
  listingId: string;
  listingTitle: string;
  listingAddress: string;
  listingImage: string;
  tenantId: string;
  tenantName: string;
  tenantPhone: string;
  landlordId: string;
  scheduledTime: string; // ISO date string
  note?: string;
  status: ViewingStatus;
  createdAt: string;
}

// Contract & Leasing types
export type ContractStatus = "Còn hiệu lực" | "Sắp hết hạn" | "Đã kết thúc" | "Đã hủy";

export interface LeaseContract {
  id: string;
  contractNumber: string;
  listingId: string;
  roomName: string;
  tenantId: string;
  tenantName: string;
  tenantPhone: string;
  tenantCccd?: string;
  landlordId: string;
  startDate: string;
  endDate: string;
  monthlyRent: number;
  depositAmount: number;
  paymentCycle: number; // Số tháng thanh toán 1 lần (1, 3, 6)
  status: ContractStatus;
  terms?: string;
  daysRemaining?: number;
}

// Billing & Utilities types
export type BillStatus = "Chưa thanh toán" | "Thanh toán một phần" | "Đã thanh toán" | "Quá hạn";

export interface UtilityReading {
  id: string;
  roomId: string;
  roomName: string;
  period: string; // MM/YYYY
  electricityStart: number;
  electricityEnd: number;
  electricityUsed: number;
  waterStart: number;
  waterEnd: number;
  waterUsed: number;
  recordedAt: string;
}

export interface MonthlyBill {
  id: string;
  contractId: string;
  roomName: string;
  tenantName: string;
  period: string; // MM/YYYY
  rentAmount: number;
  electricityUsage: number;
  electricityCost: number;
  waterUsage: number;
  waterCost: number;
  serviceCost: number;
  totalDue: number;
  paidAmount: number;
  remainingDebt: number; // Conno = Tongphaithu - Tongdathanhtoan
  dueDate: string;
  status: BillStatus;
}

// Messaging types
export interface ChatMessage {
  id: string;
  senderId: string;
  senderName: string;
  receiverId: string;
  content: string;
  timestamp: string;
  isRead: boolean;
  listingId?: string;
}

export interface ChatConversation {
  id: string;
  partnerId: string;
  partnerName: string;
  partnerRole: UserRole;
  partnerAvatar?: string;
  lastMessage: string;
  lastMessageTime: string;
  unreadCount: number;
  listingTitle?: string;
}

// Admin types
export interface SystemReport {
  id: string;
  targetId: string;
  targetType: "Tin đăng" | "Chủ nhà" | "Người dùng";
  targetTitle: string;
  reporterName: string;
  reporterEmail: string;
  reason: string;
  evidence?: string;
  status: "Chờ xử lý" | "Đã xử lý" | "Đã bác bỏ";
  createdAt: string;
}
