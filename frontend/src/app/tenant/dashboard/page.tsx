"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import { listingService, leasingService, billingService, viewingService, messagingService } from "@/lib/services";
import { LeaseContract, MonthlyBill, PropertyListing } from "@/types";
import { useToast } from "@/components/Toast";
import { ListingCard } from "@/features/listings/ListingCard";
import {
  FileText,
  DollarSign,
  Heart,
  Calendar,
  Eye,
  MessageSquare,
  AlertTriangle,
  CheckCircle2,
  Clock,
  Zap,
  Droplet,
  ShieldCheck,
  CreditCard,
  Building,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function TenantDashboardPage() {
  const { user } = useAuth();
  const { showToast } = useToast();

  const [activeTab, setActiveTab] = useState<"contracts" | "bills" | "history">("contracts");
  const [contracts, setContracts] = useState<LeaseContract[]>([]);
  const [bills, setBills] = useState<MonthlyBill[]>([]);
  const [recentListings, setRecentListings] = useState<PropertyListing[]>([]);

  const [stats, setStats] = useState({
    viewedCount: 0,
    favCount: 0,
    viewingCount: 0,
    chatCount: 0,
  });

  const loadData = () => {
    const allContracts = leasingService.getAll();
    const allBills = billingService.getAll();
    const favs = listingService.getFavorites();
    const viewings = viewingService.getAll();
    const convs = messagingService.getConversations();
    const recentIds = listingService.getRecentIds();
    const allListings = listingService.getAll();

    setContracts(allContracts);
    setBills(allBills);
    setRecentListings(recentIds.map((id) => allListings.find((x) => x.id === id)).filter(Boolean) as PropertyListing[]);

    setStats({
      viewedCount: recentIds.length,
      favCount: favs.length,
      viewingCount: viewings.length,
      chatCount: convs.length,
    });
  };

  useEffect(() => {
    loadData();
  }, []);

  const handlePayBill = (billId: string, amount: number) => {
    billingService.payBill(billId, amount);
    loadData();
    showToast("Thanh toán thành công! Công nợ kỳ này đã được cập nhật.", "success");
  };

  if (!user) {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <FileText className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Yêu Cầu Đăng Nhập</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập tài khoản Người thuê để xem hợp đồng thuê nhà, hóa đơn điện nước và tiến trình thuê phòng của bạn.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/tenant/dashboard"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng nhập tài khoản Người thuê</span>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-8">
      {/* Welcome Banner */}
      <div className="bg-gradient-to-r from-blue-700 via-blue-800 to-indigo-900 rounded-2xl p-6 sm:p-8 text-white shadow-xl flex flex-col sm:flex-row items-start sm:items-center justify-between gap-6">
        <div>
          <span className="inline-block px-3 py-1 rounded-full text-xs font-semibold bg-white/20 backdrop-blur-md mb-2">
            Cổng Thông Tin Người Thuê
          </span>
          <h1 className="text-2xl sm:text-3xl font-black">
            Xin chào, {user.displayName}!
          </h1>
          <p className="text-xs sm:text-sm text-blue-100/80 mt-1">
            Theo dõi hợp đồng, thanh toán hóa đơn điện nước và tiến trình tìm kiếm phòng của bạn.
          </p>
        </div>

        <Link
          href="/listings"
          className="px-5 py-2.5 rounded-xl bg-white text-blue-700 font-bold text-xs hover:bg-blue-50 shadow-md transition-all shrink-0"
        >
          Khám phá thêm phòng →
        </Link>
      </div>

      {/* 4 Stats Cards (Section 3.4) */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center shrink-0">
            <Eye className="w-6 h-6" />
          </div>
          <div>
            <span className="block text-2xl font-black text-slate-900">{stats.viewedCount}</span>
            <span className="text-xs text-slate-500 font-medium">Phòng đã xem</span>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-rose-50 text-rose-500 flex items-center justify-center shrink-0">
            <Heart className="w-6 h-6" />
          </div>
          <div>
            <span className="block text-2xl font-black text-slate-900">{stats.favCount}</span>
            <span className="text-xs text-slate-500 font-medium">Tin đã lưu yêu thích</span>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center shrink-0">
            <Calendar className="w-6 h-6" />
          </div>
          <div>
            <span className="block text-2xl font-black text-slate-900">{stats.viewingCount}</span>
            <span className="text-xs text-slate-500 font-medium">Lịch hẹn xem phòng</span>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-xs flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center shrink-0">
            <MessageSquare className="w-6 h-6" />
          </div>
          <div>
            <span className="block text-2xl font-black text-slate-900">{stats.chatCount}</span>
            <span className="text-xs text-slate-500 font-medium">Phiên chat với chủ trọ</span>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-slate-200 gap-4">
        <button
          onClick={() => setActiveTab("contracts")}
          className={cn(
            "pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2",
            activeTab === "contracts"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          )}
        >
          <FileText className="w-4 h-4" />
          <span>Hợp Đồng Thuê Nhà ({contracts.length})</span>
        </button>

        <button
          onClick={() => setActiveTab("bills")}
          className={cn(
            "pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2",
            activeTab === "bills"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          )}
        >
          <DollarSign className="w-4 h-4" />
          <span>Hóa Đơn & Tiền Điện Nước ({bills.length})</span>
        </button>

        <button
          onClick={() => setActiveTab("history")}
          className={cn(
            "pb-3 text-xs sm:text-sm font-bold border-b-2 transition-all flex items-center gap-2",
            activeTab === "history"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-slate-500 hover:text-slate-800"
          )}
        >
          <Eye className="w-4 h-4" />
          <span>Phòng Đã Xem Gần Đây ({recentListings.length})</span>
        </button>
      </div>

      {/* Tab 1: Contracts (Section 3.5) */}
      {activeTab === "contracts" && (
        <div className="space-y-4">
          {contracts.map((ctr) => (
            <div
              key={ctr.id}
              className="bg-white rounded-2xl border border-slate-200/80 shadow-xs p-6 space-y-4"
            >
              <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2 pb-3 border-b border-slate-100">
                <div className="flex items-center gap-3">
                  <span className="font-mono text-xs font-bold bg-slate-100 px-2.5 py-1 rounded-md text-slate-800">
                    {ctr.contractNumber}
                  </span>
                  <h3 className="font-bold text-slate-900 text-base">{ctr.roomName}</h3>
                </div>

                <div className="flex items-center gap-2">
                  <span
                    className={cn(
                      "px-3 py-1 rounded-full text-xs font-bold border",
                      ctr.status === "Còn hiệu lực"
                        ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                        : "bg-amber-50 text-amber-700 border-amber-200"
                    )}
                  >
                    {ctr.status} ({ctr.daysRemaining} ngày còn lại)
                  </span>
                </div>
              </div>

              <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs">
                <div>
                  <span className="text-slate-400 block font-medium">Giá thuê hàng tháng</span>
                  <span className="font-black text-blue-600 text-sm">
                    {ctr.monthlyRent.toLocaleString()} đ/tháng
                  </span>
                </div>
                <div>
                  <span className="text-slate-400 block font-medium">Tiền cọc giữ chỗ</span>
                  <span className="font-bold text-slate-800 text-sm">
                    {ctr.depositAmount.toLocaleString()} đ
                  </span>
                </div>
                <div>
                  <span className="text-slate-400 block font-medium">Thời hạn hợp đồng</span>
                  <span className="font-medium text-slate-700">
                    {ctr.startDate} → {ctr.endDate}
                  </span>
                </div>
                <div>
                  <span className="text-slate-400 block font-medium">Kỳ hạn thanh toán</span>
                  <span className="font-medium text-slate-700">
                    {ctr.paymentCycle} tháng / lần
                  </span>
                </div>
              </div>

              {ctr.terms && (
                <div className="p-3 bg-slate-50 rounded-xl text-xs text-slate-600 border border-slate-100">
                  <span className="font-bold text-slate-800 block mb-0.5">Điều khoản chính:</span>
                  {ctr.terms}
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Tab 2: Monthly Bills & Utilities (Section 3.5 & 3.8) */}
      {activeTab === "bills" && (
        <div className="space-y-4">
          {bills.map((bill) => (
            <div
              key={bill.id}
              className="bg-white rounded-2xl border border-slate-200/80 shadow-xs p-6 space-y-4"
            >
              <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2 pb-3 border-b border-slate-100">
                <div>
                  <span className="text-xs font-bold text-blue-600 uppercase tracking-wider block">
                    Kỳ thanh toán: {bill.period}
                  </span>
                  <h3 className="font-bold text-slate-900 text-base">{bill.roomName}</h3>
                </div>

                <div className="flex items-center gap-3">
                  <span
                    className={cn(
                      "px-3 py-1 rounded-full text-xs font-bold border",
                      bill.status === "Đã thanh toán"
                        ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                        : "bg-rose-50 text-rose-700 border-rose-200"
                    )}
                  >
                    {bill.status}
                  </span>

                  {bill.remainingDebt > 0 && (
                    <button
                      onClick={() => handlePayBill(bill.id, bill.remainingDebt)}
                      className="px-4 py-1.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-sm transition-colors flex items-center gap-1.5"
                    >
                      <CreditCard className="w-3.5 h-3.5" />
                      <span>Thanh toán ngay ({bill.remainingDebt.toLocaleString()} đ)</span>
                    </button>
                  )}
                </div>
              </div>

              {/* Breakdown */}
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs">
                <div className="p-3 rounded-xl bg-slate-50 border border-slate-100">
                  <span className="text-slate-500 block">Tiền phòng cơ bản</span>
                  <span className="font-bold text-slate-900 text-sm">
                    {bill.rentAmount.toLocaleString()} đ
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-amber-50/60 border border-amber-100">
                  <span className="text-amber-800 font-medium block">
                    Tiền điện ({bill.electricityUsage} kWh)
                  </span>
                  <span className="font-bold text-amber-950 text-sm">
                    {bill.electricityCost.toLocaleString()} đ
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-sky-50/60 border border-sky-100">
                  <span className="text-sky-800 font-medium block">
                    Tiền nước ({bill.waterUsage} m³)
                  </span>
                  <span className="font-bold text-sky-950 text-sm">
                    {bill.waterCost.toLocaleString()} đ
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-slate-50 border border-slate-100">
                  <span className="text-slate-500 block">Phí dịch vụ chung</span>
                  <span className="font-bold text-slate-900 text-sm">
                    {bill.serviceCost.toLocaleString()} đ
                  </span>
                </div>
              </div>

              {/* Total Debt Formula: Conno = Tongphaithu - Tongdathanhtoan */}
              <div className="p-4 rounded-xl bg-slate-900 text-white flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 text-xs">
                <div>
                  <span className="text-slate-400 block">
                    Công thức công nợ: Còn nợ = Tổng phải thu − Đã thanh toán
                  </span>
                  <span className="text-sm font-semibold text-slate-200">
                    Hạn thanh toán: trước ngày {bill.dueDate}
                  </span>
                </div>

                <div className="flex items-center gap-6">
                  <div>
                    <span className="text-slate-400 block text-[11px]">Tổng phải thu</span>
                    <span className="font-black text-white text-base">
                      {bill.totalDue.toLocaleString()} đ
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Đã thanh toán</span>
                    <span className="font-bold text-emerald-400 text-base">
                      {bill.paidAmount.toLocaleString()} đ
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Số tiền còn nợ</span>
                    <span className="font-black text-rose-400 text-base">
                      {bill.remainingDebt.toLocaleString()} đ
                    </span>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Tab 3: History */}
      {activeTab === "history" && (
        <div className="space-y-4">
          {recentListings.length === 0 ? (
            <div className="bg-white rounded-2xl border border-slate-200/80 p-12 text-center text-xs text-slate-500">
              Chưa có lịch sử xem phòng nào được ghi nhận trên phiên này.
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              {recentListings.map((listing) => (
                <ListingCard key={listing.id} listing={listing} />
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
