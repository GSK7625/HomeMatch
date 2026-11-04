"use client";

import React, { useState, useEffect, Suspense } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { authApi } from "@/lib/api-client";
import { useToast } from "@/components/Toast";
import {
  Home,
  Lock,
  Eye,
  EyeOff,
  CheckCircle2,
  XCircle,
  AlertCircle,
  Loader2,
  ArrowLeft,
  LogIn,
  KeyRound,
} from "lucide-react";

function ResetPasswordContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { showToast } = useToast();

  const userIdParam = searchParams.get("userId");
  const tokenParam = searchParams.get("token");

  const [userId, setUserId] = useState<number | null>(null);
  const [token, setToken] = useState<string>("");

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);
  const [countdown, setCountdown] = useState(3);

  useEffect(() => {
    if (!userIdParam || !tokenParam) {
      setErrorMessage("Liên kết đặt lại mật khẩu không hợp lệ hoặc thiếu thông tin xác thực.");
      return;
    }

    const parsedId = parseInt(userIdParam, 10);
    if (isNaN(parsedId)) {
      setErrorMessage("Mã người dùng không hợp lệ.");
      return;
    }

    setUserId(parsedId);
    setToken(tokenParam);
  }, [userIdParam, tokenParam]);

  // Countdown auto redirect on success
  useEffect(() => {
    if (!isSuccess) return;

    const timer = setInterval(() => {
      setCountdown((prev) => {
        if (prev <= 1) {
          clearInterval(timer);
          router.push("/auth/login");
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [isSuccess, router]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!userId || !token) {
      setErrorMessage("Thông tin token hoặc người dùng không hợp lệ. Vui lòng thử yêu cầu lại liên kết mới.");
      return;
    }

    if (newPassword.length < 8) {
      setErrorMessage("Mật khẩu mới phải có độ dài tối thiểu 8 ký tự.");
      return;
    }

    const hasLetter = /[a-zA-Z]/.test(newPassword);
    const hasDigit = /\d/.test(newPassword);
    if (!hasLetter || !hasDigit) {
      setErrorMessage("Mật khẩu mới phải bao gồm cả chữ cái và chữ số.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setErrorMessage("Mật khẩu xác nhận không khớp!");
      return;
    }

    setLoading(true);
    setErrorMessage(null);

    try {
      const res = await authApi.resetPassword(userId, token, newPassword);
      setLoading(false);

      if (res.isSuccess) {
        setIsSuccess(true);
        showToast("Đặt lại mật khẩu thành công!", "success");
      } else {
        setErrorMessage(
          res.error?.message ||
            "Mã token đặt lại mật khẩu không hợp lệ, đã hết hạn hoặc đã được sử dụng. Vui lòng yêu cầu lại."
        );
      }
    } catch {
      setLoading(false);
      setErrorMessage("Lỗi kết nối máy chủ. Vui lòng thử lại sau.");
    }
  };

  return (
    <div className="w-full max-w-md">
      {/* Back Link */}
      <div className="mb-4">
        <Link
          href="/auth/login"
          className="inline-flex items-center gap-1.5 text-xs font-semibold text-slate-500 hover:text-blue-600 transition-colors"
        >
          <ArrowLeft className="w-3.5 h-3.5" />
          <span>Quay lại trang Đăng nhập</span>
        </Link>
      </div>

      {/* Card */}
      <div className="bg-white rounded-3xl shadow-xl shadow-slate-200/50 border border-slate-100 p-6 sm:p-8">
        {isSuccess ? (
          <div className="text-center py-4 space-y-4 animate-in fade-in">
            <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-emerald-50 text-emerald-600 mb-2">
              <CheckCircle2 className="w-10 h-10" />
            </div>

            <h1 className="text-2xl font-black text-slate-900 tracking-tight">Thành Công!</h1>
            <p className="text-xs text-slate-600 leading-relaxed px-2">
              Mật khẩu mới của bạn đã được cập nhật thành công. Toàn bộ các phiên đăng nhập cũ trên các thiết bị khác đã được thu hồi vì lý do an toàn.
            </p>

            <div className="p-3 bg-emerald-50/70 border border-emerald-100 rounded-2xl text-[11px] text-emerald-800 font-medium">
              Tự động chuyển về trang đăng nhập sau <span className="font-bold text-emerald-900">{countdown}s</span>...
            </div>

            <Link
              href="/auth/login"
              className="w-full py-3 px-4 rounded-xl font-bold text-sm text-white bg-blue-600 hover:bg-blue-700 shadow-md shadow-blue-500/25 transition-all flex items-center justify-center gap-2"
            >
              <LogIn className="w-4 h-4" />
              <span>Đăng Nhập Ngay</span>
            </Link>
          </div>
        ) : (
          <>
            {/* Header */}
            <div className="text-center mb-6">
              <div className="inline-flex items-center justify-center w-12 h-12 rounded-2xl bg-blue-600 text-white shadow-lg shadow-blue-500/30 mb-3">
                <KeyRound className="w-6 h-6" />
              </div>
              <h1 className="text-2xl font-black text-slate-900 tracking-tight">Đặt Lại Mật Khẩu Mới</h1>
              <p className="text-xs text-slate-500 mt-1">
                Tạo mật khẩu an toàn mới cho tài khoản HomeMatch của bạn
              </p>
            </div>

            {/* Error Notice */}
            {errorMessage && (
              <div className="mb-5 p-3.5 bg-rose-50 border border-rose-200 rounded-2xl text-xs text-rose-700 font-medium leading-relaxed flex items-start gap-2.5">
                <AlertCircle className="w-4 h-4 text-rose-500 shrink-0 mt-0.5" />
                <div className="space-y-1">
                  <span>{errorMessage}</span>
                  {(!userId || !token) && (
                    <div>
                      <Link
                        href="/auth/forgot-password"
                        className="font-bold text-blue-600 hover:text-blue-700 underline text-[11px]"
                      >
                        Yêu cầu liên kết đặt lại mật khẩu mới
                      </Link>
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* Form */}
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
                  Mật khẩu mới
                </label>
                <div className="relative">
                  <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                  <input
                    type={showPassword ? "text" : "password"}
                    required
                    autoComplete="new-password"
                    placeholder="Mật khẩu mới"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    className="w-full pl-10 pr-10 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 bg-slate-50/50 hover:bg-white focus:bg-white transition-all"
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

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
                  Xác nhận mật khẩu mới
                </label>
                <div className="relative">
                  <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                  <input
                    type={showPassword ? "text" : "password"}
                    required
                    autoComplete="new-password"
                    placeholder="Nhập lại mật khẩu mới"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 bg-slate-50/50 hover:bg-white focus:bg-white transition-all"
                  />
                </div>
              </div>

              <p className="text-[11px] text-slate-500 flex items-center gap-1.5 pt-0.5">
                <span className="w-1.5 h-1.5 rounded-full bg-blue-500 shrink-0"></span>
                <span>Tối thiểu 8 ký tự, phải bao gồm cả chữ cái và chữ số.</span>
              </p>

              <button
                type="submit"
                disabled={loading || !userId || !token}
                className="w-full mt-2 py-3 px-4 rounded-xl font-bold text-sm text-white bg-blue-600 hover:bg-blue-700 active:bg-blue-800 shadow-md shadow-blue-500/25 transition-all flex items-center justify-center gap-2 disabled:opacity-60 cursor-pointer"
              >
                {loading ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    <span>Đang cập nhật mật khẩu...</span>
                  </>
                ) : (
                  <span>Cập Nhật Mật Khẩu</span>
                )}
              </button>
            </form>
          </>
        )}
      </div>
    </div>
  );
}

export default function ResetPasswordPage() {
  return (
    <div className="flex-1 flex items-center justify-center p-4 sm:p-8 bg-linear-to-b from-slate-50 via-slate-50/50 to-blue-50/20">
      <Suspense fallback={<div className="text-center text-xs text-slate-500 py-12">Đang tải biểu mẫu...</div>}>
        <ResetPasswordContent />
      </Suspense>
    </div>
  );
}
