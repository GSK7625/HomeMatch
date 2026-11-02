"use client";

import React, { createContext, useContext, useState, useEffect } from "react";
import { CurrentUserProfile, UserRole } from "@/types";
import {
  authApi,
  getStoredToken,
  setStoredToken,
  getStoredUser,
  setStoredUser,
  RegistrationBody,
} from "@/lib/api-client";

interface AuthContextType {
  user: CurrentUserProfile | null;
  token: string | null;
  role: UserRole | "Guest";
  isLoading: boolean;
  login: (email: string, password: string) => Promise<{ success: boolean; message?: string }>;
  register: (body: RegistrationBody) => Promise<{ success: boolean; challengeId?: string; message?: string }>;
  verifyOtp: (challengeId: string, code: string) => Promise<{ success: boolean; message?: string }>;
  logout: () => Promise<void>;
  switchDemoRole: (role: UserRole) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const DEMO_USERS: Record<UserRole, CurrentUserProfile> = {
  Tenant: {
    accountId: "demo-tenant-id",
    email: "tenant@homematch.vn",
    displayName: "Đỗ Trung Kiên (Người thuê)",
    role: "Tenant",
    phoneNumber: "0977 123 456",
    emailConfirmed: true,
    createdAt: "2026-10-01T00:00:00Z",
  },
  Landlord: {
    accountId: "demo-landlord-id",
    email: "landlord@homematch.vn",
    displayName: "Nguyễn Văn Hùng (Chủ nhà)",
    role: "Landlord",
    phoneNumber: "0912 345 678",
    emailConfirmed: true,
    createdAt: "2026-09-15T00:00:00Z",
  },
  Admin: {
    accountId: "demo-admin-id",
    email: "admin@homematch.vn",
    displayName: "Admin Quản Trị Hệ Thống",
    role: "Admin",
    phoneNumber: "0999 888 777",
    emailConfirmed: true,
    createdAt: "2026-01-01T00:00:00Z",
  },
};

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<CurrentUserProfile | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Initial hydration
    const savedToken = getStoredToken();
    const savedUser = getStoredUser();

    if (savedToken && savedUser) {
      setToken(savedToken);
      setUser(savedUser);

      // Verify token with backend
      authApi.getMe().then((res) => {
        if (res.isSuccess && res.data) {
          const profile = res.data;
          setUser(profile);
          setStoredUser(profile);
        }
      }).catch(() => {
        // Fallback or offline
      }).finally(() => {
        setIsLoading(false);
      });
    } else if (savedUser) {
      // Demo session without real JWT
      setUser(savedUser);
      setIsLoading(false);
    } else {
      // Default to demo Tenant for seamless onboarding
      const defaultUser = DEMO_USERS.Tenant;
      setUser(defaultUser);
      setStoredUser(defaultUser);
      setIsLoading(false);
    }
  }, []);

  const login = async (email: string, password: string) => {
    setIsLoading(true);
    try {
      const res = await authApi.login({ email, password });
      if (res.isSuccess && res.data) {
        const accessToken = res.data.accessToken;
        setToken(accessToken);
        setStoredToken(accessToken);

        // Fetch user profile from /me
        const profileRes = await authApi.getMe();
        if (profileRes.isSuccess && profileRes.data) {
          setUser(profileRes.data);
          setStoredUser(profileRes.data);
        } else {
          // Fallback mock profile based on email
          const isLandlord = email.includes("chu") || email.includes("landlord");
          const fallbackUser: CurrentUserProfile = {
            accountId: "user-" + Date.now(),
            email,
            displayName: email.split("@")[0],
            role: isLandlord ? "Landlord" : "Tenant",
            phoneNumber: "0900000000",
            emailConfirmed: true,
            createdAt: new Date().toISOString(),
          };
          setUser(fallbackUser);
          setStoredUser(fallbackUser);
        }
        return { success: true };
      } else {
        return {
          success: false,
          message: res.error?.message || "Đăng nhập không thành công. Vui lòng kiểm tra lại thông tin.",
        };
      }
    } catch {
      return { success: false, message: "Lỗi kết nối máy chủ xác thực." };
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (body: RegistrationBody) => {
    setIsLoading(true);
    try {
      const res = await authApi.register(body);
      if (res.isSuccess) {
        return {
          success: true,
          challengeId: res.data?.challengeId,
          message: "Mã OTP xác thực đã được gửi đến email của bạn.",
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

  const verifyOtp = async (challengeId: string, code: string) => {
    setIsLoading(true);
    try {
      const res = await authApi.verifyRegistrationOtp(challengeId, code);
      if (res.isSuccess) {
        return { success: true, message: "Xác thực email thành công! Bạn có thể đăng nhập ngay." };
      }
      return { success: false, message: res.error?.message || "Mã OTP không chính xác hoặc đã hết hạn." };
    } catch {
      return { success: false, message: "Lỗi kiểm tra mã xác thực." };
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async () => {
    try {
      await authApi.logout();
    } catch {
      // Ignored
    } finally {
      setToken(null);
      setUser(null);
      setStoredToken(null);
      setStoredUser(null);
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
        verifyOtp,
        logout,
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
