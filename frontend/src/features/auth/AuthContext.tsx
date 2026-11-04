"use client";

import React, { createContext, useContext, useState, useEffect, useCallback } from "react";
import { CurrentUserProfile, UserRole } from "@/types";
import {
  authApi,
  getStoredToken,
  setStoredToken,
  getStoredUser,
  setStoredUser,
  RegistrationBody,
} from "@/lib/api-client";

export interface AuthResult {
  success: boolean;
  message?: string;
  errorCode?: string;
  user?: CurrentUserProfile;
}

interface AuthContextType {
  user: CurrentUserProfile | null;
  token: string | null;
  role: UserRole | "Guest";
  isLoading: boolean;
  login: (email: string, password: string) => Promise<AuthResult>;
  register: (body: RegistrationBody) => Promise<{ success: boolean; message?: string }>;
  resendConfirmationEmail: (email: string) => Promise<{ success: boolean; message?: string }>;
  logout: (allDevices?: boolean) => Promise<void>;
  refreshProfile: () => Promise<void>;
  switchDemoRole: (role: UserRole) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const DEMO_USERS: Record<UserRole, CurrentUserProfile> = {
  Tenant: {
    accountId: "demo-tenant-id",
    email: "tenant@homematch.vn",
    displayName: "Đỗ Trung Kiên",
    role: "Tenant",
    phoneNumber: "0977 123 456",
    emailConfirmed: true,
    emailVerified: true,
    createdAt: "2026-10-01T00:00:00Z",
  },
  Landlord: {
    accountId: "demo-landlord-id",
    email: "landlord@homematch.vn",
    displayName: "Nguyễn Văn Hùng",
    role: "Landlord",
    phoneNumber: "0912 345 678",
    emailConfirmed: true,
    emailVerified: true,
    createdAt: "2026-09-15T00:00:00Z",
  },
  Admin: {
    accountId: "demo-admin-id",
    email: "admin@homematch.vn",
    displayName: "Quản Trị Viên",
    role: "Admin",
    phoneNumber: "0999 888 777",
    emailConfirmed: true,
    emailVerified: true,
    createdAt: "2026-01-01T00:00:00Z",
  },
};

function normalizeProfile(raw: CurrentUserProfile): CurrentUserProfile {
  return {
    ...raw,
    accountId: raw.accountId || String(raw.id || ""),
    displayName: raw.fullName || raw.displayName || raw.email.split("@")[0],
    emailConfirmed: raw.emailVerified ?? raw.emailConfirmed ?? false,
    emailVerified: raw.emailVerified ?? raw.emailConfirmed ?? false,
  };
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<CurrentUserProfile | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const refreshProfile = useCallback(async () => {
    try {
      const res = await authApi.getMe();
      if (res.isSuccess && res.data) {
        const profile = normalizeProfile(res.data);
        setUser(profile);
        setStoredUser(profile);
      }
    } catch {
      // Ignore
    }
  }, []);

  useEffect(() => {
    // Initial hydration strictly adhering to real credentials
    const savedToken = getStoredToken();
    const savedUser = getStoredUser();

    if (savedToken) {
      setToken(savedToken);
      if (savedUser) {
        setUser(savedUser);
      }

      // Verify token with backend /api/v1/identity/me
      authApi.getMe().then((res) => {
        if (res.isSuccess && res.data) {
          const profile = normalizeProfile(res.data);
          setUser(profile);
          setStoredUser(profile);
        } else if (res.error?.code === "HTTP_401") {
          // Token expired or invalid: reset to Guest
          setToken(null);
          setUser(null);
          setStoredToken(null);
          setStoredUser(null);
        }
      }).catch(() => {
        // Offline or backend unavailable, maintain cached user if exists
      }).finally(() => {
        setIsLoading(false);
      });
    } else {
      // Default to Guest - NOT auto-logged in
      setUser(null);
      setToken(null);
      setStoredToken(null);
      setStoredUser(null);
      setIsLoading(false);
    }
  }, []);

  const login = async (email: string, password: string): Promise<AuthResult> => {
    setIsLoading(true);
    try {
      const res = await authApi.login({ email, password });
      if (res.isSuccess && res.data) {
        const accessToken = res.data.accessToken;
        setToken(accessToken);
        setStoredToken(accessToken);

        // Fetch user profile from backend /api/v1/identity/me
        const profileRes = await authApi.getMe();
        let userProfile: CurrentUserProfile;
        if (profileRes.isSuccess && profileRes.data) {
          userProfile = normalizeProfile(profileRes.data);
          setUser(userProfile);
          setStoredUser(userProfile);
        } else {
          // Fallback profile if /me endpoint has slight latency
          userProfile = {
            accountId: "user-" + Date.now(),
            email,
            displayName: email.split("@")[0],
            role: "Tenant",
            phoneNumber: null,
            emailConfirmed: true,
            emailVerified: true,
            createdAt: new Date().toISOString(),
          };
          setUser(userProfile);
          setStoredUser(userProfile);
        }
        return { success: true, user: userProfile };
      } else {
        return {
          success: false,
          errorCode: res.error?.code,
          message: res.error?.message || "Đăng nhập không thành công. Vui lòng kiểm tra lại thông tin.",
        };
      }
    } catch {
      return { success: false, message: "Lỗi kết nối máy chủ xác thực." };
    } finally {
      setIsLoading(false);
    }
  };

  const resendConfirmationEmail = async (email: string): Promise<{ success: boolean; message?: string }> => {
    try {
      const res = await authApi.resendConfirmation(email);
      if (res.isSuccess) {
        return {
          success: true,
          message: res.data?.message || "Email kích hoạt đã được gửi lại. Vui lòng kiểm tra hộp thư của bạn.",
        };
      }
      return {
        success: false,
        message: res.error?.message || "Không thể gửi email kích hoạt. Vui lòng thử lại sau.",
      };
    } catch {
      return {
        success: false,
        message: "Lỗi kết nối đến máy chủ khi gửi lại email.",
      };
    }
  };

  const register = async (body: RegistrationBody) => {
    setIsLoading(true);
    try {
      const res = await authApi.register(body);
      if (res.isSuccess) {
        return {
          success: true,
          message: res.data?.message || "Đăng ký thành công! Vui lòng kiểm tra hộp thư email để kích hoạt tài khoản.",
        };
      } else {
        return {
          success: false,
          message: res.error?.message || "Đăng ký không thành công. Email có thể đã tồn tại.",
        };
      }
    } catch {
      return { success: false, message: "Không thể kết nối đến máy chủ." };
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async (allDevices = false) => {
    try {
      if (allDevices) {
        await authApi.logoutAll();
      } else {
        await authApi.logout();
      }
    } catch {
      // Ignored
    } finally {
      setToken(null);
      setUser(null);
      setStoredToken(null);
      setStoredUser(null);
      if (typeof window !== "undefined") {
        window.location.href = "/";
      }
    }
  };

  const switchDemoRole = (newRole: UserRole) => {
    const demoUser = DEMO_USERS[newRole];
    setUser(demoUser);
    setStoredUser(demoUser);
  };

  const role = user ? user.role : "Guest";

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        role,
        isLoading,
        login,
        register,
        resendConfirmationEmail,
        logout,
        refreshProfile,
        switchDemoRole,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
