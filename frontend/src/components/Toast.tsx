"use client";

import React, { createContext, useContext, useState, useCallback } from "react";
import { CheckCircle2, AlertCircle, Info, X } from "lucide-react";
import { cn } from "@/lib/utils";

type ToastType = "success" | "error" | "info";

interface Toast {
  id: string;
  message: string;
  type: ToastType;
}

interface ToastContextType {
  showToast: (message: string, type?: ToastType) => void;
}

const ToastContext = createContext<ToastContextType | undefined>(undefined);

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const showToast = useCallback((message: string, type: ToastType = "success") => {
    const id = `toast-${Date.now()}-${Math.random()}`;
    setToasts((prev) => [...prev, { id, message, type }]);

    setTimeout(() => {
      setToasts((prev) => prev.filter((t) => t.id !== id));
    }, 4000);
  }, []);

  const removeToast = (id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  };

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div className="fixed bottom-20 lg:bottom-6 left-4 right-4 sm:left-auto sm:right-6 z-[110] flex flex-col gap-2 max-w-sm sm:w-full pointer-events-none">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            className={cn(
              "pointer-events-auto flex items-center justify-between gap-3 px-4 py-3.5 rounded-2xl shadow-xl border text-sm transition-all transform animate-in fade-in slide-in-from-bottom-3 duration-200",
              toast.type === "success" && "bg-white border-emerald-200 text-emerald-950 shadow-emerald-900/10",
              toast.type === "error" && "bg-white border-rose-200 text-rose-950 shadow-rose-900/10",
              toast.type === "info" && "bg-white border-sky-200 text-sky-950 shadow-sky-900/10"
            )}
          >
            {toast.type === "success" && <CheckCircle2 className="w-5 h-5 text-emerald-600 shrink-0" />}
            {toast.type === "error" && <AlertCircle className="w-5 h-5 text-rose-600 shrink-0" />}
            {toast.type === "info" && <Info className="w-5 h-5 text-sky-600 shrink-0" />}
            <p className="flex-1 font-semibold text-slate-800 leading-snug">{toast.message}</p>
            <button
              onClick={() => removeToast(toast.id)}
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
