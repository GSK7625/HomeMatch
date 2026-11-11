"use client";

import React, { useEffect, useRef, useState, useCallback } from "react";
import type L from "leaflet";
import "leaflet/dist/leaflet.css";
import { PropertyListing } from "@/types";
import {
  MapPin,
  Navigation,
  Sparkles,
  Plus,
  Minus,
  Maximize2,
  Minimize2,
  RotateCcw,
  X,
} from "lucide-react";

interface ListingsInteractiveMapProps {
  listings: PropertyListing[];
  userLocation?: { lat: number; lng: number } | null;
  onLocateMe?: () => void;
}

// Fallback coordinate mapping for Vietnam districts
const DISTRICT_COORDINATES: Record<string, [number, number]> = {
  "cầu giấy": [21.0333, 105.7928],
  "đống đa": [21.0181, 105.8299],
  "hai bà trưng": [21.0069, 105.8524],
  "ba đình": [21.0341, 105.8244],
  "hoàn kiếm": [21.0285, 105.8542],
  "thanh xuân": [20.9937, 105.8115],
  "tây hồ": [21.0717, 105.8239],
  "hoàng mai": [20.9765, 105.8584],
  "nam từ liêm": [21.0135, 105.7645],
  "bắc từ liêm": [21.0631, 105.7588],
  "hà đông": [20.9723, 105.7766],
  "long biên": [21.0422, 105.8924],
  "quận 1": [10.7758, 106.7004],
  "quận 3": [10.7844, 106.6845],
  "quận 7": [10.7340, 106.7218],
  "bình thạnh": [10.8106, 106.7091],
  "gò vấp": [10.8387, 106.6653],
  "tân bình": [10.8015, 106.6526],
  "thủ đức": [10.8504, 106.7533],
  "hải châu": [16.0592, 108.2208],
  "thanh khê": [16.0617, 108.1902],
  "sơn trà": [16.0848, 108.2435],
  "hà nội": [21.0285, 105.8542],
  "hồ chí minh": [10.8231, 106.6297],
  "đà nẵng": [16.0544, 108.2022],
};

function getListingCoordinates(listing: PropertyListing): [number, number] {
  if (
    typeof listing.latitude === "number" &&
    typeof listing.longitude === "number" &&
    listing.latitude !== 0 &&
    listing.longitude !== 0 &&
    !isNaN(listing.latitude) &&
    !isNaN(listing.longitude)
  ) {
    return [listing.latitude, listing.longitude];
  }

  const query = `${listing.address || ""} ${listing.ward || ""} ${listing.district || ""} ${listing.city || ""}`.toLowerCase();
  for (const [key, coords] of Object.entries(DISTRICT_COORDINATES)) {
    if (query.includes(key)) {
      const idNum = Number(listing.id) || 1;
      const offsetLat = ((idNum % 7) - 3) * 0.0025;
      const offsetLng = (((idNum * 3) % 7) - 3) * 0.0025;
      return [coords[0] + offsetLat, coords[1] + offsetLng];
    }
  }

  const defaultId = Number(listing.id) || 1;
  return [
    21.0285 + ((defaultId % 5) - 2) * 0.003,
    105.8542 + (((defaultId * 2) % 5) - 2) * 0.003,
  ];
}

function escapeHtml(str: string): string {
  return str
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}

export const ListingsInteractiveMap: React.FC<ListingsInteractiveMapProps> = ({
  listings,
  userLocation,
  onLocateMe,
}) => {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<L.Map | null>(null);
  const leafletRef = useRef<typeof L | null>(null);
  const markersLayerRef = useRef<L.LayerGroup | null>(null);
  const lastBoundsRef = useRef<L.LatLngBounds | null>(null);

  const [isMapReady, setIsMapReady] = useState(false);
  const [activeListingCount, setActiveListingCount] = useState(0);
  const [isFullscreen, setIsFullscreen] = useState(false);

  // Hook 1: Initialize map ONCE on component mount
  useEffect(() => {
    if (!containerRef.current) return;
    let isCancelled = false;
    let resizeObserver: ResizeObserver | null = null;
    let timer1: NodeJS.Timeout | null = null;
    let timer2: NodeJS.Timeout | null = null;
    let timer3: NodeJS.Timeout | null = null;

    const setupMap = async () => {
      const LModule = (await import("leaflet")).default;
      if (isCancelled || !containerRef.current) return;

      // Clean up existing leaflet ID if container was recycled
      if ((containerRef.current as any)._leaflet_id) {
        delete (containerRef.current as any)._leaflet_id;
      }

      leafletRef.current = LModule;

      const map = LModule
        .map(containerRef.current, {
          zoomControl: false, // Sleek custom controls to prevent layout clashes
          scrollWheelZoom: true,
          fadeAnimation: false, // Turn off to eliminate _leaflet_pos race condition
          zoomAnimation: false, // Turn off animated transitions to prevent unmount clashes
        })
        .setView([21.0285, 105.8542], 13);

      // Google Maps standard raster tiles - ultra fast in Vietnam (<100ms), no ERR_CONNECTION_REFUSED
      LModule
        .tileLayer("https://mt{s}.google.com/vt/lyrs=m&x={x}&y={y}&z={z}", {
          subdomains: ["0", "1", "2", "3"],
          maxZoom: 20,
          attribution:
            '&copy; <a href="https://maps.google.com" target="_blank" rel="noopener noreferrer">Google Maps</a>',
        })
        .addTo(map);

      const markersGroup = LModule.layerGroup().addTo(map);
      markersLayerRef.current = markersGroup;
      mapRef.current = map;

      // Multi-stage size invalidation to handle mobile reflow & container mount
      const triggerSizeAndFit = () => {
        if (!isCancelled && mapRef.current) {
          mapRef.current.invalidateSize({ pan: false });
          if (lastBoundsRef.current && lastBoundsRef.current.isValid()) {
            const isMobile = typeof window !== "undefined" && window.innerWidth < 640;
            mapRef.current.fitBounds(lastBoundsRef.current, {
              padding: isMobile ? [20, 20] : [40, 40],
              maxZoom: 16,
              animate: false,
            });
          }
        }
      };

      if (typeof ResizeObserver !== "undefined" && containerRef.current) {
        resizeObserver = new ResizeObserver(() => {
          triggerSizeAndFit();
        });
        resizeObserver.observe(containerRef.current);
      }

      timer1 = setTimeout(triggerSizeAndFit, 100);
      timer2 = setTimeout(triggerSizeAndFit, 350);
      timer3 = setTimeout(triggerSizeAndFit, 700);

      setIsMapReady(true);
    };

    setupMap();

    return () => {
      isCancelled = true;
      if (timer1) clearTimeout(timer1);
      if (timer2) clearTimeout(timer2);
      if (timer3) clearTimeout(timer3);
      if (resizeObserver) resizeObserver.disconnect();
      if (mapRef.current) {
        try {
          mapRef.current.stop();
          mapRef.current.off();
          mapRef.current.remove();
        } catch {
          // Ignore any unmount race condition
        }
        mapRef.current = null;
      }
      setIsMapReady(false);
    };
  }, []);

  // Hook 2: Update markers safely on existing map without recreating L.Map
  useEffect(() => {
    const map = mapRef.current;
    const LModule = leafletRef.current;
    const markersGroup = markersLayerRef.current;

    if (!isMapReady || !map || !LModule || !markersGroup) return;

    try {
      map.stop();
      markersGroup.clearLayers();
      const bounds = LModule.latLngBounds([]);

      // 1. Add user location marker if present
      if (userLocation) {
        const userIcon = LModule.divIcon({
          className: "hm-user-gps-marker-wrapper",
          html: `
            <div class="hm-user-gps-marker">
              <span class="hm-user-gps-ping"></span>
              <span class="hm-user-gps-dot"></span>
            </div>
          `,
          iconSize: [24, 24],
          iconAnchor: [12, 12],
        });

        LModule
          .marker([userLocation.lat, userLocation.lng], { icon: userIcon })
          .bindTooltip("Vị trí của bạn", { permanent: false, direction: "top" })
          .addTo(markersGroup);

        bounds.extend([userLocation.lat, userLocation.lng]);
      }

      // 2. Add listing price markers in Airbnb style
      let validCount = 0;
      listings.forEach((listing) => {
        const coords = getListingCoordinates(listing);
        bounds.extend(coords);
        validCount++;

        const priceText =
          listing.price >= 1000000
            ? (listing.price / 1000000).toLocaleString("vi-VN", {
                maximumFractionDigits: 1,
              }) + " tr"
            : (listing.price / 1000).toLocaleString("vi-VN") + "k";

        const markerHtml = `
          <div class="hm-price-pin" title="${escapeHtml(listing.title)} - ${priceText}">
            <span>${priceText}</span>
          </div>
        `;

        const priceIcon = LModule.divIcon({
          className: "hm-price-marker-wrapper",
          html: markerHtml,
          iconSize: [64, 30],
          iconAnchor: [32, 30],
          popupAnchor: [0, -32],
        });

        const coverImage =
          listing.images && listing.images.length > 0
            ? listing.images[0]
            : "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=500&q=80";

        const popupContent = `
          <div class="hm-map-popup-card">
            <div class="hm-map-popup-img-wrap">
              <img src="${coverImage}" alt="${escapeHtml(listing.title)}" />
              <span class="hm-map-popup-type-tag">${escapeHtml(listing.roomType || "Phòng trọ")}</span>
            </div>
            <div class="hm-map-popup-body">
              <h4 class="hm-map-popup-title" title="${escapeHtml(listing.title)}">
                ${escapeHtml(listing.title)}
              </h4>
              <p class="hm-map-popup-address">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" style="display: inline-block; vertical-align: -1px; margin-right: 3px; color: #ef4444;"><path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"/><circle cx="12" cy="10" r="3"/></svg>
                ${escapeHtml(listing.address || "")}
              </p>
              <div class="hm-map-popup-meta">
                <span class="hm-map-popup-price">
                  ${priceText}<span class="hm-map-popup-price-unit">/tháng</span>
                </span>
                ${listing.area ? `<span class="hm-map-popup-area">${listing.area} m²</span>` : ""}
              </div>
              <a href="/listings/${listing.id}" class="hm-map-popup-btn">
                <span>Xem chi tiết</span>
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12h14"/><path d="m12 5 7 7-7 7"/></svg>
              </a>
            </div>
          </div>
        `;

        LModule
          .marker(coords, { icon: priceIcon })
          .bindPopup(popupContent, {
            maxWidth: 240,
            autoPan: true,
            autoPanPadding: [12, 12],
            closeButton: true,
            className: "hm-leaflet-popup",
          })
          .addTo(markersGroup);
      });

      lastBoundsRef.current = bounds;
      setActiveListingCount(validCount);

      if (bounds.isValid() && validCount > 0) {
        const isMobile = typeof window !== "undefined" && window.innerWidth < 640;
        map.invalidateSize({ pan: false });
        map.fitBounds(bounds, {
          padding: isMobile ? [20, 20] : [45, 45],
          maxZoom: 16,
          animate: false,
        });
      }
    } catch (err) {
      console.warn("Could not update map markers", err);
    }
  }, [isMapReady, listings, userLocation]);

  // Hook 3: Fly to user location if changed and available
  useEffect(() => {
    if (!isMapReady || !mapRef.current || !userLocation) return;
    mapRef.current.flyTo([userLocation.lat, userLocation.lng], 15, {
      animate: true,
      duration: 0.8,
    });
  }, [userLocation, isMapReady]);

  // Hook 4: Re-calculate map size & bounds when entering/exiting fullscreen
  useEffect(() => {
    if (!mapRef.current) return;
    const timer = setTimeout(() => {
      if (mapRef.current) {
        mapRef.current.invalidateSize({ pan: false });
        if (lastBoundsRef.current && lastBoundsRef.current.isValid()) {
          const isMobile = typeof window !== "undefined" && window.innerWidth < 640;
          mapRef.current.fitBounds(lastBoundsRef.current, {
            padding: isMobile ? [20, 20] : [40, 40],
            maxZoom: 16,
            animate: false,
          });
        }
      }
    }, 150);
    return () => clearTimeout(timer);
  }, [isFullscreen]);

  // Zoom and fit controls
  const handleZoomIn = useCallback(() => {
    mapRef.current?.zoomIn();
  }, []);

  const handleZoomOut = useCallback(() => {
    mapRef.current?.zoomOut();
  }, []);

  const handleFitAll = useCallback(() => {
    const map = mapRef.current;
    const LModule = leafletRef.current;
    if (!map || !LModule || listings.length === 0) return;

    const bounds = LModule.latLngBounds([]);
    if (userLocation) {
      bounds.extend([userLocation.lat, userLocation.lng]);
    }
    listings.forEach((l) => bounds.extend(getListingCoordinates(l)));

    if (bounds.isValid()) {
      const isMobile = typeof window !== "undefined" && window.innerWidth < 640;
      map.invalidateSize({ pan: false });
      map.fitBounds(bounds, {
        padding: isMobile ? [20, 20] : [45, 45],
        maxZoom: 16,
      });
    }
  }, [listings, userLocation]);

  const toggleFullscreen = useCallback(() => {
    setIsFullscreen((prev) => !prev);
  }, []);

  return (
    <div
      className={
        isFullscreen
          ? "fixed inset-0 z-[9999] bg-white flex flex-col p-3 sm:p-5"
          : "bg-white rounded-2xl border border-slate-200/90 shadow-sm p-3.5 sm:p-5 space-y-3 transition-all"
      }
    >
      {/* Map Header */}
      <div className="flex items-center justify-between gap-2.5">
        <div className="flex items-center gap-2.5 min-w-0">
          <div className="w-8 h-8 sm:w-9 sm:h-9 rounded-xl bg-blue-50 border border-blue-100 text-blue-600 flex items-center justify-center shrink-0 shadow-xs">
            <MapPin className="w-4 h-4 text-blue-600" />
          </div>
          <div className="min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <h3 className="font-bold text-xs sm:text-base text-slate-900 tracking-tight truncate">
                Bản Đồ Vị Trí Phòng Trọ
              </h3>
              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] sm:text-xs font-semibold bg-blue-50 text-blue-700 border border-blue-200/80 shrink-0">
                <Sparkles className="w-2.5 h-2.5 sm:w-3 sm:h-3 text-blue-600" />
                {activeListingCount > 0 ? `${activeListingCount} phòng` : "Đang tìm phòng"}
              </span>
            </div>
            <p className="text-[11px] sm:text-xs text-slate-500 truncate hidden xs:block">
              Định vị tọa độ thực tế • Bấm vào giá để xem chi tiết
            </p>
          </div>
        </div>

        <div className="flex items-center gap-1.5 shrink-0">
          {onLocateMe && (
            <button
              type="button"
              onClick={onLocateMe}
              className="inline-flex items-center gap-1 sm:gap-1.5 px-2.5 sm:px-3.5 py-1.5 sm:py-2 rounded-xl bg-blue-600 hover:bg-blue-700 active:scale-95 text-white text-[11px] sm:text-xs font-semibold shadow-xs transition-all cursor-pointer"
              title="Định vị vị trí hiện tại của bạn trên bản đồ"
            >
              <Navigation className="w-3 h-3 sm:w-3.5 sm:h-3.5 fill-current" />
              <span className="hidden sm:inline">Tìm gần tôi</span>
              <span className="sm:hidden">Gần tôi</span>
            </button>
          )}

          <button
            type="button"
            onClick={toggleFullscreen}
            className="p-1.5 sm:p-2 rounded-xl border border-slate-200 bg-slate-50 hover:bg-slate-100 text-slate-700 active:scale-95 transition-all cursor-pointer"
            title={isFullscreen ? "Thu nhỏ lại" : "Mở rộng toàn màn hình"}
            aria-label={isFullscreen ? "Thu nhỏ lại" : "Mở rộng toàn màn hình"}
          >
            {isFullscreen ? (
              <Minimize2 className="w-4 h-4 text-slate-700" />
            ) : (
              <Maximize2 className="w-4 h-4 text-slate-700" />
            )}
          </button>

          {isFullscreen && (
            <button
              type="button"
              onClick={() => setIsFullscreen(false)}
              className="p-1.5 sm:p-2 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-700 active:scale-95 transition-all cursor-pointer"
              title="Đóng bản đồ"
              aria-label="Đóng bản đồ"
            >
              <X className="w-4 h-4" />
            </button>
          )}
        </div>
      </div>

      {/* Interactive Leaflet Map Container */}
      <div
        className={
          isFullscreen
            ? "relative flex-1 w-full rounded-xl overflow-hidden border border-slate-200 bg-slate-100 shadow-inner"
            : "relative w-full rounded-xl overflow-hidden border border-slate-200 bg-slate-100 shadow-inner h-[320px] sm:h-[420px]"
        }
      >
        <div
          ref={containerRef}
          className="w-full h-full z-0"
        />

        {/* Floating Custom Map Controls (Top-Right) */}
        <div className="absolute top-2.5 right-2.5 sm:top-3 sm:right-3 z-[400] flex flex-col gap-1 bg-white/95 backdrop-blur-md p-1 rounded-xl border border-slate-200/90 shadow-md">
          <button
            type="button"
            onClick={handleZoomIn}
            className="w-8 h-8 sm:w-9 sm:h-9 rounded-lg flex items-center justify-center text-slate-700 hover:bg-slate-100 hover:text-blue-600 active:scale-95 transition-all cursor-pointer"
            title="Phóng to bản đồ"
            aria-label="Phóng to bản đồ"
          >
            <Plus className="w-4 h-4 sm:w-4.5 sm:h-4.5" />
          </button>
          <div className="w-full h-px bg-slate-200/80" />
          <button
            type="button"
            onClick={handleZoomOut}
            className="w-8 h-8 sm:w-9 sm:h-9 rounded-lg flex items-center justify-center text-slate-700 hover:bg-slate-100 hover:text-blue-600 active:scale-95 transition-all cursor-pointer"
            title="Thu nhỏ bản đồ"
            aria-label="Thu nhỏ bản đồ"
          >
            <Minus className="w-4 h-4 sm:w-4.5 sm:h-4.5" />
          </button>
          <div className="w-full h-px bg-slate-200/80" />
          <button
            type="button"
            onClick={handleFitAll}
            className="w-8 h-8 sm:w-9 sm:h-9 rounded-lg flex items-center justify-center text-slate-700 hover:bg-slate-100 hover:text-blue-600 active:scale-95 transition-all cursor-pointer"
            title="Căn chỉnh hiển thị toàn bộ phòng"
            aria-label="Căn chỉnh hiển thị toàn bộ phòng"
          >
            <RotateCcw className="w-3.5 h-3.5 sm:w-4 sm:h-4" />
          </button>
        </div>

        {/* Floating helper pill (Bottom-Left - compact on mobile) */}
        <div className="absolute bottom-2.5 left-2.5 sm:bottom-3 sm:left-3 z-[400] max-w-[calc(100%-140px)] bg-white/95 backdrop-blur-md px-2.5 sm:px-3 py-1 sm:py-1.5 rounded-full border border-slate-200/90 shadow-md text-[11px] sm:text-xs font-medium text-slate-700 flex items-center gap-1.5 sm:gap-2 pointer-events-none truncate">
          <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse shrink-0" />
          <span className="sm:hidden truncate">Chạm ghim để xem</span>
          <span className="hidden sm:inline">Bấm vào ghim giá tiền để xem phòng</span>
        </div>
      </div>
    </div>
  );
};
