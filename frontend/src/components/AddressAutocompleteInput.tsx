"use client";

import React, { useState, useEffect, useRef, useCallback } from "react";
import { MapPin, RefreshCw, X, Check } from "lucide-react";
import { cn } from "@/lib/utils";

interface SuggestionItem {
  place_id: number | string;
  display_name: string;
  lat: string;
  lon: string;
  address?: {
    road?: string;
    house_number?: string;
    suburb?: string;
    quarter?: string;
    neighbourhood?: string;
    city?: string;
    state?: string;
  };
}

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
  onSelectCoords,
  provinceName,
  wardName,
  placeholder = "VD: Số 18 Ngõ 68 Cầu Giấy",
  required = false,
  className = "",
}) => {
  const [suggestions, setSuggestions] = useState<SuggestionItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const debounceTimerRef = useRef<NodeJS.Timeout | null>(null);

  // Close dropdown on click outside
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // Fetch geocoding suggestions
  const fetchSuggestions = useCallback(
    async (text: string) => {
      if (!text || text.trim().length < 2) {
        setSuggestions([]);
        setIsOpen(false);
        return;
      }

      setIsLoading(true);
      try {
        // Tối ưu từ khóa tìm kiếm kèm theo ngữ cảnh Tỉnh / Phường đã chọn
        const contextParts = [text.trim()];
        if (wardName && !text.toLowerCase().includes(wardName.toLowerCase())) {
          contextParts.push(wardName);
        }
        if (provinceName && !text.toLowerCase().includes(provinceName.toLowerCase())) {
          contextParts.push(provinceName);
        }
        const query = contextParts.join(", ");

        const res = await fetch(`/api/geocode?q=${encodeURIComponent(query)}`);
        if (res.ok) {
          const data: SuggestionItem[] = await res.json();
          if (Array.isArray(data) && data.length > 0) {
            setSuggestions(data);
            setIsOpen(true);
          } else {
            // Nếu tìm kèm ngữ cảnh không ra, thử tìm chỉ theo từ khóa thô
            const fallbackRes = await fetch(`/api/geocode?q=${encodeURIComponent(text.trim())}`);
            if (fallbackRes.ok) {
              const fallbackData: SuggestionItem[] = await fallbackRes.json();
              if (Array.isArray(fallbackData) && fallbackData.length > 0) {
                setSuggestions(fallbackData);
                setIsOpen(true);
              } else {
                setSuggestions([]);
              }
            }
          }
        }
      } catch (err) {
        console.warn("Geocoding fetch error", err);
      } finally {
        setIsLoading(false);
      }
    },
    [provinceName, wardName]
  );

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    onChange(val);

    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }

    debounceTimerRef.current = setTimeout(() => {
      fetchSuggestions(val);
    }, 350);
  };

  const handleSelectSuggestion = (item: SuggestionItem) => {
    // Trích xuất địa chỉ ngắn gọn dễ đọc nhất (số nhà + tên đường)
    const addr = item.address;
    let shortAddress = "";
    if (addr?.house_number && addr?.road) {
      shortAddress = `Số ${addr.house_number} ${addr.road}`;
    } else if (addr?.road) {
      shortAddress = addr.road;
    } else {
      // Lấy 2 phần tử đầu của display_name
      shortAddress = item.display_name.split(",").slice(0, 2).join(",").trim();
    }

    onChange(shortAddress || item.display_name);

    const lat = parseFloat(item.lat);
    const lng = parseFloat(item.lon);
    if (!isNaN(lat) && !isNaN(lng) && onSelectCoords) {
      onSelectCoords(lat, lng);
    }

    setIsOpen(false);
    setSuggestions([]);
  };

  return (
    <div ref={containerRef} className={cn("relative w-full", className)}>
      {/* Main Address Input Box */}
      <div className="relative flex items-center">
        <MapPin className="w-4 h-4 text-blue-600 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
        <input
          ref={inputRef}
          type="text"
          required={required}
          value={value}
          onChange={handleInputChange}
          onFocus={() => {
            if (suggestions.length > 0) setIsOpen(true);
          }}
          placeholder={placeholder}
          className="w-full pl-9 pr-9 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 bg-white font-medium text-slate-900 transition-all shadow-xs"
        />

        {/* Status icon / Clear */}
        <div className="absolute right-2.5 top-1/2 -translate-y-1/2 flex items-center gap-1">
          {isLoading && <RefreshCw className="w-3.5 h-3.5 animate-spin text-slate-400" />}
          {value && !isLoading && (
            <button
              type="button"
              onClick={() => {
                onChange("");
                setSuggestions([]);
                setIsOpen(false);
                inputRef.current?.focus();
              }}
              className="p-1 rounded-md text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-colors"
              title="Xóa địa chỉ"
            >
              <X className="w-3 h-3" />
            </button>
          )}
        </div>
      </div>

      {/* Suggestions Popup */}
      {isOpen && suggestions.length > 0 && (
        <div className="absolute top-full left-0 right-0 mt-1 z-50 bg-white border border-slate-200/90 rounded-xl shadow-xl overflow-hidden divide-y divide-slate-50 animate-in fade-in zoom-in-95 duration-150">
          <div className="px-3 py-1.5 bg-slate-50 text-[10px] font-bold text-slate-500 uppercase tracking-wider flex items-center justify-between">
            <span>Địa chỉ gợi ý (Bấm để chọn & gán GPS)</span>
            <span>{suggestions.length} kết quả</span>
          </div>
          <div className="max-h-56 overflow-y-auto p-1">
            {suggestions.map((item) => (
              <div
                key={item.place_id}
                onClick={() => handleSelectSuggestion(item)}
                className="p-2 rounded-lg hover:bg-blue-50/80 cursor-pointer transition-colors group flex items-start gap-2"
              >
                <MapPin className="w-4 h-4 text-slate-400 group-hover:text-blue-600 shrink-0 mt-0.5 transition-colors" />
                <div className="flex-1 min-w-0">
                  <div className="text-xs font-bold text-slate-800 group-hover:text-blue-900 truncate">
                    {item.address?.house_number && item.address?.road
                      ? `Số ${item.address.house_number} ${item.address.road}`
                      : item.address?.road || item.display_name.split(",")[0]}
                  </div>
                  <div className="text-[11px] text-slate-500 line-clamp-1 leading-snug">
                    {item.display_name}
                  </div>
                </div>
                <div className="text-[10px] font-mono text-emerald-600 bg-emerald-50 px-1.5 py-0.5 rounded shrink-0">
                  GPS
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
