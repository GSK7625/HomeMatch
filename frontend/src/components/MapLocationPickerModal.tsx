"use client";

import React, { useEffect, useRef, useState } from "react";
import type L from "leaflet";
import "leaflet/dist/leaflet.css";
import { Modal } from "@/components/Modal";
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

// Default center coordinate (Hanoi centre)
const DEFAULT_CENTER: [number, number] = [21.0285, 105.8542];

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
