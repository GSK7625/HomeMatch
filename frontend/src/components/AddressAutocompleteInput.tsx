"use client";

import React, { useRef } from "react";
import { MapPin, X } from "lucide-react";
import { cn } from "@/lib/utils";

interface AddressAutocompleteInputProps {
  value: string;
  onChange: (value: string) => void;
  onSelectCoords?: (lat: number, lng: number) => void;
  provinceName?: string;
  wardName?: string;
  placeholder?: string;
  required?: boolean;
  className?: string;
}

export const AddressAutocompleteInput: React.FC<AddressAutocompleteInputProps> = ({
  value,
  onChange,
  placeholder = "VD: Số 18 Ngõ 68 Cầu Giấy",
  required = false,
  className = "",
}) => {
  const inputRef = useRef<HTMLInputElement>(null);

  return (
    <div className={cn("relative w-full", className)}>
      <div className="relative flex items-center">
        <MapPin className="w-4 h-4 text-blue-600 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
        <input
          ref={inputRef}
          type="text"
          required={required}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          className="w-full pl-9 pr-9 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 bg-white font-medium text-slate-900 transition-all shadow-xs"
        />

        {value && (
          <div className="absolute right-2.5 top-1/2 -translate-y-1/2 flex items-center gap-1">
            <button
              type="button"
              onClick={() => {
                onChange("");
                inputRef.current?.focus();
              }}
              className="p-1 rounded-md text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-colors"
              title="Xóa địa chỉ"
            >
              <X className="w-3 h-3" />
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
