import { ApiResponse, CurrentUserProfile, HealthCheck, SystemStatus } from "@/types";

const API_BASE =
  process.env.NEXT_PUBLIC_API_URL !== undefined && process.env.NEXT_PUBLIC_API_URL !== ""
    ? process.env.NEXT_PUBLIC_API_URL
    : process.env.NODE_ENV === "production"
      ? ""
      : "http://localhost:5000";

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
