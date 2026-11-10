"use client";

import React, { useEffect, useState } from "react";
import dynamic from "next/dynamic";
import { PropertyListing } from "@/types";
import "leaflet/dist/leaflet.css";

// Dynamic import các component của react-leaflet để tránh lỗi SSR (Window is not defined)
const MapContainer = dynamic(
  () => import("react-leaflet").then((mod) => mod.MapContainer),
  { ssr: false }
);
const TileLayer = dynamic(
  () => import("react-leaflet").then((mod) => mod.TileLayer),
  { ssr: false }
);
const Marker = dynamic(
  () => import("react-leaflet").then((mod) => mod.Marker),
  { ssr: false }
);
const Popup = dynamic(
  () => import("react-leaflet").then((mod) => mod.Popup),
  { ssr: false }
);

interface LeafletMapViewProps {
  listings: PropertyListing[];
  center?: [number, number];
  zoom?: number;
  height?: string;
  selectedId?: string;
}

export default function LeafletMapView({
  listings,
  center = [21.0285, 105.8542], // Mặc định Hà Nội
  zoom = 12,
  height = "420px",
  selectedId,
}: LeafletMapViewProps) {
  const [mounted, setMounted] = useState(false);
  const [customIcon, setCustomIcon] = useState<any>(null);

  useEffect(() => {
    setMounted(true);
    // Khắc phục lỗi thiếu icon mặc định của Leaflet trong Next.js / Webpack
    import("leaflet").then((L) => {
      delete (L.Icon.Default.prototype as any)._getIconUrl;
      L.Icon.Default.mergeOptions({
        iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
        iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
        shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
      });

      const icon = L.divIcon({
        className: "custom-leaflet-marker",
        html: `<div style="background-color: #2563eb; color: white; padding: 4px 8px; border-radius: 9999px; font-size: 11px; font-weight: bold; border: 2px solid white; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.3); white-space: nowrap;">📍 Nhà trọ</div>`,
        iconSize: [60, 24],
        iconAnchor: [30, 24],
      });
      setCustomIcon(icon);
    });
  }, []);

  if (!mounted) {
    return (
      <div
        style={{ height }}
        className="w-full bg-slate-100 rounded-2xl flex items-center justify-center text-slate-400 text-xs font-semibold animate-pulse"
      >
        Đang tải bản đồ OpenStreetMap...
      </div>
    );
  }

  // Lọc danh sách các listing có tọa độ hợp lệ
  const validListings = listings.filter(
    (l) => typeof l.latitude === "number" && typeof l.longitude === "number"
  );

  // Tính lại tâm bản đồ nếu có danh sách phòng
  const mapCenter: [number, number] =
    validListings.length > 0
      ? [validListings[0].latitude, validListings[0].longitude]
      : center;

  return (
    <div
      style={{ height }}
      className="w-full rounded-2xl overflow-hidden border border-slate-200 shadow-sm relative z-0"
    >
      <MapContainer
        center={mapCenter}
        zoom={zoom}
        scrollWheelZoom={false}
        style={{ height: "100%", width: "100%" }}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />

        {validListings.map((item) => (
          <Marker
            key={item.id}
            position={[item.latitude, item.longitude]}
            icon={customIcon || undefined}
          >
            <Popup className="custom-leaflet-popup">
              <div className="p-1 max-w-[220px] space-y-2 text-xs">
                {item.images && item.images[0] && (
                  <img
                    src={item.images[0]}
                    alt={item.title}
                    className="w-full h-24 object-cover rounded-lg"
                  />
                )}
                <div>
                  <h4 className="font-bold text-slate-900 line-clamp-1">{item.title}</h4>
                  <p className="text-[11px] text-slate-500 line-clamp-1">{item.address}</p>
                </div>
                <div className="flex items-center justify-between pt-1 border-t border-slate-100">
                  <span className="font-black text-blue-600 text-sm">
                    {(item.price / 1000000).toFixed(1)} tr/tháng
                  </span>
                  <a
                    href={`/listings/${item.id}`}
                    className="px-2.5 py-1 bg-blue-600 hover:bg-blue-700 text-white rounded-md font-bold text-[10px]"
                  >
                    Xem chi tiết
                  </a>
                </div>
              </div>
            </Popup>
          </Marker>
        ))}
      </MapContainer>
    </div>
  );
}
