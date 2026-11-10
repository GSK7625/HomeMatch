"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import { adminListingsApi, listingsApi, getStoredToken } from "@/lib/api-client";
import { PropertyListing, SystemReport, ListingSummaryDto, ListingDetailDto } from "@/types";
import { useToast } from "@/components/Toast";
import { Modal } from "@/components/Modal";
import {
  ShieldCheck,
  Users,
  FileText,
  AlertTriangle,
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

  // Real backend listings
  const [backendListings, setBackendListings] = useState<ListingSummaryDto[]>([]);
  const [pendingListings, setPendingListings] = useState<ListingSummaryDto[]>([]);
  const [loadingPending, setLoadingPending] = useState(false);
  const [isBackendConnected, setIsBackendConnected] = useState(false);

  // Reject Modal State
  const [rejectingListingId, setRejectingListingId] = useState<number | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [rejectLoading, setRejectLoading] = useState(false);

  // Admin Review Detail Modal State (GET /api/v1/admin/listings/{listingId})
  const [reviewDetailListing, setReviewDetailListing] = useState<ListingDetailDto | null>(null);
  const [loadingReviewDetail, setLoadingReviewDetail] = useState(false);

  const handleOpenReviewDetail = async (listingId: number) => {
    setLoadingReviewDetail(true);
    try {
      const res = await adminListingsApi.getReviewListing(listingId);
      if (res.isSuccess && res.data) {
        setReviewDetailListing(res.data);
      } else {
        showToast(res.error?.message || "Không thể tải chi tiết tin chờ duyệt", "error");
      }
    } catch {
      showToast("Lỗi kết nối máy chủ", "error");
    } finally {
      setLoadingReviewDetail(false);
    }
  };

  // System user accounts list
  const [accounts, setAccounts] = useState<DemoAccount[]>([
    {
      id: "usr-01",
      name: "Trần Bình An (Người Thuê)",
      email: "tenant@homematch.vn",
      role: "Tenant",
      status: "Hoạt động",
      joinedDate: "2026-10-01",
    },
    {
      id: "usr-02",
      name: "Nguyễn Văn Hoàng (Chủ Trọ)",
      email: "landlord@homematch.vn",
      role: "Landlord",
      status: "Hoạt động",
      joinedDate: "2026-09-15",
    },
  ]);

  const loadData = useCallback(async () => {
    setReports([]);

    // Load backend pending listings and public listings
    const token = getStoredToken();
    setLoadingPending(true);
    try {
      const [pendingRes, publicRes] = await Promise.all([
        token
          ? adminListingsApi.getPending(1, 50)
          : Promise.resolve({ isSuccess: false, data: null as any }),
        listingsApi.getPublicListings(50),
      ]);

      if (pendingRes.isSuccess && pendingRes.data?.items) {
        setPendingListings(pendingRes.data.items);
      } else {
        setPendingListings([]);
      }

      if (publicRes.isSuccess && publicRes.data) {
        setBackendListings(publicRes.data);
      } else {
        setBackendListings([]);
      }

      setIsBackendConnected(true);
      setListings([]);
    } catch {
      setIsBackendConnected(false);
      setListings([]);
    } finally {
      setLoadingPending(false);
    }
  }, []);

  const getStatusBadge = (status: string) => {
    const norm = (status || "").toLowerCase();
    if (norm === "published" || norm === "còn trống" || norm === "active") {
      return {
        label: "Đã duyệt công khai",
        badgeClass: "bg-emerald-50 text-emerald-700 border-emerald-200/90",
        dotClass: "bg-emerald-500",
      };
    }
    if (
      norm === "pending_approval" ||
      norm === "pendingapproval" ||
      norm === "pendingreview" ||
      norm === "chờ duyệt" ||
      norm === "pending"
    ) {
      return {
        label: "Chờ phê duyệt",
        badgeClass: "bg-amber-50 text-amber-800 border-amber-200/90",
        dotClass: "bg-amber-500",
      };
    }
    if (norm === "draft" || norm === "nháp") {
      return {
        label: "Bản nháp",
        badgeClass: "bg-slate-100 text-slate-700 border-slate-200/90",
        dotClass: "bg-slate-400",
      };
    }
    if (norm === "rejected" || norm === "từ chối") {
      return {
        label: "Đã từ chối",
        badgeClass: "bg-red-50 text-red-700 border-red-200/90",
        dotClass: "bg-red-500",
      };
    }
    return {
      label: "Tạm khóa / Ẩn",
      badgeClass: "bg-rose-50 text-rose-700 border-rose-200/90",
      dotClass: "bg-rose-500",
    };
  };

  const displayListings = React.useMemo(() => {
    if (backendListings.length > 0 || pendingListings.length > 0) {
      const map = new Map<number, any>();
      pendingListings.forEach((p) => map.set(p.id, p));
      backendListings.forEach((b) => {
        if (!map.has(b.id)) map.set(b.id, b);
      });
      return Array.from(map.values());
    }
    return [];
  }, [backendListings, pendingListings, listings]);

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
    setReports((prev) =>
      prev.map((r) => (r.id === id ? { ...r, status: resolution } : r))
    );
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
          {/* General Listings Table / Cards */}
          <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
            <div className="p-4 border-b border-slate-100 flex items-center justify-between">
              <div>
                <h3 className="font-black text-slate-900 text-sm">Toàn Bộ Danh Sách Tin Đăng</h3>
                <p className="text-[11px] text-slate-400 mt-0.5">
                  {isBackendConnected
                    ? `Dữ liệu đồng bộ trực tiếp từ hệ thống (${displayListings.length} tin)`
                    : `Danh sách tin đăng hệ thống (${displayListings.length} tin)`}
                </p>
              </div>
            </div>

            {/* Desktop View: Multi-column Table */}
            <div className="hidden md:block overflow-x-auto">
              <table className="w-full text-xs text-left border-collapse min-w-[940px]">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50/70 text-slate-500 uppercase tracking-wider">
                    <th className="p-4 whitespace-nowrap">Tin đăng</th>
                    <th className="p-4 whitespace-nowrap">Chủ nhà đăng</th>
                    <th className="p-4 whitespace-nowrap">Mức giá & Diện tích</th>
                    <th className="p-4 whitespace-nowrap">Trạng thái duyệt</th>
                    <th className="p-4 text-right whitespace-nowrap">Hành động quyết định</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {displayListings.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="p-8 text-center text-slate-400 text-xs">
                        Chưa có tin đăng nào trong cơ sở dữ liệu.
                      </td>
                    </tr>
                  ) : (
                    displayListings.map((item: any) => {
                      const badge = getStatusBadge(item.status);
                      const isPending =
                        (item.status || "").toLowerCase() === "pending_approval" ||
                        (item.status || "").toLowerCase() === "pendingapproval" ||
                        (item.status || "").toLowerCase() === "pendingreview" ||
                        (item.status || "").toLowerCase() === "chờ duyệt";
                      const isBackendItem = typeof item.id === "number";

                      return (
                        <tr key={item.id} className="hover:bg-slate-50/60 transition-colors">
                          <td className="p-4 whitespace-nowrap">
                            <div className="flex items-center gap-3">
                              <img
                                src={
                                  item.coverImageUrl ||
                                  (item.images && item.images[0]) ||
                                  "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=400&q=80"
                                }
                                alt=""
                                className="w-12 h-9 rounded-lg object-cover border border-slate-100 shrink-0"
                              />
                              <div className="max-w-[280px]">
                                <h4 className="font-bold text-slate-900 truncate" title={item.title}>
                                  {item.title}
                                </h4>
                                <span className="text-[11px] text-slate-400 block truncate">
                                  {item.address || `Mã tin: #${item.id} • Phòng #${item.roomId || item.id}`}
                                </span>
                              </div>
                            </div>
                          </td>
                          <td className="p-4 whitespace-nowrap">
                            <span className="font-bold text-slate-800 block">
                              {item.landlordName || `Chủ nhà (ID #${item.landlordId || 2})`}
                            </span>
                            <span className="text-[11px] text-slate-400 block">
                              {item.landlordPhone || `Phòng: P.${item.roomId || 201}`}
                            </span>
                          </td>
                          <td className="p-4 whitespace-nowrap">
                            <span className="font-bold text-blue-600 block">
                              {item.price ? item.price.toLocaleString("vi-VN") : "0"} đ/tháng
                            </span>
                            <span className="text-slate-500 text-[11px] block">
                              {item.area ? `${item.area} m²` : "Phòng khép kín"}
                            </span>
                          </td>
                          <td className="p-4 whitespace-nowrap">
                            <span
                              className={cn(
                                "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold whitespace-nowrap border shadow-2xs",
                                badge.badgeClass
                              )}
                            >
                              <span className={cn("w-1.5 h-1.5 rounded-full shrink-0", badge.dotClass)} />
                              <span>{badge.label}</span>
                            </span>
                          </td>
                          <td className="p-4 text-right whitespace-nowrap">
                            <div className="flex items-center justify-end gap-2 shrink-0">
                              {isPending ? (
                                <>
                                  <button
                                    type="button"
                                    onClick={() => handleOpenReviewDetail(item.id)}
                                    className="px-3 py-1.5 rounded-lg border border-slate-200 text-slate-700 hover:bg-slate-100 font-bold text-xs whitespace-nowrap shrink-0 transition-colors inline-flex items-center gap-1"
                                  >
                                    <Eye className="w-3.5 h-3.5" />
                                    <span>Chi tiết</span>
                                  </button>
                                  <button
                                    onClick={() =>
                                      handleApproveBackend(item.id)
                                    }
                                    className="px-3 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs whitespace-nowrap shrink-0 transition-colors shadow-2xs"
                                  >
                                    Phê duyệt
                                  </button>
                                  <button
                                    onClick={() => {
                                      setRejectingListingId(item.id);
                                      setRejectReason("");
                                    }}
                                    className="px-3 py-1.5 rounded-lg border border-rose-200 text-rose-600 hover:bg-rose-50 font-bold text-xs whitespace-nowrap shrink-0 transition-colors"
                                  >
                                    Từ chối
                                  </button>
                                </>
                              ) : (
                                <>
                                  <Link
                                    href={`/listings/${item.id}`}
                                    target="_blank"
                                    className="px-3 py-1.5 rounded-lg border border-slate-200 text-slate-700 hover:bg-slate-100 font-bold text-xs whitespace-nowrap shrink-0 transition-colors inline-flex items-center gap-1"
                                  >
                                    <Eye className="w-3.5 h-3.5" />
                                    <span>Xem tin</span>
                                  </Link>
                                  <button
                                    onClick={() => {
                                      setRejectingListingId(item.id);
                                      setRejectReason("");
                                    }}
                                    className="px-3 py-1.5 rounded-lg border border-rose-200 text-rose-600 hover:bg-rose-50 font-bold text-xs whitespace-nowrap shrink-0 transition-colors"
                                  >
                                    Khóa / Ẩn
                                  </button>
                                </>
                              )}
                            </div>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>

            {/* Mobile View: Responsive Cards (100% width, no horizontal scroll) */}
            <div className="block md:hidden divide-y divide-slate-100">
              {displayListings.length === 0 ? (
                <div className="p-6 text-center text-slate-400 text-xs">
                  Chưa có tin đăng nào trong cơ sở dữ liệu.
                </div>
              ) : (
                displayListings.map((item: any) => {
                  const badge = getStatusBadge(item.status);
                  const isPending =
                    (item.status || "").toLowerCase() === "pending_approval" ||
                    (item.status || "").toLowerCase() === "pendingapproval" ||
                    (item.status || "").toLowerCase() === "pendingreview" ||
                    (item.status || "").toLowerCase() === "chờ duyệt";
                  const isBackendItem = typeof item.id === "number";

                  return (
                    <div key={item.id} className="p-4 space-y-3">
                      <div className="flex gap-3">
                        <img
                          src={
                            item.coverImageUrl ||
                            (item.images && item.images[0]) ||
                            "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=400&q=80"
                          }
                          alt=""
                          className="w-16 h-16 rounded-xl object-cover shrink-0 border border-slate-100"
                        />
                        <div className="flex-1 min-w-0">
                          <h4 className="font-bold text-slate-900 text-xs line-clamp-2" title={item.title}>
                            {item.title}
                          </h4>
                          <p className="text-[11px] text-slate-400 mt-0.5 truncate">
                            {item.address || `Mã tin: #${item.id} • Phòng #${item.roomId || item.id}`}
                          </p>
                          <div className="flex items-center justify-between mt-1">
                            <span className="font-bold text-blue-600 text-xs">
                              {item.price ? item.price.toLocaleString("vi-VN") : "0"} đ/tháng
                            </span>
                            <span className="text-[11px] text-slate-500">
                              {item.area ? `${item.area} m²` : "Phòng khép kín"}
                            </span>
                          </div>
                        </div>
                      </div>

                      {/* Meta & Status Row */}
                      <div className="flex items-center justify-between text-[11px] pt-1">
                        <span className="text-slate-600 font-medium truncate max-w-[160px]">
                          {item.landlordName || `Chủ nhà (ID #${item.landlordId || 2})`}
                        </span>
                        <span
                          className={cn(
                            "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold whitespace-nowrap border shadow-2xs",
                            badge.badgeClass
                          )}
                        >
                          <span className={cn("w-1.5 h-1.5 rounded-full shrink-0", badge.dotClass)} />
                          <span>{badge.label}</span>
                        </span>
                      </div>

                      {/* Action Buttons Row */}
                      <div className="flex items-center gap-2 pt-2 border-t border-slate-100">
                        {isPending ? (
                          <>
                            <button
                              onClick={() =>
                                handleApproveBackend(item.id)
                              }
                              className="flex-1 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs transition-colors shadow-2xs text-center"
                            >
                              Phê duyệt
                            </button>
                            <button
                              onClick={() => {
                                setRejectingListingId(item.id);
                                setRejectReason("");
                              }}
                              className="flex-1 py-2 rounded-xl border border-rose-200 text-rose-600 hover:bg-rose-50 font-bold text-xs transition-colors text-center"
                            >
                              Từ chối
                            </button>
                          </>
                        ) : (
                          <>
                            <Link
                              href={`/listings/${item.id}`}
                              target="_blank"
                              className="flex-1 py-2 rounded-xl border border-slate-200 text-slate-700 hover:bg-slate-100 font-bold text-xs transition-colors inline-flex items-center justify-center gap-1 text-center"
                            >
                              <Eye className="w-3.5 h-3.5" />
                              <span>Xem tin</span>
                            </Link>
                            <button
                              onClick={() => {
                                setRejectingListingId(item.id);
                                setRejectReason("");
                              }}
                              className="flex-1 py-2 rounded-xl border border-rose-200 text-rose-600 hover:bg-rose-50 font-bold text-xs transition-colors text-center"
                            >
                              Khóa / Ẩn
                            </button>
                          </>
                        )}
                      </div>
                    </div>
                  );
                })
              )}
            </div>
          </div>
        </div>
      )}

      {/* Tab 2: Users Management */}
      {activeTab === "users" && (
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
          {/* Desktop Table */}
          <div className="hidden md:block overflow-x-auto">
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
                    <td className="p-4 whitespace-nowrap">
                      <span
                        className={cn(
                          "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold whitespace-nowrap border shadow-2xs",
                          acc.status === "Hoạt động"
                            ? "bg-emerald-50 text-emerald-700 border-emerald-200/90"
                            : "bg-rose-50 text-rose-700 border-rose-200/90"
                        )}
                      >
                        <span
                          className={cn(
                            "w-1.5 h-1.5 rounded-full shrink-0",
                            acc.status === "Hoạt động" ? "bg-emerald-500" : "bg-rose-500"
                          )}
                        />
                        <span>{acc.status}</span>
                      </span>
                    </td>
                    <td className="p-4 text-right whitespace-nowrap">
                      <button
                        onClick={() => handleToggleLockUser(acc.id)}
                        className={cn(
                          "px-3 py-1.5 rounded-lg text-xs font-bold border transition-colors whitespace-nowrap shrink-0",
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

          {/* Mobile Cards View */}
          <div className="block md:hidden divide-y divide-slate-100">
            {accounts.map((acc) => (
              <div key={acc.id} className="p-4 space-y-2.5">
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <h4 className="font-bold text-slate-900 text-xs">{acc.name}</h4>
                    <p className="text-[11px] text-slate-500 font-medium">{acc.email}</p>
                  </div>
                  <span
                    className={cn(
                      "px-2 py-0.5 rounded-full text-[10px] font-bold border shrink-0",
                      acc.role === "Landlord"
                        ? "bg-purple-50 text-purple-700 border border-purple-200"
                        : "bg-blue-50 text-blue-700 border border-blue-200"
                    )}
                  >
                    {acc.role === "Landlord" ? "Chủ nhà" : "Khách thuê"}
                  </span>
                </div>

                <div className="flex items-center justify-between text-[11px] pt-1">
                  <span className="text-slate-400">Tham gia: {acc.joinedDate}</span>
                  <span
                    className={cn(
                      "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold whitespace-nowrap border shadow-2xs",
                      acc.status === "Hoạt động"
                        ? "bg-emerald-50 text-emerald-700 border-emerald-200/90"
                        : "bg-rose-50 text-rose-700 border-rose-200/90"
                    )}
                  >
                    <span
                      className={cn(
                        "w-1.5 h-1.5 rounded-full shrink-0",
                        acc.status === "Hoạt động" ? "bg-emerald-500" : "bg-rose-500"
                      )}
                    />
                    <span>{acc.status}</span>
                  </span>
                </div>

                <div className="pt-2 border-t border-slate-100">
                  <button
                    onClick={() => handleToggleLockUser(acc.id)}
                    className={cn(
                      "w-full py-2 rounded-xl text-xs font-bold border transition-colors text-center shadow-2xs",
                      acc.status === "Hoạt động"
                        ? "border-rose-200 text-rose-600 hover:bg-rose-50"
                        : "border-emerald-200 text-emerald-600 hover:bg-emerald-50"
                    )}
                  >
                    {acc.status === "Hoạt động" ? "Khóa tài khoản" : "Mở khóa"}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Tab 3: Reports */}
      {activeTab === "reports" && (
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
          {/* Desktop Table */}
          <div className="hidden md:block overflow-x-auto">
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
                            className="px-3 py-1 rounded-lg bg-rose-600 hover:bg-rose-700 text-white font-bold text-xs whitespace-nowrap shrink-0 shadow-2xs"
                          >
                            Xác nhận vi phạm
                          </button>
                          <button
                            onClick={() => handleResolveReport(rep.id, "Đã bác bỏ")}
                            className="px-3 py-1 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 font-bold text-xs whitespace-nowrap shrink-0"
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

          {/* Mobile Cards View */}
          <div className="block md:hidden divide-y divide-slate-100">
            {reports.map((rep) => (
              <div key={rep.id} className="p-4 space-y-2.5">
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <h4 className="font-bold text-slate-900 text-xs">{rep.targetTitle}</h4>
                    <p className="text-[11px] text-slate-500">Người báo: {rep.reporterName}</p>
                  </div>
                  <span
                    className={cn(
                      "px-2 py-0.5 rounded-full text-[10px] font-bold border shrink-0",
                      rep.status === "Chờ xử lý"
                        ? "bg-amber-50 text-amber-700 border-amber-200"
                        : "bg-slate-100 text-slate-700 border-slate-200"
                    )}
                  >
                    {rep.status}
                  </span>
                </div>

                <div className="bg-rose-50/60 p-2.5 rounded-xl border border-rose-100 text-[11px] text-rose-700">
                  <span className="font-bold block">Lý do:</span>
                  <span>{rep.reason}</span>
                </div>

                <div className="flex items-center justify-between text-[11px] text-slate-400">
                  <span>Thời gian: {rep.createdAt}</span>
                </div>

                {rep.status === "Chờ xử lý" && (
                  <div className="flex items-center gap-2 pt-2 border-t border-slate-100">
                    <button
                      onClick={() => handleResolveReport(rep.id, "Đã xử lý")}
                      className="flex-1 py-2 rounded-xl bg-rose-600 hover:bg-rose-700 text-white font-bold text-xs transition-colors shadow-2xs text-center"
                    >
                      Xác nhận vi phạm
                    </button>
                    <button
                      onClick={() => handleResolveReport(rep.id, "Đã bác bỏ")}
                      className="flex-1 py-2 rounded-xl border border-slate-200 text-slate-600 hover:bg-slate-50 font-bold text-xs transition-colors text-center"
                    >
                      Bác bỏ
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Modal: Review Listing Detail (GET /api/v1/admin/listings/{listingId}) */}
      {reviewDetailListing && (
        <Modal
          isOpen={true}
          onClose={() => setReviewDetailListing(null)}
          title={`Kiểm Duyệt Chi Tiết Tin Đăng #${reviewDetailListing.id}`}
          maxWidth="2xl"
        >
          <div className="space-y-4 text-xs">
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 p-3 rounded-xl bg-slate-50 border border-slate-200">
              <div>
                <span className="text-[10px] font-bold text-slate-400 uppercase block">Giá thuê</span>
                <span className="font-black text-blue-600 text-sm">
                  {reviewDetailListing.price.toLocaleString("vi-VN")} đ/tháng
                </span>
              </div>
              <div>
                <span className="text-[10px] font-bold text-slate-400 uppercase block">Tiền cọc</span>
                <span className="font-bold text-slate-800">
                  {reviewDetailListing.depositAmount.toLocaleString("vi-VN")} đ
                </span>
              </div>
              <div>
                <span className="text-[10px] font-bold text-slate-400 uppercase block">Hợp đồng tối thiểu</span>
                <span className="font-bold text-slate-800">
                  {reviewDetailListing.minLeaseMonths} tháng
                </span>
              </div>
              <div>
                <span className="text-[10px] font-bold text-slate-400 uppercase block">Chủ nhà & Phòng</span>
                <span className="font-bold text-slate-800">
                  Chủ #{reviewDetailListing.landlordId} • Phòng #{reviewDetailListing.roomId}
                </span>
              </div>
            </div>

            {reviewDetailListing.review && (
              <div className="p-3 rounded-xl bg-amber-50 border border-amber-200 flex items-center justify-between">
                <span>
                  Lần gửi kiểm duyệt (Revision):{" "}
                  <strong>#{reviewDetailListing.review.revision}</strong>
                </span>
                {reviewDetailListing.review.rejectionReason && (
                  <span className="text-rose-700 font-semibold">
                    Lý do từ chối trước: {reviewDetailListing.review.rejectionReason}
                  </span>
                )}
              </div>
            )}

            <div>
              <span className="font-bold text-slate-700 uppercase block mb-1">Tiêu đề tin đăng</span>
              <p className="p-2.5 rounded-xl border border-slate-200 font-bold text-slate-900 bg-white">
                {reviewDetailListing.title}
              </p>
            </div>

            <div>
              <span className="font-bold text-slate-700 uppercase block mb-1">Mô tả chi tiết</span>
              <p className="p-3 rounded-xl border border-slate-200 text-slate-700 whitespace-pre-line bg-white max-h-40 overflow-y-auto">
                {reviewDetailListing.description}
              </p>
            </div>

            {reviewDetailListing.houseRules && (
              <div>
                <span className="font-bold text-slate-700 uppercase block mb-1">
                  Nội quy phòng / tòa nhà (House Rules)
                </span>
                <p className="p-2.5 rounded-xl border border-slate-200 text-slate-700 bg-white">
                  {reviewDetailListing.houseRules}
                </p>
              </div>
            )}

            {reviewDetailListing.images && reviewDetailListing.images.length > 0 && (
              <div>
                <span className="font-bold text-slate-700 uppercase block mb-1.5">
                  Hình ảnh đính kèm ({reviewDetailListing.images.length})
                </span>
                <div className="grid grid-cols-3 gap-2">
                  {reviewDetailListing.images.map((img) => (
                    <div
                      key={img.id}
                      className="aspect-[16/10] rounded-lg overflow-hidden border border-slate-200 bg-slate-100 relative"
                    >
                      <img src={img.imageUrl} alt="" className="w-full h-full object-cover" />
                      {img.isPrimary && (
                        <span className="absolute top-1 left-1 px-1.5 py-0.5 rounded bg-blue-600 text-white text-[9px] font-bold">
                          Ảnh chính
                        </span>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div className="flex gap-3 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setReviewDetailListing(null)}
                className="flex-1 py-2.5 rounded-xl border border-slate-200 font-bold text-slate-700 hover:bg-slate-50"
              >
                Đóng
              </button>
              <button
                type="button"
                onClick={() => {
                  const id = reviewDetailListing.id;
                  setReviewDetailListing(null);
                  setRejectingListingId(id);
                  setRejectReason("");
                }}
                className="flex-1 py-2.5 rounded-xl border border-rose-200 text-rose-600 font-bold hover:bg-rose-50"
              >
                Từ chối tin này
              </button>
              <button
                type="button"
                onClick={async () => {
                  const id = reviewDetailListing.id;
                  setReviewDetailListing(null);
                  await handleApproveBackend(id);
                }}
                className="flex-1 py-2.5 rounded-xl bg-emerald-600 text-white font-bold hover:bg-emerald-700 shadow-md shadow-emerald-500/20"
              >
                Phê duyệt công khai
              </button>
            </div>
          </div>
        </Modal>
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
