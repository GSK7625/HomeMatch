import React from "react";
import Link from "next/link";
import { Home, ShieldCheck, Award, CheckCircle2, Lock, HelpCircle } from "lucide-react";

export function Footer() {
  return (
    <footer className="bg-slate-950 text-slate-400 text-sm mt-auto border-t border-slate-800/80">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-14">
        {/* Top Trust Banner */}
        <div className="pb-10 mb-10 border-b border-slate-800/80 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-blue-900/50 text-blue-400 border border-blue-800/50 flex items-center justify-center shrink-0">
              <ShieldCheck className="w-5 h-5" />
            </div>
            <div>
              <h4 className="text-white font-bold text-xs">Xác Thực 100% Thực Địa</h4>
              <p className="text-[11px] text-slate-400">Kiểm tra thông tin & pháp lý</p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-emerald-900/50 text-emerald-400 border border-emerald-800/50 flex items-center justify-center shrink-0">
              <Award className="w-5 h-5" />
            </div>
            <div>
              <h4 className="text-white font-bold text-xs">0đ Phí Dịch Vụ Môi Giới</h4>
              <p className="text-[11px] text-slate-400">Kết nối trực tiếp chủ trọ</p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-amber-900/50 text-amber-400 border border-amber-800/50 flex items-center justify-center shrink-0">
              <Lock className="w-5 h-5" />
            </div>
            <div>
              <h4 className="text-white font-bold text-xs">Bảo Vệ Tiền Đặt Cọc</h4>
              <p className="text-[11px] text-slate-400">Hợp đồng điện tử minh bạch</p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-indigo-900/50 text-indigo-400 border border-indigo-800/50 flex items-center justify-center shrink-0">
              <HelpCircle className="w-5 h-5" />
            </div>
            <div>
              <h4 className="text-white font-bold text-xs">Hỗ Trợ Người Dùng</h4>
              <p className="text-[11px] text-slate-400">Đồng hành & hỗ trợ trực tuyến</p>
            </div>
          </div>
        </div>

        {/* Links Grid */}
        <div className="grid grid-cols-1 md:grid-cols-5 gap-8 mb-10">
          {/* Col 1 & 2: Brand & Overview Info */}
          <div className="md:col-span-2 space-y-4">
            <div className="flex items-center gap-2.5">
              <div className="w-9 h-9 rounded-xl bg-blue-600 flex items-center justify-center text-white font-black text-lg shadow-md">
                <Home className="w-5 h-5" />
              </div>
              <span className="text-white font-black text-xl tracking-tight">HomeMatch</span>
            </div>

            <p className="text-xs text-slate-400 leading-relaxed max-w-sm">
              Nền tảng kết nối trực tiếp giữa người thuê và chủ nhà trọ, giúp quá trình tìm phòng, so sánh tiện ích và quản lý hợp đồng hóa đơn trở nên minh bạch và thuận tiện.
            </p>

            <div className="space-y-1.5 text-xs text-slate-400 pt-1">
              <div className="flex items-center gap-2">
                <ShieldCheck className="w-3.5 h-3.5 text-blue-400 shrink-0" />
                <span>Tin đăng phòng xác thực trực tiếp</span>
              </div>
              <div className="flex items-center gap-2">
                <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400 shrink-0" />
                <span>Không thu phí trung gian người thuê</span>
              </div>
            </div>
          </div>

          {/* Col 3: Dành cho người thuê */}
          <div>
            <h4 className="text-white font-bold text-xs uppercase tracking-wider mb-4">
              Dành cho người thuê
            </h4>
            <ul className="space-y-2 text-xs">
              <li>
                <Link href="/listings" className="hover:text-white transition-colors">
                  Khám phá phòng trọ & Căn hộ
                </Link>
              </li>
              <li>
                <Link href="/compare" className="hover:text-white transition-colors">
                  So sánh phòng đối chiếu (tối đa 3)
                </Link>
              </li>
              <li>
                <Link href="/tenant/viewings" className="hover:text-white transition-colors">
                  Quản lý lịch hẹn xem phòng
                </Link>
              </li>
              <li>
                <Link href="/tenant/dashboard" className="hover:text-white transition-colors">
                  Hợp đồng & Hóa đơn điện nước
                </Link>
              </li>
              <li>
                <Link href="/tenant/favorites" className="hover:text-white transition-colors">
                  Danh sách phòng yêu thích
                </Link>
              </li>
            </ul>
          </div>

          {/* Col 4: Dành cho chủ nhà */}
          <div>
            <h4 className="text-white font-bold text-xs uppercase tracking-wider mb-4">
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
                  Ghi chỉ số & Tính tiền điện nước
                </Link>
              </li>
              <li>
                <Link href="/landlord/contracts" className="hover:text-white transition-colors">
                  Quản lý hợp đồng & Khách thuê
                </Link>
              </li>
            </ul>
          </div>

          {/* Col 5: Khu vực & Trường ĐH Hot */}
          <div>
            <h4 className="text-white font-bold text-xs uppercase tracking-wider mb-4">
              Khu vực sinh viên
            </h4>
            <ul className="space-y-2 text-xs">
              <li>
                <Link href="/listings?city=Hà%20Nội&district=Cầu%20Giấy" className="hover:text-white transition-colors">
                  Trọ gần ĐH Quốc Gia Hà Nội
                </Link>
              </li>
              <li>
                <Link href="/listings?city=Hà%20Nội&district=Hai%20Bà%20Trưng" className="hover:text-white transition-colors">
                  Trọ Bách - Kinh - Xây
                </Link>
              </li>
              <li>
                <Link href="/listings?city=Hồ%20Chí%20Minh&district=Bình%20Thạnh" className="hover:text-white transition-colors">
                  Trọ gần ĐH HUTECH & UEF
                </Link>
              </li>
              <li>
                <Link href="/listings?city=Hồ%20Chí%20Minh&district=Quận%201" className="hover:text-white transition-colors">
                  Căn hộ dịch vụ Quận 1
                </Link>
              </li>
            </ul>
          </div>
        </div>

        {/* Bottom Copyright & Tech Standards */}
        <div className="pt-8 border-t border-slate-800/80 flex flex-col sm:flex-row items-center justify-between gap-4 text-xs">
          <p>© 2026 HomeMatch. Nền tảng kết nối người thuê và chủ nhà.</p>
          <div className="flex items-center gap-3 text-slate-500 text-[11px]">
            <span>Quy chế hoạt động</span>
            <span>•</span>
            <span>Chính sách bảo mật</span>
            <span>•</span>
            <span>Giải quyết tranh chấp</span>
          </div>
        </div>
      </div>
    </footer>
  );
}
