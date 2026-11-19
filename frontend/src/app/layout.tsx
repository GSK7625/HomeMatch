import type { Metadata, Viewport } from "next";
import { Dancing_Script } from "next/font/google";
import "leaflet/dist/leaflet.css";
import "./globals.css";
import { AuthProvider } from "@/features/auth/AuthContext";
import { ToastProvider } from "@/components/Toast";
import { Navbar } from "@/components/Navbar";
import { BottomNav } from "@/components/BottomNav";
import { Footer } from "@/components/Footer";

const dancingScript = Dancing_Script({
  subsets: ["latin", "vietnamese"],
  weight: ["600", "700"],
  variable: "--font-signature",
  display: "swap",
});

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  maximumScale: 5,
  themeColor: "#2563eb",
};

export const metadata: Metadata = {
  title: "HomeMatch - Hệ Thống Hỗ Trợ Tìm Kiếm Thuê Nhà & Quản Lý Cho Thuê",
  description:
    "Nền tảng kết nối trực tiếp người thuê và chủ nhà, tối ưu hóa tìm kiếm phòng trọ, quản lý hợp đồng, hóa đơn điện nước và vận hành thông minh.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi" className={dancingScript.variable}>
      <body className="antialiased min-h-screen flex flex-col bg-slate-50 print:bg-white text-slate-900 selection:bg-blue-100 selection:text-blue-900">
        <AuthProvider>
          <ToastProvider>
            <div className="print:hidden">
              <Navbar />
            </div>
            <main className="flex-1 flex flex-col pb-16 lg:pb-0 print:pb-0">{children}</main>
            <div className="print:hidden">
              <Footer />
            </div>
            <div className="print:hidden">
              <BottomNav />
            </div>
          </ToastProvider>
        </AuthProvider>
      </body>
    </html>
  );
}
