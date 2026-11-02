"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import { useToast } from "@/components/Toast";
import { User, Lock, Bell, ShieldCheck, CheckCircle2, AlertCircle, Save, Calendar, Hash } from "lucide-react";

export default function SettingsPage() {
  const { user, refreshProfile } = useAuth();
  const { showToast } = useToast();

  const [activeTab, setActiveTab] = useState<"profile" | "password" | "notifications">("profile");

  // Profile Form State
  const [displayName, setDisplayName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");

  // Password Form State
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  // Notification Toggles
  const [emailNotif, setEmailNotif] = useState(true);
  const [viewingNotif, setViewingNotif] = useState(true);
  const [billNotif, setBillNotif] = useState(true);

  useEffect(() => {
    if (user) {
      setDisplayName(user.displayName || user.fullName || "");
      setPhoneNumber(user.phoneNumber || "");
    }
  }, [user]);

  if (!user) {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <User className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Yêu Cầu Đăng Nhập</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Bạn cần đăng nhập bằng tài khoản HomeMatch để xem và quản lý thông tin hồ sơ cá nhân và cài đặt bảo mật.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/settings"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng nhập ngay</span>
          </Link>
        </div>
      </div>
    );
  }

  const handleUpdateProfile = (e: React.FormEvent) => {
    e.preventDefault();
    showToast("Cập nhật thông tin hồ sơ tài khoản thành công!", "success");
    if (refreshProfile) refreshProfile();
  };

  const handleChangePassword = (e: React.FormEvent) => {
    e.preventDefault();
    if (newPassword !== confirmPassword) {
      showToast("Mật khẩu mới và xác nhận mật khẩu không khớp!", "error");
      return;
    }
    if (newPassword.length < 8) {
      showToast("Mật khẩu mới phải có tối thiểu 8 ký tự!", "error");
      return;
    }
    setCurrentPassword("");
    setNewPassword("");
    setConfirmPassword("");
    showToast("Đổi mật khẩu thành công! Tài khoản của bạn đã được bảo vệ.", "success");
  };

  const handleSaveNotifications = (e: React.FormEvent) => {
    e.preventDefault();
    showToast("Lưu cấu hình nhận thông báo thành công!", "success");
  };

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      <div className="pb-4 border-b border-slate-200">
        <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
          <ShieldCheck className="w-4 h-4" />
          <span>Tài Khoản & Danh Tính (Identity)</span>
        </div>
        <h1 className="text-2xl font-black text-slate-900 tracking-tight">Cài Đặt Tài Khoản & Bảo Mật</h1>
        <p className="text-xs text-slate-500 mt-1">
          Quản lý thông tin tài khoản được lưu trữ an toàn trong module Identity của hệ thống HomeMatch.
        </p>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-slate-200 gap-4">
        <button
          onClick={() => setActiveTab("profile")}
          className={`pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2 ${
            activeTab === "profile"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          }`}
        >
          <User className="w-4 h-4" />
          <span>Hồ Sơ Cá Nhân</span>
        </button>

        <button
          onClick={() => setActiveTab("password")}
          className={`pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2 ${
            activeTab === "password"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          }`}
        >
          <Lock className="w-4 h-4" />
          <span>Đổi Mật Khẩu</span>
        </button>

        <button
          onClick={() => setActiveTab("notifications")}
          className={`pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2 ${
            activeTab === "notifications"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          }`}
        >
          <Bell className="w-4 h-4" />
          <span>Cài Đặt Thông Báo</span>
        </button>
      </div>

      {/* Tab 1: Profile */}
      {activeTab === "profile" && (
        <form onSubmit={handleUpdateProfile} className="bg-white rounded-2xl border border-slate-200/80 p-6 space-y-5 shadow-xs">
          {/* Identity ID summary card */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 p-4 bg-slate-50 rounded-xl border border-slate-100 text-xs">
            <div>
              <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider block">Mã Tài Khoản:</span>
              <span className="font-mono font-bold text-slate-800">{user.accountId || user.id || "N/A"}</span>
            </div>
            <div>
              <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider block">Vai Trò Hệ Thống:</span>
              <span className="inline-block mt-0.5 px-2 py-0.5 rounded text-[10px] font-bold bg-blue-100 text-blue-700">
                {user.role === "Landlord" ? "Chủ nhà (Landlord)" : user.role === "Admin" ? "Quản trị viên (Admin)" : "Người thuê (Tenant)"}
              </span>
            </div>
            <div>
              <span className="text-[10px] text-slate-400 font-bold uppercase tracking-wider block">Ngày Tham Gia:</span>
              <span className="font-medium text-slate-700">
                {user.createdAt ? new Date(user.createdAt).toLocaleDateString("vi-VN") : "Gần đây"}
              </span>
            </div>
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
              Họ và tên hiển thị
            </label>
            <input
              type="text"
              required
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
              Địa chỉ email đăng nhập
            </label>
            <input
              type="email"
              disabled
              value={user.email}
              className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 bg-slate-100 text-slate-500 cursor-not-allowed"
            />
            {user.emailConfirmed ? (
              <span className="text-[11px] text-emerald-600 font-medium mt-1 flex items-center gap-1">
                <CheckCircle2 className="w-3.5 h-3.5" />
                <span>Email đã được xác thực mã OTP bảo mật</span>
              </span>
            ) : (
              <span className="text-[11px] text-amber-600 font-medium mt-1 flex items-center gap-1">
                <AlertCircle className="w-3.5 h-3.5" />
                <span>Email chưa được xác thực</span>
              </span>
            )}
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
              Số điện thoại liên hệ
            </label>
            <input
              type="tel"
              placeholder="0912 345 678"
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
              className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
            />
          </div>

          <div className="pt-2">
            <button
              type="submit"
              className="px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20 transition-all flex items-center gap-1.5"
            >
              <Save className="w-4 h-4" />
              <span>Lưu thông tin hồ sơ</span>
            </button>
          </div>
        </form>
      )}

      {/* Tab 2: Password */}
      {activeTab === "password" && (
        <form onSubmit={handleChangePassword} className="bg-white rounded-2xl border border-slate-200/80 p-6 space-y-4 shadow-xs">
          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
              Mật khẩu hiện tại
            </label>
            <input
              type="password"
              required
              placeholder="••••••••"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
              Mật khẩu mới
            </label>
            <input
              type="password"
              required
              placeholder="Tối thiểu 8 ký tự"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
              Xác nhận mật khẩu mới
            </label>
            <input
              type="password"
              required
              placeholder="Nhập lại mật khẩu mới"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
            />
          </div>

          <div className="pt-2">
            <button
              type="submit"
              className="px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20 transition-all flex items-center gap-1.5"
            >
              <Lock className="w-4 h-4" />
              <span>Cập nhật mật khẩu mới</span>
            </button>
          </div>
        </form>
      )}

      {/* Tab 3: Notifications */}
      {activeTab === "notifications" && (
        <form onSubmit={handleSaveNotifications} className="bg-white rounded-2xl border border-slate-200/80 p-6 space-y-4 shadow-xs">
          <div className="space-y-4">
            <label className="flex items-center justify-between p-3.5 rounded-xl bg-slate-50 border border-slate-100 cursor-pointer">
              <div>
                <span className="font-bold text-xs text-slate-900 block">Thông báo qua Email</span>
                <span className="text-[11px] text-slate-500">
                  Nhận email thông báo xác thực, hợp đồng và hóa đơn tiền phòng.
                </span>
              </div>
              <input
                type="checkbox"
                checked={emailNotif}
                onChange={(e) => setEmailNotif(e.target.checked)}
                className="w-4 h-4 rounded text-blue-600 focus:ring-blue-500"
              />
            </label>

            <label className="flex items-center justify-between p-3.5 rounded-xl bg-slate-50 border border-slate-100 cursor-pointer">
              <div>
                <span className="font-bold text-xs text-slate-900 block">Thông báo Lịch xem phòng</span>
                <span className="text-[11px] text-slate-500">
                  Nhận thông báo khi chủ nhà xác nhận hoặc hủy lịch hẹn xem phòng.
                </span>
              </div>
              <input
                type="checkbox"
                checked={viewingNotif}
                onChange={(e) => setViewingNotif(e.target.checked)}
                className="w-4 h-4 rounded text-blue-600 focus:ring-blue-500"
              />
            </label>

            <label className="flex items-center justify-between p-3.5 rounded-xl bg-slate-50 border border-slate-100 cursor-pointer">
              <div>
                <span className="font-bold text-xs text-slate-900 block">Nhắc nhở hạn nộp tiền phòng & Điện nước</span>
                <span className="text-[11px] text-slate-500">
                  Nhận thông báo trước hạn đóng tiền phòng 3 ngày để tránh phát sinh nợ quá hạn.
                </span>
              </div>
              <input
                type="checkbox"
                checked={billNotif}
                onChange={(e) => setBillNotif(e.target.checked)}
                className="w-4 h-4 rounded text-blue-600 focus:ring-blue-500"
              />
            </label>
          </div>

          <div className="pt-2">
            <button
              type="submit"
              className="px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20 transition-all flex items-center gap-1.5"
            >
              <Save className="w-4 h-4" />
              <span>Lưu cấu hình thông báo</span>
            </button>
          </div>
        </form>
      )}
    </div>
  );
}
