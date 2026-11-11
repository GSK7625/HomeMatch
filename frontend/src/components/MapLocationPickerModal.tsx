"use client";

import React, { useEffect, useRef, useState } from "react";
import type L from "leaflet";
import "leaflet/dist/leaflet.css";
import { Modal } from "@/components/Modal";
import {
  MapPin,
  Navigation,
  Search,
  Check,
  Info,
  Compass,
} from "lucide-react";

interface MapLocationPickerModalProps {
  isOpen: boolean;
  onClose: () => void;
  initialLatitude?: number | null;
  initialLongitude?: number | null;
  addressContext?: string;
  onConfirm: (coords: { latitude: number; longitude: number }) => void;
}

// Custom modern SVG pin icon for Leaflet
const createCustomPinIcon = (leaflet: typeof L) => {
  return leaflet.divIcon({
    className: "hm-leaflet-marker",
    html: `
      <div style="position: relative; width: 34px; height: 34px; transform: translate(-50%, -100%);">
        <div style="width: 34px; height: 34px; background: #2563eb; border: 2.5px solid #ffffff; border-radius: 50% 50% 50% 0; transform: rotate(-45deg); box-shadow: 0 4px 10px rgba(0,0,0,0.35); display: flex; align-items: center; justify-content: center;">
          <div style="width: 10px; height: 10px; background: #ffffff; border-radius: 50%; transform: rotate(45deg);"></div>
        </div>
        <div style="position: absolute; bottom: -4px; left: 50%; transform: translateX(-50%); width: 8px; height: 3px; background: rgba(0,0,0,0.25); border-radius: 50%; filter: blur(1px);"></div>
      </div>
    `,
    iconSize: [34, 34],
    iconAnchor: [17, 34],
  });
};

// Default fallback coordinate (Hanoi centre)
const DEFAULT_CENTER: [number, number] = [21.0285, 105.8542];

// Predefined center coordinates for major Vietnamese administrative regions
const VIETNAM_FALLBACK_COORDINATES: Array<{
  keywords: string[];
  lat: number;
  lng: number;
  name: string;
}> = [
  // Hà Nội
  { keywords: ["bach dang", "bạch đằng"], lat: 21.0188, lng: 105.8631, name: "Phường Bạch Đằng, Hà Nội" },
  { keywords: ["dich vong", "dịch vọng"], lat: 21.0333, lng: 105.7928, name: "Cầu Giấy, Hà Nội" },
  { keywords: ["cau giay", "cầu giấy"], lat: 21.0362, lng: 105.7906, name: "Quận Cầu Giấy, Hà Nội" },
  { keywords: ["dong da", "đống đa"], lat: 21.0181, lng: 105.8299, name: "Quận Đống Đa, Hà Nội" },
  { keywords: ["ba dinh", "ba đình"], lat: 21.0341, lng: 105.8244, name: "Quận Ba Đình, Hà Nội" },
  { keywords: ["hoan kiem", "hoàn kiếm"], lat: 21.0285, lng: 105.8542, name: "Quận Hoàn Kiếm, Hà Nội" },
  { keywords: ["hai ba trung", "hai bà trưng"], lat: 21.0069, lng: 105.8524, name: "Quận Hai Bà Trưng, Hà Nội" },
  { keywords: ["thanh xuan", "thanh xuân"], lat: 20.9937, lng: 105.8115, name: "Quận Thanh Xuân, Hà Nội" },
  { keywords: ["tay ho", "tây hồ"], lat: 21.0717, lng: 105.8239, name: "Quận Tây Hồ, Hà Nội" },
  { keywords: ["hoang mai", "hoàng mai"], lat: 20.9765, lng: 105.8584, name: "Quận Hoàng Mai, Hà Nội" },
  { keywords: ["long bien", "long biên"], lat: 21.0422, lng: 105.8924, name: "Quận Long Biên, Hà Nội" },
  { keywords: ["nam tu liem", "nam từ liêm"], lat: 21.0135, lng: 105.7645, name: "Quận Nam Từ Liêm, Hà Nội" },
  { keywords: ["bac tu liem", "bắc từ liêm"], lat: 21.0631, lng: 105.7588, name: "Quận Bắc Từ Liêm, Hà Nội" },
  { keywords: ["ha dong", "hà đông"], lat: 20.9723, lng: 105.7766, name: "Quận Hà Đông, Hà Nội" },
  { keywords: ["ha noi", "hà nội"], lat: 21.0285, lng: 105.8542, name: "Thành phố Hà Nội" },

  // TP Hồ Chí Minh
  { keywords: ["quan 1", "quận 1", "district 1"], lat: 10.7758, lng: 106.7004, name: "Quận 1, TP.HCM" },
  { keywords: ["quan 3", "quận 3", "district 3"], lat: 10.7844, lng: 106.6845, name: "Quận 3, TP.HCM" },
  { keywords: ["quan 5", "quận 5", "district 5"], lat: 10.7554, lng: 106.6669, name: "Quận 5, TP.HCM" },
  { keywords: ["quan 7", "quận 7", "district 7"], lat: 10.7340, lng: 106.7218, name: "Quận 7, TP.HCM" },
  { keywords: ["quan 10", "quận 10", "district 10"], lat: 10.7716, lng: 106.6675, name: "Quận 10, TP.HCM" },
  { keywords: ["binh thanh", "bình thạnh"], lat: 10.8106, lng: 106.7091, name: "Bình Thạnh, TP.HCM" },
  { keywords: ["go vap", "gò vấp"], lat: 10.8387, lng: 106.6653, name: "Gò Vấp, TP.HCM" },
  { keywords: ["tan binh", "tân bình"], lat: 10.8015, lng: 106.6526, name: "Tân Bình, TP.HCM" },
  { keywords: ["thu duc", "thủ đức"], lat: 10.8504, lng: 106.7533, name: "TP. Thủ Đức" },
  { keywords: ["ho chi minh", "hồ chí minh", "sai gon", "sài gòn"], lat: 10.8231, lng: 106.6297, name: "TP. Hồ Chí Minh" },

  // Đà Nẵng
  { keywords: ["hai chau", "hải châu"], lat: 16.0592, lng: 108.2208, name: "Hải Châu, Đà Nẵng" },
  { keywords: ["thanh khe", "thanh khê"], lat: 16.0617, lng: 108.1902, name: "Thanh Khê, Đà Nẵng" },
  { keywords: ["son tra", "sơn trà"], lat: 16.0848, lng: 108.2435, name: "Sơn Trà, Đà Nẵng" },
  { keywords: ["da nang", "đà nẵng"], lat: 16.0544, lng: 108.2022, name: "Đà Nẵng" },

  // Các tỉnh thành phố khác
  { keywords: ["hai phong", "hải phòng"], lat: 20.8449, lng: 106.6881, name: "Hải Phòng" },
  { keywords: ["can tho", "cần thơ"], lat: 10.0452, lng: 105.7469, name: "Cần Thơ" },
  { keywords: ["binh duong", "bình dương"], lat: 10.9805, lng: 106.6519, name: "Bình Dương" },
  { keywords: ["dong nai", "đồng nai", "bien hoa", "biên hòa"], lat: 10.9574, lng: 106.8427, name: "Biên Hòa, Đồng Nai" },
];

export const MapLocationPickerModal: React.FC<MapLocationPickerModalProps> = ({
  isOpen,
  onClose,
  initialLatitude,
  initialLongitude,
  addressContext,
  onConfirm,
}) => {
  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const markerRef = useRef<L.Marker | null>(null);

  const [lat, setLat] = useState<number>(() => initialLatitude ?? DEFAULT_CENTER[0]);
  const [lng, setLng] = useState<number>(() => initialLongitude ?? DEFAULT_CENTER[1]);
  const [searchQuery, setSearchQuery] = useState(addressContext || "");
  const [isSearching, setIsSearching] = useState(false);
  const [isLocating, setIsLocating] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [infoMessage, setInfoMessage] = useState<string | null>(null);

  // Initialize or update Leaflet map when modal is opened
  useEffect(() => {
    if (!isOpen) return;

    const startLat = initialLatitude ?? DEFAULT_CENTER[0];
    const startLng = initialLongitude ?? DEFAULT_CENTER[1];
    setLat(startLat);
    setLng(startLng);
    setErrorMessage(null);
    setInfoMessage(null);
    if (addressContext) {
      setSearchQuery(addressContext);
    }

    let isCancelled = false;
    let resizeObserver: ResizeObserver | null = null;
    let resizeTimer1: NodeJS.Timeout | null = null;
    let resizeTimer2: NodeJS.Timeout | null = null;

    const timer = setTimeout(async () => {
      if (!mapContainerRef.current) return;

      const leaflet = (await import("leaflet")).default;
      if (isCancelled || !mapContainerRef.current) return;

      // Clean up previous instance if any
      if (mapInstanceRef.current) {
        mapInstanceRef.current.remove();
        mapInstanceRef.current = null;
      }

      const map = leaflet.map(mapContainerRef.current, {
        zoomControl: true,
      }).setView([startLat, startLng], initialLatitude ? 16 : 13);

      // Google Maps standard raster tiles - ultra fast in Vietnam (<150ms), reliable, crisp Vietnamese labels
      leaflet.tileLayer("https://mt{s}.google.com/vt/lyrs=m&x={x}&y={y}&z={z}", {
        subdomains: ["0", "1", "2", "3"],
        maxZoom: 20,
        attribution: '&copy; <a href="https://maps.google.com" target="_blank" rel="noopener noreferrer">Google Maps</a>',
      }).addTo(map);

      // Add draggable pin marker
      const pin = leaflet.marker([startLat, startLng], {
        icon: createCustomPinIcon(leaflet),
        draggable: true,
      }).addTo(map);

      pin.on("dragend", () => {
        const pos = pin.getLatLng();
        setLat(Number(pos.lat.toFixed(6)));
        setLng(Number(pos.lng.toFixed(6)));
      });

      // Click on map to place/move marker
      map.on("click", (e: L.LeafletMouseEvent) => {
        const { lat: clickLat, lng: clickLng } = e.latlng;
        const roundedLat = Number(clickLat.toFixed(6));
        const roundedLng = Number(clickLng.toFixed(6));
        pin.setLatLng([roundedLat, roundedLng]);
        setLat(roundedLat);
        setLng(roundedLng);
      });

      mapInstanceRef.current = map;
      markerRef.current = pin;

      // Continuous size invalidation as modal completes its transition
      map.invalidateSize();
      resizeTimer1 = setTimeout(() => {
        if (!isCancelled && mapInstanceRef.current) {
          mapInstanceRef.current.invalidateSize();
        }
      }, 150);
      resizeTimer2 = setTimeout(() => {
        if (!isCancelled && mapInstanceRef.current) {
          mapInstanceRef.current.invalidateSize();
        }
      }, 400);

      // Observe container size adjustments
      if (typeof ResizeObserver !== "undefined" && mapContainerRef.current) {
        resizeObserver = new ResizeObserver(() => {
          if (!isCancelled && mapInstanceRef.current) {
            mapInstanceRef.current.invalidateSize();
          }
        });
        resizeObserver.observe(mapContainerRef.current);
      }
    }, 100);

    return () => {
      isCancelled = true;
      clearTimeout(timer);
      if (resizeTimer1) clearTimeout(resizeTimer1);
      if (resizeTimer2) clearTimeout(resizeTimer2);
      if (resizeObserver) {
        resizeObserver.disconnect();
      }
      if (mapInstanceRef.current) {
        mapInstanceRef.current.remove();
        mapInstanceRef.current = null;
      }
    };
  }, [isOpen, initialLatitude, initialLongitude, addressContext]);

  // Handle Geolocation (Get current device position)
  const handleGetCurrentLocation = () => {
    if (!navigator.geolocation) {
      setErrorMessage("Trình duyệt của bạn không hỗ trợ định vị GPS.");
      return;
    }

    setIsLocating(true);
    setErrorMessage(null);

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setIsLocating(false);
        const userLat = Number(pos.coords.latitude.toFixed(6));
        const userLng = Number(pos.coords.longitude.toFixed(6));
        setLat(userLat);
        setLng(userLng);

        if (mapInstanceRef.current && markerRef.current) {
          mapInstanceRef.current.setView([userLat, userLng], 17);
          markerRef.current.setLatLng([userLat, userLng]);
        }
      },
      (err) => {
        setIsLocating(false);
        setErrorMessage(
          err.code === 1
            ? "Bạn đã từ chối quyền truy cập vị trí trên trình duyệt."
            : "Không thể lấy vị trí hiện tại qua GPS. Vui lòng ghim tay trên bản đồ."
        );
      },
      { timeout: 10000, enableHighAccuracy: true }
    );
  };

  // Search location using internal Next.js geocoding proxy API with progressive fallback
  const handleSearchAddress = async (queryToSearch: string) => {
    const q = queryToSearch.trim();
    if (!q) return;

    setIsSearching(true);
    setErrorMessage(null);
    setInfoMessage(null);

    // Helper to call internal proxy route
    const fetchGeocode = async (text: string) => {
      try {
        const res = await fetch(`/api/geocode?q=${encodeURIComponent(text)}`);
        if (!res.ok) return null;
        const data = await res.json();
        return Array.isArray(data) && data.length > 0 ? data : null;
      } catch {
        return null;
      }
    };

    // Helper to find local fallback coordinates
    const findLocalFallback = (text: string) => {
      const lower = text.toLowerCase();
      return VIETNAM_FALLBACK_COORDINATES.find((item) =>
        item.keywords.some((kw) => lower.includes(kw))
      );
    };

    try {
      // 1. Try full search query first
      let results = await fetchGeocode(q);

      // 2. If no result and address has multiple segments (e.g. "Số 18 Ngõ 68 Cầu Giấy, Phường Bạch Đằng, Hà Nội"),
      // progressively strip the specific detail to search the ward / district
      if (!results && q.includes(",")) {
        const parts = q.split(",").map((s) => s.trim()).filter(Boolean);
        if (parts.length > 1) {
          const broaderQuery = parts.slice(1).join(", ");
          results = await fetchGeocode(broaderQuery);
        }
      }

      if (results && results.length > 0) {
        const found = results[0];
        const foundLat = Number(parseFloat(found.lat).toFixed(6));
        const foundLng = Number(parseFloat(found.lon).toFixed(6));
        setLat(foundLat);
        setLng(foundLng);

        if (mapInstanceRef.current && markerRef.current) {
          mapInstanceRef.current.setView([foundLat, foundLng], 16);
          markerRef.current.setLatLng([foundLat, foundLng]);
        }
        setInfoMessage("Đã định vị thành công. Bạn có thể kéo thả ghim để tinh chỉnh vị trí.");
        return;
      }

      // 3. Fallback to predefined local coordinate dictionary
      const fallback = findLocalFallback(q);
      if (fallback) {
        setLat(fallback.lat);
        setLng(fallback.lng);

        if (mapInstanceRef.current && markerRef.current) {
          mapInstanceRef.current.setView([fallback.lat, fallback.lng], 14);
          markerRef.current.setLatLng([fallback.lat, fallback.lng]);
        }
        setInfoMessage(`Đã định vị đến khu vực ${fallback.name}. Bạn có thể kéo thả ghim đến đúng số nhà cụ thể.`);
      } else {
        setErrorMessage(`Không tìm thấy vị trí tự động cho "${q}". Bạn có thể click trực tiếp trên bản đồ để ghim.`);
      }
    } catch {
      // 4. Offline / network error fallback
      const fallback = findLocalFallback(q);
      if (fallback) {
        setLat(fallback.lat);
        setLng(fallback.lng);

        if (mapInstanceRef.current && markerRef.current) {
          mapInstanceRef.current.setView([fallback.lat, fallback.lng], 14);
          markerRef.current.setLatLng([fallback.lat, fallback.lng]);
        }
        setInfoMessage(`Đã định vị đến khu vực ${fallback.name}. Bạn có thể kéo thả ghim đến đúng số nhà cụ thể.`);
      } else {
        setErrorMessage("Dịch vụ tìm kiếm tạm thời gián đoạn. Vui lòng click trực tiếp trên bản đồ để ghim.");
      }
    } finally {
      setIsSearching(false);
    }
  };

  const handleConfirm = () => {
    onConfirm({ latitude: lat, longitude: lng });
    onClose();
  };

  if (!isOpen) return null;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Ghim Địa Chỉ Tọa Độ GPS Nhà Trọ"
      maxWidth="lg"
    >
      <div className="space-y-3.5">
        {/* Quick Toolbar: Search & GPS Button */}
        <div className="flex flex-col sm:flex-row gap-2">
          <div className="relative flex-1">
            <Search className="w-3.5 h-3.5 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  handleSearchAddress(searchQuery);
                }
              }}
              placeholder="Nhập địa chỉ, tên đường hoặc địa danh để tìm..."
              className="w-full pl-8 pr-20 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
            />
            <button
              type="button"
              onClick={() => handleSearchAddress(searchQuery)}
              disabled={isSearching || !searchQuery.trim()}
              className="absolute right-1.5 top-1/2 -translate-y-1/2 px-2.5 py-1 text-[11px] font-bold rounded-lg bg-blue-50 text-blue-600 hover:bg-blue-100 disabled:opacity-50 transition-colors"
            >
              {isSearching ? "Đang tìm..." : "Tìm"}
            </button>
          </div>

          <button
            type="button"
            onClick={handleGetCurrentLocation}
            disabled={isLocating}
            className="flex items-center justify-center gap-1.5 px-3 py-2 text-xs font-bold rounded-xl border border-blue-200 bg-blue-50/80 text-blue-700 hover:bg-blue-100 transition-colors shrink-0"
          >
            <Navigation className={`w-3.5 h-3.5 ${isLocating ? "animate-spin text-blue-600" : ""}`} />
            <span>{isLocating ? "Đang dò GPS..." : "Vị trí hiện tại"}</span>
          </button>
        </div>

        {addressContext && (
          <div className="flex items-center justify-between text-[11px] bg-slate-50 border border-slate-200/80 rounded-xl px-3 py-1.5 text-slate-600">
            <span className="truncate mr-2">
              <strong>Địa chỉ đăng ký:</strong> {addressContext}
            </span>
            <button
              type="button"
              onClick={() => {
                setSearchQuery(addressContext);
                handleSearchAddress(addressContext);
              }}
              className="text-blue-600 hover:underline font-bold shrink-0 inline-flex items-center gap-1"
            >
              <Compass className="w-3 h-3" />
              <span>Định vị theo địa chỉ</span>
            </button>
          </div>
        )}

        {infoMessage && (
          <div className="flex items-center gap-2 p-2.5 bg-blue-50 text-blue-800 rounded-xl border border-blue-200 text-xs">
            <Check className="w-4 h-4 shrink-0 text-blue-600" />
            <span>{infoMessage}</span>
          </div>
        )}

        {errorMessage && (
          <div className="flex items-center gap-2 p-2.5 bg-amber-50 text-amber-800 rounded-xl border border-amber-200 text-xs">
            <Info className="w-4 h-4 shrink-0 text-amber-600" />
            <span>{errorMessage}</span>
          </div>
        )}

        {/* Map Container */}
        <div className="relative rounded-2xl overflow-hidden border border-slate-200/80 shadow-inner bg-slate-100">
          <div
            ref={mapContainerRef}
            className="w-full h-[320px] sm:h-[380px] z-0"
          />
          <div className="absolute top-2 left-2 z-[400] bg-white/95 backdrop-blur-xs px-2.5 py-1 rounded-lg border border-slate-200 text-[11px] font-medium text-slate-700 shadow-xs flex items-center gap-1.5">
            <MapPin className="w-3.5 h-3.5 text-blue-600" />
            <span>Click trên bản đồ hoặc kéo thả ghim màu xanh</span>
          </div>
        </div>

        {/* Coordinate details & inputs */}
        <div className="p-3 bg-slate-50 border border-slate-200/80 rounded-xl flex flex-col sm:flex-row items-center justify-between gap-3 text-xs">
          <div className="flex items-center gap-3 w-full sm:w-auto">
            <div>
              <span className="text-[10px] uppercase font-bold text-slate-400 block">Vĩ độ (Lat)</span>
              <input
                type="number"
                step="any"
                value={lat}
                onChange={(e) => {
                  const val = parseFloat(e.target.value);
                  if (!isNaN(val)) {
                    setLat(val);
                    if (markerRef.current && mapInstanceRef.current) {
                      markerRef.current.setLatLng([val, lng]);
                      mapInstanceRef.current.panTo([val, lng]);
                    }
                  }
                }}
                className="w-28 px-2 py-1 text-xs font-mono font-semibold rounded-lg border border-slate-200 bg-white"
              />
            </div>
            <div>
              <span className="text-[10px] uppercase font-bold text-slate-400 block">Kinh độ (Lng)</span>
              <input
                type="number"
                step="any"
                value={lng}
                onChange={(e) => {
                  const val = parseFloat(e.target.value);
                  if (!isNaN(val)) {
                    setLng(val);
                    if (markerRef.current && mapInstanceRef.current) {
                      markerRef.current.setLatLng([lat, val]);
                      mapInstanceRef.current.panTo([lat, val]);
                    }
                  }
                }}
                className="w-28 px-2 py-1 text-xs font-mono font-semibold rounded-lg border border-slate-200 bg-white"
              />
            </div>
          </div>

          <div className="flex gap-2 w-full sm:w-auto justify-end">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-xs font-bold text-slate-600 hover:bg-slate-200/60 rounded-xl border border-slate-200 transition-colors"
            >
              Hủy
            </button>
            <button
              type="button"
              onClick={handleConfirm}
              className="px-5 py-2 text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 rounded-xl shadow-md shadow-blue-500/20 inline-flex items-center gap-1.5 transition-all"
            >
              <Check className="w-3.5 h-3.5" />
              <span>Xác nhận vị trí GPS</span>
            </button>
          </div>
        </div>
      </div>
    </Modal>
  );
};
