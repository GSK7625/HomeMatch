"use client";

import React, { createContext, useContext, useState, useCallback } from "react";
import { CheckCircle2, AlertCircle, Info, X } from "lucide-react";
import { cn } from "@/lib/utils";

type ToastType = "success" | "error" | "info";

export interface ShowToastOptions {
  onClick?: () => void;
  actionLabel?: string;
  durationMs?: number;
}

interface Toast {
  id: string;
  message: string;
  type: ToastType;
  onClick?: () => void;
  actionLabel?: string;
}

interface ToastContextType {
  showToast: (message: string, type?: ToastType, options?: ShowToastOptions) => void;
}

const ToastContext = createContext<ToastContextType | undefined>(undefined);

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const showToast = useCallback(
    (message: string, type: ToastType = "success", options?: ShowToastOptions) => {
      const id = `toast-${Date.now()}-${Math.random()}`;
      setToasts((prev) => [
        ...prev,
        {
          id,
          message,
          type,
          onClick: options?.onClick,
          actionLabel: options?.actionLabel,
        },
      ]);

      setTimeout(() => {
        setToasts((prev) => prev.filter((t) => t.id !== id));
      }, options?.durationMs ?? 4000);
    },
    []
  );

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div className="fixed bottom-20 lg:bottom-6 left-4 right-4 sm:left-auto sm:right-6 z-[110] flex flex-col gap-2 max-w-sm sm:w-full pointer-events-none">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            role={toast.onClick ? "button" : undefined}
            tabIndex={toast.onClick ? 0 : undefined}
            onClick={() => {
              if (toast.onClick) {
                toast.onClick();
                removeToast(toast.id);
              }
            }}
            onKeyDown={(e) => {
              if (toast.onClick && (e.key === "Enter" || e.key === " ")) {
                e.preventDefault();
                toast.onClick();
                removeToast(toast.id);
              }
            }}
            className={cn(
              "pointer-events-auto flex items-center justify-between gap-3 px-4 py-3.5 rounded-2xl shadow-xl border text-sm transition-all transform animate-in fade-in slide-in-from-bottom-3 duration-200",
              toast.onClick && "cursor-pointer hover:shadow-2xl hover:-translate-y-0.5",
              toast.type === "success" && "bg-white border-emerald-200 text-emerald-950 shadow-emerald-900/10",
              toast.type === "error" && "bg-white border-rose-200 text-rose-950 shadow-rose-900/10",
              toast.type === "info" && "bg-white border-sky-200 text-sky-950 shadow-sky-900/10"
            )}
          >
            {toast.type === "success" && <CheckCircle2 className="w-5 h-5 text-emerald-600 shrink-0" />}
            {toast.type === "error" && <AlertCircle className="w-5 h-5 text-rose-600 shrink-0" />}
            {toast.type === "info" && <Info className="w-5 h-5 text-sky-600 shrink-0" />}
            <div className="flex-1 min-w-0">
              <p className="font-semibold text-slate-800 leading-snug">{toast.message}</p>
              {toast.onClick && (
                <span className="inline-block mt-1 text-xs font-bold text-blue-600">
                  {toast.actionLabel || "Xem chi tiết →"}
                </span>
              )}
            </div>
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                removeToast(toast.id);
              }}
              className="text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition-colors shrink-0"
              title="Đóng thông báo"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error("useToast must be used within a ToastProvider");
  }
  return context;
}
