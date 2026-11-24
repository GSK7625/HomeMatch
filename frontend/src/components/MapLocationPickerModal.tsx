"use client";

import React, { useEffect, useRef, useState } from "react";
import type L from "leaflet";
import "leaflet/dist/leaflet.css";
import { Modal } from "@/components/Modal";
import { propertiesApi } from "@/lib/api-client";
import type { GeocodingResult } from "@/types";
import {
  MapPin,
  Navigation,
  Check,
  Info,
} from "lucide-react";

interface MapLocationPickerModalProps {
  isOpen: boolean;
  onClose: () => void;
  initialLatitude?: number | null;
  initialLongitude?: number | null;
  addressContext?: string;
  addressQuery?: { addressDetail: string; provinceCode: string; wardCode: string };
  onConfirm: (coords: { latitude: number; longitude: number; address?: GeocodingResult }) => void;
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

// Default center coordinate (Hanoi centre)
const DEFAULT_CENTER: [number, number] = [21.0285, 105.8542];

export const MapLocationPickerModal: React.FC<MapLocationPickerModalProps> = ({
  isOpen,
  onClose,
  initialLatitude,
  initialLongitude,
  addressContext,
  addressQuery,
  onConfirm,
}) => {
  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const markerRef = useRef<L.Marker | null>(null);

  const [lat, setLat] = useState<number>(() => initialLatitude ?? DEFAULT_CENTER[0]);
  const [lng, setLng] = useState<number>(() => initialLongitude ?? DEFAULT_CENTER[1]);
  const [isLocating, setIsLocating] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [infoMessage, setInfoMessage] = useState<string | null>(null);
  const [isSearching, setIsSearching] = useState(false);
  const [results, setResults] = useState<GeocodingResult[]>([]);
  const [selectedAddress, setSelectedAddress] = useState<GeocodingResult | null>(null);
  const [applyAddress, setApplyAddress] = useState(false);
  const [confirmed, setConfirmed] = useState(false);
  const requestRef = useRef<AbortController | null>(null);
  const revisionRef = useRef(0);
  const activeRef = useRef(false);

  const invalidateLookup = () => {
    revisionRef.current += 1;
    requestRef.current?.abort();
    setIsSearching(false);
    setIsLocating(false);
    setSelectedAddress(null);
    setResults([]);
    setApplyAddress(false);
    setConfirmed(false);
    setInfoMessage(null);
  };

  const selectResult = (result: GeocodingResult) => {
    setLat(result.latitude);
    setLng(result.longitude);
    markerRef.current?.setLatLng([result.latitude, result.longitude]);
    mapInstanceRef.current?.setView([result.latitude, result.longitude], 17);
    setSelectedAddress(result);
    setApplyAddress(false);
    setConfirmed(false);
    setInfoMessage("Đây là vị trí gợi ý. Hãy kiểm tra ghim và địa chỉ trước khi xác nhận.");
  };

  const lookup = async (reverse: boolean) => {
    invalidateLookup();
    const revision = revisionRef.current;
    const controller = new AbortController();
    requestRef.current = controller;
    setIsSearching(true);
    setErrorMessage(null);
    try {
      if (reverse) {
        const response = await propertiesApi.reverseGeocode(lat, lng, controller.signal);
        if (revision !== revisionRef.current || !activeRef.current) return;
        if (!response.isSuccess || !response.data) {
          setErrorMessage(response.error?.message || "Không tìm được địa chỉ. Bạn có thể nhập tay và xác nhận ghim.");
          return;
        }
        setSelectedAddress(response.data);
        setApplyAddress(true);
      } else if (addressQuery) {
        const response = await propertiesApi.geocode(addressQuery.addressDetail, addressQuery.provinceCode, addressQuery.wardCode, controller.signal);
        if (revision !== revisionRef.current || !activeRef.current) return;
        if (!response.isSuccess || !response.data?.length) {
          setErrorMessage(response.error?.message || "Không tìm được vị trí. Vui lòng chọn ghim thủ công.");
          return;
        }
        setResults(response.data);
      }
    } catch {
      if (revision === revisionRef.current && activeRef.current) setErrorMessage("Không thể tra cứu. Vui lòng thử lại hoặc ghim thủ công.");
    } finally {
      if (revision === revisionRef.current && activeRef.current) setIsSearching(false);
    }
  };

  // Initialize or update Leaflet map when modal is opened
  useEffect(() => {
    if (!isOpen) return;
    activeRef.current = true;
    invalidateLookup();

    const startLat = initialLatitude ?? DEFAULT_CENTER[0];
    const startLng = initialLongitude ?? DEFAULT_CENTER[1];
    setLat(startLat);
    setLng(startLng);
    setErrorMessage(null);
    setInfoMessage(null);

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
      }).setView([startLat, startLng], initialLatitude != null ? 16 : 13);

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
        invalidateLookup();
        const pos = pin.getLatLng();
        setLat(Number(pos.lat.toFixed(6)));
        setLng(Number(pos.lng.toFixed(6)));
      });

      // Click on map to place/move marker
      map.on("click", (e: L.LeafletMouseEvent) => {
        invalidateLookup();
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
      activeRef.current = false;
      revisionRef.current += 1;
      requestRef.current?.abort();
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

    invalidateLookup();
    setIsLocating(true);
    const revision = revisionRef.current;
    setErrorMessage(null);

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        if (!activeRef.current || revision !== revisionRef.current) return;
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
        if (!activeRef.current || revision !== revisionRef.current) return;
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

  const handleConfirm = () => {
    if (!confirmed || !Number.isFinite(lat) || !Number.isFinite(lng) || lat < -90 || lat > 90 || lng < -180 || lng > 180) return;
    onConfirm({ latitude: lat, longitude: lng, address: applyAddress && selectedAddress ? selectedAddress : undefined });
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
        {/* Quick Toolbar: GPS Button */}
        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="text-xs text-slate-600">
            Click trực tiếp trên bản đồ hoặc kéo thả ghim để chọn tọa độ chính xác.
          </span>
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
          <div className="text-[11px] bg-slate-50 border border-slate-200/80 rounded-xl px-3 py-1.5 text-slate-600 truncate">
            <strong>Địa chỉ đăng ký:</strong> {addressContext}
          </div>
        )}

        {infoMessage && (
          <div className="flex items-center gap-2 p-2.5 bg-blue-50 text-blue-800 rounded-xl border border-blue-200 text-xs">
            <Check className="w-4 h-4 shrink-0 text-blue-600" />
            <span>{infoMessage}</span>
          </div>
        )}

        <div className="flex flex-wrap gap-2 text-xs">
          <button type="button" onClick={() => lookup(false)}
            disabled={isSearching || isLocating || !addressQuery?.addressDetail.trim() || !addressQuery.provinceCode || !addressQuery.wardCode}
            className="px-3 py-2 rounded-lg border border-blue-200 text-blue-700 disabled:opacity-50">
            Tìm địa chỉ trên bản đồ
          </button>
          <button type="button" onClick={() => lookup(true)} disabled={isSearching || isLocating}
            className="px-3 py-2 rounded-lg border border-blue-200 text-blue-700 disabled:opacity-50">
            Lấy địa chỉ tại ghim
          </button>
          {isSearching && <span role="status" className="py-2">Đang tra cứu...</span>}
        </div>
        {results.length > 0 && <ul className="max-h-36 overflow-auto space-y-1" aria-label="Kết quả tìm vị trí">
          {results.map((result, index) => <li key={index}>
            <button type="button" onClick={() => selectResult(result)} className="w-full text-left text-xs border rounded-lg p-2 hover:bg-blue-50">
              {result.formattedAddress}
              <span className="block text-amber-700">{result.precision === "AREA" ? "Chỉ tìm được khu vực chung" : result.precision === "STREET" ? "Vị trí theo đường, cần kiểm tra số nhà" : "Vị trí gợi ý theo tòa nhà"}
                {result.matchStatus !== "MATCHED" && " · Chưa đối chiếu đủ tỉnh/phường"}</span>
            </button>
          </li>)}
        </ul>}
        {selectedAddress && <div className="text-xs p-3 border rounded-xl space-y-2" aria-live="polite">
          <p><strong>Địa chỉ gợi ý:</strong> {selectedAddress.formattedAddress}</p>
          <p className="text-amber-700">Kiểm tra địa chỉ gần ghim và bổ sung số nhà/ngõ nếu thiếu. Tỉnh/phường được giữ theo lựa chọn trong form.</p>
          <label className="flex gap-2 items-start"><input type="checkbox" checked={applyAddress} onChange={e => { setApplyAddress(e.target.checked); setConfirmed(false); }} />
            Điền địa chỉ gợi ý vào form (kiểm tra và bổ sung trước khi lưu)
          </label>
        </div>}
        <p className="text-[10px] text-slate-500">Tra cứu địa chỉ: <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener noreferrer" className="underline">© OpenStreetMap contributors</a></p>

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

        <label className="flex gap-2 text-xs items-start"><input type="checkbox" checked={confirmed} onChange={e => setConfirmed(e.target.checked)} />
          Tôi đã kiểm tra ghim và địa chỉ của tòa nhà.
        </label>
        {/* Coordinate details & inputs */}
        <div className="p-3 bg-slate-50 border border-slate-200/80 rounded-xl flex flex-col sm:flex-row items-center justify-between gap-3 text-xs">
          <div className="flex items-center gap-3 w-full sm:w-auto">
            <div>
              <span className="text-[10px] uppercase font-bold text-slate-400 block">Vĩ độ (Lat)</span>
              <input
                type="number"
                aria-label="Vĩ độ"
                min={-90}
                max={90}
                step="any"
                value={lat}
                onChange={(e) => {
                  const val = parseFloat(e.target.value);
                  if (Number.isFinite(val) && val >= -90 && val <= 90) {
                    invalidateLookup();
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
                aria-label="Kinh độ"
                min={-180}
                max={180}
                step="any"
                value={lng}
                onChange={(e) => {
                  const val = parseFloat(e.target.value);
                  if (Number.isFinite(val) && val >= -180 && val <= 180) {
                    invalidateLookup();
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
              disabled={!confirmed || isSearching || isLocating}
              className="px-5 py-2 text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-50 rounded-xl shadow-md shadow-blue-500/20 inline-flex items-center gap-1.5 transition-all"
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
