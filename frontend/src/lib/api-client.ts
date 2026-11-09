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

let csrfTokenCache: string | null = null;
let csrfPromise: Promise<string | null> | null = null;

export const TOKEN_STORAGE_KEY = "homematch_access_token";
export const USER_STORAGE_KEY = "homematch_current_user";

export function getStoredToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_STORAGE_KEY);
}

export function setStoredToken(token: string | null) {
  if (typeof window === "undefined") return;
  if (token) {
    localStorage.setItem(TOKEN_STORAGE_KEY, token);
  } else {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
  }
}

export function getStoredUser(): CurrentUserProfile | null {
  if (typeof window === "undefined") return null;
  const raw = localStorage.getItem(USER_STORAGE_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

export function setStoredUser(user: CurrentUserProfile | null) {
  if (typeof window === "undefined") return;
  if (user) {
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user));
  } else {
    localStorage.removeItem(USER_STORAGE_KEY);
  }
}

export function clearCsrfToken(): void {
  csrfTokenCache = null;
  csrfPromise = null;
}

/**
 * Fetch or reuse CSRF Request Token from ASP.NET Core Identity
 */
export async function getCsrfToken(forceRefresh = false): Promise<string | null> {
  if (forceRefresh) {
    csrfTokenCache = null;
    csrfPromise = null;
  }
  if (csrfTokenCache) return csrfTokenCache;
  if (csrfPromise) return csrfPromise;

  csrfPromise = (async () => {
    try {
      const res = await fetch(`${API_BASE}/api/v1/identity/auth/csrf`, {
        method: "GET",
        credentials: "include",
        headers: {
          Accept: "application/json",
        },
      });

      if (!res.ok) {
        csrfPromise = null;
        return null;
      }

      const json = (await res.json()) as ApiResponse<{ requestToken: string }>;
      if (json.isSuccess && json.data?.requestToken) {
        csrfTokenCache = json.data.requestToken;
        return csrfTokenCache;
      }
      return null;
    } catch {
      return null;
    } finally {
      csrfPromise = null;
    }
  })();

  return csrfPromise;
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

  // Attach JWT if available
  const token = getStoredToken();
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
      try {
        const refreshRes = await fetch(`${API_BASE}/api/v1/identity/auth/refresh`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        });
        if (refreshRes.ok) {
          const refreshData = await refreshRes.json();
          if (refreshData?.data?.accessToken) {
            setStoredToken(refreshData.data.accessToken);
            return apiRequest<T>(endpoint, options, true);
          }
        }
      } catch {
        // Refresh token failed, proceed to return 401 error
      }
    }

    if (!res.ok) {
      return {
        isSuccess: false,
        error: data?.error || {
          code: `HTTP_${res.status}`,
          message: data?.message || res.statusText || "Yêu cầu thất bại",
        },
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
    csrfTokenCache = null;
    return res;
  },

  logoutAll: async () => {
    const res = await apiRequest<void>("/api/v1/identity/auth/logout-all", {
      method: "POST",
    });
    setStoredToken(null);
    setStoredUser(null);
    csrfTokenCache = null;
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
 * Helper to convert listing IDs like "lst-001", "lst-002", or "101" into a valid numeric ListingId (> 0)
 */
export function parseListingNumericId(rawId: string | number | undefined | null): number {
  if (typeof rawId === "number" && Number.isFinite(rawId) && rawId > 0) {
    return Math.floor(rawId);
  }
  if (typeof rawId === "string") {
    const digits = rawId.replace(/\D+/g, "");
    const parsed = parseInt(digits, 10);
    if (Number.isFinite(parsed) && parsed > 0) {
      return parsed;
    }
  }
  return 1;
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

