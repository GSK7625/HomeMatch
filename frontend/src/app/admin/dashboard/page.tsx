"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import {
  adminDashboardApi,
  adminListingsApi,
  adminUsersApi,
  adminReportsApi,
  adminCatalogsApi,
  getStoredToken,
} from "@/lib/api-client";
import {
  AdminDashboardSummaryDto,
  AdminListingItemDto,
  AdminUserItemDto,
  ListingDetailDto,
  ViolationReportDto,
  AmenityDto,
  RoomTypeDto,
} from "@/types";
import { useToast } from "@/components/Toast";
import { Modal } from "@/components/Modal";
import {
  ShieldCheck,
  Users,
  FileText,
  AlertTriangle,
  Eye,
  EyeOff,
  Building,
  RefreshCw,
  Search,
  Lock,
  Unlock,
  Plus,
  Pencil,
  Tags,
  ExternalLink,
  CheckCircle2,
  Ban,
} from "lucide-react";
import { cn } from "@/lib/utils";

type AdminTab = "listings" | "users" | "reports" | "catalogs";

export default function AdminDashboardPage() {
  const { user, role } = useAuth();
  const { showToast } = useToast();

  const [activeTab, setActiveTab] = useState<AdminTab>("listings");
  const [period, setPeriod] = useState<"all" | "today" | "week" | "month" | "year">("all");
  const [dashboard, setDashboard] = useState<AdminDashboardSummaryDto | null>(null);
  const [loadingDashboard, setLoadingDashboard] = useState(false);

  // Tab 1: Listings
  const [listings, setListings] = useState<AdminListingItemDto[]>([]);
  const [listingsTotal, setListingsTotal] = useState(0);
  const [listingStatusFilter, setListingStatusFilter] = useState("");
  const [listingKeyword, setListingKeyword] = useState("");
  const [loadingListings, setLoadingListings] = useState(false);

  // Reject & Hide & Review Modals
  const [rejectingListingId, setRejectingListingId] = useState<number | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [rejectLoading, setRejectLoading] = useState(false);

  const [hidingListing, setHidingListing] = useState<AdminListingItemDto | null>(null);
  const [hideReason, setHideReason] = useState("");
  const [hideLoading, setHideLoading] = useState(false);

  const [reviewDetailListing, setReviewDetailListing] = useState<ListingDetailDto | null>(null);

  // Tab 2: Users
  const [users, setUsers] = useState<AdminUserItemDto[]>([]);
  const [usersTotal, setUsersTotal] = useState(0);
  const [userSearch, setUserSearch] = useState("");
  const [userRoleFilter, setUserRoleFilter] = useState("");
  const [userStatusFilter, setUserStatusFilter] = useState("");
  const [loadingUsers, setLoadingUsers] = useState(false);
  const [togglingUserId, setTogglingUserId] = useState<number | null>(null);

  // Tab 3: Reports
  const [reports, setReports] = useState<ViolationReportDto[]>([]);
  const [reportsTotal, setReportsTotal] = useState(0);
  const [reportStatusFilter, setReportStatusFilter] = useState("");
  const [reportTargetFilter, setReportTargetFilter] = useState("");
  const [loadingReports, setLoadingReports] = useState(false);
  const [resolvingReport, setResolvingReport] = useState<ViolationReportDto | null>(null);
  const [resolutionChoice, setResolutionChoice] = useState<"RESOLVED" | "DISMISSED">("RESOLVED");
  const [resolutionNote, setResolutionNote] = useState("");
  const [resolutionHideListing, setResolutionHideListing] = useState(true);
  const [resolveLoading, setResolveLoading] = useState(false);

  // Tab 4: Catalogs
  const [catalogSubTab, setCatalogSubTab] = useState<"roomTypes" | "amenities">("roomTypes");
  const [roomTypes, setRoomTypes] = useState<RoomTypeDto[]>([]);
  const [amenities, setAmenities] = useState<AmenityDto[]>([]);
  const [loadingCatalogs, setLoadingCatalogs] = useState(false);
  const [catalogModalOpen, setCatalogModalOpen] = useState(false);
  const [editingCatalogId, setEditingCatalogId] = useState<number | null>(null);
  const [catalogCode, setCatalogCode] = useState("");
  const [catalogName, setCatalogName] = useState("");
  const [catalogIsActive, setCatalogIsActive] = useState(true);
  const [savingCatalog, setSavingCatalog] = useState(false);

  const loadDashboard = useCallback(async () => {
    if (!getStoredToken()) return;
    setLoadingDashboard(true);
    try {
      const res = await adminDashboardApi.getSummary(period);
      if (res.isSuccess && res.data) {
        setDashboard(res.data);
      }
    } catch {
      // keep previous state
    } finally {
      setLoadingDashboard(false);
    }
  }, [period]);

  const loadListings = useCallback(async () => {
    if (!getStoredToken()) return;
    setLoadingListings(true);
    try {
      const res = await adminListingsApi.getAll({
        status: listingStatusFilter || undefined,
        keyword: listingKeyword.trim() || undefined,
        page: 1,
        pageSize: 50,
      });
      if (res.isSuccess && res.data) {
        setListings(res.data.items);
        setListingsTotal(res.data.totalCount);
      } else {
        setListings([]);
        setListingsTotal(0);
      }
    } catch {
      setListings([]);
    } finally {
      setLoadingListings(false);
    }
  }, [listingStatusFilter, listingKeyword]);

  const loadUsers = useCallback(async () => {
    if (!getStoredToken()) return;
    setLoadingUsers(true);
    try {
      const res = await adminUsersApi.getUsers({
        search: userSearch.trim() || undefined,
        role: userRoleFilter || undefined,
        status: userStatusFilter || undefined,
        page: 1,
        pageSize: 50,
      });
      if (res.isSuccess && res.data) {
        setUsers(res.data.items);
        setUsersTotal(res.data.totalCount);
      } else {
        setUsers([]);
        setUsersTotal(0);
      }
    } catch {
      setUsers([]);
    } finally {
      setLoadingUsers(false);
    }
  }, [userSearch, userRoleFilter, userStatusFilter]);

  const loadReports = useCallback(async () => {
    if (!getStoredToken()) return;
    setLoadingReports(true);
    try {
      const res = await adminReportsApi.getReports({
        status: reportStatusFilter || undefined,
        targetType: reportTargetFilter || undefined,
        page: 1,
        pageSize: 50,
      });
      if (res.isSuccess && res.data) {
        setReports(res.data.items);
        setReportsTotal(res.data.totalCount);
      } else {
        setReports([]);
        setReportsTotal(0);
      }
    } catch {
      setReports([]);
    } finally {
      setLoadingReports(false);
    }
  }, [reportStatusFilter, reportTargetFilter]);

  const loadCatalogs = useCallback(async () => {
    if (!getStoredToken()) return;
    setLoadingCatalogs(true);
    try {
      const [rtRes, amRes] = await Promise.all([
        adminCatalogsApi.getRoomTypes(),
        adminCatalogsApi.getAmenities(),
      ]);
      if (rtRes.isSuccess && rtRes.data) setRoomTypes(rtRes.data);
      if (amRes.isSuccess && amRes.data) setAmenities(amRes.data);
    } finally {
      setLoadingCatalogs(false);
    }
  }, []);

  const refreshAll = useCallback(async () => {
    await Promise.all([loadDashboard(), loadListings(), loadUsers(), loadReports(), loadCatalogs()]);
  }, [loadDashboard, loadListings, loadUsers, loadReports, loadCatalogs]);

  useEffect(() => {
    if (user && role === "Admin") {
      loadDashboard();
    }
  }, [user, role, loadDashboard]);

  useEffect(() => {
    if (user && role === "Admin") {
      loadListings();
    }
  }, [user, role, loadListings]);

  useEffect(() => {
    if (user && role === "Admin") {
      loadUsers();
    }
  }, [user, role, loadUsers]);

  useEffect(() => {
    if (user && role === "Admin") {
      loadReports();
    }
  }, [user, role, loadReports]);

  useEffect(() => {
    if (user && role === "Admin") {
      loadCatalogs();
    }
  }, [user, role, loadCatalogs]);

  const getStatusBadge = (status: string) => {
    const norm = (status || "").toUpperCase();
    if (norm === "PUBLISHED") {
      return {
        label: "Đã duyệt công khai",
        badgeClass: "bg-emerald-50 text-emerald-700 border-emerald-200/90",
        dotClass: "bg-emerald-500",
      };
    }
    if (norm === "PENDING_APPROVAL") {
      return {
        label: "Chờ phê duyệt",
        badgeClass: "bg-amber-50 text-amber-800 border-amber-200/90",
        dotClass: "bg-amber-500",
      };
    }
    if (norm === "DRAFT") {
      return {
        label: "Bản nháp",
        badgeClass: "bg-slate-100 text-slate-700 border-slate-200/90",
        dotClass: "bg-slate-400",
      };
    }
    if (norm === "REJECTED") {
      return {
        label: "Đã từ chối",
        badgeClass: "bg-red-50 text-red-700 border-red-200/90",
        dotClass: "bg-red-500",
      };
    }
    if (norm === "EXPIRED") {
      return {
        label: "Hết hạn",
        badgeClass: "bg-slate-100 text-slate-600 border-slate-200/90",
        dotClass: "bg-slate-500",
      };
    }
    return {
      label: "Đã ẩn / Khóa",
      badgeClass: "bg-rose-50 text-rose-700 border-rose-200/90",
      dotClass: "bg-rose-500",
    };
  };

  const handleOpenReviewDetail = async (listingId: number) => {
    try {
      const res = await adminListingsApi.getReviewListing(listingId);
      if (res.isSuccess && res.data) {
        setReviewDetailListing(res.data);
      } else {
        showToast(res.error?.message || "Không thể tải chi tiết tin đăng", "error");
      }
    } catch {
      showToast("Lỗi kết nối máy chủ", "error");
    }
  };

  const handleApproveBackend = async (listingId: number) => {
    try {
      const res = await adminListingsApi.approve(listingId);
      if (res.isSuccess) {
        showToast(`Đã duyệt tin #${listingId} thành công!`, "success");
        await Promise.all([loadListings(), loadDashboard()]);
      } else {
        showToast(res.error?.message || "Duyệt tin thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    }
  };

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
        await Promise.all([loadListings(), loadDashboard()]);
      } else {
        showToast(res.error?.message || "Từ chối thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    } finally {
      setRejectLoading(false);
    }
  };

  const handleHideListing = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!hidingListing) return;
    setHideLoading(true);
    try {
      const res = await adminListingsApi.hide(hidingListing.id, hideReason.trim() || undefined);
      if (res.isSuccess) {
        showToast(`Đã ẩn tin #${hidingListing.id} khỏi sàn.`, "success");
        setHidingListing(null);
        setHideReason("");
        await Promise.all([loadListings(), loadDashboard()]);
      } else {
        showToast(res.error?.message || "Không thể ẩn tin đăng", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    } finally {
      setHideLoading(false);
    }
  };

  const handleToggleLockUser = async (acc: AdminUserItemDto) => {
    setTogglingUserId(acc.id);
    const isLocked = acc.status === "Suspended";
    try {
      const res = isLocked
        ? await adminUsersApi.unlock(acc.id)
        : await adminUsersApi.lock(acc.id);
      if (res.isSuccess && res.data) {
        showToast(
          isLocked
            ? `Đã mở khóa tài khoản ${acc.fullName}!`
            : `Đã khóa tài khoản ${acc.fullName}.`,
          isLocked ? "success" : "info"
        );
        await Promise.all([loadUsers(), loadDashboard()]);
      } else {
        showToast(res.error?.message || "Thao tác thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối máy chủ", "error");
    } finally {
      setTogglingUserId(null);
    }
  };

  const handleResolveReport = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!resolvingReport) return;
    setResolveLoading(true);
    try {
      const res = await adminReportsApi.resolve(resolvingReport.id, {
        resolution: resolutionChoice,
        adminNote: resolutionNote.trim() || undefined,
        hideListing: resolutionChoice === "RESOLVED" ? resolutionHideListing : false,
      });
      if (res.isSuccess) {
        showToast(
          resolutionChoice === "RESOLVED"
            ? "Đã xác nhận vi phạm và cập nhật báo cáo."
            : "Đã bác bỏ báo cáo vi phạm.",
          "success"
        );
        setResolvingReport(null);
        setResolutionNote("");
        await Promise.all([loadReports(), loadListings(), loadDashboard()]);
      } else {
        showToast(res.error?.message || "Không thể xử lý báo cáo", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    } finally {
      setResolveLoading(false);
    }
  };

  const openCreateCatalogModal = () => {
    setEditingCatalogId(null);
    setCatalogCode("");
    setCatalogName("");
    setCatalogIsActive(true);
    setCatalogModalOpen(true);
  };

  const openEditCatalogModal = (item: RoomTypeDto | AmenityDto) => {
    setEditingCatalogId(item.id);
    setCatalogCode(item.code);
    setCatalogName(item.name);
    setCatalogIsActive(item.isActive ?? true);
    setCatalogModalOpen(true);
  };

  const handleSaveCatalog = async (e: React.FormEvent) => {
    e.preventDefault();
    const code = catalogCode.trim().toUpperCase();
    const name = catalogName.trim();
    if (!code || !name) {
      showToast("Vui lòng nhập đầy đủ mã code và tên hiển thị.", "error");
      return;
    }
    setSavingCatalog(true);
    try {
      const res =
        catalogSubTab === "roomTypes"
          ? editingCatalogId
            ? await adminCatalogsApi.updateRoomType(editingCatalogId, {
                code,
                name,
                isActive: catalogIsActive,
              })
            : await adminCatalogsApi.createRoomType({ code, name, isActive: catalogIsActive })
          : editingCatalogId
          ? await adminCatalogsApi.updateAmenity(editingCatalogId, {
              code,
              name,
              isActive: catalogIsActive,
            })
          : await adminCatalogsApi.createAmenity({ code, name, isActive: catalogIsActive });

      if (res.isSuccess) {
        showToast(editingCatalogId ? "Đã cập nhật danh mục." : "Đã thêm mới danh mục.", "success");
        setCatalogModalOpen(false);
        await loadCatalogs();
      } else {
        showToast(res.error?.message || "Lưu danh mục thất bại.", "error");
      }
    } catch {
      showToast("Lỗi kết nối máy chủ", "error");
    } finally {
      setSavingCatalog(false);
    }
  };

  const handleToggleCatalogActive = async (item: RoomTypeDto | AmenityDto) => {
    const nextActive = !(item.isActive ?? true);
    try {
      const res =
        catalogSubTab === "roomTypes"
          ? await adminCatalogsApi.updateRoomType(item.id, { isActive: nextActive })
          : await adminCatalogsApi.updateAmenity(item.id, { isActive: nextActive });
      if (res.isSuccess) {
        showToast(nextActive ? `Đã bật hiển thị "${item.name}".` : `Đã ẩn "${item.name}".`, "success");
        await loadCatalogs();
      } else {
        showToast(res.error?.message || "Cập nhật thất bại", "error");
      }
    } catch {
      showToast("Lỗi kết nối", "error");
    }
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

  const pendingListingsCount = dashboard?.listings.pendingApprovalListings ?? 0;
  const pendingReportsCount = dashboard?.reports.pendingReports ?? 0;

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
            Thống kê tổng quan, kiểm duyệt tin đăng, quản lý tài khoản, xử lý vi phạm và danh mục tìm kiếm.
          </p>
        </div>

        <div className="flex items-center gap-2.5">
          <select
            value={period}
            onChange={(e) => setPeriod(e.target.value as typeof period)}
            aria-label="Kỳ thống kê"
            className="px-3 py-2 rounded-xl border border-slate-200 bg-white text-xs font-bold text-slate-700"
          >
            <option value="all">Kỳ: Toàn thời gian</option>
            <option value="today">Kỳ: Hôm nay</option>
            <option value="week">Kỳ: 7 ngày qua</option>
            <option value="month">Kỳ: 30 ngày qua</option>
            <option value="year">Kỳ: 1 năm qua</option>
          </select>

          <button
            type="button"
            onClick={() => refreshAll()}
            className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-800 text-xs font-bold transition-colors"
          >
            <RefreshCw
              className={cn(
                "w-4 h-4",
                (loadingDashboard || loadingListings || loadingUsers || loadingReports) && "animate-spin"
              )}
            />
            <span>Làm mới</span>
          </button>
        </div>
      </div>

      {/* KPI Summary Cards (Dashboard quản trị) */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-slate-500 uppercase">Người dùng</span>
            <div className="w-9 h-9 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center">
              <Users className="w-4 h-4" />
            </div>
          </div>
          <div className="flex items-baseline gap-2">
            <span className="text-2xl font-black text-slate-900">
              {dashboard?.users.totalUsers ?? 0}
            </span>
            {period !== "all" && (
              <span className="text-[11px] font-bold text-emerald-600">
                +{dashboard?.users.newUsersInPeriod ?? 0} trong kỳ
              </span>
            )}
          </div>
          <p className="text-[11px] text-slate-500">
            {dashboard?.users.totalTenants ?? 0} người thuê • {dashboard?.users.totalLandlords ?? 0} chủ nhà •{" "}
            <span className="text-rose-600 font-semibold">
              {dashboard?.users.suspendedUsers ?? 0} bị khóa
            </span>
          </p>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-slate-500 uppercase">Tin đăng</span>
            <div className="w-9 h-9 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
              <FileText className="w-4 h-4" />
            </div>
          </div>
          <div className="flex items-baseline gap-2">
            <span className="text-2xl font-black text-slate-900">
              {dashboard?.listings.totalListings ?? 0}
            </span>
            {period !== "all" && (
              <span className="text-[11px] font-bold text-emerald-600">
                +{dashboard?.listings.newListingsInPeriod ?? 0} trong kỳ
              </span>
            )}
          </div>
          <p className="text-[11px] text-slate-500">
            {dashboard?.listings.publishedListings ?? 0} công khai •{" "}
            <span className="text-amber-700 font-semibold">
              {pendingListingsCount} chờ duyệt
            </span>{" "}
            • {dashboard?.listings.hiddenListings ?? 0} đã ẩn
          </p>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-slate-500 uppercase">Phòng & Tòa nhà</span>
            <div className="w-9 h-9 rounded-xl bg-indigo-50 text-indigo-600 flex items-center justify-center">
              <Building className="w-4 h-4" />
            </div>
          </div>
          <div className="flex items-baseline gap-2">
            <span className="text-2xl font-black text-slate-900">
              {dashboard?.properties.totalRooms ?? 0} phòng
            </span>
            {period !== "all" && (
              <span className="text-[11px] font-bold text-emerald-600">
                +{dashboard?.properties.newRoomsInPeriod ?? 0} mới
              </span>
            )}
          </div>
          <p className="text-[11px] text-slate-500">
            {dashboard?.properties.totalProperties ?? 0} tòa nhà •{" "}
            {dashboard?.properties.availableRooms ?? 0} trống •{" "}
            {dashboard?.properties.rentedRooms ?? 0} đã thuê
          </p>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-slate-500 uppercase">Báo cáo & Lượt xem</span>
            <div className="w-9 h-9 rounded-xl bg-rose-50 text-rose-600 flex items-center justify-center">
              <AlertTriangle className="w-4 h-4" />
            </div>
          </div>
          <div className="flex items-baseline gap-2">
            <span className="text-2xl font-black text-slate-900">
              {pendingReportsCount} chờ xử lý
            </span>
            <span className="text-[11px] text-slate-500">
              / {dashboard?.reports.totalReports ?? 0} báo cáo
            </span>
          </div>
          <p className="text-[11px] text-slate-500">
            {dashboard?.reports.resolvedReports ?? 0} đã xác nhận •{" "}
            {(dashboard?.listings.totalViews ?? 0).toLocaleString("vi-VN")} lượt xem tin
          </p>
        </div>
      </div>

      {/* Tabs Header */}
      <div className="flex items-center gap-6 border-b border-slate-200 overflow-x-auto">
        <button
          type="button"
          onClick={() => setActiveTab("listings")}
          className={cn(
            "pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2 whitespace-nowrap",
            activeTab === "listings"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          )}
        >
          <FileText className="w-4 h-4" />
          <span>
            Quản Lý & Duyệt Tin ({listingsTotal})
            {pendingListingsCount > 0 ? ` • ${pendingListingsCount} chờ` : ""}
          </span>
        </button>

        <button
          type="button"
          onClick={() => setActiveTab("users")}
          className={cn(
            "pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2 whitespace-nowrap",
            activeTab === "users"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          )}
        >
          <Users className="w-4 h-4" />
          <span>Quản Lý Tài Khoản ({usersTotal})</span>
        </button>

        <button
          type="button"
          onClick={() => setActiveTab("reports")}
          className={cn(
            "pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2 whitespace-nowrap",
            activeTab === "reports"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          )}
        >
          <AlertTriangle className="w-4 h-4" />
          <span>
            Báo Cáo Vi Phạm ({reportsTotal})
            {pendingReportsCount > 0 ? ` • ${pendingReportsCount} mới` : ""}
          </span>
        </button>

        <button
          type="button"
          onClick={() => setActiveTab("catalogs")}
          className={cn(
            "pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2 whitespace-nowrap",
            activeTab === "catalogs"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          )}
        >
          <Tags className="w-4 h-4" />
          <span>Quản Lý Danh Mục ({roomTypes.length + amenities.length})</span>
        </button>
      </div>

      {/* TAB 1: Listings Moderation & Management */}
      {activeTab === "listings" && (
        <div className="space-y-4">
          <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-xs flex flex-col sm:flex-row gap-3">
            <div className="relative flex-1">
              <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                placeholder="Tìm theo tiêu đề tin, mô tả hoặc ID tin / phòng / chủ nhà..."
                value={listingKeyword}
                onChange={(e) => setListingKeyword(e.target.value)}
                className="w-full pl-10 pr-4 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>

            <select
              value={listingStatusFilter}
              onChange={(e) => setListingStatusFilter(e.target.value)}
              className="px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white font-semibold text-slate-700"
            >
              <option value="">Tất cả trạng thái</option>
              <option value="PENDING_APPROVAL">Chờ phê duyệt</option>
              <option value="PUBLISHED">Đã duyệt công khai</option>
              <option value="HIDDEN">Đã ẩn</option>
              <option value="REJECTED">Đã từ chối</option>
              <option value="DRAFT">Bản nháp</option>
              <option value="EXPIRED">Hết hạn</option>
            </select>
          </div>

          <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
            <div className="hidden md:block overflow-x-auto">
              <table className="w-full text-xs text-left border-collapse min-w-[960px]">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50/70 text-slate-500 uppercase tracking-wider">
                    <th className="p-4">Tin đăng</th>
                    <th className="p-4">Chủ nhà</th>
                    <th className="p-4">Giá & Lượt xem</th>
                    <th className="p-4">Trạng thái</th>
                    <th className="p-4 text-right">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {listings.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="p-8 text-center text-slate-400 text-xs">
                        {loadingListings ? "Đang tải danh sách tin đăng..." : "Không tìm thấy tin đăng phù hợp."}
                      </td>
                    </tr>
                  ) : (
                    listings.map((item) => {
                      const badge = getStatusBadge(item.status);
                      const isPending = item.status === "PENDING_APPROVAL";
                      const canHide = item.status === "PUBLISHED" || item.status === "PENDING_APPROVAL";

                      return (
                        <tr key={item.id} className="hover:bg-slate-50/60 transition-colors">
                          <td className="p-4">
                            <div className="flex items-center gap-3">
                              <img
                                src={
                                  item.coverImageUrl ||
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
                                  #{item.id} • Phòng #{item.roomId}
                                  {item.addressDetail ? ` • ${item.addressDetail}` : ""}
                                </span>
                                {item.rejectionReason && (
                                  <span className="text-[10px] text-rose-600 block truncate">
                                    Ghi chú: {item.rejectionReason}
                                  </span>
                                )}
                              </div>
                            </div>
                          </td>
                          <td className="p-4 whitespace-nowrap">
                            <span className="font-bold text-slate-800 block">{item.landlordName}</span>
                            <span className="text-[11px] text-slate-400 block">ID #{item.landlordId}</span>
                          </td>
                          <td className="p-4 whitespace-nowrap">
                            <span className="font-bold text-blue-600 block">
                              {item.price.toLocaleString("vi-VN")} đ/tháng
                            </span>
                            <span className="text-slate-500 text-[11px] block">
                              {item.areaSqm ? `${item.areaSqm} m² • ` : ""}
                              {item.viewCount} lượt xem
                            </span>
                          </td>
                          <td className="p-4 whitespace-nowrap">
                            <span
                              className={cn(
                                "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold border",
                                badge.badgeClass
                              )}
                            >
                              <span className={cn("w-1.5 h-1.5 rounded-full shrink-0", badge.dotClass)} />
                              <span>{badge.label}</span>
                            </span>
                          </td>
                          <td className="p-4 text-right whitespace-nowrap">
                            <div className="flex items-center justify-end gap-2">
                              <button
                                type="button"
                                onClick={() => handleOpenReviewDetail(item.id)}
                                className="px-3 py-1.5 rounded-lg border border-slate-200 text-slate-700 hover:bg-slate-100 font-bold text-xs inline-flex items-center gap-1"
                              >
                                <Eye className="w-3.5 h-3.5" />
                                <span>Chi tiết</span>
                              </button>

                              {item.status === "PUBLISHED" && (
                                <Link
                                  href={`/listings/${item.id}`}
                                  target="_blank"
                                  className="px-3 py-1.5 rounded-lg border border-slate-200 text-slate-700 hover:bg-slate-100 font-bold text-xs inline-flex items-center gap-1"
                                >
                                  <ExternalLink className="w-3.5 h-3.5" />
                                  <span>Mở tin</span>
                                </Link>
                              )}

                              {isPending && (
                                <>
                                  <button
                                    type="button"
                                    onClick={() => handleApproveBackend(item.id)}
                                    className="px-3 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs"
                                  >
                                    Phê duyệt
                                  </button>
                                  <button
                                    type="button"
                                    onClick={() => {
                                      setRejectingListingId(item.id);
                                      setRejectReason("");
                                    }}
                                    className="px-3 py-1.5 rounded-lg border border-rose-200 text-rose-600 hover:bg-rose-50 font-bold text-xs"
                                  >
                                    Từ chối
                                  </button>
                                </>
                              )}

                              {canHide && (
                                <button
                                  type="button"
                                  onClick={() => {
                                    setHidingListing(item);
                                    setHideReason("");
                                  }}
                                  className="px-3 py-1.5 rounded-lg border border-amber-200 text-amber-700 hover:bg-amber-50 font-bold text-xs inline-flex items-center gap-1"
                                >
                                  <EyeOff className="w-3.5 h-3.5" />
                                  <span>Ẩn vi phạm</span>
                                </button>
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

            {/* Mobile Cards */}
            <div className="block md:hidden divide-y divide-slate-100">
              {listings.map((item) => {
                const badge = getStatusBadge(item.status);
                const isPending = item.status === "PENDING_APPROVAL";
                const canHide = item.status === "PUBLISHED" || item.status === "PENDING_APPROVAL";
                return (
                  <div key={item.id} className="p-4 space-y-2.5 text-xs">
                    <div className="flex items-start justify-between gap-2">
                      <div className="min-w-0">
                        <h4 className="font-bold text-slate-900 line-clamp-2">{item.title}</h4>
                        <p className="text-[11px] text-slate-500">
                          #{item.id} • {item.landlordName} • {item.price.toLocaleString("vi-VN")} đ/tháng
                        </p>
                      </div>
                      <span className={cn("px-2 py-0.5 rounded-full text-[10px] font-bold border shrink-0", badge.badgeClass)}>
                        {badge.label}
                      </span>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <button
                        type="button"
                        onClick={() => handleOpenReviewDetail(item.id)}
                        className="px-3 py-1.5 rounded-lg border border-slate-200 font-bold text-slate-700"
                      >
                        Chi tiết
                      </button>
                      {isPending && (
                        <>
                          <button
                            type="button"
                            onClick={() => handleApproveBackend(item.id)}
                            className="px-3 py-1.5 rounded-lg bg-emerald-600 text-white font-bold"
                          >
                            Phê duyệt
                          </button>
                          <button
                            type="button"
                            onClick={() => {
                              setRejectingListingId(item.id);
                              setRejectReason("");
                            }}
                            className="px-3 py-1.5 rounded-lg border border-rose-200 text-rose-600 font-bold"
                          >
                            Từ chối
                          </button>
                        </>
                      )}
                      {canHide && (
                        <button
                          type="button"
                          onClick={() => {
                            setHidingListing(item);
                            setHideReason("");
                          }}
                          className="px-3 py-1.5 rounded-lg border border-amber-200 text-amber-700 font-bold"
                        >
                          Ẩn vi phạm
                        </button>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      )}

      {/* TAB 2: Users Management */}
      {activeTab === "users" && (
        <div className="space-y-4">
          <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-xs flex flex-col sm:flex-row gap-3">
            <div className="relative flex-1">
              <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                placeholder="Tìm theo họ tên, email hoặc số điện thoại..."
                value={userSearch}
                onChange={(e) => setUserSearch(e.target.value)}
                className="w-full pl-10 pr-4 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>

            <select
              value={userRoleFilter}
              onChange={(e) => setUserRoleFilter(e.target.value)}
              className="px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white font-semibold text-slate-700"
            >
              <option value="">Tất cả vai trò</option>
              <option value="Tenant">Khách thuê (Tenant)</option>
              <option value="Landlord">Chủ nhà (Landlord)</option>
              <option value="Admin">Quản trị viên (Admin)</option>
            </select>

            <select
              value={userStatusFilter}
              onChange={(e) => setUserStatusFilter(e.target.value)}
              className="px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white font-semibold text-slate-700"
            >
              <option value="">Tất cả trạng thái</option>
              <option value="Active">Đang hoạt động</option>
              <option value="Suspended">Đang bị khóa</option>
            </select>
          </div>

          <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-xs text-left border-collapse min-w-[760px]">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50/70 text-slate-500 uppercase tracking-wider">
                    <th className="p-4">Tài khoản</th>
                    <th className="p-4">Email & SĐT</th>
                    <th className="p-4">Vai trò</th>
                    <th className="p-4">Ngày tham gia</th>
                    <th className="p-4">Trạng thái</th>
                    <th className="p-4 text-right">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {users.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="p-8 text-center text-slate-400">
                        {loadingUsers ? "Đang tải danh sách tài khoản..." : "Không tìm thấy tài khoản nào."}
                      </td>
                    </tr>
                  ) : (
                    users.map((acc) => {
                      const isLocked = acc.status === "Suspended";
                      const isAdminAcc = acc.role === "Admin";
                      return (
                        <tr key={acc.id} className="hover:bg-slate-50/60">
                          <td className="p-4">
                            <span className="font-bold text-slate-900 block">{acc.fullName}</span>
                            <span className="text-[11px] text-slate-400">ID #{acc.id}</span>
                          </td>
                          <td className="p-4">
                            <span className="font-medium text-slate-700 block">{acc.email}</span>
                            <span className="text-[11px] text-slate-400">
                              {acc.phoneNumber || "Chưa cập nhật SĐT"} •{" "}
                              {acc.emailConfirmed ? "Đã xác thực email" : "Chưa xác thực"}
                            </span>
                          </td>
                          <td className="p-4">
                            <span
                              className={cn(
                                "px-2.5 py-1 rounded-full text-xs font-bold border",
                                acc.role === "Landlord"
                                  ? "bg-purple-50 text-purple-700 border-purple-200"
                                  : acc.role === "Admin"
                                  ? "bg-rose-50 text-rose-700 border-rose-200"
                                  : "bg-blue-50 text-blue-700 border-blue-200"
                              )}
                            >
                              {acc.role === "Landlord"
                                ? "Chủ nhà"
                                : acc.role === "Admin"
                                ? "Admin"
                                : "Khách thuê"}
                            </span>
                          </td>
                          <td className="p-4 text-slate-500">
                            {new Date(acc.createdAt).toLocaleDateString("vi-VN")}
                          </td>
                          <td className="p-4 whitespace-nowrap">
                            <span
                              className={cn(
                                "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold border",
                                !isLocked
                                  ? "bg-emerald-50 text-emerald-700 border-emerald-200/90"
                                  : "bg-rose-50 text-rose-700 border-rose-200/90"
                              )}
                            >
                              <span
                                className={cn(
                                  "w-1.5 h-1.5 rounded-full shrink-0",
                                  !isLocked ? "bg-emerald-500" : "bg-rose-500"
                                )}
                              />
                              <span>{!isLocked ? "Hoạt động" : "Bị khóa"}</span>
                            </span>
                          </td>
                          <td className="p-4 text-right whitespace-nowrap">
                            {!isAdminAcc ? (
                              <button
                                type="button"
                                disabled={togglingUserId === acc.id}
                                onClick={() => handleToggleLockUser(acc)}
                                className={cn(
                                  "px-3 py-1.5 rounded-lg text-xs font-bold border transition-colors inline-flex items-center gap-1.5 disabled:opacity-50",
                                  !isLocked
                                    ? "border-rose-200 text-rose-600 hover:bg-rose-50"
                                    : "border-emerald-200 text-emerald-600 hover:bg-emerald-50"
                                )}
                              >
                                {!isLocked ? (
                                  <>
                                    <Lock className="w-3.5 h-3.5" />
                                    <span>Khóa tài khoản</span>
                                  </>
                                ) : (
                                  <>
                                    <Unlock className="w-3.5 h-3.5" />
                                    <span>Mở khóa</span>
                                  </>
                                )}
                              </button>
                            ) : (
                              <span className="text-[11px] text-slate-400 italic">Quản trị viên</span>
                            )}
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* TAB 3: Violation Reports */}
      {activeTab === "reports" && (
        <div className="space-y-4">
          <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-xs flex flex-col sm:flex-row gap-3">
            <select
              value={reportStatusFilter}
              onChange={(e) => setReportStatusFilter(e.target.value)}
              className="px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white font-semibold text-slate-700"
            >
              <option value="">Tất cả trạng thái xử lý</option>
              <option value="PENDING">Chờ xử lý</option>
              <option value="RESOLVED">Đã xác nhận vi phạm</option>
              <option value="DISMISSED">Đã bác bỏ</option>
            </select>

            <select
              value={reportTargetFilter}
              onChange={(e) => setReportTargetFilter(e.target.value)}
              className="px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white font-semibold text-slate-700"
            >
              <option value="">Tất cả đối tượng</option>
              <option value="LISTING">Tin đăng</option>
              <option value="LANDLORD">Chủ nhà</option>
            </select>
          </div>

          <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-xs text-left border-collapse min-w-[860px]">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50/70 text-slate-500 uppercase tracking-wider">
                    <th className="p-4">Đối tượng bị báo cáo</th>
                    <th className="p-4">Người báo cáo</th>
                    <th className="p-4">Lý do & Bằng chứng</th>
                    <th className="p-4">Thời gian</th>
                    <th className="p-4">Trạng thái</th>
                    <th className="p-4 text-right">Phán quyết</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {reports.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="p-8 text-center text-slate-400">
                        {loadingReports ? "Đang tải danh sách báo cáo..." : "Chưa có báo cáo vi phạm nào."}
                      </td>
                    </tr>
                  ) : (
                    reports.map((rep) => (
                      <tr key={rep.id} className="hover:bg-slate-50/60">
                        <td className="p-4">
                          <span className="inline-block px-2 py-0.5 rounded bg-slate-100 text-slate-600 text-[10px] font-bold mb-1">
                            {rep.targetType === "LISTING" ? "Tin đăng" : "Chủ nhà"} #{rep.targetId}
                          </span>
                          <span className="font-bold text-slate-900 block max-w-[240px] truncate" title={rep.targetTitle}>
                            {rep.targetTitle}
                          </span>
                          {rep.listingId && (
                            <Link
                              href={`/listings/${rep.listingId}`}
                              target="_blank"
                              className="text-[11px] text-blue-600 hover:underline inline-flex items-center gap-1 mt-0.5"
                            >
                              <span>Xem tin #{rep.listingId}</span>
                              <ExternalLink className="w-3 h-3" />
                            </Link>
                          )}
                        </td>
                        <td className="p-4">
                          <span className="font-bold text-slate-800 block">{rep.reporterName}</span>
                          <span className="text-[11px] text-slate-400">{rep.reporterEmail}</span>
                        </td>
                        <td className="p-4 max-w-[280px]">
                          <span className="text-rose-600 font-bold block">{rep.reason}</span>
                          {rep.description && (
                            <p className="text-[11px] text-slate-600 line-clamp-2 mt-0.5">{rep.description}</p>
                          )}
                          {rep.evidenceUrl && (
                            <a
                              href={rep.evidenceUrl}
                              target="_blank"
                              rel="noreferrer"
                              className="text-[11px] text-blue-600 hover:underline inline-flex items-center gap-1 mt-1"
                            >
                              <span>Xem bằng chứng</span>
                              <ExternalLink className="w-3 h-3" />
                            </a>
                          )}
                          {rep.adminNote && (
                            <p className="text-[11px] text-slate-500 italic mt-1">
                              Ghi chú Admin: {rep.adminNote}
                            </p>
                          )}
                        </td>
                        <td className="p-4 text-slate-500 whitespace-nowrap">
                          {new Date(rep.createdAt).toLocaleString("vi-VN")}
                        </td>
                        <td className="p-4 whitespace-nowrap">
                          <span
                            className={cn(
                              "px-2.5 py-1 rounded-full text-xs font-bold border",
                              rep.status === "PENDING"
                                ? "bg-amber-50 text-amber-700 border-amber-200"
                                : rep.status === "RESOLVED"
                                ? "bg-rose-50 text-rose-700 border-rose-200"
                                : "bg-slate-100 text-slate-600 border-slate-200"
                            )}
                          >
                            {rep.status === "PENDING"
                              ? "Chờ xử lý"
                              : rep.status === "RESOLVED"
                              ? "Đã xác nhận vi phạm"
                              : "Đã bác bỏ"}
                          </span>
                        </td>
                        <td className="p-4 text-right whitespace-nowrap">
                          {rep.status === "PENDING" ? (
                            <button
                              type="button"
                              onClick={() => {
                                setResolvingReport(rep);
                                setResolutionChoice("RESOLVED");
                                setResolutionNote("");
                                setResolutionHideListing(true);
                              }}
                              className="px-3 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs"
                            >
                              Xem xét & Xử lý
                            </button>
                          ) : (
                            <span className="text-[11px] text-slate-400">Đã đóng</span>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* TAB 4: System Catalogs Management (RoomTypes & Amenities) */}
      {activeTab === "catalogs" && (
        <div className="space-y-4">
          <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => setCatalogSubTab("roomTypes")}
                className={cn(
                  "px-4 py-2 rounded-xl text-xs font-bold transition-colors",
                  catalogSubTab === "roomTypes"
                    ? "bg-blue-600 text-white"
                    : "bg-slate-100 text-slate-700 hover:bg-slate-200"
                )}
              >
                Loại phòng ({roomTypes.length})
              </button>
              <button
                type="button"
                onClick={() => setCatalogSubTab("amenities")}
                className={cn(
                  "px-4 py-2 rounded-xl text-xs font-bold transition-colors",
                  catalogSubTab === "amenities"
                    ? "bg-blue-600 text-white"
                    : "bg-slate-100 text-slate-700 hover:bg-slate-200"
                )}
              >
                Tiện ích ({amenities.length})
              </button>
            </div>

            <button
              type="button"
              onClick={openCreateCatalogModal}
              className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold"
            >
              <Plus className="w-4 h-4" />
              <span>
                {catalogSubTab === "roomTypes" ? "Thêm loại phòng" : "Thêm tiện ích"}
              </span>
            </button>
          </div>

          <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-xs text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50/70 text-slate-500 uppercase tracking-wider">
                    <th className="p-4">ID</th>
                    <th className="p-4">Mã định danh (Code)</th>
                    <th className="p-4">Tên hiển thị</th>
                    <th className="p-4">Trạng thái</th>
                    <th className="p-4 text-right">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {loadingCatalogs ? (
                    <tr>
                      <td colSpan={5} className="p-8 text-center text-slate-400">
                        Đang tải danh mục...
                      </td>
                    </tr>
                  ) : (
                    (catalogSubTab === "roomTypes" ? roomTypes : amenities).map((item) => {
                      const active = item.isActive ?? true;
                      return (
                        <tr key={item.id} className="hover:bg-slate-50/60">
                          <td className="p-4 font-mono text-slate-500">#{item.id}</td>
                          <td className="p-4 font-mono font-bold text-slate-800">{item.code}</td>
                          <td className="p-4 font-bold text-slate-900">{item.name}</td>
                          <td className="p-4">
                            <span
                              className={cn(
                                "px-2.5 py-1 rounded-full text-xs font-bold border",
                                active
                                  ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                                  : "bg-slate-100 text-slate-500 border-slate-200"
                              )}
                            >
                              {active ? "Đang hiển thị" : "Đang ẩn"}
                            </span>
                          </td>
                          <td className="p-4 text-right whitespace-nowrap">
                            <div className="flex items-center justify-end gap-2">
                              <button
                                type="button"
                                onClick={() => openEditCatalogModal(item)}
                                className="px-3 py-1.5 rounded-lg border border-slate-200 text-slate-700 hover:bg-slate-100 font-bold text-xs inline-flex items-center gap-1"
                              >
                                <Pencil className="w-3.5 h-3.5" />
                                <span>Sửa</span>
                              </button>
                              <button
                                type="button"
                                onClick={() => handleToggleCatalogActive(item)}
                                className={cn(
                                  "px-3 py-1.5 rounded-lg border font-bold text-xs inline-flex items-center gap-1",
                                  active
                                    ? "border-amber-200 text-amber-700 hover:bg-amber-50"
                                    : "border-emerald-200 text-emerald-700 hover:bg-emerald-50"
                                )}
                              >
                                {active ? <EyeOff className="w-3.5 h-3.5" /> : <Eye className="w-3.5 h-3.5" />}
                                <span>{active ? "Ẩn" : "Hiện"}</span>
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* Modal: Review Listing Detail */}
      {reviewDetailListing && (
        <Modal
          isOpen={true}
          onClose={() => setReviewDetailListing(null)}
          title={`Chi Tiết Tin Đăng #${reviewDetailListing.id}`}
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
                <span className="text-[10px] font-bold text-slate-400 uppercase block">Trạng thái</span>
                <span className="font-bold text-slate-800">{reviewDetailListing.status}</span>
              </div>
              <div>
                <span className="text-[10px] font-bold text-slate-400 uppercase block">Chủ nhà & Phòng</span>
                <span className="font-bold text-slate-800">
                  Chủ #{reviewDetailListing.landlordId} • Phòng #{reviewDetailListing.roomId}
                </span>
              </div>
            </div>

            {reviewDetailListing.review?.rejectionReason && (
              <div className="p-3 rounded-xl bg-amber-50 border border-amber-200 text-rose-700 font-semibold">
                Lý do / Ghi chú gần nhất: {reviewDetailListing.review.rejectionReason}
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
              {reviewDetailListing.status === "PENDING_APPROVAL" && (
                <>
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
                    className="flex-1 py-2.5 rounded-xl bg-emerald-600 text-white font-bold hover:bg-emerald-700"
                  >
                    Phê duyệt công khai
                  </button>
                </>
              )}
            </div>
          </div>
        </Modal>
      )}

      {/* Modal: Reject Pending Listing */}
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

      {/* Modal: Hide Violating Listing */}
      {hidingListing && (
        <Modal
          isOpen={true}
          onClose={() => setHidingListing(null)}
          title={`Ẩn Tin Đăng Vi Phạm #${hidingListing.id}`}
          maxWidth="md"
        >
          <form onSubmit={handleHideListing} className="space-y-4">
            <p className="text-xs text-slate-600">
              Tin đăng <strong>{hidingListing.title}</strong> sẽ bị ẩn khỏi kết quả tìm kiếm công khai.
            </p>
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Lý do ẩn tin vi phạm (tùy chọn)
              </label>
              <textarea
                rows={3}
                maxLength={500}
                placeholder="VD: Vi phạm quy định nội dung / Thông tin sai sự thật..."
                value={hideReason}
                onChange={(e) => setHideReason(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>
            <div className="flex gap-3 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setHidingListing(null)}
                className="flex-1 py-2 rounded-xl border border-slate-200 text-xs font-bold text-slate-700"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={hideLoading}
                className="flex-1 py-2 rounded-xl bg-amber-600 text-white text-xs font-bold hover:bg-amber-700 disabled:opacity-50"
              >
                {hideLoading ? "Đang ẩn..." : "Xác nhận ẩn tin"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Modal: Resolve Violation Report */}
      {resolvingReport && (
        <Modal
          isOpen={true}
          onClose={() => setResolvingReport(null)}
          title={`Xử Lý Báo Cáo Vi Phạm #${resolvingReport.id}`}
          maxWidth="md"
        >
          <form onSubmit={handleResolveReport} className="space-y-4 text-xs">
            <div className="p-3 rounded-xl bg-slate-50 border border-slate-200 space-y-1">
              <span className="font-bold text-slate-900 block">{resolvingReport.targetTitle}</span>
              <span className="text-rose-600 font-semibold block">Lý do: {resolvingReport.reason}</span>
              {resolvingReport.description && (
                <p className="text-slate-600">{resolvingReport.description}</p>
              )}
            </div>

            <div className="grid grid-cols-2 gap-2">
              <button
                type="button"
                onClick={() => setResolutionChoice("RESOLVED")}
                className={cn(
                  "py-2.5 px-3 rounded-xl font-bold border flex items-center justify-center gap-1.5",
                  resolutionChoice === "RESOLVED"
                    ? "bg-rose-600 text-white border-rose-600"
                    : "border-slate-200 text-slate-700"
                )}
              >
                <CheckCircle2 className="w-4 h-4" />
                <span>Xác nhận vi phạm</span>
              </button>
              <button
                type="button"
                onClick={() => setResolutionChoice("DISMISSED")}
                className={cn(
                  "py-2.5 px-3 rounded-xl font-bold border flex items-center justify-center gap-1.5",
                  resolutionChoice === "DISMISSED"
                    ? "bg-slate-800 text-white border-slate-800"
                    : "border-slate-200 text-slate-700"
                )}
              >
                <Ban className="w-4 h-4" />
                <span>Bác bỏ báo cáo</span>
              </button>
            </div>

            {resolutionChoice === "RESOLVED" &&
              (resolvingReport.listingId || resolvingReport.targetType === "LISTING") && (
                <label className="flex items-center gap-2 cursor-pointer p-2.5 rounded-xl bg-amber-50 border border-amber-200">
                  <input
                    type="checkbox"
                    checked={resolutionHideListing}
                    onChange={(e) => setResolutionHideListing(e.target.checked)}
                    className="w-4 h-4 rounded text-rose-600"
                  />
                  <span className="font-semibold text-amber-900">
                    Tự động ẩn tin đăng liên quan (#{resolvingReport.listingId || resolvingReport.targetId})
                  </span>
                </label>
              )}

            <div>
              <label className="block font-bold text-slate-700 uppercase mb-1">
                Ghi chú kết quả xử lý
              </label>
              <textarea
                rows={3}
                maxLength={1000}
                placeholder="Nhập kết luận xử lý hoặc lý do bác bỏ..."
                value={resolutionNote}
                onChange={(e) => setResolutionNote(e.target.value)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </div>

            <div className="flex gap-3 pt-2 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setResolvingReport(null)}
                className="flex-1 py-2.5 rounded-xl border border-slate-200 font-bold text-slate-700"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={resolveLoading}
                className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white font-bold hover:bg-blue-700 disabled:opacity-50"
              >
                {resolveLoading ? "Đang lưu..." : "Lưu kết quả"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Modal: Create / Edit Catalog Item */}
      {catalogModalOpen && (
        <Modal
          isOpen={true}
          onClose={() => setCatalogModalOpen(false)}
          title={
            editingCatalogId
              ? `Chỉnh Sửa ${catalogSubTab === "roomTypes" ? "Loại Phòng" : "Tiện Ích"}`
              : `Thêm Mới ${catalogSubTab === "roomTypes" ? "Loại Phòng" : "Tiện Ích"}`
          }
          maxWidth="md"
        >
          <form onSubmit={handleSaveCatalog} className="space-y-4 text-xs">
            <div>
              <label className="block font-bold text-slate-700 uppercase mb-1">
                Mã code (chữ hoa, số, _, -)
              </label>
              <input
                type="text"
                required
                maxLength={50}
                pattern={"[A-Za-z0-9_\\-]+"}
                placeholder={catalogSubTab === "roomTypes" ? "VD: DUPLEX" : "VD: SMART_TV"}
                value={catalogCode}
                onChange={(e) => setCatalogCode(e.target.value.toUpperCase())}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 font-mono"
              />
            </div>

            <div>
              <label className="block font-bold text-slate-700 uppercase mb-1">
                Tên hiển thị
              </label>
              <input
                type="text"
                required
                maxLength={100}
                placeholder={catalogSubTab === "roomTypes" ? "VD: Phòng Duplex" : "VD: Smart TV"}
                value={catalogName}
                onChange={(e) => setCatalogName(e.target.value)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200"
              />
            </div>

            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={catalogIsActive}
                onChange={(e) => setCatalogIsActive(e.target.checked)}
                className="w-4 h-4 rounded text-blue-600"
              />
              <span className="font-semibold text-slate-700">Hiển thị trong hệ thống</span>
            </label>

            <div className="flex gap-3 pt-2 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setCatalogModalOpen(false)}
                className="flex-1 py-2.5 rounded-xl border border-slate-200 font-bold text-slate-700"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={savingCatalog}
                className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white font-bold hover:bg-blue-700 disabled:opacity-50"
              >
                {savingCatalog ? "Đang lưu..." : "Lưu danh mục"}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
