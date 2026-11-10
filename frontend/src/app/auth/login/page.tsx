"use client";

import React, { useState, Suspense } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { useToast } from "@/components/Toast";
import {
  Home,
  Mail,
  Lock,
  LogIn,
  Eye,
  EyeOff,
  AlertCircle,
  AlertTriangle,
  CheckCircle2,
  Loader2,
  ArrowLeft,
  Send,
} from "lucide-react";

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const redirectUrl = searchParams.get("redirect");
  const isSessionExpired = searchParams.get("reason") === "expired";
  const { login, resendConfirmationEmail } = useAuth();
  const { showToast } = useToast();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);

  // Error & Status states matching Identity errors
  const [errorCode, setErrorCode] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Resend confirmation email state
  const [resendingEmail, setResendingEmail] = useState(false);
  const [resendSuccess, setResendSuccess] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim() || !password) {
      setErrorMessage("Vui lòng nhập đầy đủ email và mật khẩu.");
      setErrorCode(null);
      return;
    }

    setLoading(true);
    setErrorMessage(null);
    setErrorCode(null);
    setResendSuccess(null);

    const res = await login(email.trim(), password);
    setLoading(false);

    if (res.success) {
      showToast("Đăng nhập thành công!", "success");

      // Smart redirect based on query param or user role
      if (redirectUrl && redirectUrl !== "/") {
        router.push(redirectUrl);
      } else if (res.user?.role === "Landlord") {
        router.push("/landlord/dashboard");
      } else if (res.user?.role === "Admin") {
        router.push("/admin/dashboard");
      } else {
        router.push("/");
      }
    } else {
      setErrorCode(res.errorCode || null);
      if (res.errorCode === "Identity.InvalidCredentials") {
        setErrorMessage("Email hoặc mật khẩu không chính xác. Vui lòng kiểm tra lại.");
      } else if (res.errorCode === "Identity.EmailNotVerified") {
        setErrorMessage(
          "Địa chỉ email chưa được xác thực. Bạn cần kích hoạt tài khoản qua email trước khi đăng nhập."
        );
      } else if (res.errorCode === "Identity.AccountSuspended") {
        setErrorMessage(
          "Tài khoản của bạn đã bị khóa hoặc tạm ngừng hoạt động. Vui lòng liên hệ ban quản trị để được hỗ trợ."
        );
      } else if (res.errorCode === "Identity.RateLimitExceeded" || res.errorCode === "HTTP_429") {
        setErrorMessage("Bạn đã gửi yêu cầu quá nhiều lần. Vui lòng chờ vài phút rồi thử lại.");
      } else {
        setErrorMessage(res.message || "Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin kết nối.");
      }
    }
  };

  const handleResendConfirmation = async () => {
    if (!email.trim()) {
      showToast("Vui lòng nhập địa chỉ email để gửi lại xác thực.", "info");
      return;
    }
    setResendingEmail(true);
    setResendSuccess(null);
    const result = await resendConfirmationEmail(email.trim());
    setResendingEmail(false);

    if (result.success) {
      setResendSuccess(result.message || "Đã gửi lại email kích hoạt! Vui lòng kiểm tra hộp thư của bạn.");
      showToast("Đã gửi email kích hoạt tài khoản!", "success");
    } else {
      showToast(result.message || "Gửi email thất bại. Vui lòng thử lại sau.", "error");
    }
  };

  return (
    <div className="w-full max-w-md">
      {/* Return home link */}
      <div className="mb-4">
        <Link
          href="/"
          className="inline-flex items-center gap-1.5 text-xs font-semibold text-slate-500 hover:text-blue-600 transition-colors"
        >
          <ArrowLeft className="w-3.5 h-3.5" />
          <span>Về trang chủ HomeMatch</span>
        </Link>
      </div>

      {/* Main Card */}
      <div className="bg-white rounded-3xl shadow-xl shadow-slate-200/50 border border-slate-100 p-6 sm:p-8">
        {/* Brand Header */}
        <div className="text-center mb-6">
          <Link href="/" className="inline-flex items-center justify-center w-12 h-12 rounded-2xl bg-blue-600 text-white shadow-lg shadow-blue-500/30 mb-3 hover:scale-105 transition-transform">
            <Home className="w-6 h-6" />
          </Link>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">Chào mừng trở lại!</h1>
          <p className="text-xs text-slate-500 mt-1">
            Đăng nhập để tiếp tục quản lý phòng trọ hoặc tìm kiếm căn hộ
          </p>
        </div>

        {/* Session Expired Notice */}
        {isSessionExpired && !errorMessage && !errorCode && (
          <div className="mb-5 p-3.5 bg-amber-50 border border-amber-200 rounded-2xl text-xs text-amber-800 font-medium leading-relaxed flex items-start gap-2.5 animate-in fade-in">
            <AlertTriangle className="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
            <span>Phiên làm việc của bạn đã hết hạn. Vui lòng đăng nhập lại để tiếp tục.</span>
          </div>
        )}

        {/* Email Not Verified Special Warning Banner */}
        {errorCode === "Identity.EmailNotVerified" && (
          <div className="mb-5 p-4 bg-amber-50 border border-amber-200 rounded-2xl text-xs space-y-2.5 animate-in fade-in">
            <div className="flex items-start gap-2.5 text-amber-800 font-medium">
              <AlertTriangle className="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
              <span>{errorMessage}</span>
            </div>
            {resendSuccess ? (
              <div className="flex items-center gap-2 p-2.5 bg-emerald-50 border border-emerald-200 rounded-xl text-emerald-800 font-medium text-[11px]">
                <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600 shrink-0" />
                <span>{resendSuccess}</span>
              </div>
            ) : (
              <button
                type="button"
                onClick={handleResendConfirmation}
                disabled={resendingEmail}
                className="w-full py-2 px-3 bg-amber-600 hover:bg-amber-700 text-white font-bold rounded-xl text-[11px] flex items-center justify-center gap-1.5 transition-colors disabled:opacity-60"
              >
                {resendingEmail ? (
                  <>
                    <Loader2 className="w-3.5 h-3.5 animate-spin" />
                    <span>Đang gửi lại email...</span>
                  </>
                ) : (
                  <>
                    <Send className="w-3.5 h-3.5" />
                    <span>Gửi lại email kích hoạt</span>
                  </>
                )}
              </button>
            )}
          </div>
        )}

        {/* General Error Notice */}
        {errorMessage && errorCode !== "Identity.EmailNotVerified" && (
          <div className="mb-5 p-3.5 bg-rose-50 border border-rose-200 rounded-2xl text-xs text-rose-700 font-medium leading-relaxed flex items-start gap-2.5 animate-in fade-in">
            <AlertCircle className="w-4 h-4 text-rose-500 shrink-0 mt-0.5" />
            <span>{errorMessage}</span>
          </div>
        )}

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
              Địa chỉ Email
            </label>
            <div className="relative">
              <Mail className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
              <input
                type="email"
                required
                autoComplete="email"
                placeholder="nguoidung@homematch.vn"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all bg-slate-50/50 hover:bg-white focus:bg-white"
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
                className="text-xs text-blue-600 hover:text-blue-700 font-semibold transition-colors"
              >
                Quên mật khẩu?
              </Link>
            </div>
            <div className="relative">
              <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
              <input
                type={showPassword ? "text" : "password"}
                required
                autoComplete="current-password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full pl-10 pr-10 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all bg-slate-50/50 hover:bg-white focus:bg-white"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3.5 top-3 text-slate-400 hover:text-slate-600 focus:outline-none"
                tabIndex={-1}
              >
                {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
              </button>
            </div>
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full mt-2 py-3 px-4 rounded-xl font-bold text-sm text-white bg-blue-600 hover:bg-blue-700 active:bg-blue-800 shadow-md shadow-blue-500/25 transition-all flex items-center justify-center gap-2 disabled:opacity-60 cursor-pointer"
          >
            {loading ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" />
                <span>Đang xác thực thông tin...</span>
              </>
            ) : (
              <>
                <LogIn className="w-4 h-4" />
                <span>Đăng Nhập</span>
              </>
            )}
          </button>
        </form>

        {/* Link to Register */}
        <p className="mt-6 text-center text-xs text-slate-600">
          Chưa có tài khoản HomeMatch?{" "}
          <Link href="/auth/register" className="font-bold text-blue-600 hover:text-blue-700">
            Đăng ký ngay
          </Link>
        </p>
      </div>
    </div>
  );
}

export default function LoginPage() {
  return (
    <div className="flex-1 flex items-center justify-center p-4 sm:p-8 bg-linear-to-b from-slate-50 via-slate-50/50 to-blue-50/20">
      <Suspense fallback={<div className="text-center text-xs text-slate-500 py-12">Đang tải biểu mẫu...</div>}>
        <LoginForm />
      </Suspense>
    </div>
  );
}
