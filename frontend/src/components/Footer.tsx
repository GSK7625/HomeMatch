import React from "react";
import Link from "next/link";
import { Home, ShieldCheck, Mail, Phone, MapPin } from "lucide-react";

export function Footer() {
  return (
    <footer className="bg-slate-900 text-slate-400 text-sm mt-auto border-t border-slate-800">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8 mb-8">
          {/* Col 1 */}
          <div className="space-y-4">
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 rounded-lg bg-blue-600 flex items-center justify-center text-white font-black">
                <Home className="w-4 h-4" />
              </div>
              <span className="text-white font-extrabold text-lg tracking-tight">HomeMatch</span>
            </div>
            <p className="text-xs text-slate-400 leading-relaxed">
              Hệ thống hỗ trợ tìm kiếm thuê nhà và quản lý cho thuê thông minh, kết nối trực tiếp
              người thuê và chủ nhà, minh bạch chi phí và an tâm vận hành.
            </p>
            <div className="flex items-center gap-2 text-xs text-emerald-400">
              <ShieldCheck className="w-4 h-4" />
              <span>Bảo mật JWT, RBAC & SSL/TLS chuẩn Cloudflare</span>
            </div>
          </div>

          {/* Col 2 */}
          <div>
            <h4 className="text-white font-semibold text-xs uppercase tracking-wider mb-4">
              Dành cho người thuê
            </h4>
            <ul className="space-y-2 text-xs">
              <li>
                <Link href="/listings" className="hover:text-white transition-colors">
                  Tìm kiếm phòng trọ & Căn hộ
                </Link>
              </li>
              <li>
                <Link href="/compare" className="hover:text-white transition-colors">
                  So sánh phòng đối chiếu
                </Link>
              </li>
              <li>
                <Link href="/tenant/viewings" className="hover:text-white transition-colors">
                  Lịch xem phòng đã hẹn
                </Link>
              </li>
              <li>
                <Link href="/tenant/dashboard" className="hover:text-white transition-colors">
                  Hợp đồng & Hóa đơn điện nước
                </Link>
              </li>
            </ul>
          </div>

          {/* Col 3 */}
          <div>
            <h4 className="text-white font-semibold text-xs uppercase tracking-wider mb-4">
              Dành cho chủ nhà
            </h4>
            <ul className="space-y-2 text-xs">
              <li>
                <Link href="/landlord/dashboard" className="hover:text-white transition-colors">
                  Dashboard tài chính & Công nợ
                </Link>
              </li>
              <li>
                <Link href="/landlord/properties" className="hover:text-white transition-colors">
                  Quản lý phòng & Trạng thái
                </Link>
              </li>
              <li>
                <Link href="/landlord/listings" className="hover:text-white transition-colors">
                  Đăng tin cho thuê mới
                </Link>
              </li>
              <li>
                <Link href="/landlord/utilities" className="hover:text-white transition-colors">
                  Tính tiền điện nước tự động
                </Link>
              </li>
              <li>
                <Link href="/landlord/contracts" className="hover:text-white transition-colors">
                  Quản lý hợp đồng & Khách thuê
                </Link>
              </li>
            </ul>
          </div>

          {/* Col 4 */}
          <div>
            <h4 className="text-white font-semibold text-xs uppercase tracking-wider mb-4">
              Liên hệ & Hỗ trợ
            </h4>
            <ul className="space-y-2.5 text-xs">
              <li className="flex items-center gap-2">
                <Mail className="w-3.5 h-3.5 text-blue-400" />
                <span>support@homematch.id.vn</span>
              </li>
              <li className="flex items-center gap-2">
                <Phone className="w-3.5 h-3.5 text-blue-400" />
                <span>Hotline: 1900 8899 (8:00 - 21:00)</span>
              </li>
              <li className="flex items-start gap-2">
                <MapPin className="w-3.5 h-3.5 text-blue-400 shrink-0 mt-0.5" />
                <span>Hà Nội & TP. Hồ Chí Minh, Việt Nam</span>
              </li>
            </ul>
          </div>
        </div>

        <div className="pt-8 border-t border-slate-800 flex flex-col sm:flex-row items-center justify-between gap-4 text-xs">
          <p>© 2026 HomeMatch Platform. Hệ thống hỗ trợ tìm kiếm thuê nhà & quản lý cho thuê.</p>
          <div className="flex gap-4">
            <span className="text-slate-500">Next.js 15 App Router</span>
            <span className="text-slate-600">•</span>
            <span className="text-slate-500">ASP.NET Core .NET 10 Modular Monolith</span>
          </div>
        </div>
      </div>
    </footer>
  );
}
