"use client";

import React, { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { useToast } from "@/components/Toast";
import { authApi, setStoredToken, setStoredUser } from "@/lib/api-client";
import { cn } from "@/lib/utils";
import {
  User,
  Lock,
  Bell,
  ShieldCheck,
  Save,
  Eye,
  EyeOff,
  Loader2,
  CheckCircle2,
  AlertCircle,
} from "lucide-react";

type SettingsTab = "profile" | "password" | "notifications";

export default function AccountSettingsPage() {
  const router = useRouter();
  const { user, role, isLoading, refreshProfile } = useAuth();
  const { showToast } = useToast();

  const [activeTab, setActiveTab] = useState<SettingsTab>("profile");

  // 1. Hồ sơ tài khoản
  const [fullName, setFullName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [dateOfBirth, setDateOfBirth] = useState("");
  const [gender, setGender] = useState("");
  const [savingProfile, setSavingProfile] = useState(false);
  const [profileError, setProfileError] = useState<string | null>(null);
  const [profileFieldErrors, setProfileFieldErrors] = useState<Record<string, string>>({});

  // 2. Đổi mật khẩu
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [changingPassword, setChangingPassword] = useState(false);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [passwordFieldErrors, setPasswordFieldErrors] = useState<Record<string, string>>({});

  // 3. Cài đặt thông báo
  const [notifyPush, setNotifyPush] = useState(true);
  const [notifUpdatedAt, setNotifUpdatedAt] = useState<string | null>(null);
  const [loadingNotif, setLoadingNotif] = useState(false);
  const [savingNotif, setSavingNotif] = useState(false);

  // Chuyển hướng nếu chưa đăng nhập
  useEffect(() => {
    if (!isLoading && !user) {
      router.replace("/auth/login?redirect=/settings");
    }
  }, [isLoading, user, router]);

  // Đồng bộ dữ liệu hồ sơ từ AuthContext
  useEffect(() => {
    if (!user) return;
    setFullName(user.fullName || user.displayName || "");
    setPhoneNumber(user.phoneNumber || "");
    setDateOfBirth(user.dateOfBirth ? user.dateOfBirth.slice(0, 10) : "");
    setGender(user.gender || "");
    if (typeof user.notifyPush === "boolean") {
      setNotifyPush(user.notifyPush);
    }
  }, [user]);

  // Tải cấu hình thông báo mới nhất khi mở tab Thông báo
  useEffect(() => {
    if (!user || activeTab !== "notifications") return;
    let active = true;
    setLoadingNotif(true);
    authApi
      .getNotificationSettings()
      .then((res) => {
        if (!active) return;
        if (res.isSuccess && res.data) {
          setNotifyPush(res.data.notifyPush);
          setNotifUpdatedAt(res.data.updatedAt);
        }
      })
      .finally(() => {
        if (active) setLoadingNotif(false);
      });
    return () => {
      active = false;
    };
  }, [user, activeTab]);

  const handleSaveProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    setProfileError(null);
    setProfileFieldErrors({});

    const trimmedName = fullName.trim();
    if (trimmedName.length < 2 || trimmedName.length > 100) {
      setProfileFieldErrors({ fullName: "Họ và tên phải có độ dài từ 2 đến 100 ký tự." });
      return;
    }

    setSavingProfile(true);
    try {
      const res = await authApi.updateProfile({
        fullName: trimmedName,
        phoneNumber: phoneNumber.trim() || null,
        dateOfBirth: dateOfBirth || null,
        gender: gender || null,
      });

      if (res.isSuccess) {
        await refreshProfile();
        showToast("Đã cập nhật thông tin hồ sơ tài khoản.", "success");
      } else {
        if (res.error?.details?.length) {
          const map: Record<string, string> = {};
          res.error.details.forEach((d) => {
            map[d.field] = d.message;
          });
          setProfileFieldErrors(map);
        }
        setProfileError(res.error?.message || "Không thể cập nhật hồ sơ. Vui lòng thử lại.");
      }
    } catch {
      setProfileError("Lỗi kết nối máy chủ khi cập nhật hồ sơ.");
    } finally {
      setSavingProfile(false);
    }
  };

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordError(null);
    setPasswordFieldErrors({});

    const clientErrors: Record<string, string> = {};
    if (!currentPassword) {
      clientErrors.currentPassword = "Vui lòng nhập mật khẩu hiện tại.";
    }
    if (newPassword.length < 8 || newPassword.length > 128) {
      clientErrors.newPassword = "Mật khẩu mới phải có độ dài từ 8 đến 128 ký tự.";
    } else if (!/[A-Za-z]/.test(newPassword) || !/[0-9]/.test(newPassword)) {
      clientErrors.newPassword = "Mật khẩu mới phải chứa ít nhất một chữ cái và một chữ số.";
    } else if (newPassword === currentPassword) {
      clientErrors.newPassword = "Mật khẩu mới không được trùng với mật khẩu hiện tại.";
    }
    if (!confirmNewPassword) {
      clientErrors.confirmNewPassword = "Vui lòng xác nhận mật khẩu mới.";
    } else if (confirmNewPassword !== newPassword) {
      clientErrors.confirmNewPassword = "Mật khẩu xác nhận không khớp với mật khẩu mới.";
    }

    if (Object.keys(clientErrors).length > 0) {
      setPasswordFieldErrors(clientErrors);
      return;
    }

    setChangingPassword(true);
    try {
      const res = await authApi.changePassword({
        currentPassword,
        newPassword,
        confirmNewPassword,
      });

      if (res.isSuccess) {
        showToast(
          res.data?.message || "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.",
          "success"
        );
        setStoredToken(null);
        setStoredUser(null);
        window.location.href = "/auth/login";
      } else {
        if (res.error?.details?.length) {
          const map: Record<string, string> = {};
          res.error.details.forEach((d) => {
            map[d.field] = d.message;
          });
          setPasswordFieldErrors(map);
        }
        setPasswordError(res.error?.message || "Đổi mật khẩu thất bại. Vui lòng kiểm tra lại.");
      }
    } catch {
      setPasswordError("Lỗi kết nối máy chủ khi đổi mật khẩu.");
    } finally {
      setChangingPassword(false);
    }
  };

  const handleSaveNotificationSettings = async () => {
    setSavingNotif(true);
    try {
      const res = await authApi.updateNotificationSettings(notifyPush);
      if (res.isSuccess && res.data) {
        setNotifyPush(res.data.notifyPush);
        setNotifUpdatedAt(res.data.updatedAt);
        await refreshProfile();
        showToast("Đã lưu cấu hình thông báo.", "success");
      } else {
        showToast(res.error?.message || "Không thể lưu cấu hình thông báo.", "error");
      }
    } catch {
      showToast("Lỗi kết nối máy chủ khi lưu cấu hình thông báo.", "error");
    } finally {
      setSavingNotif(false);
    }
  };

  if (isLoading || !user) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <Loader2 className="w-7 h-7 text-blue-600 animate-spin" />
      </div>
    );
  }

  const roleLabel =
    role === "Landlord"
      ? "Chủ nhà"
      : role === "Admin"
      ? "Quản trị viên"
      : "Người thuê";

  const tabs: Array<{ id: SettingsTab; label: string; icon: React.ComponentType<{ className?: string }> }> = [
    { id: "profile", label: "Quản lý hồ sơ", icon: User },
    { id: "password", label: "Đổi mật khẩu", icon: Lock },
    { id: "notifications", label: "Cài đặt thông báo", icon: Bell },
  ];

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-5 sm:py-8">
      <div className="mb-6">
        <h1 className="text-2xl font-black text-slate-900 tracking-tight">
          Cài đặt tài khoản
        </h1>
        <p className="text-sm text-slate-500 mt-1">
          Quản lý thông tin hồ sơ cá nhân, bảo mật mật khẩu và tùy chọn nhận thông báo.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-6">
        {/* Sidebar Tabs */}
        <aside className="md:col-span-4 space-y-1.5">
          <div className="bg-white rounded-2xl border border-slate-200/90 p-2 shadow-2xs">
            {tabs.map((tab) => {
              const Icon = tab.icon;
              const isActive = activeTab === tab.id;
              return (
                <button
                  key={tab.id}
                  type="button"
                  onClick={() => setActiveTab(tab.id)}
                  className={cn(
                    "w-full flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-bold transition-colors text-left cursor-pointer",
                    isActive
                      ? "bg-blue-50 text-blue-700"
                      : "text-slate-600 hover:bg-slate-50 hover:text-slate-900"
                  )}
                >
                  <Icon
                    className={cn(
                      "w-4 h-4 shrink-0",
                      isActive ? "text-blue-600" : "text-slate-400"
                    )}
                  />
                  <span>{tab.label}</span>
                </button>
              );
            })}
          </div>

          <div className="bg-slate-50 rounded-2xl border border-slate-200/80 p-4 text-xs text-slate-600 space-y-1.5">
            <div className="flex items-center justify-between">
              <span className="text-slate-500">Vai trò</span>
              <span className="font-bold text-slate-800">{roleLabel}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-slate-500">Xác thực email</span>
              <span className="inline-flex items-center gap-1 font-bold text-emerald-700">
                <ShieldCheck className="w-3.5 h-3.5" />
                {user.emailVerified ? "Đã xác thực" : "Chưa xác thực"}
              </span>
            </div>
          </div>
        </aside>

        {/* Main Panel */}
        <section className="md:col-span-8 bg-white rounded-2xl border border-slate-200/90 p-4 sm:p-6 shadow-2xs">
          {activeTab === "profile" && (
            <form onSubmit={handleSaveProfile} className="space-y-5">
              <div>
                <h2 className="text-lg font-bold text-slate-900">Thông tin hồ sơ</h2>
                <p className="text-xs text-slate-500 mt-0.5">
                  Cập nhật thông tin cá nhân hiển thị trên tài khoản HomeMatch của bạn.
                </p>
              </div>

              {profileError && (
                <div className="flex items-start gap-2.5 p-3 rounded-xl bg-rose-50 border border-rose-200 text-xs text-rose-700">
                  <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
                  <span>{profileError}</span>
                </div>
              )}

              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1.5">
                  Email đăng nhập
                </label>
                <input
                  type="email"
                  value={user.email}
                  disabled
                  className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-slate-50 text-sm text-slate-500 cursor-not-allowed"
                />
              </div>

              <div>
                <label
                  htmlFor="settings-fullname"
                  className="block text-xs font-bold text-slate-700 mb-1.5"
                >
                  Họ và tên <span className="text-rose-500">*</span>
                </label>
                <input
                  id="settings-fullname"
                  type="text"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  maxLength={100}
                  required
                  placeholder="Nhập họ và tên"
                  className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                />
                {profileFieldErrors.fullName && (
                  <p className="text-xs text-rose-600 mt-1">{profileFieldErrors.fullName}</p>
                )}
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label
                    htmlFor="settings-phone"
                    className="block text-xs font-bold text-slate-700 mb-1.5"
                  >
                    Số điện thoại
                  </label>
                  <input
                    id="settings-phone"
                    type="tel"
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                    placeholder="Ví dụ: 0912345678"
                    className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                  />
                  {profileFieldErrors.phoneNumber && (
                    <p className="text-xs text-rose-600 mt-1">{profileFieldErrors.phoneNumber}</p>
                  )}
                </div>

                <div>
                  <label
                    htmlFor="settings-dob"
                    className="block text-xs font-bold text-slate-700 mb-1.5"
                  >
                    Ngày sinh
                  </label>
                  <input
                    id="settings-dob"
                    type="date"
                    value={dateOfBirth}
                    onChange={(e) => setDateOfBirth(e.target.value)}
                    max={new Date().toISOString().slice(0, 10)}
                    className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                  />
                  {profileFieldErrors.dateOfBirth && (
                    <p className="text-xs text-rose-600 mt-1">{profileFieldErrors.dateOfBirth}</p>
                  )}
                </div>
              </div>

              <div>
                <label
                  htmlFor="settings-gender"
                  className="block text-xs font-bold text-slate-700 mb-1.5"
                >
                  Giới tính
                </label>
                <select
                  id="settings-gender"
                  value={gender}
                  onChange={(e) => setGender(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm text-slate-900 bg-white focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                >
                  <option value="">Chưa cập nhật</option>
                  <option value="MALE">Nam</option>
                  <option value="FEMALE">Nữ</option>
                  <option value="OTHER">Khác</option>
                </select>
                {profileFieldErrors.gender && (
                  <p className="text-xs text-rose-600 mt-1">{profileFieldErrors.gender}</p>
                )}
              </div>

              <div className="pt-2 flex justify-end">
                <button
                  type="submit"
                  disabled={savingProfile}
                  className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-sm font-bold shadow-sm transition-colors disabled:opacity-60 cursor-pointer"
                >
                  {savingProfile ? (
                    <Loader2 className="w-4 h-4 animate-spin" />
                  ) : (
                    <Save className="w-4 h-4" />
                  )}
                  <span>{savingProfile ? "Đang lưu..." : "Lưu thay đổi"}</span>
                </button>
              </div>
            </form>
          )}

          {activeTab === "password" && (
            <form onSubmit={handleChangePassword} className="space-y-5">
              <div>
                <h2 className="text-lg font-bold text-slate-900">Đổi mật khẩu</h2>
                <p className="text-xs text-slate-500 mt-0.5">
                  Để bảo vệ tài khoản, mật khẩu mới cần dài 8–128 ký tự và bao gồm cả chữ cái lẫn chữ số. Sau khi đổi mật khẩu, bạn sẽ được yêu cầu đăng nhập lại.
                </p>
              </div>

              {passwordError && (
                <div className="flex items-start gap-2.5 p-3 rounded-xl bg-rose-50 border border-rose-200 text-xs text-rose-700">
                  <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
                  <span>{passwordError}</span>
                </div>
              )}

              <div>
                <label
                  htmlFor="current-password"
                  className="block text-xs font-bold text-slate-700 mb-1.5"
                >
                  Mật khẩu hiện tại <span className="text-rose-500">*</span>
                </label>
                <div className="relative">
                  <input
                    id="current-password"
                    type={showCurrentPassword ? "text" : "password"}
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    required
                    autoComplete="current-password"
                    placeholder="Nhập mật khẩu hiện tại"
                    className="w-full px-3.5 py-2.5 pr-10 rounded-xl border border-slate-200 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                  />
                  <button
                    type="button"
                    onClick={() => setShowCurrentPassword((v) => !v)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 cursor-pointer"
                    aria-label="Ẩn/hiện mật khẩu hiện tại"
                  >
                    {showCurrentPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
                {passwordFieldErrors.currentPassword && (
                  <p className="text-xs text-rose-600 mt-1">{passwordFieldErrors.currentPassword}</p>
                )}
              </div>

              <div>
                <label
                  htmlFor="new-password"
                  className="block text-xs font-bold text-slate-700 mb-1.5"
                >
                  Mật khẩu mới <span className="text-rose-500">*</span>
                </label>
                <div className="relative">
                  <input
                    id="new-password"
                    type={showNewPassword ? "text" : "password"}
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    required
                    autoComplete="new-password"
                    placeholder="Tối thiểu 8 ký tự, gồm chữ và số"
                    className="w-full px-3.5 py-2.5 pr-10 rounded-xl border border-slate-200 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                  />
                  <button
                    type="button"
                    onClick={() => setShowNewPassword((v) => !v)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 cursor-pointer"
                    aria-label="Ẩn/hiện mật khẩu mới"
                  >
                    {showNewPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
                {passwordFieldErrors.newPassword && (
                  <p className="text-xs text-rose-600 mt-1">{passwordFieldErrors.newPassword}</p>
                )}
              </div>

              <div>
                <label
                  htmlFor="confirm-new-password"
                  className="block text-xs font-bold text-slate-700 mb-1.5"
                >
                  Xác nhận mật khẩu mới <span className="text-rose-500">*</span>
                </label>
                <div className="relative">
                  <input
                    id="confirm-new-password"
                    type={showConfirmPassword ? "text" : "password"}
                    value={confirmNewPassword}
                    onChange={(e) => setConfirmNewPassword(e.target.value)}
                    required
                    autoComplete="new-password"
                    placeholder="Nhập lại mật khẩu mới"
                    className="w-full px-3.5 py-2.5 pr-10 rounded-xl border border-slate-200 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                  />
                  <button
                    type="button"
                    onClick={() => setShowConfirmPassword((v) => !v)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 cursor-pointer"
                    aria-label="Ẩn/hiện xác nhận mật khẩu"
                  >
                    {showConfirmPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
                {passwordFieldErrors.confirmNewPassword && (
                  <p className="text-xs text-rose-600 mt-1">
                    {passwordFieldErrors.confirmNewPassword}
                  </p>
                )}
              </div>

              <div className="pt-2 flex justify-end">
                <button
                  type="submit"
                  disabled={changingPassword}
                  className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-sm font-bold shadow-sm transition-colors disabled:opacity-60 cursor-pointer"
                >
                  {changingPassword ? (
                    <Loader2 className="w-4 h-4 animate-spin" />
                  ) : (
                    <Lock className="w-4 h-4" />
                  )}
                  <span>{changingPassword ? "Đang cập nhật..." : "Đổi mật khẩu"}</span>
                </button>
              </div>
            </form>
          )}

          {activeTab === "notifications" && (
            <div className="space-y-5">
              <div>
                <h2 className="text-lg font-bold text-slate-900">Cài đặt thông báo</h2>
                <p className="text-xs text-slate-500 mt-0.5">
                  Bật hoặc tắt thông báo liên quan đến tài khoản và hoạt động thuê nhà trên hệ thống.
                </p>
              </div>

              {loadingNotif ? (
                <div className="py-10 flex items-center justify-center">
                  <Loader2 className="w-6 h-6 text-blue-600 animate-spin" />
                </div>
              ) : (
                <>
                  <div className="flex items-start justify-between gap-4 p-4 rounded-xl border border-slate-200 bg-slate-50/60">
                    <div>
                      <p className="text-sm font-bold text-slate-900">
                        Thông báo trên hệ thống
                      </p>
                      <p className="text-xs text-slate-500 mt-1">
                        Nhận thông báo tức thời và lưu trong mục Thông báo về lịch xem phòng, tin nhắn mới và các hoạt động thuê nhà.
                      </p>
                    </div>
                    <button
                      type="button"
                      role="switch"
                      aria-checked={notifyPush}
                      onClick={() => setNotifyPush((prev) => !prev)}
                      className={cn(
                        "relative inline-flex h-6 w-11 shrink-0 rounded-full border-2 border-transparent transition-colors cursor-pointer focus:outline-none focus:ring-2 focus:ring-blue-500/30",
                        notifyPush ? "bg-blue-600" : "bg-slate-300"
                      )}
                    >
                      <span
                        className={cn(
                          "pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow-xs transition",
                          notifyPush ? "translate-x-5" : "translate-x-0"
                        )}
                      />
                    </button>
                  </div>

                  {notifUpdatedAt && (
                    <p className="text-[11px] text-slate-400 flex items-center gap-1.5">
                      <CheckCircle2 className="w-3.5 h-3.5 text-emerald-500" />
                      Cập nhật lần cuối: {new Date(notifUpdatedAt).toLocaleString("vi-VN")}
                    </p>
                  )}

                  <div className="pt-2 flex justify-end">
                    <button
                      type="button"
                      onClick={handleSaveNotificationSettings}
                      disabled={savingNotif}
                      className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-sm font-bold shadow-sm transition-colors disabled:opacity-60 cursor-pointer"
                    >
                      {savingNotif ? (
                        <Loader2 className="w-4 h-4 animate-spin" />
                      ) : (
                        <Save className="w-4 h-4" />
                      )}
                      <span>{savingNotif ? "Đang lưu..." : "Lưu cấu hình"}</span>
                    </button>
                  </div>
                </>
              )}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
