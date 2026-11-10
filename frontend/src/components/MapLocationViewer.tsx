"use client";

import React, { useEffect, useRef } from "react";
import type L from "leaflet";
import "leaflet/dist/leaflet.css";
import { MapPin, Navigation, ExternalLink } from "lucide-react";

interface MapLocationViewerProps {
  latitude?: number | null;
  longitude?: number | null;
  title?: string;
  address?: string;
  heightClassName?: string;
}

// Custom modern SVG pin icon for Leaflet viewer
const createViewerPinIcon = (leaflet: typeof L) => {
  return leaflet.divIcon({
    className: "hm-leaflet-viewer-marker",
    html: `
      <div style="position: relative; width: 32px; height: 32px; transform: translate(-50%, -100%);">
        <div style="width: 32px; height: 32px; background: #2563eb; border: 2.5px solid #ffffff; border-radius: 50% 50% 50% 0; transform: rotate(-45deg); box-shadow: 0 4px 10px rgba(0,0,0,0.35); display: flex; align-items: center; justify-content: center;">
          <div style="width: 10px; height: 10px; background: #ffffff; border-radius: 50%; transform: rotate(45deg);"></div>
        </div>
        <div style="position: absolute; bottom: -4px; left: 50%; transform: translateX(-50%); width: 8px; height: 3px; background: rgba(0,0,0,0.25); border-radius: 50%; filter: blur(1px);"></div>
      </div>
    `,
    iconSize: [32, 32],
    iconAnchor: [16, 32],
  });
};

export const MapLocationViewer: React.FC<MapLocationViewerProps> = ({
  latitude,
  longitude,
  title,
  address,
  heightClassName = "h-[260px] sm:h-[320px]",
}) => {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<L.Map | null>(null);

  const hasCoords = typeof latitude === "number" && typeof longitude === "number";

  useEffect(() => {
    if (!hasCoords || !containerRef.current) return;
    let isCancelled = false;

    let resizeObserver: ResizeObserver | null = null;
    let timer1: NodeJS.Timeout | null = null;
    let timer2: NodeJS.Timeout | null = null;

    const initMap = async () => {
      const leaflet = (await import("leaflet")).default;
      if (isCancelled || !containerRef.current) return;

      if (mapRef.current) {
        mapRef.current.remove();
        mapRef.current = null;
      }

      const map = leaflet.map(containerRef.current, {
        zoomControl: true,
        scrollWheelZoom: true,
        doubleClickZoom: true,
        touchZoom: true,
      }).setView([latitude, longitude], 16);

      // Google Maps standard raster tiles - ultra fast in Vietnam (<150ms), reliable, crisp Vietnamese labels
      leaflet.tileLayer("https://mt{s}.google.com/vt/lyrs=m&x={x}&y={y}&z={z}", {
        subdomains: ["0", "1", "2", "3"],
        maxZoom: 20,
        attribution: '&copy; <a href="https://maps.google.com" target="_blank" rel="noopener noreferrer">Google Maps</a>',
      }).addTo(map);

      const marker = leaflet.marker([latitude, longitude], {
        icon: createViewerPinIcon(leaflet),
      }).addTo(map);

      if (title || address) {
        marker.bindPopup(`
          <div style="font-size: 12px; font-family: inherit; line-height: 1.4;">
            ${title ? `<strong>${title}</strong><br/>` : ""}
            ${address ? `<span style="color: #64748b;">${address}</span>` : ""}
          </div>
        `);
      }

      mapRef.current = map;
      map.invalidateSize();

      timer1 = setTimeout(() => {
        if (!isCancelled && mapRef.current) {
          mapRef.current.invalidateSize();
        }
      }, 150);

      timer2 = setTimeout(() => {
        if (!isCancelled && mapRef.current) {
          mapRef.current.invalidateSize();
        }
      }, 400);

      if (typeof ResizeObserver !== "undefined" && containerRef.current) {
        resizeObserver = new ResizeObserver(() => {
          if (!isCancelled && mapRef.current) {
            mapRef.current.invalidateSize();
          }
        });
        resizeObserver.observe(containerRef.current);
      }
    };

    initMap();

    return () => {
      isCancelled = true;
      if (timer1) clearTimeout(timer1);
      if (timer2) clearTimeout(timer2);
      if (resizeObserver) {
        resizeObserver.disconnect();
      }
      if (mapRef.current) {
        mapRef.current.remove();
        mapRef.current = null;
      }
    };
  }, [hasCoords, latitude, longitude, title, address]);

  if (!hasCoords) {
    return (
      <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50/70 p-6 text-center text-slate-500 space-y-2">
        <MapPin className="w-6 h-6 mx-auto text-slate-400" />
        <p className="text-xs font-semibold">Tòa nhà này chưa có tọa độ GPS định vị chính xác.</p>
        {address && (
          <a
            href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(address)}`}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1 text-xs text-blue-600 font-bold hover:underline"
          >
            <span>Tìm địa chỉ trên Google Maps</span>
            <ExternalLink className="w-3 h-3" />
          </a>
        )}
      </div>
    );
  }

  const googleMapsDirectionsUrl = `https://www.google.com/maps/dir/?api=1&destination=${latitude},${longitude}`;
  const openStreetMapUrl = `https://www.openstreetmap.org/?mlat=${latitude}&mlon=${longitude}#map=17/${latitude}/${longitude}`;

  return (
    <div className="space-y-2.5">
      <div className="relative rounded-2xl overflow-hidden border border-slate-200/80 shadow-xs bg-slate-100">
        <div ref={containerRef} className={`w-full ${heightClassName} z-0`} />

        {/* Top-right quick actions */}
        <div className="absolute top-2 right-2 z-[400] flex items-center gap-1.5">
          <a
            href={googleMapsDirectionsUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1 px-3 py-1.5 rounded-xl bg-blue-600/95 hover:bg-blue-700 text-white text-[11px] font-bold shadow-md shadow-blue-500/20 backdrop-blur-xs transition-colors"
          >
            <Navigation className="w-3 h-3" />
            <span>Chỉ đường</span>
          </a>
          <a
            href={openStreetMapUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-xl bg-white/95 hover:bg-white text-slate-700 text-[11px] font-bold shadow-xs border border-slate-200/80 backdrop-blur-xs transition-colors"
            title="Mở trên OpenStreetMap"
          >
            <ExternalLink className="w-3 h-3" />
          </a>
        </div>
      </div>

      <div className="flex items-center justify-between text-[11px] text-slate-500 px-1 flex-wrap gap-2">
        <div className="flex items-center gap-1 font-mono text-slate-600">
          <MapPin className="w-3 h-3 text-blue-600 shrink-0" />
          <span>Tọa độ GPS: {latitude.toFixed(6)}, {longitude.toFixed(6)}</span>
        </div>
        <a
          href={googleMapsDirectionsUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="text-blue-600 hover:underline font-bold inline-flex items-center gap-1"
        >
          <span>Xem đường đi trên Google Maps</span>
          <ExternalLink className="w-3 h-3" />
        </a>
      </div>
    </div>
  );
};
