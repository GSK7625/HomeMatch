"use client";

import React, { useEffect, useState, Suspense } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { authApi } from "@/lib/api-client";
import { useToast } from "@/components/Toast";
import {
  Home,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Loader2,
  ArrowRight,
  Mail,
  Send,
  LogIn,
} from "lucide-react";

function VerifyEmailContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { showToast } = useToast();

  const userIdParam = searchParams.get("userId");
  const tokenParam = searchParams.get("token");

  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");
  const [message, setMessage] = useState<string>("Đang kích hoạt tài khoản của bạn...");
  const [countdown, setCountdown] = useState<number>(3);

  // Resend state for error fallback
  const [resendEmail, setResendEmail] = useState("");
  const [resending, setResending] = useState(false);
  const [resendSuccess, setResendSuccess] = useState<string | null>(null);

  useEffect(() => {
    if (!userIdParam || !tokenParam) {
      setStatus("error");
      setMessage("Đường dẫn kích hoạt không hợp lệ hoặc thiếu thông tin mã xác thực.");
      return;
    }

    const userId = parseInt(userIdParam, 10);
    if (isNaN(userId)) {
      setStatus("error");
      setMessage("Mã định danh người dùng không hợp lệ.");
      return;
    }

    let isMounted = true;

    async function executeVerification() {
      try {
        const res = await authApi.confirmEmail(userId, tokenParam || "");
        if (!isMounted) return;

        if (res.isSuccess) {
          setStatus("success");
          setMessage(res.data?.message || "Tài khoản của bạn đã được kích hoạt thành công!");
          showToast("Kích hoạt tài khoản thành công!", "success");
        } else {
          setStatus("error");
          setMessage(res.error?.message || "Mã kích hoạt không hợp lệ, đã hết hạn hoặc đã được sử dụng.");
        }
      } catch {
        if (!isMounted) return;
        setStatus("error");
        setMessage("Lỗi kết nối máy chủ khi xác thực email. Vui lòng thử lại sau.");
      }
    }

    executeVerification();

    return () => {
      isMounted = false;
    };
  }, [userIdParam, tokenParam, showToast]);

  // Auto-redirect countdown on success
  useEffect(() => {
    if (status !== "success") return;

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
  }, [status, router]);

  const handleResend = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!resendEmail.trim()) {
      showToast("Vui lòng nhập email để gửi lại link kích hoạt.", "error");
      return;
    }

    setResending(true);
    setResendSuccess(null);
    try {
      const res = await authApi.resendConfirmation(resendEmail.trim());
      setResending(false);
      if (res.isSuccess) {
        setResendSuccess(res.data?.message || "Đã gửi email kích hoạt mới. Vui lòng kiểm tra hộp thư!");
        showToast("Đã gửi email kích hoạt!", "success");
      } else {
        showToast(res.error?.message || "Không thể gửi email. Vui lòng kiểm tra lại địa chỉ email.", "error");
      }
    } catch {
      setResending(false);
      showToast("Lỗi kết nối máy chủ.", "error");
    }
  };

  return (
    <div className="w-full max-w-md">
      <div className="bg-white rounded-3xl shadow-xl shadow-slate-200/50 border border-slate-100 p-8 text-center">
        {/* Brand Logo */}
        <div className="inline-flex items-center justify-center w-12 h-12 rounded-2xl bg-blue-600 text-white shadow-lg shadow-blue-500/30 mb-6">
          <Home className="w-6 h-6" />
        </div>

        {/* Status: Loading */}
        {status === "loading" && (
          <div className="space-y-4 py-4 animate-in fade-in">
            <div className="flex justify-center">
              <Loader2 className="w-12 h-12 text-blue-600 animate-spin" />
            </div>
            <h1 className="text-xl font-black text-slate-900 tracking-tight">Đang kích hoạt tài khoản</h1>
            <p className="text-xs text-slate-500 max-w-xs mx-auto leading-relaxed">
              Hệ thống đang đối chiếu mã xác thực và kích hoạt quyền truy cập cho bạn...
            </p>
          </div>
        )}

        {/* Status: Success */}
        {status === "success" && (
          <div className="space-y-4 py-2 animate-in fade-in">
            <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-emerald-50 text-emerald-600 mb-2">
              <CheckCircle2 className="w-10 h-10" />
            </div>
            <h1 className="text-2xl font-black text-slate-900 tracking-tight">Kích Hoạt Thành Công!</h1>
            <p className="text-xs text-slate-600 leading-relaxed px-4">{message}</p>

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
        )}

        {/* Status: Error */}
        {status === "error" && (
          <div className="space-y-4 py-2 animate-in fade-in text-left">
            <div className="text-center">
              <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-rose-50 text-rose-600 mb-2">
                <XCircle className="w-10 h-10" />
              </div>
              <h1 className="text-2xl font-black text-slate-900 tracking-tight">Kích Hoạt Thất Bại</h1>
              <p className="text-xs text-slate-600 mt-1 leading-relaxed">{message}</p>
            </div>

            {/* Resend Link Box */}
            <div className="p-4 bg-slate-50 border border-slate-200/80 rounded-2xl text-xs space-y-3 mt-4">
              <div className="flex items-center gap-2 font-bold text-slate-800">
                <AlertTriangle className="w-4 h-4 text-amber-500 shrink-0" />
                <span>Bạn cần gửi lại link kích hoạt mới?</span>
              </div>
              <p className="text-[11px] text-slate-500 leading-relaxed">
                Nhập địa chỉ email tài khoản của bạn để nhận liên kết xác thực mới:
              </p>

              {resendSuccess ? (
                <div className="p-2.5 bg-emerald-50 border border-emerald-200 rounded-xl text-emerald-800 text-[11px] font-medium flex items-center gap-2">
                  <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600 shrink-0" />
                  <span>{resendSuccess}</span>
                </div>
              ) : (
                <form onSubmit={handleResend} className="space-y-2">
                  <div className="relative">
                    <Mail className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
                    <input
                      type="email"
                      required
                      placeholder="email@example.com"
                      value={resendEmail}
                      onChange={(e) => setResendEmail(e.target.value)}
                      className="w-full pl-9 pr-3 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                    />
                  </div>
                  <button
                    type="submit"
                    disabled={resending}
                    className="w-full py-2 px-3 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-xl text-xs flex items-center justify-center gap-1.5 transition-colors disabled:opacity-60 cursor-pointer"
                  >
                    {resending ? (
                      <>
                        <Loader2 className="w-3.5 h-3.5 animate-spin" />
                        <span>Đang gửi email...</span>
                      </>
                    ) : (
                      <>
                        <Send className="w-3.5 h-3.5" />
                        <span>Gửi lại email kích hoạt</span>
                      </>
                    )}
                  </button>
                </form>
              )}
            </div>

            <div className="pt-2 text-center">
              <Link
                href="/auth/login"
                className="inline-flex items-center gap-1.5 text-xs font-bold text-blue-600 hover:text-blue-700"
              >
                <span>Về trang đăng nhập</span>
                <ArrowRight className="w-3.5 h-3.5" />
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default function VerifyEmailPage() {
  return (
    <div className="flex-1 flex items-center justify-center p-4 sm:p-8 bg-linear-to-b from-slate-50 via-slate-50/50 to-blue-50/20">
      <Suspense fallback={<div className="text-center text-xs text-slate-500 py-12">Đang tải trang xác thực...</div>}>
        <VerifyEmailContent />
      </Suspense>
    </div>
  );
}
