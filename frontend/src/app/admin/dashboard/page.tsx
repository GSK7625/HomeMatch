"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import { listingService, adminService } from "@/lib/services";
import { PropertyListing, SystemReport } from "@/types";
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
      name: "Nguyễn Văn Hùng",
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

  const loadData = () => {
    setListings(listingService.getAll());
    setReports(adminService.getReports());
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleApproveListing = (id: string) => {
    listingService.update(id, { status: "Còn trống" });
    loadData();
    showToast("Đã phê duyệt tin đăng công khai trên toàn hệ thống!", "success");
  };

  const handleRejectListing = (id: string) => {
    listingService.update(id, { status: "Tạm ngừng" });
    loadData();
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
    loadData();
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
      {/* Top Banner */}
      <div className="bg-slate-900 rounded-2xl p-6 sm:p-8 text-white shadow-xl flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
        <div>
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-xs font-semibold bg-white/10 text-emerald-400 mb-2">
            <ShieldCheck className="w-3.5 h-3.5" />
            <span>Quyền Hạn Quản Trị Hệ Thống (Module 3.11)</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-black">Admin Management Portal</h1>
          <p className="text-xs sm:text-sm text-slate-400 mt-1">
            Kiểm soát tin đăng, quản lý người dùng, xử lý phản ánh vi phạm và giám sát toàn diện hệ thống HomeMatch.
          </p>
        </div>

        <div className="flex gap-4 text-center">
          <div className="p-3 bg-slate-800 rounded-xl border border-slate-700 min-w-[100px]">
            <span className="block text-2xl font-black text-white">{accounts.length}</span>
            <span className="text-[11px] text-slate-400">Người dùng</span>
          </div>
          <div className="p-3 bg-slate-800 rounded-xl border border-slate-700 min-w-[100px]">
            <span className="block text-2xl font-black text-blue-400">{listings.length}</span>
            <span className="text-[11px] text-slate-400">Tin đăng</span>
          </div>
          <div className="p-3 bg-slate-800 rounded-xl border border-slate-700 min-w-[100px]">
            <span className="block text-2xl font-black text-rose-400">{reports.length}</span>
            <span className="text-[11px] text-slate-400">Báo cáo vi phạm</span>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-slate-200 gap-4">
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
          <span>Kiểm Duyệt Tin Đăng ({listings.length})</span>
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
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-x-auto">
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
                        onClick={() => handleApproveListing(item.id)}
                        className="px-3 py-1 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs"
                      >
                        Phê duyệt
                      </button>
                      <button
                        onClick={() => handleRejectListing(item.id)}
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
      )}

      {/* Tab 2: Users Management */}
      {activeTab === "users" && (
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-x-auto">
          <table className="w-full text-xs text-left border-collapse min-w-[700px]">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50/70 text-slate-500 uppercase tracking-wider">
                <th className="p-4">Họ và tên</th>
                <th className="p-4">Email</th>
                <th className="p-4">Vai trò (Role)</th>
                <th className="p-4">Trạng thái tài khoản</th>
                <th className="p-4 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {accounts.map((acc) => (
                <tr key={acc.id} className="hover:bg-slate-50/60">
                  <td className="p-4 font-bold text-slate-900">{acc.name}</td>
                  <td className="p-4 text-slate-600">{acc.email}</td>
                  <td className="p-4">
                    <span className="px-2.5 py-0.5 rounded-full text-xs font-bold bg-slate-100 text-slate-700">
                      {acc.role}
                    </span>
                  </td>
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
                        "px-3 py-1.5 rounded-xl font-bold text-xs border transition-colors inline-flex items-center gap-1.5",
                        acc.status === "Hoạt động"
                          ? "border-rose-200 text-rose-600 hover:bg-rose-50"
                          : "bg-emerald-600 text-white hover:bg-emerald-700"
                      )}
                    >
                      {acc.status === "Hoạt động" ? (
                        <>
                          <Lock className="w-3.5 h-3.5" />
                          <span>Khóa tài khoản</span>
                        </>
                      ) : (
                        <>
                          <Unlock className="w-3.5 h-3.5" />
                          <span>Mở khóa tài khoản</span>
                        </>
                      )}
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
        <div className="space-y-4">
          {reports.map((rep) => (
            <div
              key={rep.id}
              className="bg-white rounded-2xl border border-slate-200/80 p-6 space-y-3 shadow-xs"
            >
              <div className="flex items-start justify-between gap-4">
                <div>
                  <span className="text-[11px] font-bold text-rose-600 uppercase tracking-wider block">
                    Báo cáo {rep.targetType}: {rep.targetTitle}
                  </span>
                  <span className="text-xs text-slate-500">
                    Người gửi phản ánh: {rep.reporterName} ({rep.reporterEmail})
                  </span>
                </div>

                <span
                  className={cn(
                    "px-2.5 py-1 rounded-full text-xs font-bold border",
                    rep.status === "Chờ xử lý"
                      ? "bg-amber-50 text-amber-700 border-amber-200"
                      : "bg-emerald-50 text-emerald-700 border-emerald-200"
                  )}
                >
                  {rep.status}
                </span>
              </div>

              <p className="text-xs text-slate-700 bg-slate-50 p-3 rounded-xl border border-slate-100">
                <span className="font-bold text-slate-900">Nội dung phản ánh:</span> {rep.reason}
              </p>

              {rep.status === "Chờ xử lý" && (
                <div className="flex items-center justify-end gap-2 pt-2">
                  <button
                    onClick={() => handleResolveReport(rep.id, "Đã xử lý")}
                    className="px-3.5 py-1.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700"
                  >
                    Xử lý & Cảnh cáo vi phạm
                  </button>
                  <button
                    onClick={() => handleResolveReport(rep.id, "Đã bác bỏ")}
                    className="px-3.5 py-1.5 rounded-xl border border-slate-200 text-slate-600 text-xs font-bold hover:bg-slate-50"
                  >
                    Bác bỏ phản ánh
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
