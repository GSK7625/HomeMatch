"use client";

import React, { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { messagingApi } from "@/lib/api-client";
import { useToast } from "@/components/Toast";
import { formatDateTime } from "@/features/leasing/leasing-helpers";
import {
  LandlordLeadDto,
  LandlordLeadFilterOptionDto,
  LandlordLeadStatusSummaryDto,
  LeadStatus,
} from "@/types";
import { cn } from "@/lib/utils";
import {
  Check,
  FilePlus2,
  Filter,
  Loader2,
  Mail,
  MessageSquare,
  Pencil,
  Phone,
  RefreshCw,
  Search,
  Users,
  X,
} from "lucide-react";

const LEAD_STATUS_META: Record<
  LeadStatus,
  { label: string; badgeClass: string }
> = {
  NEW: {
    label: "Mới quan tâm",
    badgeClass: "bg-blue-50 text-blue-700 border-blue-200",
  },
  CONTACTED: {
    label: "Đã liên hệ",
    badgeClass: "bg-indigo-50 text-indigo-700 border-indigo-200",
  },
  VIEWING_SCHEDULED: {
    label: "Đã hẹn xem phòng",
    badgeClass: "bg-amber-50 text-amber-700 border-amber-200",
  },
  CONTRACT_SIGNED: {
    label: "Đã ký hợp đồng",
    badgeClass: "bg-emerald-50 text-emerald-700 border-emerald-200",
  },
  LOST: {
    label: "Ngừng quan tâm",
    badgeClass: "bg-slate-100 text-slate-600 border-slate-200",
  },
};

const SOURCE_LABELS: Record<string, string> = {
  DIRECT: "Liên hệ trực tiếp",
  SEARCH_FILTER: "Tìm kiếm tin đăng",
  COMPARISON: "So sánh phòng",
  AI_CHAT: "Gợi ý AI",
};

interface LandlordLeadsPanelProps {
  onCreateContractClick: () => void;
}

export function LandlordLeadsPanel({ onCreateContractClick }: LandlordLeadsPanelProps) {
  const router = useRouter();
  const { showToast } = useToast();

  const [leads, setLeads] = useState<LandlordLeadDto[]>([]);
  const [summary, setSummary] = useState<LandlordLeadStatusSummaryDto>({
    total: 0,
    newCount: 0,
    contactedCount: 0,
    viewingScheduledCount: 0,
    contractSignedCount: 0,
    lostCount: 0,
  });
  const [listingOptions, setListingOptions] = useState<LandlordLeadFilterOptionDto[]>([]);
  const [loading, setLoading] = useState(true);

  const [selectedListingId, setSelectedListingId] = useState<string>("ALL");
  const [selectedStatus, setSelectedStatus] = useState<string>("ALL");
  const [searchInput, setSearchInput] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  const [updatingId, setUpdatingId] = useState<number | null>(null);
  const [openingChatId, setOpeningChatId] = useState<number | null>(null);
  const [editingNoteId, setEditingNoteId] = useState<number | null>(null);
  const [noteDraft, setNoteDraft] = useState("");

  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(searchInput.trim()), 250);
    return () => clearTimeout(t);
  }, [searchInput]);

  const loadLeads = useCallback(async () => {
    setLoading(true);
    try {
      const res = await messagingApi.getLandlordLeads({
        listingId: selectedListingId !== "ALL" ? Number(selectedListingId) : undefined,
        status: selectedStatus !== "ALL" ? selectedStatus : undefined,
        search: debouncedSearch || undefined,
        page: 1,
        pageSize: 50,
      });

      if (res.isSuccess && res.data) {
        setLeads(res.data.items);
        setSummary(res.data.summary);
        setListingOptions(res.data.listingOptions);
      } else {
        showToast(res.error?.message || "Không tải được danh sách khách quan tâm.", "error");
      }
    } finally {
      setLoading(false);
    }
  }, [selectedListingId, selectedStatus, debouncedSearch, showToast]);

  useEffect(() => {
    loadLeads();
  }, [loadLeads]);

  const handleStatusChange = async (lead: LandlordLeadDto, nextStatus: LeadStatus) => {
    if (lead.status === nextStatus) return;
    setUpdatingId(lead.id);
    try {
      const res = await messagingApi.updateLandlordLead(lead.id, { status: nextStatus });
      if (res.isSuccess && res.data) {
        showToast(
          `Đã chuyển trạng thái khách ${lead.tenantName} sang "${LEAD_STATUS_META[nextStatus].label}".`,
          "success"
        );
        await loadLeads();
      } else {
        showToast(res.error?.message || "Không thể cập nhật trạng thái khách quan tâm.", "error");
      }
    } finally {
      setUpdatingId(null);
    }
  };

  const handleSaveNote = async (lead: LandlordLeadDto) => {
    setUpdatingId(lead.id);
    try {
      const res = await messagingApi.updateLandlordLead(lead.id, { notes: noteDraft });
      if (res.isSuccess && res.data) {
        setLeads((prev) => prev.map((item) => (item.id === lead.id ? res.data! : item)));
        setEditingNoteId(null);
        showToast("Đã lưu ghi chú khách quan tâm.", "success");
      } else {
        showToast(res.error?.message || "Không thể lưu ghi chú.", "error");
      }
    } finally {
      setUpdatingId(null);
    }
  };

  const handleOpenChat = async (lead: LandlordLeadDto) => {
    if (lead.conversationId) {
      router.push(`/messages?conversationId=${lead.conversationId}`);
      return;
    }

    setOpeningChatId(lead.id);
    try {
      const res = await messagingApi.startOrGetConversation({
        listingId: lead.listingId,
        recipientUserId: lead.tenantId,
      });
      if (res.isSuccess && res.data) {
        router.push(`/messages?conversationId=${res.data.id}`);
      } else {
        showToast(res.error?.message || "Không thể mở cuộc trò chuyện với khách thuê này.", "error");
      }
    } finally {
      setOpeningChatId(null);
    }
  };

  const statusTabs = [
    { key: "ALL", label: "Tất cả", count: summary.total },
    { key: "NEW", label: "Mới quan tâm", count: summary.newCount },
    { key: "CONTACTED", label: "Đã liên hệ", count: summary.contactedCount },
    { key: "VIEWING_SCHEDULED", label: "Đã hẹn xem", count: summary.viewingScheduledCount },
    { key: "CONTRACT_SIGNED", label: "Đã ký HĐ", count: summary.contractSignedCount },
    { key: "LOST", label: "Ngừng quan tâm", count: summary.lostCount },
  ];

  return (
    <div className="space-y-5">
      {/* Filter Toolbar */}
      <div className="bg-white p-4 rounded-2xl border border-slate-200/80 shadow-xs space-y-4">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-3">
          {/* Status Filter Pills */}
          <div className="flex items-center gap-1.5 overflow-x-auto pb-1 lg:pb-0">
            {statusTabs.map((tab) => (
              <button
                key={tab.key}
                type="button"
                onClick={() => setSelectedStatus(tab.key)}
                className={cn(
                  "px-3 py-1.5 rounded-xl text-xs font-bold whitespace-nowrap transition-all border inline-flex items-center gap-1.5 cursor-pointer",
                  selectedStatus === tab.key
                    ? "bg-slate-900 text-white border-slate-900 shadow-xs"
                    : "bg-white text-slate-600 border-slate-200 hover:bg-slate-50"
                )}
              >
                <span>{tab.label}</span>
                <span
                  className={cn(
                    "px-1.5 py-0.5 rounded-md text-[10px] font-black",
                    selectedStatus === tab.key
                      ? "bg-white/20 text-white"
                      : "bg-slate-100 text-slate-700"
                  )}
                >
                  {tab.count}
                </span>
              </button>
            ))}
          </div>

          <button
            type="button"
            onClick={loadLeads}
            disabled={loading}
            className="self-start lg:self-auto p-2 rounded-xl border border-slate-200 bg-white hover:bg-slate-50 text-slate-600 transition-colors cursor-pointer"
            title="Làm mới danh sách khách quan tâm"
          >
            <RefreshCw className={cn("w-4 h-4", loading && "animate-spin text-blue-600")} />
          </button>
        </div>

        {/* Listing/Room Filter + Search */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3 pt-3 border-t border-slate-100">
          <div className="flex items-center gap-2">
            <Filter className="w-4 h-4 text-slate-400 shrink-0" />
            <select
              value={selectedListingId}
              onChange={(e) => setSelectedListingId(e.target.value)}
              className="w-full text-xs font-medium rounded-xl border border-slate-200 bg-slate-50/70 py-2 px-3 text-slate-700 focus:border-blue-500 focus:bg-white focus:outline-hidden cursor-pointer"
            >
              <option value="ALL">Tất cả tin đăng &amp; phòng ({listingOptions.length} tin)</option>
              {listingOptions.map((opt) => (
                <option key={opt.listingId} value={String(opt.listingId)}>
                  {opt.propertyName ? `${opt.propertyName}` : "Nhà trọ"}
                  {opt.roomNumber ? ` · P.${opt.roomNumber}` : ""} — {opt.listingTitle}
                </option>
              ))}
            </select>
          </div>

          <div className="relative">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder="Tìm theo tên khách, SĐT, email, phòng hoặc ghi chú..."
              className="w-full text-xs pl-9 pr-3 py-2 rounded-xl border border-slate-200 bg-slate-50/70 focus:bg-white focus:border-blue-500 focus:outline-hidden"
            />
          </div>
        </div>
      </div>

      {/* Leads Table */}
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
        {loading ? (
          <div className="p-8 space-y-3">
            {[1, 2, 3].map((i) => (
              <div key={i} className="h-14 rounded-xl bg-slate-100 animate-pulse" />
            ))}
          </div>
        ) : leads.length === 0 ? (
          <div className="py-16 px-6 text-center space-y-3">
            <div className="w-14 h-14 rounded-2xl bg-blue-50 text-blue-500 flex items-center justify-center mx-auto">
              <Users className="w-7 h-7" />
            </div>
            <h3 className="text-sm font-bold text-slate-900">
              Chưa có khách quan tâm phù hợp bộ lọc
            </h3>
            <p className="text-xs text-slate-500 max-w-md mx-auto">
              Khi người thuê nhắn tin hỏi phòng hoặc liên hệ từ tin đăng của bạn, hệ thống sẽ tự động
              ghi nhận khách quan tâm tại đây để bạn theo dõi trạng thái chuyển đổi.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-slate-50/80 border-b border-slate-200 text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                  <th className="py-3 px-4">Khách quan tâm</th>
                  <th className="py-3 px-4">Tin đăng / Phòng</th>
                  <th className="py-3 px-4">Trạng thái liên hệ</th>
                  <th className="py-3 px-4">Ghi chú &amp; Nguồn</th>
                  <th className="py-3 px-4 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-xs">
                {leads.map((lead) => {
                  const statusMeta = LEAD_STATUS_META[lead.status] ?? LEAD_STATUS_META.NEW;
                  const isBusy = updatingId === lead.id;
                  const isEditingNote = editingNoteId === lead.id;

                  return (
                    <tr key={lead.id} className="hover:bg-slate-50/70 transition-colors">
                      {/* Tenant Info */}
                      <td className="py-3.5 px-4 align-top">
                        <div className="font-bold text-slate-900">{lead.tenantName}</div>
                        <div className="mt-1 space-y-0.5 text-[11px] text-slate-500">
                          {lead.tenantPhone && (
                            <div className="flex items-center gap-1">
                              <Phone className="w-3 h-3 text-slate-400" />
                              <span>{lead.tenantPhone}</span>
                            </div>
                          )}
                          {lead.tenantEmail && (
                            <div className="flex items-center gap-1">
                              <Mail className="w-3 h-3 text-slate-400" />
                              <span className="truncate max-w-[190px]">{lead.tenantEmail}</span>
                            </div>
                          )}
                          <div className="text-[10px] text-slate-400 pt-0.5">
                            Quan tâm lúc: {formatDateTime(lead.createdAt)}
                          </div>
                        </div>
                      </td>

                      {/* Listing & Room */}
                      <td className="py-3.5 px-4 align-top">
                        <div className="font-bold text-slate-900 max-w-[240px] line-clamp-2">
                          {lead.listingTitle}
                        </div>
                        <div className="mt-1 text-[11px] text-slate-500">
                          {lead.propertyName ? (
                            <span>
                              {lead.propertyName}
                              {lead.roomNumber ? ` · Phòng ${lead.roomNumber}` : ""}
                            </span>
                          ) : lead.roomNumber ? (
                            <span>Phòng {lead.roomNumber}</span>
                          ) : (
                            <span>Tin #{lead.listingId}</span>
                          )}
                        </div>
                      </td>

                      {/* Status Selector */}
                      <td className="py-3.5 px-4 align-top">
                        <div className="flex items-center gap-1.5">
                          <select
                            value={lead.status}
                            disabled={isBusy}
                            onChange={(e) =>
                              handleStatusChange(lead, e.target.value as LeadStatus)
                            }
                            className={cn(
                              "text-[11px] font-bold px-2.5 py-1.5 rounded-lg border cursor-pointer focus:outline-hidden",
                              statusMeta.badgeClass
                            )}
                          >
                            {(Object.keys(LEAD_STATUS_META) as LeadStatus[]).map((st) => (
                              <option key={st} value={st} className="bg-white text-slate-800">
                                {LEAD_STATUS_META[st].label}
                              </option>
                            ))}
                          </select>
                          {isBusy && <Loader2 className="w-3.5 h-3.5 animate-spin text-blue-600" />}
                        </div>
                      </td>

                      {/* Notes & Source */}
                      <td className="py-3.5 px-4 align-top max-w-[260px]">
                        <span className="inline-block px-2 py-0.5 rounded-md bg-slate-100 text-slate-600 text-[10px] font-semibold mb-1.5">
                          {SOURCE_LABELS[lead.source] || lead.source}
                        </span>

                        {isEditingNote ? (
                          <div className="space-y-1.5">
                            <input
                              type="text"
                              value={noteDraft}
                              onChange={(e) => setNoteDraft(e.target.value)}
                              placeholder="Nhập ghi chú trao đổi với khách..."
                              maxLength={1000}
                              className="w-full px-2.5 py-1.5 text-[11px] rounded-lg border border-blue-300 bg-white focus:outline-hidden"
                            />
                            <div className="flex items-center gap-1.5">
                              <button
                                type="button"
                                disabled={isBusy}
                                onClick={() => handleSaveNote(lead)}
                                className="px-2.5 py-1 rounded-md bg-blue-600 hover:bg-blue-700 text-white text-[10px] font-bold inline-flex items-center gap-1 cursor-pointer"
                              >
                                <Check className="w-3 h-3" />
                                <span>Lưu</span>
                              </button>
                              <button
                                type="button"
                                onClick={() => setEditingNoteId(null)}
                                className="px-2 py-1 rounded-md border border-slate-200 text-slate-600 hover:bg-slate-50 text-[10px] font-bold inline-flex items-center gap-1 cursor-pointer"
                              >
                                <X className="w-3 h-3" />
                                <span>Hủy</span>
                              </button>
                            </div>
                          </div>
                        ) : (
                          <div className="flex items-start justify-between gap-2 group">
                            <p className="text-[11px] text-slate-600 leading-relaxed">
                              {lead.notes || (
                                <span className="italic text-slate-400">Chưa có ghi chú</span>
                              )}
                            </p>
                            <button
                              type="button"
                              onClick={() => {
                                setEditingNoteId(lead.id);
                                setNoteDraft(lead.notes || "");
                              }}
                              className="p-1 rounded-md text-slate-400 hover:text-blue-600 hover:bg-blue-50 shrink-0 cursor-pointer"
                              title="Chỉnh sửa ghi chú"
                            >
                              <Pencil className="w-3 h-3" />
                            </button>
                          </div>
                        )}
                      </td>

                      {/* Actions */}
                      <td className="py-3.5 px-4 align-top text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          <button
                            type="button"
                            disabled={openingChatId === lead.id}
                            onClick={() => handleOpenChat(lead)}
                            className="px-3 py-1.5 rounded-lg bg-blue-50 hover:bg-blue-600 text-blue-700 hover:text-white text-[11px] font-bold inline-flex items-center gap-1.5 transition-colors cursor-pointer"
                          >
                            {openingChatId === lead.id ? (
                              <Loader2 className="w-3.5 h-3.5 animate-spin" />
                            ) : (
                              <MessageSquare className="w-3.5 h-3.5" />
                            )}
                            <span>Nhắn tin</span>
                          </button>

                          {lead.status !== "CONTRACT_SIGNED" && lead.status !== "LOST" && (
                            <button
                              type="button"
                              onClick={onCreateContractClick}
                              className="px-2.5 py-1.5 rounded-lg border border-emerald-200 bg-emerald-50 hover:bg-emerald-600 text-emerald-700 hover:text-white text-[11px] font-bold inline-flex items-center gap-1 transition-colors cursor-pointer"
                              title="Mở biểu mẫu soạn hợp đồng mới"
                            >
                              <FilePlus2 className="w-3.5 h-3.5" />
                              <span>Soạn HĐ</span>
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
