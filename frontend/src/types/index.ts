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

// ============================================================================
// Backend API DTOs for Mục 3.3 (Viewings & Messaging) & Mục 3.4 (Tenant Overview & Notifications)
// ============================================================================

export type BackendViewingStatus = "PENDING" | "CONFIRMED" | "CANCELLED" | "COMPLETED";

export interface VirtualViewingDto {
  id: number;
  roomUrl: string;
  status: string;
  startedAt?: string | null;
  endedAt?: string | null;
  createdAt: string;
}

export interface ViewingAppointmentDto {
  id: number;
  tenantId: number;
  tenantName: string;
  tenantPhone?: string | null;
  listingId: number;
  listingTitle: string;
  listingPrice: number;
  listingAddress?: string | null;
  listingPrimaryImageUrl?: string | null;
  landlordId: number;
  landlordName: string;
  landlordPhone?: string | null;
  appointmentTime: string;
  durationMinutes: number;
  notes?: string | null;
  status: BackendViewingStatus;
  cancelledBy?: string | null;
  cancelledReason?: string | null;
  remindAt?: string | null;
  isReminded: boolean;
  virtualViewing?: VirtualViewingDto | null;
  createdAt: string;
  updatedAt: string;
}

export interface PagedViewingAppointmentsDto {
  items: ViewingAppointmentDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface BookViewingAppointmentBody {
  listingId: number;
  appointmentTime: string;
  durationMinutes?: number;
  notes?: string;
  isVirtualViewing?: boolean;
}

// Mục 3.3.2: Messaging DTOs
export interface MessageDto {
  id: number;
  conversationId: number;
  senderId: number;
  senderName: string;
  messageType: string;
  content: string;
  isRead: boolean;
  createdAt: string;
}

export interface ConversationDto {
  id: number;
  conversationType: string;
  listingId?: number | null;
  listingTitle?: string | null;
  listingPrice?: number | null;
  listingPrimaryImageUrl?: string | null;
  initiatorId: number;
  initiatorName: string;
  recipientId: number;
  recipientName: string;
  partnerId: number;
  partnerName: string;
  partnerPhone?: string | null;
  lastMessageContent?: string | null;
  lastMessageSenderId?: number | null;
  unreadCount: number;
  lastMessageAt: string;
  createdAt: string;
}

export interface MessageDraftDto {
  id: number;
  conversationId: number;
  authorId: number;
  source: string;
  content: string;
  updatedAt: string;
}

export interface PagedConversationsDto {
  items: ConversationDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface PagedMessagesDto {
  conversation: ConversationDto;
  items: MessageDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface StartConversationBody {
  listingId: number;
  recipientUserId?: number;
  initialMessage?: string;
}

export interface SendMessageBody {
  content: string;
  messageType?: string;
}

export interface SaveMessageDraftBody {
  content: string;
  source?: string;
}

// Mục 3.4.3: Notifications DTOs
export interface NotificationDto {
  id: number;
  userId: number;
  title: string;
  content: string;
  type: string;
  referenceType?: string | null;
  referenceId?: number | null;
  isRead: boolean;
  createdAt: string;
}

export interface PagedNotificationsDto {
  items: NotificationDto[];
  unreadCount: number;
  page: number;
  pageSize: number;
  totalCount: number;
}

// Mục 3.4.1 & 3.4.2: Tenant Dashboard & Viewed Listings DTOs
export interface TenantDashboardSummaryDto {
  tenantId: number;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  totalViewedListings: number;
  totalSavedListings: number;
  totalAppointments: number;
  totalConversations: number;
}

export interface ViewedListingContractItem {
  viewId: number;
  listingId: number;
  roomId: number;
  landlordId: number;
  landlordName: string;
  title: string;
  price: number;
  areaSqm: number;
  address?: string | null;
  primaryImageUrl?: string | null;
  isPublished: boolean;
  viewedAt: string;
}

export interface PagedViewedListingsDto {
  items: ViewedListingContractItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

