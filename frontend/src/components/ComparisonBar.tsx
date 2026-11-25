"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { ArrowRight, Scale, X } from "lucide-react";
import { useAuth } from "@/features/auth/AuthContext";
import { compareSelection } from "@/lib/compare-selection";

export function ComparisonBar() {
  const { user, isLoading } = useAuth();
  const pathname = usePathname();
  const [ids, setIds] = useState<string[]>([]);

  useEffect(() => {
    const sync = () => setIds(compareSelection.getIds());
    sync();
    window.addEventListener("homematch:compare-updated", sync);
    return () => window.removeEventListener("homematch:compare-updated", sync);
  }, []);

  const showOnPage = pathname === "/" || pathname === "/listings"
    || pathname?.startsWith("/listings/") || pathname === "/tenant/favorites";
  if (isLoading || !user || !showOnPage || ids.length === 0) return null;

  return (
    <div className="fixed bottom-20 lg:bottom-6 inset-x-3 sm:inset-x-auto sm:left-1/2 sm:-translate-x-1/2 z-40 bg-slate-900/95 backdrop-blur-md text-white px-4 py-3 rounded-2xl shadow-2xl border border-slate-700 flex items-center justify-center gap-3 print:hidden">
      <div className="flex items-center gap-2">
        <Scale className="w-5 h-5 text-blue-400 shrink-0" />
        <span className="text-xs font-bold whitespace-nowrap">
          Đang chọn <span className="text-blue-300 font-black">{ids.length}/3</span> phòng
        </span>
      </div>
      <Link href="/compare" className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold whitespace-nowrap">
        So sánh ngay
        <ArrowRight className="w-3.5 h-3.5" />
      </Link>
      <button onClick={() => compareSelection.clear()} className="text-slate-400 hover:text-white p-1 rounded" title="Bỏ chọn so sánh">
        <X className="w-4 h-4" />
      </button>
    </div>
  );
}
