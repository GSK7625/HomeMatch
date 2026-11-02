"use client";

import React, { useState, useEffect, useRef } from "react";
import Link from "next/link";
import { useAuth } from "@/features/auth/AuthContext";
import { messagingService } from "@/lib/services";
import { ChatConversation, ChatMessage } from "@/types";
import { MessageSquare, Send, User, Phone, Check, CheckCheck, ArrowLeft } from "lucide-react";
import { cn } from "@/lib/utils";

export default function MessagesPage() {
  const { user } = useAuth();
  const [conversations, setConversations] = useState<ChatConversation[]>([]);
  const [activeConvId, setActiveConvId] = useState<string>("");
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputText, setInputText] = useState("");
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const loadConversations = () => {
    const list = messagingService.getConversations();
    setConversations(list);
    if (list.length > 0 && !activeConvId) {
      setActiveConvId(list[0].id);
    }
  };

  useEffect(() => {
    loadConversations();
  }, []);

  useEffect(() => {
    if (activeConvId) {
      setMessages(messagingService.getMessages(activeConvId));
    }
  }, [activeConvId]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  const activeConv = conversations.find((c) => c.id === activeConvId);

  const handleSendMessage = (e: React.FormEvent) => {
    e.preventDefault();
    if (!inputText.trim() || !activeConvId || !activeConv) return;

    const senderId = user?.accountId || String(user?.id || "current-user");
    const senderName = user?.displayName || user?.fullName || "Bạn";

    const newMsg = messagingService.sendMessage(
      activeConvId,
      inputText.trim(),
      senderId,
      senderName,
      activeConv.partnerId
    );

    setMessages((prev) => [...prev, newMsg]);
    setInputText("");
    loadConversations();

    // Simulated partner response after 1.5 seconds for interactive experience
    setTimeout(() => {
      const replyMsg = messagingService.sendMessage(
        activeConvId,
        "Dạ vâng, mình đã ghi nhận thông tin. Bạn có thể qua xem trực tiếp hoặc gọi cho mình qua số điện thoại nhé!",
        activeConv.partnerId,
        activeConv.partnerName,
        senderId
      );
      setMessages((prev) => [...prev, replyMsg]);
      loadConversations();
    }, 1500);
  };

  if (!user) {
    return (
      <div className="max-w-md mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto shadow-xs">
          <MessageSquare className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-black text-slate-900">Hộp Thư Trực Tiếp</h2>
        <p className="text-xs text-slate-500 max-w-sm mx-auto leading-relaxed">
          Vui lòng đăng nhập để trao đổi tin nhắn trực tiếp giữa Người thuê và Chủ nhà.
        </p>
        <div className="pt-2">
          <Link
            href="/auth/login?redirect=/messages"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all"
          >
            <span>Đăng nhập để xem tin nhắn</span>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 flex flex-col">
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs flex-1 flex flex-col md:flex-row overflow-hidden min-h-[600px] max-h-[750px]">
        {/* Left Side: Conversations List */}
        <div className="w-full md:w-80 border-r border-slate-200 flex flex-col bg-slate-50/50">
          <div className="p-4 border-b border-slate-200 bg-white">
            <h2 className="font-bold text-slate-900 text-sm flex items-center gap-2">
              <MessageSquare className="w-4 h-4 text-blue-600" />
              <span>Hộp Thư Trực Tiếp ({conversations.length})</span>
            </h2>
          </div>

          <div className="flex-1 overflow-y-auto divide-y divide-slate-100">
            {conversations.map((conv) => (
              <button
                key={conv.id}
                onClick={() => setActiveConvId(conv.id)}
                className={cn(
                  "w-full p-4 text-left flex items-start gap-3 transition-colors hover:bg-slate-100/70",
                  activeConvId === conv.id ? "bg-blue-50/70 border-r-2 border-blue-600" : ""
                )}
              >
                <div className="w-10 h-10 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold text-xs shrink-0 shadow-xs">
                  {conv.partnerName.charAt(0)}
                </div>

                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between gap-1 mb-1">
                    <span className="font-bold text-xs text-slate-900 truncate">
                      {conv.partnerName}
                    </span>
                    <span className="text-[10px] text-slate-400 shrink-0">
                      {new Date(conv.lastMessageTime).toLocaleTimeString("vi-VN", {
                        hour: "2-digit",
                        minute: "2-digit",
                      })}
                    </span>
                  </div>

                  <p className="text-[11px] text-slate-500 truncate">{conv.lastMessage}</p>

                  {conv.listingTitle && (
                    <span className="inline-block mt-1 text-[10px] font-medium text-blue-600 bg-blue-100/60 px-1.5 py-0.2 rounded">
                      {conv.listingTitle}
                    </span>
                  )}
                </div>
              </button>
            ))}
          </div>
        </div>

        {/* Right Side: Active Chat Box */}
        {activeConv ? (
          <div className="flex-1 flex flex-col bg-white">
            {/* Chat Header */}
            <div className="p-4 border-b border-slate-200 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-slate-900 text-white flex items-center justify-center font-bold text-xs">
                  {activeConv.partnerName.charAt(0)}
                </div>
                <div>
                  <h3 className="font-bold text-slate-900 text-sm">{activeConv.partnerName}</h3>
                  <span className="text-[11px] text-emerald-600 font-medium">● Đang trực tuyến</span>
                </div>
              </div>

              {activeConv.listingTitle && (
                <div className="hidden sm:block text-right">
                  <span className="text-[10px] text-slate-400 uppercase font-bold block">
                    Tin phòng liên quan
                  </span>
                  <span className="text-xs font-semibold text-slate-800">
                    {activeConv.listingTitle}
                  </span>
                </div>
              )}
            </div>

            {/* Messages Scroll Area */}
            <div className="flex-1 p-4 overflow-y-auto space-y-3 bg-slate-50/30">
              {messages.map((msg) => {
                const isMine = msg.senderId === (user?.accountId || "current-user") || msg.senderId === "tenant-1";
                return (
                  <div
                    key={msg.id}
                    className={cn("flex flex-col max-w-[75%]", isMine ? "ml-auto items-end" : "mr-auto items-start")}
                  >
                    <div
                      className={cn(
                        "p-3 rounded-2xl text-xs leading-relaxed shadow-xs",
                        isMine
                          ? "bg-blue-600 text-white rounded-br-none"
                          : "bg-white text-slate-800 border border-slate-200/80 rounded-bl-none"
                      )}
                    >
                      {msg.content}
                    </div>
                    <span className="text-[10px] text-slate-400 mt-1 px-1">
                      {new Date(msg.timestamp).toLocaleTimeString("vi-VN", {
                        hour: "2-digit",
                        minute: "2-digit",
                      })}
                    </span>
                  </div>
                );
              })}
              <div ref={messagesEndRef} />
            </div>

            {/* Input Bar */}
            <form onSubmit={handleSendMessage} className="p-3 border-t border-slate-200 flex gap-2 bg-white">
              <input
                type="text"
                placeholder="Nhập nội dung tin nhắn trao đổi về phòng..."
                value={inputText}
                onChange={(e) => setInputText(e.target.value)}
                className="flex-1 px-4 py-2.5 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500/20"
              />
              <button
                type="submit"
                disabled={!inputText.trim()}
                className="px-4 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold transition-colors disabled:opacity-50 flex items-center gap-1.5"
              >
                <Send className="w-3.5 h-3.5" />
                <span>Gửi</span>
              </button>
            </form>
          </div>
        ) : (
          <div className="flex-1 flex items-center justify-center p-8 text-xs text-slate-400">
            Chọn một cuộc hội thoại từ danh sách bên trái để bắt đầu nhắn tin.
          </div>
        )}
      </div>
    </div>
  );
}
