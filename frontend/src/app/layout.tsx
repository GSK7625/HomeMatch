import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "HomeMatch - Nền Tảng Tìm Kiếm & Quản Lý Nhà Trọ Thông Minh",
  description: "Monorepo Modular Monolith Architecture for HomeMatch Platform",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi">
      <body className="antialiased min-h-screen bg-slate-50 text-slate-900">
        {children}
      </body>
    </html>
  );
}
