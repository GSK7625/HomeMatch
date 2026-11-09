"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import { listingService, adminService } from "@/lib/services";
import { adminListingsApi, getStoredToken } from "@/lib/api-client";
import { PropertyListing, SystemReport, ListingSummaryDto } from "@/types";
import { useToast } from "@/components/Toast";
import { Modal } from "@/components/Modal";
import {
  ShieldCheck,
  Users,
  FileText,
  AlertTriangle,
  CheckCircle,
  XCircle,
  Lock,
  Unlock,
  Eye,
  Building,
  DollarSign,
  RefreshCw,
  Send,
  Sparkles,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface DemoAccount {
  id: string;
  name: string;
  email: string;
  role: "Tenant" | "Landlord";
  status: "Hoạt động" | "Bị khóa";
  joinedDate: string;
}

export default function AdminDashboardPage() {
  const { user, role } = useAuth();
  const { showToast } = useToast();
  const [activeTab, setActiveTab] = useState<"listings" | "users" | "reports">("listings");
  const [listings, setListings] = useState<PropertyListing[]>([]);
  const [reports, setReports] = useState<SystemReport[]>([]);

  // Real backend pending listings
  const [pendingListings, setPendingListings] = useState<ListingSummaryDto[]>([]);
  const [loadingPending, setLoadingPending] = useState(false);
  const [isBackendConnected, setIsBackendConnected] = useState(false);

  // Reject Modal State
  const [rejectingListingId, setRejectingListingId] = useState<number | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [rejectLoading, setRejectLoading] = useState(false);

  // Demo user accounts list
  const [accounts, setAccounts] = useState<DemoAccount[]>([
    {
      id: "usr-01",
      name: "Đỗ Trung Kiên",
      email: "tenant@homematch.vn",
      role: "Tenant",
      status: "Hoạt động",
      joinedDate: "2026-10-01",
    },
    {
      id: "usr-02",
      name: "Nguyễn Văn Hoàng",
      email: "landlord@homematch.vn",
      role: "Landlord",
      status: "Hoạt động",
      joinedDate: "2026-09-15",
    },
    {
      id: "usr-03",
      name: "Trần Thị Mai Lan",
      email: "lan.tran@gmail.com",
      role: "Landlord",
      status: "Hoạt động",
      joinedDate: "2026-09-20",
    },
    {
      id: "usr-04",
      name: "Tài khoản nghi vấn spam",
      email: "spam.test@badmail.com",
      role: "Tenant",
      status: "Bị khóa",
      joinedDate: "2026-10-28",
    },
  ]);

  const loadData = useCallback(async () => {
    setListings(listingService.getAll());
    setReports(adminService.getReports());

    // Load backend pending listings
    const token = getStoredToken();
    if (token) {
      setLoadingPending(true);
      try {
        const res = await adminListingsApi.getPending(1, 50);
        if (res.isSuccess && res.data) {
          setPendingListings(res.data.items);
          setIsBackendConnected(true);
        } else {
          setIsBackendConnected(false);
        }
      } catch {
        setIsBackendConnected(false);
      } finally {
        setLoadingPending(false);
      }
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Backend Approve
  const handleApproveBackend = async (listingId: number) => {
    try {
      const res = await adminListingsApi.approve(listingId);
      if (res.isSuccess) {
        showToast(`Đã duyệt tin #${listingId} thành công! Tin đã được công khai.`, "success");
        loadData();
      } else {
        showToast(res.error?.message || "Duyệt tin thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    }
  };

  // Backend Reject
  const handleRejectBackend = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!rejectingListingId || !rejectReason.trim()) {
      showToast("Vui lòng nhập lý do từ chối tin đăng!", "error");
      return;
    }
    setRejectLoading(true);
    try {
      const res = await adminListingsApi.reject(rejectingListingId, rejectReason.trim());
      if (res.isSuccess) {
        showToast(`Đã từ chối tin #${rejectingListingId} kèm lý do.`, "info");
        setRejectingListingId(null);
        setRejectReason("");
        loadData();
      } else {
        showToast(res.error?.message || "Từ chối thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    } finally {
      setRejectLoading(false);
    }
  };

  // Mock Approve / Reject
  const handleApproveMock = (id: string) => {
    listingService.update(id, { status: "Còn trống" });
    setListings(listingService.getAll());
    showToast("Đã phê duyệt tin đăng công khai trên toàn hệ thống!", "success");
  };

  const handleRejectMock = (id: string) => {
    listingService.update(id, { status: "Tạm ngừng" });
    setListings(listingService.getAll());
    showToast("Đã từ chối duyệt và ẩn tin đăng.", "info");
  };

  const handleToggleLockUser = (id: string) => {
    setAccounts((prev) =>
      prev.map((acc) => {
        if (acc.id === id) {
          const newStatus = acc.status === "Hoạt động" ? "Bị khóa" : "Hoạt động";
          showToast(
            newStatus === "Bị khóa"
              ? `Đã khóa tài khoản ${acc.name}.`
              : `Đã mở khóa tài khoản ${acc.name}!`,
            newStatus === "Bị khóa" ? "error" : "success"
          );
          return { ...acc, status: newStatus };
        }
        return acc;
      })
    );
  };

  const handleResolveReport = (id: string, resolution: "Đã xử lý" | "Đã bác bỏ") => {
    adminService.resolveReport(id, resolution);
    setReports(adminService.getReports());
    showToast(`Đã ghi nhận kết quả xử lý báo cáo: ${resolution}`, "success");
  };

  if (!user || role !== "Admin") {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-rose-50 text-rose-600 flex items-center justify-center mx-auto shadow-xs">
          <ShieldCheck className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Khu Vực Dành Riêng Cho Quản Trị Viên</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Bạn cần đăng nhập với tài khoản có quyền Quản trị viên (Admin) để truy cập cổng thông tin quản lý toàn hệ thống.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/admin/dashboard"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng nhập quyền Admin</span>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-rose-600 text-xs font-bold uppercase tracking-wider mb-1">
            <ShieldCheck className="w-4 h-4" />
            <span>Quản trị hệ thống HomeMatch</span>
            {isBackendConnected && (
              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-100 text-emerald-800">
                <CheckCircle className="w-3 h-3" /> Live Moderation API
              </span>
            )}
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Bảng Điều Khiển Quản Trị Viên (Admin)
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Kiểm duyệt tin đăng, giám sát tài khoản người dùng và giải quyết khiếu nại báo cáo vi phạm.
          </p>
        </div>

        <button
          onClick={() => loadData()}
          className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-800 text-xs font-bold transition-colors"
        >
          <RefreshCw className={cn("w-4 h-4", loadingPending && "animate-spin")} />
          <span>Làm mới hàng đợi</span>
        </button>
      </div>

      {/* Tabs Header */}
      <div className="flex items-center gap-6 border-b border-slate-200">
        <button
          onClick={() => setActiveTab("listings")}
          className={cn(
            "pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2",
            activeTab === "listings"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          )}
        >
          <FileText className="w-4 h-4" />
          <span>
            Kiểm Duyệt Tin Đăng{" "}
            {pendingListings.length > 0
              ? `(${pendingListings.length} chờ duyệt)`
              : `(${listings.length})`}
          </span>
        </button>

        <button
          onClick={() => setActiveTab("users")}
          className={cn(
            "pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2",
            activeTab === "users"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          )}
        >
          <Users className="w-4 h-4" />
          <span>Quản Lý Tài Khoản ({accounts.length})</span>
        </button>

        <button
          onClick={() => setActiveTab("reports")}
          className={cn(
            "pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2",
            activeTab === "reports"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          )}
        >
          <AlertTriangle className="w-4 h-4" />
          <span>Báo Cáo Vi Phạm ({reports.length})</span>
        </button>
      </div>

      {/* Tab 1: Listings Review */}
      {activeTab === "listings" && (
        <div className="space-y-4">
          {/* Live Pending Queue Section if available */}
          {pendingListings.length > 0 && (
            <div className="bg-amber-50/70 border border-amber-200 rounded-2xl p-4 space-y-3">
              <div className="flex items-center justify-between">
                <span className="font-bold text-amber-900 text-xs flex items-center gap-2">
                  <Sparkles className="w-4 h-4 text-amber-600" />
                  <span>Hàng Đợi Chờ Duyệt Thực Tế ({pendingListings.length} tin)</span>
                </span>
                <span className="text-[11px] text-amber-700 font-semibold">
                  Cần Admin phê duyệt trước khi xuất hiện trên trang tìm kiếm
                </span>
              </div>

              <div className="bg-white rounded-xl border border-amber-200/80 overflow-x-auto">
                <table className="w-full text-xs text-left border-collapse min-w-[700px]">
                  <thead>
                    <tr className="border-b border-amber-100 bg-amber-50/50 text-slate-600 uppercase">
                      <th className="p-3">Tin đăng</th>
                      <th className="p-3">Giá niêm yết</th>
                      <th className="p-3">Trạng thái</th>
                      <th className="p-3 text-right">Quyết định kiểm duyệt</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {pendingListings.map((p) => (
                      <tr key={p.id} className="hover:bg-slate-50/60">
                        <td className="p-3">
                          <div className="flex items-center gap-3">
                            <img
                              src={
                                p.coverImageUrl ||
                                "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=400&q=80"
                              }
                              alt=""
                              className="w-12 h-9 rounded object-cover"
                            />
                            <div>
                              <h4 className="font-bold text-slate-900 line-clamp-1">{p.title}</h4>
                              <span className="text-[10px] text-slate-400">ID: #{p.id}</span>
                            </div>
                          </div>
                        </td>
                        <td className="p-3 font-bold text-blue-600">
                          {p.price.toLocaleString()} đ/tháng
                        </td>
                        <td className="p-3">
                          <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-100 text-amber-800">
                            Chờ duyệt
                          </span>
                        </td>
                        <td className="p-3 text-right">
                          <div className="flex items-center justify-end gap-2">
                            <button
                              onClick={() => handleApproveBackend(p.id)}
                              className="px-3 py-1 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs"
                            >
                              Phê duyệt
                            </button>
                            <button
                              onClick={() => {
                                setRejectingListingId(p.id);
                                setRejectReason("");
                              }}
                              className="px-3 py-1 rounded-lg border border-rose-200 text-rose-600 hover:bg-rose-50 font-bold text-xs"
                            >
                              Từ chối
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* General Listings Table */}
          <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-x-auto">
            <div className="p-4 border-b border-slate-100 flex items-center justify-between">
              <h3 className="font-black text-slate-900 text-sm">Toàn Bộ Danh Sách Tin Đăng</h3>
            </div>
            <table className="w-full text-xs text-left border-collapse min-w-[850px]">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50/70 text-slate-500 uppercase tracking-wider">
                  <th className="p-4">Tin đăng</th>
                  <th className="p-4">Chủ nhà đăng</th>
                  <th className="p-4">Mức giá & Diện tích</th>
                  <th className="p-4">Trạng thái duyệt</th>
                  <th className="p-4 text-right">Hành động quyết định</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {listings.map((item) => (
                  <tr key={item.id} className="hover:bg-slate-50/60">
                    <td className="p-4">
                      <div className="flex items-center gap-3">
                        <img src={item.images[0]} alt="" className="w-12 h-9 rounded object-cover" />
                        <div>
                          <h4 className="font-bold text-slate-900 line-clamp-1">{item.title}</h4>
                          <span className="text-[11px] text-slate-400">{item.address}</span>
                        </div>
                      </div>
                    </td>
                    <td className="p-4">
                      <span className="font-bold text-slate-800 block">{item.landlordName}</span>
                      <span className="text-[11px] text-slate-400">{item.landlordPhone}</span>
                    </td>
                    <td className="p-4">
                      <span className="font-bold text-blue-600 block">
                        {item.price.toLocaleString()} đ/tháng
                      </span>
                      <span className="text-slate-500 text-[11px]">{item.area} m²</span>
                    </td>
                    <td className="p-4">
                      <span
                        className={cn(
                          "px-2.5 py-1 rounded-full text-xs font-bold border",
                          item.status === "Còn trống"
                            ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                            : "bg-rose-50 text-rose-700 border-rose-200"
                        )}
                      >
                        {item.status === "Còn trống" ? "Đã duyệt công khai" : "Tạm khóa / Ẩn"}
                      </span>
                    </td>
                    <td className="p-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => handleApproveMock(item.id)}
                          className="px-3 py-1 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs"
                        >
                          Phê duyệt
                        </button>
                        <button
                          onClick={() => handleRejectMock(item.id)}
                          className="px-3 py-1 rounded-lg border border-rose-200 text-rose-600 hover:bg-rose-50 font-bold text-xs"
                        >
                          Từ chối / Khóa
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab 2: Users Management */}
      {activeTab === "users" && (
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-x-auto">
          <table className="w-full text-xs text-left border-collapse min-w-[700px]">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50/70 text-slate-500 uppercase tracking-wider">
                <th className="p-4">Họ và tên</th>
                <th className="p-4">Email đăng nhập</th>
                <th className="p-4">Vai trò (Role)</th>
                <th className="p-4">Ngày tham gia</th>
                <th className="p-4">Trạng thái</th>
                <th className="p-4 text-right">Hành động</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {accounts.map((acc) => (
                <tr key={acc.id} className="hover:bg-slate-50/60">
                  <td className="p-4 font-bold text-slate-900">{acc.name}</td>
                  <td className="p-4 font-medium text-slate-600">{acc.email}</td>
                  <td className="p-4">
                    <span
                      className={cn(
                        "px-2.5 py-1 rounded-full text-xs font-bold",
                        acc.role === "Landlord"
                          ? "bg-purple-50 text-purple-700 border border-purple-200"
                          : "bg-blue-50 text-blue-700 border border-blue-200"
                      )}
                    >
                      {acc.role === "Landlord" ? "Chủ nhà" : "Khách thuê"}
                    </span>
                  </td>
                  <td className="p-4 text-slate-500">{acc.joinedDate}</td>
                  <td className="p-4">
                    <span
                      className={cn(
                        "px-2.5 py-1 rounded-full text-xs font-bold border",
                        acc.status === "Hoạt động"
                          ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                          : "bg-rose-50 text-rose-700 border-rose-200"
                      )}
                    >
                      {acc.status}
                    </span>
                  </td>
                  <td className="p-4 text-right">
                    <button
                      onClick={() => handleToggleLockUser(acc.id)}
                      className={cn(
                        "px-3 py-1 rounded-lg text-xs font-bold border transition-colors",
                        acc.status === "Hoạt động"
                          ? "border-rose-200 text-rose-600 hover:bg-rose-50"
                          : "border-emerald-200 text-emerald-600 hover:bg-emerald-50"
                      )}
                    >
                      {acc.status === "Hoạt động" ? "Khóa tài khoản" : "Mở khóa"}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Tab 3: Reports */}
      {activeTab === "reports" && (
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-x-auto">
          <table className="w-full text-xs text-left border-collapse min-w-[800px]">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50/70 text-slate-500 uppercase tracking-wider">
                <th className="p-4">Đối tượng bị báo cáo</th>
                <th className="p-4">Người khiếu nại</th>
                <th className="p-4">Lý do báo cáo</th>
                <th className="p-4">Thời gian</th>
                <th className="p-4">Trạng thái xử lý</th>
                <th className="p-4 text-right">Phán quyết</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {reports.map((rep) => (
                <tr key={rep.id} className="hover:bg-slate-50/60">
                  <td className="p-4 font-bold text-slate-900">{rep.targetTitle}</td>
                  <td className="p-4 text-slate-600">{rep.reporterName}</td>
                  <td className="p-4 text-rose-600 font-semibold">{rep.reason}</td>
                  <td className="p-4 text-slate-500">{rep.createdAt}</td>
                  <td className="p-4">
                    <span
                      className={cn(
                        "px-2.5 py-1 rounded-full text-xs font-bold border",
                        rep.status === "Chờ xử lý"
                          ? "bg-amber-50 text-amber-700 border-amber-200"
                          : "bg-slate-100 text-slate-700 border-slate-200"
                      )}
                    >
                      {rep.status}
                    </span>
                  </td>
                  <td className="p-4 text-right">
                    {rep.status === "Chờ xử lý" && (
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => handleResolveReport(rep.id, "Đã xử lý")}
                          className="px-3 py-1 rounded-lg bg-rose-600 hover:bg-rose-700 text-white font-bold text-xs"
                        >
                          Xác nhận vi phạm
                        </button>
                        <button
                          onClick={() => handleResolveReport(rep.id, "Đã bác bỏ")}
                          className="px-3 py-1 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 font-bold text-xs"
                        >
                          Bác bỏ
                        </button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Modal: Reject Listing with Reason */}
      {rejectingListingId && (
        <Modal
          isOpen={true}
          onClose={() => setRejectingListingId(null)}
          title={`Từ Chối Phê Duyệt Tin Đăng #${rejectingListingId}`}
          maxWidth="md"
        >
          <form onSubmit={handleRejectBackend} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Lý do từ chối (bắt buộc)
              </label>
              <textarea
                required
                rows={3}
                placeholder="VD: Hình ảnh không rõ ràng, giá thuê không khớp với mô tả..."
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            <div className="flex gap-3 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setRejectingListingId(null)}
                className="flex-1 py-2 rounded-xl border border-slate-200 text-xs font-bold text-slate-700"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={rejectLoading}
                className="flex-1 py-2 rounded-xl bg-rose-600 text-white text-xs font-bold hover:bg-rose-700 disabled:opacity-50"
              >
                {rejectLoading ? "Đang xử lý..." : "Xác nhận từ chối"}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
