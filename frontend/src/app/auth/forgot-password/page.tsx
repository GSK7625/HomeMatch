"use client";

import React, { useState } from "react";
import Link from "next/link";
import { authApi } from "@/lib/api-client";
import { useToast } from "@/components/Toast";
import {
  Home,
  Mail,
  ArrowLeft,
  KeyRound,
  CheckCircle2,
  Loader2,
  Send,
  LogIn,
} from "lucide-react";

export default function ForgotPasswordPage() {
  const { showToast } = useToast();

  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);
  const [serverMessage, setServerMessage] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim()) {
      setErrorMessage("Vui lòng nhập địa chỉ email của bạn.");
      return;
    }

    setLoading(true);
    setErrorMessage(null);

    try {
      const res = await authApi.forgotPassword(email.trim());
      setLoading(false);

      if (res.isSuccess) {
        setIsSuccess(true);
        setServerMessage(
          res.data?.message ||
            "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi liên kết đặt lại mật khẩu tới hộp thư của bạn."
        );
        showToast("Đã gửi yêu cầu khôi phục mật khẩu!", "success");
      } else {
        setErrorMessage(
          res.error?.message || "Không thể gửi yêu cầu khôi phục mật khẩu. Vui lòng kiểm tra lại địa chỉ email."
        );
      }
    } catch {
      setLoading(false);
      setErrorMessage("Lỗi kết nối máy chủ. Vui lòng thử lại sau.");
    }
  };

  return (
    <div className="flex-1 flex items-center justify-center p-4 sm:p-8 bg-linear-to-b from-slate-50 via-slate-50/50 to-blue-50/20">
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
            <div className="text-center py-4 space-y-5 animate-in fade-in">
              <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-blue-50 text-blue-600 mb-1">
                <Mail className="w-10 h-10" />
              </div>

              <div>
                <h1 className="text-2xl font-black text-slate-900 tracking-tight">Kiểm Tra Hộp Thư Của Bạn</h1>
                <p className="text-xs text-slate-600 mt-2 leading-relaxed px-2">
                  Chúng tôi đã gửi hướng dẫn và liên kết đặt lại mật khẩu đến:
                </p>
                <p className="text-sm font-bold text-blue-600 mt-1 break-all bg-blue-50 py-1 px-3 rounded-lg inline-block">
                  {email}
                </p>
              </div>

              <div className="p-4 bg-slate-50 border border-slate-200/80 rounded-2xl text-xs text-slate-600 text-left space-y-2">
                <p className="font-bold text-slate-800">Lưu ý bảo mật:</p>
                <ul className="list-disc list-inside space-y-1 text-[11px] text-slate-500">
                  <li>Vui lòng kiểm tra cả mục Spam / Thư rác nếu không tìm thấy email.</li>
                  <li>Liên kết đổi mật khẩu chỉ có hiệu lực một lần trong vòng 15 phút.</li>
                  <li>Nếu bạn không yêu cầu hành động này, bạn có thể yên tâm bỏ qua email.</li>
                </ul>
              </div>

              <div className="space-y-2 pt-2">
                <Link
                  href="/auth/login"
                  className="w-full py-3 px-4 rounded-xl font-bold text-sm text-white bg-blue-600 hover:bg-blue-700 shadow-md shadow-blue-500/25 transition-all flex items-center justify-center gap-2"
                >
                  <LogIn className="w-4 h-4" />
                  <span>Đi đến Đăng nhập</span>
                </Link>

                <button
                  type="button"
                  onClick={() => setIsSuccess(false)}
                  className="w-full py-2.5 px-4 rounded-xl text-xs font-semibold text-slate-600 hover:text-blue-600 hover:bg-slate-50 transition-all flex items-center justify-center gap-2"
                >
                  <Send className="w-3.5 h-3.5" />
                  <span>Gửi lại yêu cầu với email khác</span>
                </button>
              </div>
            </div>
          ) : (
            <>
              {/* Header */}
              <div className="text-center mb-6">
                <div className="inline-flex items-center justify-center w-12 h-12 rounded-2xl bg-blue-600 text-white shadow-lg shadow-blue-500/30 mb-3">
                  <KeyRound className="w-6 h-6" />
                </div>
                <h1 className="text-2xl font-black text-slate-900 tracking-tight">Khôi Phục Mật Khẩu</h1>
                <p className="text-xs text-slate-500 mt-1">
                  Nhập email tài khoản của bạn để nhận liên kết đặt lại mật khẩu mới
                </p>
              </div>

              {/* Error Notice */}
              {errorMessage && (
                <div className="mb-5 p-3.5 bg-rose-50 border border-rose-200 rounded-2xl text-xs text-rose-700 font-medium leading-relaxed">
                  {errorMessage}
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
                      placeholder="name@example.com"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 bg-slate-50/50 hover:bg-white focus:bg-white transition-all"
                    />
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
                      <span>Đang gửi liên kết...</span>
                    </>
                  ) : (
                    <span>Gửi Liên Kết Đặt Lại Mật Khẩu</span>
                  )}
                </button>
              </form>

              {/* Back to Login */}
              <p className="mt-6 text-center text-xs text-slate-600">
                Nhớ mật khẩu của bạn?{" "}
                <Link href="/auth/login" className="font-bold text-blue-600 hover:text-blue-700">
                  Đăng nhập
                </Link>
              </p>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
