"use client";

import React, { useState, useRef, useEffect, useMemo } from "react";
import { Search, ChevronDown, Check, X } from "lucide-react";
import { cn } from "@/lib/utils";

export interface SearchableSelectOption {
  value: string;
  label: string;
  subLabel?: string;
}

interface SearchableSelectProps {
  value: string;
  onChange: (value: string) => void;
  options: SearchableSelectOption[];
  placeholder?: string;
  searchPlaceholder?: string;
  disabled?: boolean;
  loading?: boolean;
  required?: boolean;
  className?: string;
  emptyText?: string;
}

// Hàm chuẩn hóa chuỗi tiếng Việt để tìm kiếm không dấu
function normalizeText(text: string): string {
  return text
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[đĐ]/g, "d")
    .trim();
}

export const SearchableSelect: React.FC<SearchableSelectProps> = ({
  value,
  onChange,
  options,
  placeholder = "Chọn phường / xã...",
  searchPlaceholder = "Tìm nhanh phường / xã...",
  disabled = false,
  loading = false,
  required = false,
  className = "",
  emptyText = "Không tìm thấy phường/xã phù hợp",
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Tìm option đang được chọn
  const selectedOption = useMemo(
    () => options.find((opt) => opt.value === value),
    [options, value]
  );

  // Lọc options theo từ khóa tìm kiếm
  const filteredOptions = useMemo(() => {
    if (!searchTerm.trim()) return options;
    const normSearch = normalizeText(searchTerm);
    return options.filter((opt) => {
      const normLabel = normalizeText(opt.label);
      const normVal = normalizeText(opt.value);
      const normSub = opt.subLabel ? normalizeText(opt.subLabel) : "";
      return (
        normLabel.includes(normSearch) ||
        normVal.includes(normSearch) ||
        normSub.includes(normSearch)
      );
    });
  }, [options, searchTerm]);

  // Đóng dropdown khi click ra ngoài
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
        setSearchTerm("");
      }
    };

    if (isOpen) {
      document.addEventListener("mousedown", handleClickOutside);
    }
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, [isOpen]);

  const handleOpen = () => {
    if (disabled || loading) return;
    setIsOpen(true);
    setTimeout(() => {
      inputRef.current?.focus();
    }, 50);
  };

  const handleSelect = (val: string) => {
    onChange(val);
    setIsOpen(false);
    setSearchTerm("");
  };

  const handleClear = (e: React.MouseEvent) => {
    e.stopPropagation();
    setSearchTerm("");
    inputRef.current?.focus();
  };

  return (
    <div ref={containerRef} className={cn("relative w-full", className)}>
      {/* Hidden input to ensure HTML5 form validation works if required */}
      {required && (
        <input
          type="text"
          value={value}
          onChange={() => {}}
          required={required}
          className="sr-only"
          tabIndex={-1}
          aria-hidden="true"
        />
      )}

      {/* Main Single Combobox Box */}
      <div
        onClick={handleOpen}
        className={cn(
          "w-full flex items-center justify-between gap-2 px-3 py-2 text-xs rounded-xl border bg-white transition-all cursor-pointer select-none",
          isOpen
            ? "border-blue-500 ring-2 ring-blue-500/20 shadow-xs"
            : "border-slate-200 hover:border-slate-300",
          disabled && "opacity-60 cursor-not-allowed bg-slate-50",
          loading && "cursor-wait"
        )}
      >
        <div className="flex items-center gap-2 flex-1 min-w-0">
          <Search className="w-3.5 h-3.5 text-slate-400 shrink-0" />
          {isOpen ? (
            <input
              ref={inputRef}
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Escape") {
                  setIsOpen(false);
                  setSearchTerm("");
                } else if (e.key === "Enter" && filteredOptions.length > 0) {
                  e.preventDefault();
                  handleSelect(filteredOptions[0].value);
                }
              }}
              placeholder={selectedOption ? selectedOption.label : searchPlaceholder}
              className="w-full text-xs font-semibold text-slate-800 placeholder:text-slate-400 outline-none bg-transparent"
              onClick={(e) => e.stopPropagation()}
            />
          ) : (
            <span
              className={cn(
                "truncate text-xs font-semibold",
                selectedOption ? "text-slate-900" : "text-slate-400"
              )}
            >
              {selectedOption ? selectedOption.label : placeholder}
            </span>
          )}
        </div>

        <div className="flex items-center gap-1 shrink-0">
          {isOpen && searchTerm && (
            <button
              type="button"
              onClick={handleClear}
              className="p-1 rounded-md text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-colors"
              title="Xóa tìm kiếm"
            >
              <X className="w-3 h-3" />
            </button>
          )}
          <ChevronDown
            className={cn(
              "w-4 h-4 text-slate-400 transition-transform duration-200",
              isOpen && "rotate-180 text-blue-600"
            )}
          />
        </div>
      </div>

      {/* Dropdown Options Popup */}
      {isOpen && (
        <div className="absolute top-full left-0 right-0 mt-1.5 z-50 bg-white border border-slate-200/90 rounded-xl shadow-xl overflow-hidden animate-in fade-in zoom-in-95 duration-150">
          <div className="max-h-56 overflow-y-auto divide-y divide-slate-50 p-1">
            {loading ? (
              <div className="px-3 py-3 text-center text-xs text-slate-400">
                Đang tải dữ liệu phường/xã...
              </div>
            ) : filteredOptions.length > 0 ? (
              filteredOptions.map((opt) => {
                const isSelected = opt.value === value;
                return (
                  <div
                    key={opt.value}
                    onClick={() => handleSelect(opt.value)}
                    className={cn(
                      "flex items-center justify-between px-3 py-2 text-xs rounded-lg cursor-pointer transition-colors",
                      isSelected
                        ? "bg-blue-50/80 font-bold text-blue-600"
                        : "text-slate-700 hover:bg-slate-50 font-medium"
                    )}
                  >
                    <div className="flex-1 min-w-0 pr-2">
                      <div className="truncate">{opt.label}</div>
                      {opt.subLabel && (
                        <div className="text-[10px] text-slate-400 font-normal">
                          {opt.subLabel}
                        </div>
                      )}
                    </div>
                    {isSelected && (
                      <Check className="w-3.5 h-3.5 text-blue-600 shrink-0 ml-1.5" />
                    )}
                  </div>
                );
              })
            ) : (
              <div className="px-3 py-4 text-center text-xs text-slate-400">
                {emptyText}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
