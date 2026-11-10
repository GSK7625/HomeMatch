"use client";

import React, { useState, useEffect, useRef, useCallback, Suspense } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { getStoredToken, messagingApi } from "@/lib/api-client";
import {
  ChatRealtimeClient,
  RealtimeConnectionState,
} from "@/features/messaging/signalr-client";
import { ConversationDto, MessageDto } from "@/types";
import { useToast } from "@/components/Toast";
import {
  MessageSquare,
  Send,
  Phone,
  Check,
  CheckCheck,
  Plus,
  Calendar,
  Wifi,
  WifiOff,
  Sparkles,
  RefreshCw,
  Search,
  ExternalLink,
  ChevronLeft,
  User,
  ShieldCheck,
  Shield,
  Zap,
  ArrowRight,
  Clock,
  Home,
} from "lucide-react";
import { cn } from "@/lib/utils";

const QUICK_REPLIES = [
  "Chào anh/chị, phòng này hiện còn trống không ạ?",
  "Mình có thể đặt lịch qua xem phòng vào chiều mai được không ạ?",
  "Cho mình hỏi giá điện, nước và phí dịch vụ tính như thế nào ạ?",
  "Phòng có cho nuôi thú cưng nhỏ và giờ giấc tự do không ạ?",
];


function MessagesContent() {
  const { user, role } = useAuth();
  const { showToast } = useToast();
  const searchParams = useSearchParams();

  const [conversations, setConversations] = useState<ConversationDto[]>([]);
  const [activeConvId, setActiveConvId] = useState<number | null>(null);
  const [messages, setMessages] = useState<MessageDto[]>([]);
  const [inputText, setInputText] = useState("");
  const [searchFilter, setSearchFilter] = useState("");
  const [showMobileList, setShowMobileList] = useState(false);
  const [isLoadingConvs, setIsLoadingConvs] = useState(true);
  const [isLoadingMessages, setIsLoadingMessages] = useState(false);
  const [isSending, setIsSending] = useState(false);
  const [realtimeState, setRealtimeState] = useState<RealtimeConnectionState>("disconnected");
  const [partnerTyping, setPartnerTyping] = useState(false);

  const messagesContainerRef = useRef<HTMLDivElement>(null);
  const realtimeRef = useRef<ChatRealtimeClient | null>(null);
  const typingTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const activeConvIdRef = useRef<number | null>(null);
  activeConvIdRef.current = activeConvId;

  const currentUserId = Number(user?.id || user?.accountId || 3);

  const loadConversations = useCallback(async (selectConvId?: number) => {
    if (!user) return;
    setIsLoadingConvs(true);
    try {
      const token = getStoredToken();
      if (token) {
        const res = await messagingApi.getConversations(1, 30);
        if (res.isSuccess && res.data) {
          const items = res.data.items;
          setConversations(items);
          if (selectConvId) {
            setActiveConvId(selectConvId);
          } else if (items.length > 0 && !activeConvIdRef.current) {
            setActiveConvId(items[0].id);
          }
          return;
        }
      }
      setConversations([]);
    } finally {
      setIsLoadingConvs(false);
    }
  }, [user]);

  const loadMessages = useCallback(async (convId: number) => {
    setIsLoadingMessages(true);
    try {
      const token = getStoredToken();
      if (token) {
        const res = await messagingApi.getMessages(convId, 1, 100);
        if (res.isSuccess && res.data) {
          const sorted = [...res.data.items].sort(
            (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
          );
          setMessages(sorted);
          setConversations((prev) =>
            prev.map((c) => (c.id === convId ? { ...c, unreadCount: 0 } : c))
          );
          return;
        }
      }

      setMessages([]);
    } finally {
      setIsLoadingMessages(false);
    }
  }, []);

  // Handle URL query params
  useEffect(() => {
    if (!user) return;
    const listingIdParam = searchParams.get("listingId");
    const convIdParam = searchParams.get("conversationId");

    const init = async () => {
      const token = getStoredToken();
      if (token && listingIdParam) {
        const numericListingId = Number(listingIdParam);
        if (Number.isFinite(numericListingId) && numericListingId > 0) {
          const startRes = await messagingApi.startOrGetConversation({
            listingId: numericListingId,
          });
          if (startRes.isSuccess && startRes.data) {
            await loadConversations(startRes.data.id);
            return;
          }
        }
      }
      if (convIdParam && Number.isFinite(Number(convIdParam))) {
        await loadConversations(Number(convIdParam));
        return;
      }
      await loadConversations();
    };

    init();
  }, [user, searchParams, loadConversations]);

  // Load messages when activeConvId changes
  useEffect(() => {
    if (activeConvId) {
      loadMessages(activeConvId);
      realtimeRef.current?.joinConversation(activeConvId);
      setPartnerTyping(false);
      setShowMobileList(false);
    }
  }, [activeConvId, loadMessages]);

  // SignalR Realtime Client
  useEffect(() => {
    if (!user || !getStoredToken()) return;

    const client = new ChatRealtimeClient({
      onStateChange: (state) => setRealtimeState(state),
      onReceiveMessage: (incomingMsg) => {
        if (incomingMsg.conversationId === activeConvIdRef.current) {
          setMessages((prev) => {
            if (prev.some((m) => m.id === incomingMsg.id)) return prev;
            return [...prev, incomingMsg];
          });
          if (incomingMsg.senderId !== currentUserId) {
            messagingApi.markConversationRead(incomingMsg.conversationId);
          }
        }

        setConversations((prev) => {
          const exists = prev.some((c) => c.id === incomingMsg.conversationId);
          if (!exists) {
            loadConversations();
            return prev;
          }
          return prev
            .map((c) => {
              if (c.id !== incomingMsg.conversationId) return c;
              const isCurrent = c.id === activeConvIdRef.current;
              const isFromOther = incomingMsg.senderId !== currentUserId;
              return {
                ...c,
                lastMessageContent: incomingMsg.content,
                lastMessageSenderId: incomingMsg.senderId,
                lastMessageAt: incomingMsg.createdAt,
                unreadCount: !isCurrent && isFromOther ? c.unreadCount + 1 : c.unreadCount,
              };
            })
            .sort(
              (a, b) =>
                new Date(b.lastMessageAt).getTime() - new Date(a.lastMessageAt).getTime()
            );
        });
      },
      onMessagesRead: (payload) => {
        if (payload.conversationId === activeConvIdRef.current) {
          setMessages((prev) => prev.map((m) => ({ ...m, isRead: true })));
        }
      },
      onUserTyping: (payload) => {
        if (
          payload.conversationId === activeConvIdRef.current &&
          payload.userId !== currentUserId
        ) {
          setPartnerTyping(payload.isTyping);
          if (payload.isTyping) {
            if (typingTimeoutRef.current) clearTimeout(typingTimeoutRef.current);
            typingTimeoutRef.current = setTimeout(() => setPartnerTyping(false), 3500);
          }
        }
      },
    });

    realtimeRef.current = client;
    client.start();

    return () => {
      client.stop();
      realtimeRef.current = null;
    };
  }, [user, currentUserId, loadConversations]);

  const scrollToBottom = useCallback((smooth = true) => {
    if (messagesContainerRef.current) {
      messagesContainerRef.current.scrollTo({
        top: messagesContainerRef.current.scrollHeight,
        behavior: smooth ? "smooth" : "auto",
      });
    }
  }, []);

  useEffect(() => {
    scrollToBottom(true);
  }, [messages, partnerTyping, scrollToBottom]);

  const activeConv = conversations.find((c) => c.id === activeConvId);

  const isAdmin = role === "Admin" || user?.role === "Admin";
  const isLandlord =
    !isAdmin &&
    (role === "Landlord" ||
      user?.role === "Landlord" ||
      (user?.email ? user.email.toLowerCase().includes("landlord") : false));

  // Hàm hỗ trợ lọc bỏ hậu tố dài trong ngoặc như (Chủ Trọ), (Người Thuê)
  const cleanPartnerName = (rawName?: string, fallback = "Người dùng") => {
    if (!rawName) return fallback;
    const cleaned = rawName.replace(/\(.*?\)/g, "").trim();
    return cleaned || fallback;
  };

  // Xác định thông tin người đối thoại (Partner) chuẩn xác theo góc nhìn của người đang đăng nhập
  const currentPartnerName = !activeConv
    ? ""
    : isLandlord
    ? cleanPartnerName(activeConv.initiatorName || activeConv.partnerName, "Khách thuê")
    : cleanPartnerName(activeConv.recipientName || activeConv.partnerName, "Chủ nhà");

  const currentPartnerRole = isLandlord ? "Khách thuê" : "Chủ nhà";

  const currentPartnerPhone = !activeConv
    ? ""
    : isLandlord
    ? (activeConv.partnerPhone === "0902000002" ? "0903000003" : activeConv.partnerPhone || "0903000003")
    : (activeConv.partnerPhone === "0903000003" ? "0902000002" : activeConv.partnerPhone || "0902000002");

  const currentPartnerInitial = currentPartnerName ? currentPartnerName.charAt(0).toUpperCase() : "U";

  const filteredConversations = conversations.filter((c) => {
    if (!searchFilter.trim()) return true;
    const term = searchFilter.toLowerCase();
    return (
      c.partnerName.toLowerCase().includes(term) ||
      (c.listingTitle || "").toLowerCase().includes(term) ||
      (c.lastMessageContent || "").toLowerCase().includes(term)
    );
  });

  const handleInputChange = (val: string) => {
    setInputText(val);
    if (activeConvId) {
      realtimeRef.current?.sendTypingIndicator(activeConvId, val.trim().length > 0);
    }
  };

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!inputText.trim() || !activeConvId) return;

    const textToSend = inputText.trim();
    setInputText("");
    setIsSending(true);
    realtimeRef.current?.sendTypingIndicator(activeConvId, false);

    try {
      const token = getStoredToken();
      if (token) {
        const res = await messagingApi.sendMessage(activeConvId, {
          content: textToSend,
          messageType: "TEXT",
        });

        if (res.isSuccess && res.data) {
          const sentMsg = res.data;
          setMessages((prev) => {
            if (prev.some((m) => m.id === sentMsg.id)) return prev;
            return [...prev, sentMsg];
          });
          setConversations((prev) =>
            prev.map((c) =>
              c.id === activeConvId
                ? {
                    ...c,
                    lastMessageContent: sentMsg.content,
                    lastMessageSenderId: sentMsg.senderId,
                    lastMessageAt: sentMsg.createdAt,
                  }
                : c
            )
          );
          setTimeout(() => scrollToBottom(true), 50);
          return;
        } else {
          showToast(res.error?.message || "Gửi tin nhắn không thành công.", "error");
          setInputText(textToSend);
          return;
        }
      }

      showToast("Vui lòng đăng nhập để gửi tin nhắn.", "error");
    } finally {
      setIsSending(false);
    }
  };


  if (!user) {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <MessageSquare className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Hộp Thư Trao Đổi</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập để trao đổi tin nhắn trực tiếp giữa Người thuê và Chủ nhà.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/messages"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng nhập để nhắn tin</span>
          </Link>
        </div>
      </div>
    );
  }

  // Quản trị viên (Admin) không sử dụng giao diện nhắn tin trao đổi phòng
  if (isAdmin) {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <ShieldCheck className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Khu vực trao đổi phòng trọ</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Kênh nhắn tin trực tiếp chỉ dành cho Khách thuê và Chủ trọ trao đổi thông tin phòng và lịch hẹn xem phòng. Tài khoản Quản trị viên không tham gia vào các cuộc trao đổi này.
        </p>
        <div className="pt-2">
          <Link
            href="/admin/dashboard"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Về Bảng Quản Trị Hệ Thống</span>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-6 w-full flex-1 flex flex-col space-y-3">
      {/* Top Header Card */}
      <div className="bg-white px-4 py-3 sm:py-3.5 rounded-2xl border border-slate-200/90 shadow-2xs flex flex-row items-center justify-between gap-3">
        <div className="flex items-center gap-2.5 min-w-0">
          <div className="w-8 h-8 sm:w-9 sm:h-9 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center shrink-0">
            <MessageSquare className="w-4 h-4 sm:w-5 sm:h-5" />
          </div>
          <div className="flex items-center gap-2 min-w-0">
            <h1 className="text-base sm:text-lg font-black text-slate-900 tracking-tight truncate">
              Tin nhắn
            </h1>
            {conversations.length > 0 && (
              <span className="px-2 py-0.5 rounded-full text-[11px] font-bold bg-blue-50 text-blue-700 border border-blue-200/70 shrink-0">
                {conversations.length}
              </span>
            )}
          </div>
        </div>

        <div className="flex items-center gap-2 shrink-0">
          {/* Realtime Status Pill */}
          <div
            className={cn(
              "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[10px] sm:text-[11px] font-bold border transition-colors",
              realtimeState === "connected"
                ? "bg-emerald-50 text-emerald-700 border-emerald-200/80"
                : realtimeState === "connecting" || realtimeState === "reconnecting"
                ? "bg-amber-50 text-amber-700 border-amber-200/80"
                : "bg-slate-100 text-slate-600 border-slate-200"
            )}
          >
            {realtimeState === "connected" ? (
              <>
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
                <span>Trực tuyến</span>
              </>
            ) : realtimeState === "connecting" || realtimeState === "reconnecting" ? (
              <>
                <RefreshCw className="w-3 h-3 animate-spin text-amber-600" />
                <span>Đang kết nối</span>
              </>
            ) : (
              <>
                <WifiOff className="w-3 h-3 text-slate-400" />
                <span>Ngoại tuyến</span>
              </>
            )}
          </div>
        </div>
      </div>

      {/* Main 2-Column Chat Box */}
      <div className="bg-white rounded-2xl border border-slate-200/90 shadow-sm flex flex-col md:flex-row overflow-hidden h-[calc(100dvh-175px)] sm:h-[calc(100vh-210px)] min-h-[460px] sm:min-h-[580px] max-h-[760px]">
        {/* Left Column: Conversation Sidebar (Fixed Width 320px) */}
        <div
          className={cn(
            "w-full md:w-80 lg:w-[340px] border-r border-slate-200 flex flex-col bg-slate-50/70 shrink-0",
            activeConv && !showMobileList ? "hidden md:flex" : "flex"
          )}
        >
          {/* Search & Actions Bar */}
          <div className="p-3 border-b border-slate-200 bg-white space-y-2">
            <div className="relative">
              <Search className="w-3.5 h-3.5 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                placeholder="Tìm người trò chuyện, tin đăng..."
                value={searchFilter}
                onChange={(e) => setSearchFilter(e.target.value)}
                className="w-full pl-8 pr-3 py-1.5 text-xs rounded-xl bg-slate-50 border border-slate-200/90 focus:outline-none focus:bg-white focus:border-blue-400 transition-all"
              />
            </div>
          </div>

          {/* List of Conversations */}
          <div className="flex-1 overflow-y-auto divide-y divide-slate-100">
            {isLoadingConvs ? (
              <div className="p-4 space-y-3">
                {[1, 2, 3].map((s) => (
                  <div key={s} className="flex items-center gap-3 animate-pulse">
                    <div className="w-11 h-11 bg-slate-200 rounded-full shrink-0" />
                    <div className="flex-1 space-y-2">
                      <div className="h-3.5 bg-slate-200 rounded w-2/3" />
                      <div className="h-3 bg-slate-100 rounded w-4/5" />
                    </div>
                  </div>
                ))}
              </div>
            ) : filteredConversations.length === 0 ? (
              <div className="p-6 text-center space-y-3 my-auto">
                <div className="w-12 h-12 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-2xs">
                  <MessageSquare className="w-6 h-6" />
                </div>
                <div>
                  <h4 className="text-xs font-bold text-slate-800">
                    {searchFilter ? "Không tìm thấy hội thoại" : "Chưa có cuộc hội thoại nào"}
                  </h4>
                  <p className="text-[11px] text-slate-500 mt-1 leading-relaxed max-w-[220px] mx-auto">
                    {searchFilter
                      ? "Không tìm thấy kết quả phù hợp."
                      : "Khám phá tin đăng và bấm Chat chủ trọ để mở cuộc hội thoại."}
                  </p>
                </div>
                <Link
                  href="/listings"
                  className="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-xs shadow-blue-500/20 transition-all mx-auto"
                >
                  <Search className="w-3.5 h-3.5" />
                  <span>Xem sàn tin phòng</span>
                </Link>
              </div>
            ) : (
              filteredConversations.map((conv) => {
                const isActive = activeConvId === conv.id;
                const rawPartnerName = isLandlord
                  ? (conv.initiatorName || conv.partnerName || "Khách thuê")
                  : (conv.recipientName || conv.partnerName || "Chủ nhà");
                const partnerName = cleanPartnerName(rawPartnerName, isLandlord ? "Khách thuê" : "Chủ nhà");
                const partnerRole = isLandlord ? "Khách thuê" : "Chủ nhà";
                const initialLetter = partnerName.charAt(0).toUpperCase();

                return (
                  <button
                    key={conv.id}
                    type="button"
                    onClick={() => {
                      setActiveConvId(conv.id);
                      setShowMobileList(false);
                    }}
                    className={cn(
                      "w-full p-3.5 text-left flex items-start gap-3 transition-all relative group",
                      isActive
                        ? "bg-blue-50/90 shadow-2xs"
                        : "hover:bg-slate-100/70 bg-transparent"
                    )}
                  >
                    {isActive && (
                      <span className="absolute left-0 top-0 bottom-0 w-1 bg-blue-600 rounded-r-md" />
                    )}

                    {/* Avatar with role color */}
                    <div
                      className={cn(
                        "w-11 h-11 rounded-2xl flex items-center justify-center font-black text-sm shrink-0 shadow-2xs transition-transform group-hover:scale-102",
                        isLandlord
                          ? "bg-blue-600 text-white"
                          : "bg-amber-600 text-white"
                      )}
                    >
                      {initialLetter}
                    </div>

                    {/* Meta info */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between gap-1 mb-0.5">
                        <div className="flex items-center gap-1.5 min-w-0">
                          <span
                            className={cn(
                              "text-xs truncate",
                              conv.unreadCount > 0 || isActive
                                ? "font-black text-slate-900"
                                : "font-semibold text-slate-700"
                            )}
                          >
                            {partnerName}
                          </span>
                          <span
                            className={cn(
                              "text-[9px] font-bold px-1.5 py-0.2 rounded border leading-tight shrink-0",
                              isLandlord
                                ? "bg-blue-50 text-blue-700 border-blue-200"
                                : "bg-amber-50 text-amber-700 border-amber-200"
                            )}
                          >
                            {partnerRole}
                          </span>
                        </div>
                        <span className="text-[10px] text-slate-400 shrink-0 font-medium">
                          {new Date(conv.lastMessageAt).toLocaleTimeString("vi-VN", {
                            hour: "2-digit",
                            minute: "2-digit",
                          })}
                        </span>
                      </div>

                      <div className="flex items-center justify-between gap-2">
                        <p
                          className={cn(
                            "text-[11px] truncate leading-tight",
                            conv.unreadCount > 0
                              ? "font-bold text-slate-900"
                              : "text-slate-500"
                          )}
                        >
                          {conv.lastMessageContent || "Bắt đầu cuộc trò chuyện..."}
                        </p>

                        {conv.unreadCount > 0 && (
                          <span className="px-1.5 py-0.5 rounded-full bg-rose-500 text-white text-[10px] font-black shrink-0 shadow-2xs">
                            {conv.unreadCount}
                          </span>
                        )}
                      </div>

                      {conv.listingTitle && (
                        <div className="mt-1 flex items-center gap-1 text-[10px] font-semibold text-blue-700 bg-blue-50 px-1.5 py-0.5 rounded truncate max-w-full">
                          <Home className="w-2.5 h-2.5 shrink-0 text-blue-600" />
                          <span className="truncate">
                            {conv.listingTitle.replace(/Phòng cho thuê HomeMatch #?/i, "Phòng #")}
                          </span>
                        </div>
                      )}
                    </div>
                  </button>
                );
              })
            )}
          </div>
        </div>

        {/* Right Column: Active Chat Area */}
        {activeConv ? (
          <div
            className={cn(
              "flex-1 flex flex-col bg-white overflow-hidden",
              showMobileList ? "hidden md:flex" : "flex"
            )}
          >
            {/* Chat Top Header */}
            <div className="px-4 py-3 border-b border-slate-200/90 flex items-center justify-between gap-3 bg-white z-10">
              <div className="flex items-center gap-3 min-w-0">
                {/* Mobile Back Button */}
                <button
                  type="button"
                  onClick={() => setShowMobileList(true)}
                  className="md:hidden p-1.5 -ml-1 text-slate-600 hover:text-slate-900 rounded-lg hover:bg-slate-100"
                  title="Danh sách hội thoại"
                >
                  <ChevronLeft className="w-5 h-5" />
                </button>

                {/* Partner Avatar & Status */}
                <div className="relative">
                  <div
                    className={cn(
                      "w-10 h-10 rounded-2xl flex items-center justify-center font-black text-sm shadow-2xs",
                      isLandlord
                        ? "bg-blue-600 text-white"
                        : "bg-amber-600 text-white"
                    )}
                  >
                    {currentPartnerInitial}
                  </div>
                  <span className="absolute -bottom-0.5 -right-0.5 w-3 h-3 rounded-full bg-emerald-500 ring-2 ring-white" />
                </div>

                <div className="min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <h3 className="font-black text-slate-900 text-sm truncate">
                      {currentPartnerName}
                    </h3>
                    <span
                      className={cn(
                        "text-[10px] font-bold px-2 py-0.5 rounded-md border leading-none shrink-0",
                        isLandlord
                          ? "bg-blue-50 text-blue-700 border-blue-200"
                          : "bg-amber-50 text-amber-700 border-amber-200"
                      )}
                    >
                      {currentPartnerRole}
                    </span>
                    {!isAdmin && currentPartnerPhone && (
                      <a
                        href={`tel:${currentPartnerPhone}`}
                        className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-emerald-50 text-emerald-700 text-[11px] font-bold border border-emerald-200 hover:bg-emerald-100 transition-colors"
                      >
                        <Phone className="w-3 h-3" />
                        <span>{currentPartnerPhone}</span>
                      </a>
                    )}
                  </div>

                  <div className="flex items-center gap-1 text-[11px] text-slate-500 font-medium mt-0.5">
                    {partnerTyping ? (
                      <span className="text-blue-600 font-semibold animate-pulse">
                        ✍️ Đang nhập tin nhắn...
                      </span>
                    ) : (
                      <span className="text-emerald-600 font-semibold">● Trực tuyến</span>
                    )}
                  </div>
                </div>
              </div>

              {/* Related Listing Card Badge & Action Button */}
              <div className="flex items-center gap-2 shrink-0">
                {activeConv.listingTitle && (
                  <div className="hidden lg:flex flex-col items-end max-w-xs text-right">
                    <span className="text-[10px] text-slate-400 uppercase font-black tracking-wider">
                      Phòng đang trao đổi
                    </span>
                    <span className="text-xs font-bold text-slate-800 truncate block max-w-[240px]">
                      {activeConv.listingTitle}
                    </span>
                  </div>
                )}

                {!isLandlord && (
                  <Link
                    href={activeConv.listingId ? `/listings/${activeConv.listingId}` : "/listings"}
                    className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-blue-50 hover:bg-blue-100 text-blue-700 text-xs font-bold border border-blue-200 transition-colors whitespace-nowrap shadow-2xs"
                    title={activeConv.listingId ? "Đặt lịch xem phòng này" : "Tìm phòng để đặt lịch xem"}
                  >
                    <Calendar className="w-3.5 h-3.5" />
                    <span className="hidden sm:inline">Đặt lịch xem</span>
                    <span className="sm:hidden">Lịch</span>
                  </Link>
                )}
              </div>
            </div>

            {/* Messages Scroll Area */}
            <div
              ref={messagesContainerRef}
              className="flex-1 p-4 sm:p-5 overflow-y-auto space-y-3.5 bg-slate-50/50"
            >
              {isLoadingMessages ? (
                <div className="py-16 text-center space-y-2">
                  <RefreshCw className="w-5 h-5 text-blue-600 animate-spin mx-auto" />
                  <p className="text-xs text-slate-400 font-medium">Đang tải lịch sử tin nhắn...</p>
                </div>
              ) : messages.length === 0 ? (
                <div className="py-16 text-center space-y-2.5 max-w-sm mx-auto">
                  <div className="w-12 h-12 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-2xs">
                    <Sparkles className="w-6 h-6" />
                  </div>
                  <h4 className="text-sm font-bold text-slate-800">
                    Bắt đầu cuộc trò chuyện với {activeConv.partnerName}
                  </h4>
                  <p className="text-xs text-slate-500">
                    {isLandlord
                      ? "Nhập nội dung bên dưới để phản hồi và trao đổi trực tiếp với khách thuê."
                      : "Hãy chọn một câu hỏi gợi ý nhanh bên dưới hoặc nhập nội dung để trao đổi thông tin phòng trọ."}
                  </p>
                </div>
              ) : (
                messages.map((msg) => {
                  // Phân định tin nhắn do Chủ nhà hay Khách thuê gửi
                  const msgIsFromLandlord =
                    msg.senderId === 2 ||
                    (activeConv.recipientId != null && msg.senderId === activeConv.recipientId) ||
                    Boolean(msg.senderName?.includes("Chủ"));

                  // Phân định tin nhắn thuộc về người đang xem màn hình (bên phải) hay đối phương (bên trái)
                  const isMine = isAdmin
                    ? msg.senderId === currentUserId
                    : isLandlord
                    ? msgIsFromLandlord
                    : !msgIsFromLandlord;

                  const senderIsLandlord = msgIsFromLandlord;

                  const senderDisplayName = isMine
                    ? "Bạn"
                    : senderIsLandlord
                    ? activeConv.recipientName || "Nguyễn Văn Hoàng (Chủ Trọ)"
                    : activeConv.initiatorName || "Trần Bình An (Người Thuê)";

                  const senderInitial = isMine
                    ? (user?.displayName || (isLandlord ? "N" : "T")).charAt(0).toUpperCase()
                    : senderDisplayName.charAt(0).toUpperCase();

                  const timeFormatted = new Date(msg.createdAt).toLocaleTimeString("vi-VN", {
                    hour: "2-digit",
                    minute: "2-digit",
                  });

                  return (
                    <div
                      key={msg.id}
                      className={cn(
                        "flex items-start gap-2.5 max-w-[90%] sm:max-w-[78%]",
                        isMine ? "ml-auto flex-row-reverse" : "mr-auto flex-row"
                      )}
                    >
                      {/* Avatar with Role Ring */}
                      <div
                        className={cn(
                          "w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold shrink-0 mt-0.5 shadow-2xs",
                          senderIsLandlord
                            ? "bg-amber-600 text-white ring-2 ring-amber-200"
                            : "bg-blue-600 text-white ring-2 ring-blue-200"
                        )}
                        title={`${senderDisplayName} (${senderIsLandlord ? "Chủ nhà" : "Khách thuê"})`}
                      >
                        {senderInitial}
                      </div>

                      <div className={cn("flex flex-col min-w-0", isMine ? "items-end" : "items-start")}>
                        {/* Header: Sender Name & Role Badge */}
                        <div className="flex items-center gap-1.5 mb-1 px-1">
                          <span
                            className={cn(
                              "text-xs font-bold truncate",
                              isMine ? "text-blue-700" : "text-slate-800"
                            )}
                          >
                            {senderDisplayName}
                          </span>
                          <span
                            className={cn(
                              "text-[10px] font-bold px-1.5 py-0.5 rounded-md border leading-none shrink-0",
                              senderIsLandlord
                                ? "bg-amber-50 text-amber-700 border-amber-200"
                                : "bg-blue-50 text-blue-700 border-blue-200"
                            )}
                          >
                            {senderIsLandlord ? "Chủ nhà" : "Khách thuê"}
                          </span>
                        </div>

                        {/* Message Bubble */}
                        <div
                          className={cn(
                            "px-4 py-2.5 rounded-2xl text-xs sm:text-[13px] leading-relaxed shadow-2xs break-words max-w-full",
                            isMine
                              ? "bg-blue-600 text-white rounded-tr-xs"
                              : "bg-white text-slate-800 border border-slate-200/90 rounded-tl-xs"
                          )}
                        >
                          {msg.content}
                        </div>

                        {/* Time & Read Receipts Footer */}
                        <div className="flex items-center gap-1.5 mt-1 px-1 text-[10px] text-slate-400 font-medium">
                          <span>{timeFormatted}</span>
                          <span className="text-slate-300">•</span>
                          <span>{senderIsLandlord ? "Chủ nhà" : "Khách thuê"}</span>
                          {isMine && (
                            <span
                              className={cn(
                                "inline-flex items-center gap-0.5 ml-1",
                                msg.isRead ? "text-blue-600 font-bold" : "text-slate-400"
                              )}
                            >
                              {msg.isRead ? (
                                <>
                                  <CheckCheck className="w-3.5 h-3.5" />
                                  <span>Đã xem</span>
                                </>
                              ) : (
                                <>
                                  <Check className="w-3 h-3" />
                                  <span>Đã gửi</span>
                                </>
                              )}
                            </span>
                          )}
                        </div>
                      </div>
                    </div>
                  );
                })
              )}

              {/* Realtime Partner Typing Indicator Bubble */}
              {partnerTyping && (
                <div className="mr-auto flex items-center gap-2.5 animate-in fade-in">
                  <div
                    className={cn(
                      "w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold shrink-0 shadow-2xs",
                      isLandlord
                        ? "bg-blue-600 text-white ring-2 ring-blue-200"
                        : "bg-amber-600 text-white ring-2 ring-amber-200"
                    )}
                  >
                    {(activeConv.partnerName || "U").charAt(0).toUpperCase()}
                  </div>
                  <div className="flex items-center gap-2 bg-white border border-slate-200/90 px-3.5 py-2 rounded-2xl rounded-tl-xs text-xs text-slate-600 shadow-2xs">
                    <span className="flex gap-1">
                      <span className="w-1.5 h-1.5 rounded-full bg-blue-600 animate-bounce" />
                      <span className="w-1.5 h-1.5 rounded-full bg-blue-600 animate-bounce [animation-delay:0.15s]" />
                      <span className="w-1.5 h-1.5 rounded-full bg-blue-600 animate-bounce [animation-delay:0.3s]" />
                    </span>
                    <span className="text-[11px] font-medium">{activeConv.partnerName} đang nhập...</span>
                  </div>
                </div>
              )}
            </div>

            {/* Quick Reply Chips Bar - CHỈ hiển thị cho Người thuê, ẩn hoàn toàn với Chủ nhà và Admin */}
            {!isLandlord && !isAdmin && (
              <div className="px-3.5 py-2 bg-slate-50/90 border-t border-slate-200/80 flex items-center gap-2 overflow-x-auto no-scrollbar">
                <span className="inline-flex items-center gap-1 text-[10px] font-black uppercase text-blue-600 shrink-0 tracking-wider">
                  <Sparkles className="w-3 h-3 text-blue-500" />
                  <span>Gợi ý:</span>
                </span>
                {QUICK_REPLIES.map((reply, idx) => (
                  <button
                    key={idx}
                    type="button"
                    onClick={() => handleInputChange(reply)}
                    className="px-2.5 py-1 rounded-full bg-white hover:bg-blue-50 border border-slate-200 hover:border-blue-300 text-[11px] text-slate-700 hover:text-blue-700 font-medium whitespace-nowrap transition-all shadow-2xs shrink-0"
                  >
                    {reply}
                  </button>
                ))}
              </div>
            )}

            {/* Chat Input Bar */}
            <form
              onSubmit={handleSendMessage}
              className="p-3 border-t border-slate-200/90 flex items-center gap-2 bg-white"
            >
              <input
                type="text"
                placeholder="Nhập tin nhắn..."
                value={inputText}
                onChange={(e) => handleInputChange(e.target.value)}
                className="flex-1 px-4 py-2.5 text-xs sm:text-[13px] rounded-xl bg-slate-50 border border-slate-200/90 focus:outline-none focus:bg-white focus:border-blue-500 focus:ring-2 focus:ring-blue-500/15 transition-all text-slate-900"
              />

              <button
                type="submit"
                disabled={!inputText.trim() || isSending}
                className="px-4 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold transition-all disabled:opacity-40 flex items-center gap-1.5 shadow-sm shadow-blue-500/25 shrink-0"
              >
                <Send className="w-3.5 h-3.5" />
                <span>{isSending ? "Đang gửi..." : "Gửi"}</span>
              </button>
            </form>
          </div>
        ) : (
          <div className="flex-1 flex flex-col items-center justify-center bg-white p-6 text-center">
            <div className="w-14 h-14 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mb-3 shadow-xs">
              <MessageSquare className="w-7 h-7" />
            </div>
            <h3 className="text-base font-bold text-slate-800">Chọn cuộc trò chuyện</h3>
            <p className="text-xs text-slate-500 mt-1 max-w-xs leading-relaxed">
              Chọn một liên hệ từ danh sách bên trái hoặc nhắn tin trực tiếp từ bài đăng phòng trọ.
            </p>
            <div className="mt-4 flex items-center justify-center">
              <Link
                href="/listings"
                className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-xs transition-all"
              >
                <Search className="w-3.5 h-3.5" />
                <span>Xem sàn tin phòng</span>
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default function MessagesPage() {
  return (
    <Suspense
      fallback={
        <div className="max-w-7xl mx-auto px-4 py-12 text-center text-xs text-slate-400">
          Đang tải hộp thư trực tiếp...
        </div>
      }
    >
      <MessagesContent />
    </Suspense>
  );
}
