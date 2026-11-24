import {
  ApiResponse,
  BookViewingAppointmentBody,
  ConversationDto,
  CurrentUserProfile,
  HealthCheck,
  MessageDraftDto,
  MessageDto,
  NotificationDto,
  PagedConversationsDto,
  PagedMessagesDto,
  PagedNotificationsDto,
  PagedViewedListingsDto,
  PagedViewingAppointmentsDto,
  SaveMessageDraftBody,
  SendMessageBody,
  StartConversationBody,
  SystemStatus,
  TenantDashboardSummaryDto,
  ViewingAppointmentDto,
  PropertyDto,
  RoomDto,
  TariffDto,
  AmenityDto,
  ProvinceDto,
  WardDto,
  CreatePropertyBody,
  GeocodingResult,
  UpdatePropertyBody,
  CreateRoomBody,
  UpdateRoomBody,
  SetPropertyTariffsBody,
  ListingSummaryDto,
  SearchListingsParams,
  SearchListingsDto,
  ListingImageDto,
  ListingDetailDto,
  PublicListingDetailDto,
  SavedListingDto,
  CreateListingBody,
  UpdateListingBody,
  AddListingImageBody,
  UpdateListingImageBody,
  PendingListingsDto,
  RoomTypeDto,
  AdminDashboardSummaryDto,
  AdminUserItemDto,
  PagedAdminUsersDto,
  PagedAdminListingsDto,
  ViolationReportDto,
  PagedViolationReportsDto,
  CreateViolationReportBody,
  ResolveViolationReportBody,
  CreateCatalogItemBody,
  UpdateCatalogItemBody,
} from "@/types";

export const API_BASE = (() => {
  const configuredUrl = process.env.NEXT_PUBLIC_API_URL?.trim();
  if (typeof window !== "undefined") {
    const isLocalBrowser =
      window.location.hostname === "localhost" ||
      window.location.hostname === "127.0.0.1";

    // Nếu người dùng đang mở trực tiếp trên cổng 3000 ở máy local (Docker hoặc npm run dev)
    if (isLocalBrowser && window.location.port === "3000") {
      if (
        configuredUrl &&
        (configuredUrl.includes("localhost") || configuredUrl.includes("127.0.0.1"))
      ) {
        return configuredUrl;
      }
      return `${window.location.protocol}//${window.location.hostname}:5000`;
    }

    // Nếu truy cập qua Nginx Reverse Proxy (cổng 80 / 443) trên localhost hoặc domain
    if (isLocalBrowser && window.location.port === "") {
      return "";
    }
  }

  if (configuredUrl) {
    return configuredUrl;
  }

  return process.env.NODE_ENV === "production" ? "" : "http://localhost:5000";
})();

let isRedirectingToLogin = false;

export const TOKEN_STORAGE_KEY = "homematch_access_token";
export const USER_STORAGE_KEY = "homematch_current_user";
export const REMEMBER_EMAIL_STORAGE_KEY = "homematch_remembered_email";

/**
 * Kiểm tra xem JWT access token đã hết hạn (hoặc sắp hết hạn trong vòng 10 giây) hay chưa.
 */
export function isTokenExpired(token: string | null): boolean {
  if (!token) return true;
  try {
    const parts = token.split(".");
    if (parts.length !== 3) return true;
    const base64Url = parts[1];
    const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    const jsonPayload =
      typeof window !== "undefined"
        ? decodeURIComponent(
            atob(base64)
              .split("")
              .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
              .join("")
          )
        : Buffer.from(base64, "base64").toString("utf8");
    const payload = JSON.parse(jsonPayload);
    if (!payload.exp) return false;
    // Đệm 10 giây trước mốc hết hạn thực tế
    return Date.now() >= payload.exp * 1000 - 10000;
  } catch {
    return true;
  }
}

let refreshingPromise: Promise<string | null> | null = null;

/**
 * Làm mới Access Token chủ động hoặc khi 401 bằng HttpOnly Refresh Cookie.
 * Gom các request đồng thời vào cùng một Promise để tránh gọi lặp lại.
 */
export async function refreshAccessTokenSilently(): Promise<string | null> {
  if (refreshingPromise) {
    return refreshingPromise;
  }

  refreshingPromise = (async () => {
    try {
      const refreshRes = await fetch(`${API_BASE}/api/v1/identity/auth/refresh`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });
      if (refreshRes.ok) {
        const refreshData = await refreshRes.json();
        const newToken = refreshData?.data?.accessToken;
        if (newToken) {
          setStoredToken(newToken);
          return newToken;
        }
      }
    } catch {
      // Bỏ qua lỗi mạng trong quá trình refresh ngầm
    } finally {
      refreshingPromise = null;
    }
    return null;
  })();

  return refreshingPromise;
}

function resolveAuthStorage(rememberMe?: boolean): Storage {
  if (rememberMe === true) return localStorage;
  if (rememberMe === false) return sessionStorage;
  return sessionStorage.getItem(TOKEN_STORAGE_KEY) !== null ? sessionStorage : localStorage;
}

export function getStoredToken(): string | null {
  if (typeof window === "undefined") return null;
  return sessionStorage.getItem(TOKEN_STORAGE_KEY) || localStorage.getItem(TOKEN_STORAGE_KEY);
}

/**
 * Lấy Access Token hợp lệ. Nếu token đã hết hạn hoặc sắp hết hạn (trong vòng 10s),
 * hàm sẽ tự động gọi refreshAccessTokenSilently() để lấy token mới nhất.
 */
export async function getValidAccessToken(): Promise<string | null> {
  let token = getStoredToken();
  if (!token) return null;

  if (isTokenExpired(token)) {
    const refreshed = await refreshAccessTokenSilently();
    token = refreshed || null;
    if (!token && typeof window !== "undefined") {
      setStoredToken(null);
      setStoredUser(null);
      window.dispatchEvent(new Event("homematch:auth-expired"));
    }
  }

  return token;
}

export function setStoredToken(token: string | null, rememberMe?: boolean) {
  if (typeof window === "undefined") return;
  if (token) {
    isRedirectingToLogin = false;
    const target = resolveAuthStorage(rememberMe);
    const other = target === localStorage ? sessionStorage : localStorage;
    other.removeItem(TOKEN_STORAGE_KEY);
    target.setItem(TOKEN_STORAGE_KEY, token);
  } else {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    sessionStorage.removeItem(TOKEN_STORAGE_KEY);
  }
}

export function getStoredUser(): CurrentUserProfile | null {
  if (typeof window === "undefined") return null;
  const raw = sessionStorage.getItem(USER_STORAGE_KEY) || localStorage.getItem(USER_STORAGE_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

export function setStoredUser(user: CurrentUserProfile | null, rememberMe?: boolean) {
  if (typeof window === "undefined") return;
  if (user) {
    const target = resolveAuthStorage(rememberMe);
    const other = target === localStorage ? sessionStorage : localStorage;
    other.removeItem(USER_STORAGE_KEY);
    target.setItem(USER_STORAGE_KEY, JSON.stringify(user));
  } else {
    localStorage.removeItem(USER_STORAGE_KEY);
    sessionStorage.removeItem(USER_STORAGE_KEY);
  }
}

export function getRememberedEmail(): string {
  if (typeof window === "undefined") return "";
  return localStorage.getItem(REMEMBER_EMAIL_STORAGE_KEY) || "";
}

export function setRememberedEmail(email: string | null) {
  if (typeof window === "undefined") return;
  if (email) {
    localStorage.setItem(REMEMBER_EMAIL_STORAGE_KEY, email);
  } else {
    localStorage.removeItem(REMEMBER_EMAIL_STORAGE_KEY);
  }
}

export function clearCsrfToken(): void {
  // Kiến trúc JWT Bearer Token không yêu cầu CSRF cookie; giữ no-op để tương thích interface
}

/**
 * Kiến trúc bảo mật HomeMatch sử dụng JWT Bearer Token trong Authorization Header,
 * an toàn mặc định trước CSRF và không yêu cầu Antiforgery Cookie.
 * Trả về null để tránh gọi endpoint 404 dư thừa.
 */
export async function getCsrfToken(_forceRefresh = false): Promise<string | null> {
  return null;
}

/**
 * Common request wrapper with Bearer token, CSRF, and standard error handling
 */
export async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {},
  isRetry = false
): Promise<ApiResponse<T>> {
  const url = endpoint.startsWith("http") ? endpoint : `${API_BASE}${endpoint}`;
  const headers = new Headers(options.headers || {});

  if (!headers.has("Content-Type") && options.body && typeof options.body === "string") {
    headers.set("Content-Type", "application/json");
  }

  // Attach JWT if available (tự động làm mới nếu token đã hết hạn trước khi gửi)
  let token = getStoredToken();
  if (token && isTokenExpired(token) && !endpoint.includes("/auth/")) {
    const refreshed = await refreshAccessTokenSilently();
    token = refreshed || null;
    if (!refreshed) {
      setStoredToken(null);
      setStoredUser(null);
      if (typeof window !== "undefined") {
        window.dispatchEvent(new Event("homematch:auth-expired"));
      }
    }
  }

  if (token && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  // Attach CSRF Token for mutating requests if available
  const method = (options.method || "GET").toUpperCase();
  if (["POST", "PUT", "PATCH", "DELETE"].includes(method)) {
    const csrf = await getCsrfToken(isRetry);
    if (csrf) {
      headers.set("X-CSRF-TOKEN", csrf);
    }
  }

  try {
    const res = await fetch(url, {
      ...options,
      headers,
      credentials: "include",
    });

    const data = await res.json().catch(() => null);

    // If CSRF token is invalid/stale, auto-refresh and retry once
    if (
      !isRetry &&
      res.status === 403 &&
      data?.error?.code === "Identity.CsrfInvalid" &&
      ["POST", "PUT", "PATCH", "DELETE"].includes(method)
    ) {
      clearCsrfToken();
      return apiRequest<T>(endpoint, options, true);
    }

    // Auto-refresh token on 401 if not already retrying, and not login/refresh endpoint
    if (
      !isRetry &&
      res.status === 401 &&
      !endpoint.includes("/auth/login") &&
      !endpoint.includes("/auth/refresh")
    ) {
      const refreshedToken = await refreshAccessTokenSilently();
      if (refreshedToken) {
        return apiRequest<T>(endpoint, options, true);
      }
    }

    if (!res.ok) {
      if (
        res.status === 401 &&
        !endpoint.includes("/auth/login") &&
        !endpoint.includes("/auth/refresh")
      ) {
        setStoredToken(null);
        setStoredUser(null);
        clearCsrfToken();
        if (typeof window !== "undefined") {
          window.dispatchEvent(new Event("homematch:auth-expired"));
          const currentPath = window.location.pathname + window.location.search;
          const isProtectedRoute =
            currentPath.startsWith("/tenant") ||
            currentPath.startsWith("/landlord") ||
            currentPath.startsWith("/admin") ||
            currentPath.startsWith("/messages");

          if (isProtectedRoute && !isRedirectingToLogin) {
            isRedirectingToLogin = true;
            window.location.href = `/auth/login?reason=expired&redirect=${encodeURIComponent(currentPath)}`;
          }
        }
      }

      const fallbackMessage =
        data?.message ||
        (res.status === 401
          ? "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
          : res.status === 403
          ? "Tài khoản không có quyền thực hiện thao tác này."
          : res.status === 404
          ? "Không tìm thấy tài nguyên yêu cầu."
          : res.status >= 500
          ? "Lỗi hệ thống máy chủ. Vui lòng thử lại sau."
          : res.statusText || "Yêu cầu thất bại");

      return {
        isSuccess: false,
        error: data?.error || {
          code: `HTTP_${res.status}`,
          message: fallbackMessage,
        },
        timestamp: new Date().toISOString(),
      };
    }

    if (res.status === 204 || !data) {
      return {
        isSuccess: true,
        timestamp: new Date().toISOString(),
      };
    }

    return data as ApiResponse<T>;
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : "Không thể kết nối đến máy chủ";
    return {
      isSuccess: false,
      error: {
        code: "NETWORK_ERROR",
        message,
      },
      timestamp: new Date().toISOString(),
    };
  }
}

// System Endpoints
export async function fetchHealth(): Promise<HealthCheck> {
  const res = await fetch(`${API_BASE}/health`, { cache: "no-store" });
  if (!res.ok) throw new Error(`Failed to fetch health: ${res.statusText}`);
  return res.json();
}

export async function fetchSystemStatus(): Promise<SystemStatus> {
  const res = await fetch(`${API_BASE}/api/v1/system/status`, { cache: "no-store" });
  if (!res.ok) throw new Error(`Failed to fetch system status: ${res.statusText}`);
  return res.json();
}

export async function pingModule(moduleName: string): Promise<Record<string, unknown>> {
  const res = await fetch(`${API_BASE}/api/v1/${moduleName.toLowerCase()}/ping`, { cache: "no-store" });
  if (!res.ok) throw new Error(`Failed to ping module ${moduleName}: ${res.statusText}`);
  return res.json();
}

// Authentication API
export interface RegistrationBody {
  email: string;
  password: string;
  fullName: string;
  role: "Tenant" | "Landlord";
}

export interface LoginBody {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface IssuedAccessToken {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  expiresAt: string;
  sessionExpiresAt: string;
}

export const authApi = {
  getCsrf: getCsrfToken,
  clearCsrf: clearCsrfToken,

  register: async (payload: RegistrationBody) => {
    // Only pass exact 4 fields required by backend Identity RegistrationRequestDto
    const body = JSON.stringify({
      email: payload.email.trim(),
      password: payload.password,
      fullName: payload.fullName.trim(),
      role: payload.role,
    });
    return apiRequest<{ message: string }>("/api/v1/identity/auth/register", {
      method: "POST",
      body,
    });
  },

  login: async (payload: LoginBody) => {
    const body = JSON.stringify({
      email: payload.email.trim(),
      password: payload.password,
      rememberMe: payload.rememberMe ?? true,
    });
    return apiRequest<IssuedAccessToken>("/api/v1/identity/auth/login", {
      method: "POST",
      body,
    });
  },

  logout: async () => {
    const res = await apiRequest<void>("/api/v1/identity/auth/logout", {
      method: "POST",
    });
    setStoredToken(null);
    setStoredUser(null);
    return res;
  },

  logoutAll: async () => {
    const res = await apiRequest<void>("/api/v1/identity/auth/logout-all", {
      method: "POST",
    });
    setStoredToken(null);
    setStoredUser(null);
    return res;
  },

  refreshToken: async (token?: string) => {
    const body = token ? JSON.stringify({ refreshToken: token }) : undefined;
    const res = await apiRequest<IssuedAccessToken>("/api/v1/identity/auth/refresh", {
      method: "POST",
      body,
    });
    if (res.isSuccess && res.data?.accessToken) {
      setStoredToken(res.data.accessToken);
    }
    return res;
  },

  resendConfirmation: async (email: string) => {
    const body = JSON.stringify({ email: email.trim() });
    return apiRequest<{ message: string }>("/api/v1/identity/auth/resend-confirmation", {
      method: "POST",
      body,
    });
  },

  confirmEmail: async (userId: number, token: string) => {
    const body = JSON.stringify({ userId, token });
    return apiRequest<{ message: string }>("/api/v1/identity/auth/confirm-email", {
      method: "POST",
      body,
    });
  },

  getMe: async () => {
    return apiRequest<CurrentUserProfile>("/api/v1/identity/me", {
      method: "GET",
    });
  },

  updateProfile: async (payload: {
    fullName: string;
    phoneNumber?: string | null;
    dateOfBirth?: string | null;
    gender?: string | null;
  }) => {
    const body = JSON.stringify({
      fullName: payload.fullName.trim(),
      phoneNumber: payload.phoneNumber?.trim() || null,
      dateOfBirth: payload.dateOfBirth || null,
      gender: payload.gender || null,
    });
    return apiRequest<CurrentUserProfile>("/api/v1/identity/me", {
      method: "PUT",
      body,
    });
  },

  changePassword: async (payload: {
    currentPassword: string;
    newPassword: string;
    confirmNewPassword: string;
  }) => {
    const body = JSON.stringify(payload);
    return apiRequest<{ message: string }>("/api/v1/identity/change-password", {
      method: "POST",
      body,
    });
  },

  getNotificationSettings: async () => {
    return apiRequest<{ notifyPush: boolean; updatedAt: string }>(
      "/api/v1/identity/me/notification-settings",
      {
        method: "GET",
      }
    );
  },

  updateNotificationSettings: async (notifyPush: boolean) => {
    const body = JSON.stringify({ notifyPush });
    return apiRequest<{ notifyPush: boolean; updatedAt: string }>(
      "/api/v1/identity/me/notification-settings",
      {
        method: "PUT",
        body,
      }
    );
  },

  forgotPassword: async (email: string) => {
    const body = JSON.stringify({ email: email.trim() });
    return apiRequest<{ message: string }>("/api/v1/identity/auth/forgot-password", {
      method: "POST",
      body,
    });
  },

  resetPassword: async (userId: number, token: string, newPassword: string) => {
    const body = JSON.stringify({ userId, token, newPassword });
    return apiRequest<{ message: string }>("/api/v1/identity/auth/reset-password", {
      method: "POST",
      body,
    });
  },
};

/**
 * Helper to convert listing IDs like "101" into a valid numeric ListingId (> 0)
 * Returns 0 if ID is invalid or a legacy mock format (e.g. "lst-001") so it never falsely targets listing 1.
 */
export function parseListingNumericId(rawId: string | number | undefined | null): number {
  if (typeof rawId === "number" && Number.isFinite(rawId) && rawId > 0) {
    return Math.floor(rawId);
  }
  if (typeof rawId === "string") {
    const trimmed = rawId.trim();
    if (trimmed.startsWith("lst-")) {
      return 0;
    }
    const parsedDirect = parseInt(trimmed, 10);
    if (Number.isFinite(parsedDirect) && parsedDirect > 0 && String(parsedDirect) === trimmed) {
      return parsedDirect;
    }
    const digits = trimmed.replace(/\D+/g, "");
    if (digits.length > 0) {
      const parsed = parseInt(digits, 10);
      if (Number.isFinite(parsed) && parsed > 0) {
        return parsed;
      }
    }
  }
  return 0;
}

// ============================================================================
// Mục 3.3.1: Viewings Module API (/api/v1/viewings/appointments)
// ============================================================================
export const viewingsApi = {
  getAppointments: async (params?: {
    status?: string;
    roleView?: "TENANT" | "LANDLORD";
    page?: number;
    pageSize?: number;
  }) => {
    const search = new URLSearchParams();
    if (params?.status && params.status !== "ALL") search.set("status", params.status);
    if (params?.roleView) search.set("roleView", params.roleView);
    if (params?.page) search.set("page", String(params.page));
    if (params?.pageSize) search.set("pageSize", String(params.pageSize));
    const queryString = search.toString();
    return apiRequest<PagedViewingAppointmentsDto>(
      `/api/v1/viewings/appointments${queryString ? `?${queryString}` : ""}`,
      { method: "GET" }
    );
  },

  getById: async (id: number) => {
    return apiRequest<ViewingAppointmentDto>(`/api/v1/viewings/appointments/${id}`, {
      method: "GET",
    });
  },

  bookAppointment: async (payload: BookViewingAppointmentBody) => {
    return apiRequest<ViewingAppointmentDto>("/api/v1/viewings/appointments", {
      method: "POST",
      body: JSON.stringify({
        listingId: payload.listingId,
        appointmentTime: payload.appointmentTime,
        durationMinutes: payload.durationMinutes ?? 30,
        notes: payload.notes || null,
        isVirtualViewing: payload.isVirtualViewing ?? false,
      }),
    });
  },

  confirmAppointment: async (id: number, virtualRoomUrl?: string) => {
    return apiRequest<ViewingAppointmentDto>(`/api/v1/viewings/appointments/${id}/confirm`, {
      method: "PATCH",
      body: JSON.stringify({ virtualRoomUrl: virtualRoomUrl || null }),
    });
  },

  cancelAppointment: async (id: number, reason?: string) => {
    return apiRequest<ViewingAppointmentDto>(`/api/v1/viewings/appointments/${id}/cancel`, {
      method: "PATCH",
      body: JSON.stringify({ reason: reason || null }),
    });
  },

  completeAppointment: async (id: number) => {
    return apiRequest<ViewingAppointmentDto>(`/api/v1/viewings/appointments/${id}/complete`, {
      method: "PATCH",
    });
  },
};

// ============================================================================
// Mục 3.3.2: Messaging Module API (/api/v1/messaging/conversations)
// ============================================================================
export const messagingApi = {
  getConversations: async (page = 1, pageSize = 20) => {
    return apiRequest<PagedConversationsDto>(
      `/api/v1/messaging/conversations?page=${page}&pageSize=${pageSize}`,
      { method: "GET" }
    );
  },

  startOrGetConversation: async (payload: StartConversationBody) => {
    return apiRequest<ConversationDto>("/api/v1/messaging/conversations", {
      method: "POST",
      body: JSON.stringify({
        listingId: payload.listingId,
        recipientUserId: payload.recipientUserId ?? null,
        initialMessage: payload.initialMessage || null,
      }),
    });
  },

  getMessages: async (conversationId: number, page = 1, pageSize = 50) => {
    return apiRequest<PagedMessagesDto>(
      `/api/v1/messaging/conversations/${conversationId}/messages?page=${page}&pageSize=${pageSize}`,
      { method: "GET" }
    );
  },

  sendMessage: async (conversationId: number, payload: SendMessageBody) => {
    return apiRequest<MessageDto>(`/api/v1/messaging/conversations/${conversationId}/messages`, {
      method: "POST",
      body: JSON.stringify({
        content: payload.content,
        messageType: payload.messageType || "TEXT",
      }),
    });
  },

  markConversationRead: async (conversationId: number) => {
    return apiRequest<{ conversationId: number; markedReadCount: number }>(
      `/api/v1/messaging/conversations/${conversationId}/read`,
      { method: "PATCH" }
    );
  },

  saveDraft: async (conversationId: number, payload: SaveMessageDraftBody) => {
    return apiRequest<MessageDraftDto>(
      `/api/v1/messaging/conversations/${conversationId}/draft`,
      {
        method: "PUT",
        body: JSON.stringify({
          content: payload.content,
          source: payload.source || "MANUAL",
        }),
      }
    );
  },
};

// ============================================================================
// Mục 3.4.3: Notifications Module API (/api/v1/notifications)
// ============================================================================
export const notificationsApi = {
  getNotifications: async (params?: { isRead?: boolean; page?: number; pageSize?: number }) => {
    const search = new URLSearchParams();
    if (params?.isRead !== undefined) search.set("isRead", String(params.isRead));
    if (params?.page) search.set("page", String(params.page));
    if (params?.pageSize) search.set("pageSize", String(params.pageSize));
    const queryString = search.toString();
    return apiRequest<PagedNotificationsDto>(
      `/api/v1/notifications${queryString ? `?${queryString}` : ""}`,
      { method: "GET" }
    );
  },

  markAsRead: async (id: number) => {
    return apiRequest<NotificationDto>(`/api/v1/notifications/${id}/read`, {
      method: "PATCH",
    });
  },

  markAllAsRead: async () => {
    return apiRequest<{ markedReadCount: number }>("/api/v1/notifications/read-all", {
      method: "PATCH",
    });
  },
};

// ============================================================================
// Mục 3.4.1 & 3.4.2: Tenant Overview API (/api/v1/identity/tenant/*)
// ============================================================================
export const tenantOverviewApi = {
  getDashboard: async () => {
    return apiRequest<TenantDashboardSummaryDto>("/api/v1/identity/tenant/dashboard", {
      method: "GET",
    });
  },

  getViewedListings: async (page = 1, pageSize = 20) => {
    return apiRequest<PagedViewedListingsDto>(
      `/api/v1/identity/tenant/viewed-listings?page=${page}&pageSize=${pageSize}`,
      { method: "GET" }
    );
  },
};



// ============================================================================
// Properties & Rooms Module API (/api/v1/properties)
// ============================================================================
export const propertiesApi = {
  geocode: (addressDetail: string, provinceCode: string, wardCode: string, signal?: AbortSignal) => {
    const query = new URLSearchParams({ addressDetail, provinceCode, wardCode });
    return apiRequest<GeocodingResult[]>(`/api/v1/properties/geocoding?${query}`, { signal });
  },

  reverseGeocode: (latitude: number, longitude: number, signal?: AbortSignal) => {
    const query = new URLSearchParams({ latitude: String(latitude), longitude: String(longitude) });
    return apiRequest<GeocodingResult>(`/api/v1/properties/reverse-geocoding?${query}`, { signal });
  },

  getMyProperties: async () => {
    return apiRequest<PropertyDto[]>("/api/v1/properties/mine", {
      method: "GET",
    });
  },

  getPropertyById: async (id: number) => {
    return apiRequest<PropertyDto>(`/api/v1/properties/${id}`, {
      method: "GET",
    });
  },

  createProperty: async (payload: CreatePropertyBody) => {
    return apiRequest<PropertyDto>("/api/v1/properties", {
      method: "POST",
      body: JSON.stringify(payload),
    });
  },

  updateProperty: async (id: number, payload: UpdatePropertyBody) => {
    return apiRequest<PropertyDto>(`/api/v1/properties/${id}`, {
      method: "PATCH",
      body: JSON.stringify(payload),
    });
  },

  deleteProperty: async (id: number) => {
    return apiRequest<void>(`/api/v1/properties/${id}`, { method: "DELETE" });
  },

  deleteRoom: async (propertyId: number, roomId: number) => {
    return apiRequest<void>(`/api/v1/properties/${propertyId}/rooms/${roomId}`, { method: "DELETE" });
  },

  getRooms: async (propertyId: number) => {
    return apiRequest<RoomDto[]>(`/api/v1/properties/${propertyId}/rooms`, {
      method: "GET",
    });
  },

  getRoomById: async (propertyId: number, roomId: number) => {
    return apiRequest<RoomDto>(`/api/v1/properties/${propertyId}/rooms/${roomId}`, {
      method: "GET",
    });
  },

  createRoom: async (propertyId: number, payload: CreateRoomBody) => {
    return apiRequest<RoomDto>(`/api/v1/properties/${propertyId}/rooms`, {
      method: "POST",
      body: JSON.stringify(payload),
    });
  },

  updateRoom: async (propertyId: number, roomId: number, payload: UpdateRoomBody) => {
    return apiRequest<RoomDto>(`/api/v1/properties/${propertyId}/rooms/${roomId}`, {
      method: "PATCH",
      body: JSON.stringify(payload),
    });
  },

  setRoomStatus: async (propertyId: number, roomId: number, status: "AVAILABLE" | "RENTED" | "SUSPENDED") => {
    return apiRequest<RoomDto>(`/api/v1/properties/${propertyId}/rooms/${roomId}/status`, {
      method: "PATCH",
      body: JSON.stringify({ status }),
    });
  },

  getTariffs: async (propertyId: number) => {
    return apiRequest<TariffDto[]>(`/api/v1/properties/${propertyId}/tariffs`, {
      method: "GET",
    });
  },

  setTariffs: async (propertyId: number, payload: SetPropertyTariffsBody) => {
    return apiRequest<TariffDto[]>(`/api/v1/properties/${propertyId}/tariffs`, {
      method: "PUT",
      body: JSON.stringify(payload),
    });
  },
};

// ============================================================================
// Catalogs API (/api/v1/administrative-provinces, /api/v1/amenities)
// ============================================================================
export const catalogsApi = {
  getProvinces: async () => {
    return apiRequest<ProvinceDto[]>("/api/v1/administrative-provinces", {
      method: "GET",
    });
  },

  getWards: async (provinceCode: string) => {
    return apiRequest<WardDto[]>(`/api/v1/administrative-provinces/${provinceCode}/wards`, {
      method: "GET",
    });
  },

  getAmenities: async () => {
    return apiRequest<AmenityDto[]>("/api/v1/amenities", {
      method: "GET",
    });
  },

  getRoomTypes: async () => {
    return apiRequest<RoomTypeDto[]>("/api/v1/room-types", {
      method: "GET",
    });
  },
};

// ============================================================================
// Listings Module API (/api/v1/listings)
// ============================================================================
export const listingsApi = {
  deleteListing: async (id: number) => {
    return apiRequest<void>(`/api/v1/listings/${id}`, { method: "DELETE" });
  },

  searchListings: async (params?: SearchListingsParams) => {
    const search = new URLSearchParams();
    if (params?.minPrice !== undefined) search.set("minPrice", String(params.minPrice));
    if (params?.maxPrice !== undefined) search.set("maxPrice", String(params.maxPrice));
    if (params?.provinceCode) search.set("provinceCode", params.provinceCode);
    if (params?.wardCode) search.set("wardCode", params.wardCode);
    if (params?.keyword?.trim()) search.set("keyword", params.keyword.trim());
    if (params?.roomType && params.roomType !== "ALL") search.set("roomType", params.roomType);
    if (params?.occupants !== undefined && params.occupants > 0) search.set("occupants", String(params.occupants));
    if (params?.minArea !== undefined) search.set("minArea", String(params.minArea));
    if (params?.maxArea !== undefined) search.set("maxArea", String(params.maxArea));
    if (params?.amenityIds && params.amenityIds.length > 0) {
      params.amenityIds.forEach((id) => search.append("amenityIds", String(id)));
    }
    if (params?.page) search.set("page", String(params.page));
    if (params?.pageSize) search.set("pageSize", String(params.pageSize));
    const queryString = search.toString();
    return apiRequest<SearchListingsDto>(
      `/api/v1/listings/search${queryString ? `?${queryString}` : ""}`,
      { method: "GET" }
    );
  },

  getPublicListings: async (limit = 50) => {
    return apiRequest<ListingSummaryDto[]>(`/api/v1/listings?limit=${limit}`, {
      method: "GET",
    });
  },

  getPublicListingById: async (listingId: number) => {
    return apiRequest<PublicListingDetailDto>(`/api/v1/listings/${listingId}`, {
      method: "GET",
    });
  },

  compareRooms: async (roomIds: number[]) => {
    const search = new URLSearchParams();
    roomIds.forEach((id) => search.append("roomIds", String(id)));
    return apiRequest<PublicListingDetailDto[]>(
      `/api/v1/listings/compare${search.toString() ? `?${search.toString()}` : ""}`,
      { method: "GET" }
    );
  },

  getMyListings: async () => {
    return apiRequest<ListingSummaryDto[]>("/api/v1/listings/mine", {
      method: "GET",
    });
  },

  getMyListingById: async (listingId: number) => {
    return apiRequest<ListingDetailDto>(`/api/v1/listings/mine/${listingId}`, {
      method: "GET",
    });
  },

  createListing: async (payload: CreateListingBody) => {
    return apiRequest<ListingDetailDto>("/api/v1/listings", {
      method: "POST",
      body: JSON.stringify(payload),
    });
  },

  updateListing: async (listingId: number, payload: UpdateListingBody) => {
    return apiRequest<ListingDetailDto>(`/api/v1/listings/${listingId}`, {
      method: "PATCH",
      body: JSON.stringify(payload),
    });
  },

  submitListing: async (listingId: number) => {
    return apiRequest<ListingDetailDto>(`/api/v1/listings/${listingId}/submit`, {
      method: "POST",
    });
  },

  hideListing: async (listingId: number) => {
    return apiRequest<ListingDetailDto>(`/api/v1/listings/${listingId}/hide`, {
      method: "POST",
    });
  },

  getSavedListings: async () => {
    return apiRequest<SavedListingDto[]>("/api/v1/listings/saved", {
      method: "GET",
    });
  },

  toggleSaveListing: async (listingId: number) => {
    return apiRequest<boolean>(`/api/v1/listings/${listingId}/save`, {
      method: "POST",
    });
  },

  addImage: async (listingId: number, payload: AddListingImageBody) => {
    return apiRequest<ListingImageDto>(`/api/v1/listings/${listingId}/images`, {
      method: "POST",
      body: JSON.stringify(payload),
    });
  },

  updateImage: async (listingId: number, imageId: number, payload: UpdateListingImageBody) => {
    return apiRequest<ListingImageDto>(`/api/v1/listings/${listingId}/images/${imageId}`, {
      method: "PATCH",
      body: JSON.stringify(payload),
    });
  },

  deleteImage: async (listingId: number, imageId: number) => {
    return apiRequest<void>(`/api/v1/listings/${listingId}/images/${imageId}`, {
      method: "DELETE",
    });
  },
};

// ============================================================================
// Admin Listings Moderation API (/api/v1/admin/listings)
// ============================================================================
export const adminListingsApi = {
  getAll: async (params?: { status?: string; keyword?: string; page?: number; pageSize?: number }) => {
    const search = new URLSearchParams();
    if (params?.status) search.set("status", params.status);
    if (params?.keyword) search.set("keyword", params.keyword);
    search.set("page", String(params?.page ?? 1));
    search.set("pageSize", String(params?.pageSize ?? 20));
    return apiRequest<PagedAdminListingsDto>(`/api/v1/admin/listings?${search.toString()}`, {
      method: "GET",
    });
  },

  getPending: async (page = 1, pageSize = 20) => {
    return apiRequest<PendingListingsDto>(
      `/api/v1/admin/listings/pending?page=${page}&pageSize=${pageSize}`,
      { method: "GET" }
    );
  },

  getReviewListing: async (listingId: number) => {
    return apiRequest<ListingDetailDto>(`/api/v1/admin/listings/${listingId}`, {
      method: "GET",
    });
  },

  approve: async (listingId: number) => {
    return apiRequest<ListingDetailDto>(`/api/v1/admin/listings/${listingId}/approve`, {
      method: "POST",
    });
  },

  reject: async (listingId: number, reason: string) => {
    return apiRequest<ListingDetailDto>(`/api/v1/admin/listings/${listingId}/reject`, {
      method: "POST",
      body: JSON.stringify({ reason }),
    });
  },

  hide: async (listingId: number, reason?: string) => {
    return apiRequest<ListingDetailDto>(`/api/v1/admin/listings/${listingId}/hide`, {
      method: "POST",
      body: JSON.stringify({ reason: reason || null }),
    });
  },
};

export const adminDashboardApi = {
  getSummary: async (period = "all") => {
    return apiRequest<AdminDashboardSummaryDto>(
      `/api/v1/admin/dashboard?period=${encodeURIComponent(period)}`,
      { method: "GET" }
    );
  },
};

export const adminUsersApi = {
  getUsers: async (params?: { search?: string; role?: string; status?: string; page?: number; pageSize?: number }) => {
    const search = new URLSearchParams();
    if (params?.search) search.set("search", params.search);
    if (params?.role) search.set("role", params.role);
    if (params?.status) search.set("status", params.status);
    search.set("page", String(params?.page ?? 1));
    search.set("pageSize", String(params?.pageSize ?? 20));
    return apiRequest<PagedAdminUsersDto>(`/api/v1/admin/users?${search.toString()}`, {
      method: "GET",
    });
  },

  lock: async (userId: number) => {
    return apiRequest<AdminUserItemDto>(`/api/v1/admin/users/${userId}/lock`, {
      method: "POST",
    });
  },

  unlock: async (userId: number) => {
    return apiRequest<AdminUserItemDto>(`/api/v1/admin/users/${userId}/unlock`, {
      method: "POST",
    });
  },
};

export const reportsApi = {
  create: async (payload: CreateViolationReportBody) => {
    return apiRequest<ViolationReportDto>("/api/v1/reports", {
      method: "POST",
      body: JSON.stringify(payload),
    });
  },
};

export const adminReportsApi = {
  getReports: async (params?: { status?: string; targetType?: string; page?: number; pageSize?: number }) => {
    const search = new URLSearchParams();
    if (params?.status) search.set("status", params.status);
    if (params?.targetType) search.set("targetType", params.targetType);
    search.set("page", String(params?.page ?? 1));
    search.set("pageSize", String(params?.pageSize ?? 20));
    return apiRequest<PagedViolationReportsDto>(`/api/v1/admin/reports?${search.toString()}`, {
      method: "GET",
    });
  },

  resolve: async (reportId: number, payload: ResolveViolationReportBody) => {
    return apiRequest<ViolationReportDto>(`/api/v1/admin/reports/${reportId}/resolve`, {
      method: "POST",
      body: JSON.stringify(payload),
    });
  },
};

export const adminCatalogsApi = {
  getAmenities: async () => {
    return apiRequest<AmenityDto[]>("/api/v1/admin/catalogs/amenities", { method: "GET" });
  },
  createAmenity: async (payload: CreateCatalogItemBody) => {
    return apiRequest<AmenityDto>("/api/v1/admin/catalogs/amenities", {
      method: "POST",
      body: JSON.stringify(payload),
    });
  },
  updateAmenity: async (id: number, payload: UpdateCatalogItemBody) => {
    return apiRequest<AmenityDto>(`/api/v1/admin/catalogs/amenities/${id}`, {
      method: "PATCH",
      body: JSON.stringify(payload),
    });
  },
  getRoomTypes: async () => {
    return apiRequest<RoomTypeDto[]>("/api/v1/admin/catalogs/room-types", { method: "GET" });
  },
  createRoomType: async (payload: CreateCatalogItemBody) => {
    return apiRequest<RoomTypeDto>("/api/v1/admin/catalogs/room-types", {
      method: "POST",
      body: JSON.stringify(payload),
    });
  },
  updateRoomType: async (id: number, payload: UpdateCatalogItemBody) => {
    return apiRequest<RoomTypeDto>(`/api/v1/admin/catalogs/room-types/${id}`, {
      method: "PATCH",
      body: JSON.stringify(payload),
    });
  },
};

// ============================================================================
// Billing / Landlord Dashboard API (/api/v1/billing)
// ============================================================================
export const landlordDashboardApi = {
  getDashboard: async (buildingId = "all", period = "11/2026") => {
    return apiRequest<import("@/types").LandlordDashboardData>(
      `/api/v1/billing/landlord-dashboard?buildingId=${encodeURIComponent(buildingId)}&period=${encodeURIComponent(period)}`
    );
  },
};
