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


// ============================================================================
// Properties & Rooms Module DTOs (/api/v1/properties)
// ============================================================================
export interface PropertyDto {
  id: number;
  name: string;
  provinceCode: string;
  wardCode: string;
  addressDetail: string;
  propertyType: "APARTMENT" | "BOARDING_HOUSE" | "SERVICED_APARTMENT" | "HOMESTAY" | string;
  latitude?: number | null;
  longitude?: number | null;
}

export interface RoomDto {
  id: number;
  propertyId: number;
  roomNumber: string;
  floor: number;
  areaSqm: number;
  roomType: "SINGLE" | "STUDIO" | "1B1B" | "2B1B" | "DORMITORY" | string;
  maxOccupants: number;
  basePrice: number;
  status: "AVAILABLE" | "RENTED" | "SUSPENDED" | string;
  availableFrom: string;
  amenityIds: number[];
}

export interface TariffDto {
  id: number;
  propertyId: number;
  roomId?: number | null;
  tariffType: "ELECTRICITY" | "WATER" | "INTERNET" | "SERVICE" | "CLEANING" | "PARKING" | string;
  calcMethod: "PER_KWH" | "PER_M3" | "PER_PERSON" | "FIXED_PER_MONTH" | "FREE" | string;
  unitPrice: number;
}

export interface AmenityDto {
  id: number;
  code: string;
  name: string;
}

export interface ProvinceDto {
  code: string;
  name: string;
  fullName: string;
}

export interface WardDto {
  code: string;
  name: string;
  fullName: string;
  provinceCode: string;
}

export interface CreatePropertyBody {
  name: string;
  provinceCode: string;
  wardCode: string;
  addressDetail: string;
  propertyType: string;
}

export interface UpdatePropertyBody {
  name?: string;
  provinceCode?: string;
  wardCode?: string;
  addressDetail?: string;
  propertyType?: string;
}

export interface CreateRoomBody {
  roomNumber: string;
  floor: number;
  areaSqm: number;
  roomType: string;
  maxOccupants: number;
  basePrice: number;
  availableFrom?: string | null;
  amenityIds?: number[];
}

export interface UpdateRoomBody {
  roomNumber?: string;
  floor?: number;
  areaSqm?: number;
  roomType?: string;
  maxOccupants?: number;
  basePrice?: number;
  availableFrom?: string | null;
  amenityIds?: number[];
}

export interface TariffItemBody {
  tariffType: string;
  calcMethod: string;
  unitPrice: number;
}

export interface SetPropertyTariffsBody {
  tariffs: TariffItemBody[];
}

// ============================================================================
// Listings Module DTOs (/api/v1/listings)
// ============================================================================
export interface ListingSummaryDto {
  id: number;
  roomId: number;
  title: string;
  slug: string;
  price: number;
  status: "Draft" | "PendingApproval" | "Published" | "Hidden" | "Rejected" | "Banned" | string;
  coverImageUrl?: string | null;
  isFeatured: boolean;
}

export interface ListingImageDto {
  id: number;
  imageUrl: string;
  isPrimary: boolean;
  sortOrder: number;
  mediaType?: string;
  thumbnailUrl?: string | null;
  durationSeconds?: number | null;
}

export interface ListingReviewDto {
  revision: number;
  reviewedBy?: number | null;
  reviewedAt?: string | null;
  rejectionReason?: string | null;
  publishedAt?: string | null;
}

export interface ListingDetailDto {
  id: number;
  roomId: number;
  landlordId: number;
  title: string;
  slug: string;
  description: string;
  price: number;
  depositAmount: number;
  minLeaseMonths: number;
  allowPets: boolean;
  hasCurfew: boolean;
  curfewTime?: string | null;
  status: string;
  isFeatured: boolean;
  viewCount: number;
  longitude?: number | null;
  latitude?: number | null;
  images: ListingImageDto[];
  review?: ListingReviewDto | null;
}

export interface PublicListingDetailDto extends ListingDetailDto {
  room: {
    id: number;
    roomNumber: string;
    floor: number;
    areaSqm: number;
    roomType: string;
    maxOccupants: number;
    status: string;
    availableFrom: string;
    amenities: Array<{ id: number; code: string; name: string }>;
  };
  property: {
    id: number;
    name: string;
    propertyType: string;
    provinceCode: string;
    provinceName: string;
    wardCode: string;
    wardName: string;
    districtName?: string | null;
    addressDetail: string;
  };
  tariffs: Array<{
    id: number;
    tariffType: string;
    calcMethod: string;
    unitPrice: number;
  }>;
}

export interface SavedListingDto {
  id: number;
  listingId: number;
  listing: ListingSummaryDto;
  createdAt: string;
}

export interface CreateListingBody {
  roomId: number;
  title: string;
  slug?: string;
  description: string;
  price: number;
  depositAmount: number;
  minLeaseMonths: number;
  allowPets: boolean;
  hasCurfew: boolean;
  curfewTime?: string | null;
  longitude?: number | null;
  latitude?: number | null;
}

export interface UpdateListingBody {
  title?: string;
  slug?: string;
  description?: string;
  price?: number;
  depositAmount?: number;
  minLeaseMonths?: number;
  allowPets?: boolean;
  hasCurfew?: boolean;
  curfewTime?: string | null;
  longitude?: number | null;
  latitude?: number | null;
}

export interface AddListingImageBody {
  imageUrl: string;
  isPrimary?: boolean;
  sortOrder?: number;
  mediaType?: string;
  thumbnailUrl?: string | null;
  durationSeconds?: number | null;
}

export interface PendingListingsDto {
  items: ListingSummaryDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}
