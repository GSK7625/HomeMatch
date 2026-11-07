"use client";

import React, { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { useToast } from "@/components/Toast";
import {
  Home,
  Mail,
  Lock,
  User,
  Building,
  CheckCircle2,
  AlertCircle,
  Eye,
  EyeOff,
  Loader2,
  Send,
  LogIn,
  ArrowLeft,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function RegisterPage() {
  const router = useRouter();
  const { register, resendConfirmationEmail } = useAuth();
  const { showToast } = useToast();

  const [role, setRole] = useState<"Tenant" | "Landlord">("Tenant");
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Success state after registration
  const [isSuccess, setIsSuccess] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");
  const [resending, setResending] = useState(false);
  const [resendStatus, setResendStatus] = useState<string | null>(null);

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    if (fullName.trim().length < 2) {
      setErrorMessage("Họ và tên phải có tối thiểu 2 ký tự!");
      return;
    }

    if (password.length < 8) {
      setErrorMessage("Mật khẩu phải có độ dài tối thiểu 8 ký tự!");
      return;
    }

    const hasLetter = /[a-zA-Z]/.test(password);
    const hasDigit = /\d/.test(password);
    if (!hasLetter || !hasDigit) {
      setErrorMessage("Mật khẩu phải bao gồm cả chữ cái và chữ số!");
      return;
    }

    if (password !== confirmPassword) {
      setErrorMessage("Mật khẩu xác nhận không khớp!");
      return;
    }

    setLoading(true);

    const res = await register({
      email: email.trim(),
      password,
      fullName: fullName.trim(),
      role,
    });

    setLoading(false);

    if (res.success) {
      setIsSuccess(true);
      setSuccessMessage(res.message || "Đăng ký thành công. Vui lòng kiểm tra email để kích hoạt tài khoản.");
      showToast("Đăng ký thành công!", "success");
    } else {
      let msg = res.message || "Đăng ký không thành công. Vui lòng thử lại.";
      if (msg === "Dữ liệu yêu cầu không hợp lệ.") {
        msg = "Thông tin không hợp lệ. Vui lòng đảm bảo họ tên từ 2 ký tự và mật khẩu từ 8 ký tự gồm cả chữ và số.";
      }
      setErrorMessage(msg);
    }
  };

  const handleResendEmail = async () => {
    setResending(true);
    setResendStatus(null);
    const res = await resendConfirmationEmail(email.trim());
    setResending(false);
    if (res.success) {
      setResendStatus(res.message || "Đã gửi lại email kích hoạt mới thành công!");
      showToast("Đã gửi lại email kích hoạt!", "success");
    } else {
      showToast(res.message || "Gửi email thất bại. Vui lòng thử lại sau.", "error");
    }
  };

  return (
    <div className="flex-1 flex items-center justify-center p-4 sm:p-8 bg-linear-to-b from-slate-50 via-slate-50/50 to-blue-50/20">
      <div className="w-full max-w-lg">
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

        {/* Card */}
        <div className="bg-white rounded-3xl shadow-xl shadow-slate-200/50 border border-slate-100 p-6 sm:p-8">
          {/* Success Screen */}
          {isSuccess ? (
            <div className="text-center py-4 space-y-5 animate-in fade-in">
              <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-emerald-50 text-emerald-600 mb-1">
                <CheckCircle2 className="w-10 h-10" />
              </div>

              <div>
                <h1 className="text-2xl font-black text-slate-900 tracking-tight">Đăng Ký Thành Công!</h1>
                <p className="text-xs text-slate-600 mt-2 leading-relaxed px-2">
                  Hệ thống HomeMatch đã gửi một liên kết kích hoạt đến địa chỉ email:
                </p>
                <p className="text-sm font-bold text-blue-600 mt-1 break-all bg-blue-50 py-1 px-3 rounded-lg inline-block">
                  {email}
                </p>
              </div>

              <div className="p-4 bg-slate-50 border border-slate-200/80 rounded-2xl text-xs text-slate-600 text-left space-y-2">
                <p className="font-bold text-slate-800">Các bước tiếp theo:</p>
                <ul className="list-disc list-inside space-y-1 text-[11px] text-slate-500">
                  <li>Mở hộp thư đến của bạn (kiểm tra cả thư mục Spam/Quảng cáo).</li>
                  <li>Nhấp vào nút <span className="font-bold text-slate-700">"Kích hoạt tài khoản"</span> trong email.</li>
                  <li>Sau khi kích hoạt hoàn tất, bạn có thể đăng nhập ngay vào hệ thống.</li>
                </ul>
              </div>

              {resendStatus && (
                <div className="p-2.5 bg-emerald-50 border border-emerald-200 rounded-xl text-emerald-800 text-xs font-medium flex items-center justify-center gap-2">
                  <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600 shrink-0" />
                  <span>{resendStatus}</span>
                </div>
              )}

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
                  onClick={handleResendEmail}
                  disabled={resending}
                  className="w-full py-2.5 px-4 rounded-xl text-xs font-semibold text-slate-600 hover:text-blue-600 hover:bg-slate-50 transition-all flex items-center justify-center gap-2 disabled:opacity-50"
                >
                  {resending ? (
                    <>
                      <Loader2 className="w-3.5 h-3.5 animate-spin" />
                      <span>Đang gửi lại...</span>
                    </>
                  ) : (
                    <>
                      <Send className="w-3.5 h-3.5" />
                      <span>Chưa nhận được email? Gửi lại</span>
                    </>
                  )}
                </button>
              </div>
            </div>
          ) : (
            <>
              {/* Header */}
              <div className="text-center mb-6">
                <div className="inline-flex items-center justify-center w-12 h-12 rounded-2xl bg-blue-600 text-white shadow-lg shadow-blue-500/30 mb-3">
                  <Home className="w-6 h-6" />
                </div>
                <h1 className="text-2xl font-black text-slate-900 tracking-tight">Tạo Tài Khoản HomeMatch</h1>
                <p className="text-xs text-slate-500 mt-1">
                  Đăng ký để tìm kiếm phòng hoặc đăng tin quản lý nhà trọ
                </p>
              </div>

              {/* Role selector tabs */}
              <div className="grid grid-cols-2 gap-2 mb-6 p-1.5 bg-slate-100 rounded-2xl">
                <button
                  type="button"
                  onClick={() => setRole("Tenant")}
                  className={cn(
                    "py-2.5 px-3 rounded-xl text-xs font-bold transition-all flex items-center justify-center gap-2 cursor-pointer",
                    role === "Tenant"
                      ? "bg-white text-blue-600 shadow-sm"
                      : "text-slate-600 hover:text-slate-900"
                  )}
                >
                  <User className="w-4 h-4" />
                  <span>Tôi là Người thuê</span>
                </button>
                <button
                  type="button"
                  onClick={() => setRole("Landlord")}
                  className={cn(
                    "py-2.5 px-3 rounded-xl text-xs font-bold transition-all flex items-center justify-center gap-2 cursor-pointer",
                    role === "Landlord"
                      ? "bg-white text-blue-600 shadow-sm"
                      : "text-slate-600 hover:text-slate-900"
                  )}
                >
                  <Building className="w-4 h-4" />
                  <span>Tôi là Chủ nhà</span>
                </button>
              </div>

              {/* Error Notice */}
              {errorMessage && (
                <div className="mb-5 p-3.5 bg-rose-50 border border-rose-200 rounded-2xl text-xs text-rose-700 font-medium leading-relaxed flex items-start gap-2.5">
                  <AlertCircle className="w-4 h-4 text-rose-500 shrink-0 mt-0.5" />
                  <span>{errorMessage}</span>
                </div>
              )}

              {/* Form */}
              <form onSubmit={handleRegister} className="space-y-4" autoComplete="off">
                <div>
                  <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
                    Họ và tên
                  </label>
                  <div className="relative">
                    <User className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                    <input
                      type="text"
                      required
                      placeholder="Ví dụ: Nguyễn Văn A"
                      value={fullName}
                      onChange={(e) => setFullName(e.target.value)}
                      className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 bg-slate-50/50 hover:bg-white focus:bg-white transition-all"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
                    Địa chỉ Email
                  </label>
                  <div className="relative">
                    <Mail className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                    <input
                      type="email"
                      required
                      placeholder="name@example.com"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 bg-slate-50/50 hover:bg-white focus:bg-white transition-all"
                    />
                  </div>
                </div>

                <div className="space-y-1.5">
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                    <div>
                      <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
                        Mật khẩu
                      </label>
                      <div className="relative">
                        <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                        <input
                          type={showPassword ? "text" : "password"}
                          required
                          autoComplete="new-password"
                          placeholder="Mật khẩu"
                          value={password}
                          onChange={(e) => setPassword(e.target.value)}
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
                        Xác nhận mật khẩu
                      </label>
                      <div className="relative">
                        <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                        <input
                          type={showPassword ? "text" : "password"}
                          required
                          autoComplete="new-password"
                          placeholder="Nhập lại mật khẩu"
                          value={confirmPassword}
                          onChange={(e) => setConfirmPassword(e.target.value)}
                          className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 bg-slate-50/50 hover:bg-white focus:bg-white transition-all"
                        />
                      </div>
                    </div>
                  </div>
                  <p className="text-[11px] text-slate-500 flex items-center gap-1.5 pt-1">
                    <span className="w-1.5 h-1.5 rounded-full bg-blue-500 shrink-0"></span>
                    <span>Tối thiểu 8 ký tự, phải bao gồm cả chữ cái và chữ số.</span>
                  </p>
                </div>

                <button
                  type="submit"
                  disabled={loading}
                  className="w-full mt-4 py-3 px-4 rounded-xl font-bold text-sm text-white bg-blue-600 hover:bg-blue-700 active:bg-blue-800 shadow-md shadow-blue-500/25 transition-all flex items-center justify-center gap-2 disabled:opacity-60 cursor-pointer"
                >
                  {loading ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin" />
                      <span>Đang tạo tài khoản...</span>
                    </>
                  ) : (
                    <span>Đăng Ký Tài Khoản</span>
                  )}
                </button>
              </form>

              {/* Link to Login */}
              <p className="mt-6 text-center text-xs text-slate-600">
                Đã có tài khoản HomeMatch?{" "}
                <Link href="/auth/login" className="font-bold text-blue-600 hover:text-blue-700">
                  Đăng nhập ngay
                </Link>
              </p>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
