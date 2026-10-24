"use client";

import { useState, useEffect } from "react";
import { Badge } from "@/components/Badge";
import { Button } from "@/components/Button";
import { Card, CardHeader, CardTitle } from "@/components/Card";
import { fetchHealth, fetchSystemStatus } from "@/lib/api-client";
import { HealthCheck, SystemStatus } from "@/types";

export default function Home() {
  const [health, setHealth] = useState<HealthCheck | null>(null);
  const [systemStatus, setSystemStatus] = useState<SystemStatus | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const checkConnection = async () => {
    setLoading(true);
    setError(null);
    try {
      const [h, s] = await Promise.all([fetchHealth(), fetchSystemStatus()]);
      setHealth(h);
      setSystemStatus(s);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Không thể kết nối đến Backend API";
      setError(msg);
      setHealth(null);
      setSystemStatus(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    checkConnection();
  }, []);

  const backendModules = [
    { name: "Identity", desc: "Tài khoản, phân quyền, xác thực JWT" },
    { name: "Properties", desc: "Tòa nhà, phòng, tiện ích" },
    { name: "Listings", desc: "Tin đăng, tìm kiếm tối ưu với PostGIS" },
    { name: "Viewings", desc: "Lịch hẹn xem phòng trực tiếp" },
    { name: "Messaging", desc: "Chat thời gian thực (SignalR)" },
    { name: "Leasing", desc: "Hợp đồng thuê, quản lý khách thuê" },
    { name: "Billing", desc: "Hóa đơn tiền phòng, điện nước dịch vụ" },
    { name: "Notifications", desc: "Thông báo in-app, email, push notification" },
  ];

  const frontendFeatures = [
    { name: "auth", desc: "Đăng nhập, đăng ký, phân quyền" },
    { name: "properties", desc: "Quản lý phòng trọ cho chủ nhà" },
    { name: "listings", desc: "Khám phá & tìm kiếm phòng theo bản đồ" },
    { name: "favorites", desc: "Danh sách phòng đã lưu yêu thích" },
    { name: "viewings", desc: "Đặt và quản lý lịch xem phòng" },
    { name: "messaging", desc: "Khung chat trực tiếp với chủ trọ" },
    { name: "leasing", desc: "Xem hợp đồng điện tử & khách thuê" },
    { name: "billing", desc: "Xem & thanh toán hóa đơn hàng tháng" },
    { name: "notifications", desc: "Trung tâm chuông thông báo" },
  ];

  return (
    <main className="min-h-screen p-6 md:p-12 max-w-7xl mx-auto space-y-10">
      {/* Header */}
      <header className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-6 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-3xl font-extrabold tracking-tight text-slate-900">
              HomeMatch Platform
            </h1>
            <Badge variant="info">Monorepo</Badge>
            <Badge variant="success">Modular Monolith</Badge>
          </div>
          <p className="text-slate-500 mt-1">
            Nền tảng tìm kiếm và quản lý nhà trọ thông minh dành cho nhóm phát triển.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Button onClick={checkConnection} disabled={loading} variant="primary">
            {loading ? "Đang kiểm tra..." : "Kiểm tra kết nối Backend"}
          </Button>
        </div>
      </header>

      {/* Connection Status Banner */}
      <section>
        <Card className="border-l-4 border-l-blue-600 bg-gradient-to-r from-blue-50/50 to-white">
          <CardHeader>
            <div>
              <CardTitle>Trạng thái kết nối: Frontend ↔ Backend ↔ Database</CardTitle>
              <p className="text-xs text-slate-500 mt-0.5">
                Kiểm tra giao tiếp qua endpoint: <code>/health</code> & <code>/api/v1/system/status</code>
              </p>
            </div>
            {loading ? (
              <Badge variant="warning">Đang kết nối...</Badge>
            ) : health ? (
              <Badge variant="success">Backend Online (Healthy)</Badge>
            ) : (
              <Badge variant="error">Chưa kết nối Backend</Badge>
            )}
          </CardHeader>

          {health && systemStatus ? (
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 pt-2 text-sm">
              <div className="p-3 bg-white rounded-lg border border-slate-200/60">
                <span className="text-slate-400 block text-xs">Môi trường</span>
                <span className="font-semibold text-slate-800">{health.environment}</span>
              </div>
              <div className="p-3 bg-white rounded-lg border border-slate-200/60">
                <span className="text-slate-400 block text-xs">Phiên bản API</span>
                <span className="font-semibold text-slate-800">{systemStatus.version}</span>
              </div>
              <div className="p-3 bg-white rounded-lg border border-slate-200/60">
                <span className="text-slate-400 block text-xs">Thời gian máy chủ</span>
                <span className="font-semibold text-slate-800">
                  {new Date(health.timestamp).toLocaleTimeString("vi-VN")}
                </span>
              </div>
            </div>
          ) : (
            <div className="p-4 bg-amber-50 rounded-lg text-amber-800 text-sm border border-amber-200">
              {error ? (
                <p><strong>Lỗi kết nối:</strong> {error}</p>
              ) : (
                <p>
                  Backend chưa khởi động hoặc chưa bật CORS. Bạn có thể khởi động toàn bộ môi trường bằng lệnh:
                  <code className="block mt-1 font-mono text-xs bg-amber-100 p-2 rounded">
                    docker compose up -d
                  </code>
                </p>
              )}
            </div>
          )}
        </Card>
      </section>

      {/* Modules & Features Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* Backend Modules */}
        <section className="space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-bold text-slate-800">
              ASP.NET Core Modules ({backendModules.length})
            </h2>
            <Badge variant="neutral">backend/src/Modules</Badge>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {backendModules.map((m) => (
              <div
                key={m.name}
                className="p-4 bg-white rounded-lg border border-slate-200 hover:border-blue-400 transition-colors"
              >
                <div className="flex items-center justify-between mb-1">
                  <h4 className="font-semibold text-slate-900">{m.name}</h4>
                  <Badge variant="info">Module</Badge>
                </div>
                <p className="text-xs text-slate-500 leading-relaxed">{m.desc}</p>
              </div>
            ))}
          </div>
        </section>

        {/* Frontend Features */}
        <section className="space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-bold text-slate-800">
              Next.js Features ({frontendFeatures.length})
            </h2>
            <Badge variant="neutral">frontend/src/features</Badge>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {frontendFeatures.map((f) => (
              <div
                key={f.name}
                className="p-4 bg-white rounded-lg border border-slate-200 hover:border-emerald-400 transition-colors"
              >
                <div className="flex items-center justify-between mb-1">
                  <h4 className="font-semibold text-slate-900 font-mono text-sm">{f.name}</h4>
                  <Badge variant="neutral">Feature</Badge>
                </div>
                <p className="text-xs text-slate-500 leading-relaxed">{f.desc}</p>
              </div>
            ))}
          </div>
        </section>
      </div>

      {/* Quick Start Guide */}
      <section className="bg-slate-900 text-white rounded-xl p-6 md:p-8 space-y-4">
        <h3 className="text-lg font-bold">Hướng Dẫn Khởi Chạy Nhanh Cho Nhóm</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-xs font-mono">
          <div className="bg-slate-800 p-4 rounded-lg space-y-2">
            <span className="text-slate-400 block font-sans font-semibold">1. Chạy với Docker Compose</span>
            <code className="block text-emerald-400">docker compose up -d --build</code>
            <p className="text-slate-400 font-sans text-xs">Tự động khởi động PostgreSQL 16 + PostGIS, Backend API và Next.js Frontend.</p>
          </div>
          <div className="bg-slate-800 p-4 rounded-lg space-y-2">
            <span className="text-slate-400 block font-sans font-semibold">2. Chạy Backend độc lập</span>
            <code className="block text-emerald-400">dotnet run --project backend/src/HomeMatch.Api</code>
            <p className="text-slate-400 font-sans text-xs">API khởi chạy tại port 5000, tài liệu Swagger tại <code>/swagger</code>.</p>
          </div>
          <div className="bg-slate-800 p-4 rounded-lg space-y-2">
            <span className="text-slate-400 block font-sans font-semibold">3. Chạy Frontend độc lập</span>
            <code className="block text-emerald-400">cd frontend &amp;&amp; npm run dev</code>
            <p className="text-slate-400 font-sans text-xs">Giao diện người dùng mở tại <code>http://localhost:3000</code>.</p>
          </div>
        </div>
      </section>
    </main>
  );
}
