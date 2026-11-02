"use client";

import React, { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { useToast } from "@/components/Toast";
import { Home, Mail, Lock, LogIn, ArrowRight, CheckCircle2, ShieldCheck } from "lucide-react";

export default function LoginPage() {
  const router = useRouter();
  const { login, switchDemoRole } = useAuth();
  const { showToast } = useToast();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [rememberMe, setRememberMe] = useState(true);
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email || !password) {
      setErrorMessage("Vui lòng nhập đầy đủ email và mật khẩu");
      return;
    }

    setLoading(true);
    setErrorMessage(null);

    const res = await login(email, password);
    setLoading(false);

    if (res.success) {
      showToast("Đăng nhập thành công!", "success");
      router.push("/");
    } else {
      setErrorMessage(res.message || "Đăng nhập thất bại");
    }
  };

  const handleQuickDemo = (role: "Tenant" | "Landlord" | "Admin") => {
    switchDemoRole(role);
    showToast(`Đã chuyển đổi sang tài khoản Demo: ${role === "Tenant" ? "Người thuê" : role === "Landlord" ? "Chủ nhà" : "Admin"}`, "info");
    if (role === "Landlord") {
      router.push("/landlord/dashboard");
    } else if (role === "Admin") {
      router.push("/admin/dashboard");
    } else {
      router.push("/listings");
    }
  };

  return (
    <div className="flex-1 flex items-center justify-center p-4 sm:p-8">
      <div className="w-full max-w-md">
        {/* Card */}
        <div className="bg-white rounded-2xl shadow-xl border border-slate-100 p-8">
          {/* Logo Header */}
          <div className="text-center mb-8">
            <div className="inline-flex items-center justify-center w-12 h-12 rounded-2xl bg-blue-600 text-white shadow-lg shadow-blue-500/30 mb-3">
              <Home className="w-6 h-6" />
            </div>
            <h1 className="text-2xl font-black text-slate-900 tracking-tight">Đăng Nhập HomeMatch</h1>
            <p className="text-xs text-slate-500 mt-1.5">
              Hệ thống kết nối trực tiếp Người thuê và Chủ nhà
            </p>
          </div>

          {/* Error Notice */}
          {errorMessage && (
            <div className="mb-6 p-3.5 bg-rose-50 border border-rose-200 rounded-xl text-xs text-rose-700 font-medium leading-relaxed">
              {errorMessage}
            </div>
          )}

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
                Email
              </label>
              <div className="relative">
                <Mail className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                <input
                  type="email"
                  required
                  placeholder="name@example.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all"
                />
              </div>
            </div>

            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider">
                  Mật khẩu
                </label>
                <Link
                  href="/auth/forgot-password"
                  className="text-xs font-semibold text-blue-600 hover:text-blue-700"
                >
                  Quên mật khẩu?
                </Link>
              </div>
              <div className="relative">
                <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                <input
                  type="password"
                  required
                  placeholder="••••••••"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all"
                />
              </div>
            </div>

            <div className="flex items-center">
              <label className="flex items-center gap-2 cursor-pointer text-xs text-slate-600 select-none">
                <input
                  type="checkbox"
                  checked={rememberMe}
                  onChange={(e) => setRememberMe(e.target.checked)}
                  className="w-4 h-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                />
                <span>Ghi nhớ mật khẩu trên thiết bị này</span>
              </label>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full mt-2 py-3 px-4 rounded-xl font-bold text-sm text-white bg-blue-600 hover:bg-blue-700 active:bg-blue-800 shadow-md shadow-blue-500/20 transition-all flex items-center justify-center gap-2 disabled:opacity-60"
            >
              {loading ? (
                <span>Đang xác thực...</span>
              ) : (
                <>
                  <LogIn className="w-4 h-4" />
                  <span>Đăng Nhập</span>
                </>
              )}
            </button>
          </form>

          {/* Quick Demo Switchers */}
          <div className="mt-8 pt-6 border-t border-slate-100">
            <p className="text-[11px] font-bold text-slate-400 uppercase tracking-wider text-center mb-3">
              Chế độ trải nghiệm nhanh
            </p>
            <div className="grid grid-cols-3 gap-2">
              <button
                type="button"
                onClick={() => handleQuickDemo("Tenant")}
                className="p-2 text-center rounded-xl bg-slate-50 hover:bg-blue-50 hover:border-blue-200 border border-slate-200 transition-all"
              >
                <span className="block text-xs font-bold text-slate-800">Người thuê</span>
                <span className="block text-[10px] text-slate-500">Tìm phòng</span>
              </button>
              <button
                type="button"
                onClick={() => handleQuickDemo("Landlord")}
                className="p-2 text-center rounded-xl bg-slate-50 hover:bg-blue-50 hover:border-blue-200 border border-slate-200 transition-all"
              >
                <span className="block text-xs font-bold text-slate-800">Chủ nhà</span>
                <span className="block text-[10px] text-slate-500">Quản lý trọ</span>
              </button>
              <button
                type="button"
                onClick={() => handleQuickDemo("Admin")}
                className="p-2 text-center rounded-xl bg-slate-50 hover:bg-blue-50 hover:border-blue-200 border border-slate-200 transition-all"
              >
                <span className="block text-xs font-bold text-slate-800">Admin</span>
                <span className="block text-[10px] text-slate-500">Hệ thống</span>
              </button>
            </div>
          </div>

          {/* Link to Register */}
          <p className="mt-6 text-center text-xs text-slate-600">
            Chưa có tài khoản?{" "}
            <Link href="/auth/register" className="font-bold text-blue-600 hover:text-blue-700">
              Đăng ký ngay
            </Link>
          </p>
        </div>

        {/* Security badge */}
        <div className="flex items-center justify-center gap-2 mt-4 text-[11px] text-slate-400">
          <ShieldCheck className="w-4 h-4 text-emerald-500" />
          <span>Bảo vệ với ASP.NET Core JWT & Anti-CSRF Token</span>
        </div>
      </div>
    </div>
  );
}
