"use client";

import React, { useState } from "react";
import { LeasingContractDetailDto } from "@/types";
import { ContractSnapshotData, calcLeaseMonths } from "@/features/leasing/contract-snapshot";
import { formatDate, formatDateTime, formatVnd } from "@/features/leasing/leasing-helpers";
import {
  CheckCircle2,
  Clock,
  Copy,
  Download,
  FileText,
  Loader2,
  Printer,
  ShieldCheck,
  ZoomIn,
  ZoomOut,
} from "lucide-react";

interface ContractA4DocumentProps {
  detail: LeasingContractDetailDto;
  snapshot: ContractSnapshotData;
  busyDoc: boolean;
  onDownloadOriginal: () => void;
  onCopyHash: (hash: string) => void;
}

export function ContractA4Document({
  detail,
  snapshot,
  busyDoc,
  onDownloadOriginal,
  onCopyHash,
}: ContractA4DocumentProps) {
  const [zoom, setZoom] = useState(100);

  const months = calcLeaseMonths(detail.startDate, detail.endDate);
  const roomDisplay = snapshot.roomName || `Phòng #${detail.roomId}`;
  const propertyDisplay = snapshot.propertyName || "Nhà cho thuê HomeMatch";
  const addressDisplay = snapshot.propertyAddress || "Theo địa chỉ tài sản đã đăng ký trên HomeMatch";
  const landlordName = snapshot.landlord?.fullName || `Chủ nhà (#${detail.landlordId})`;
  const tenantName = snapshot.tenant?.fullName || `Người thuê (#${detail.tenantId})`;

  const createdDate = new Date(detail.createdAt);
  const createdText = Number.isNaN(createdDate.getTime())
    ? formatDate(detail.startDate)
    : `ngày ${createdDate.getDate()} tháng ${createdDate.getMonth() + 1} năm ${createdDate.getFullYear()}`;

  const handlePrintPdf = () => {
    const prevTitle = document.title;
    document.title = `HopDong_${detail.contractCode}`;
    const restore = () => {
      document.title = prevTitle;
      window.removeEventListener("afterprint", restore);
    };
    window.addEventListener("afterprint", restore);
    window.print();
    setTimeout(restore, 1500);
  };

  return (
    <div className="bg-white rounded-2xl border border-slate-200/90 shadow-xs overflow-hidden print:border-0 print:shadow-none print:rounded-none print:overflow-visible">
      {/* Document Toolbar */}
      <div className="px-4 py-3 border-b border-slate-200 bg-slate-50/80 flex flex-wrap items-center justify-between gap-3 print:hidden">
        <div className="flex items-center gap-2 text-xs font-bold text-slate-800">
          <FileText className="w-4 h-4 text-blue-600" />
          <span>Bản Thảo Pháp Lý Hợp Đồng Thuê Nhà Ở</span>
          <span className="px-2 py-0.5 rounded-md bg-slate-200/70 text-[10px] font-semibold text-slate-600">
            v{detail.documentVersion}
          </span>
        </div>

        <div className="flex items-center gap-1.5">
          <div className="hidden sm:flex items-center gap-1 bg-white border border-slate-200 rounded-lg px-1.5 py-1 text-[11px] font-semibold text-slate-600">
            <button
              type="button"
              onClick={() => setZoom((z) => Math.max(80, z - 10))}
              className="p-1 hover:bg-slate-100 rounded"
              title="Thu nhỏ"
            >
              <ZoomOut className="w-3.5 h-3.5" />
            </button>
            <span className="w-10 text-center">{zoom}%</span>
            <button
              type="button"
              onClick={() => setZoom((z) => Math.min(120, z + 10))}
              className="p-1 hover:bg-slate-100 rounded"
              title="Phóng to"
            >
              <ZoomIn className="w-3.5 h-3.5" />
            </button>
          </div>

          {detail.contractFileUrl && (
            <button
              type="button"
              onClick={onDownloadOriginal}
              disabled={busyDoc}
              className="px-3 py-1.5 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 text-[11px] font-bold text-slate-700 inline-flex items-center gap-1.5"
            >
              {busyDoc ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Download className="w-3.5 h-3.5" />}
              <span>Tải file gốc</span>
            </button>
          )}

          <button
            type="button"
            onClick={handlePrintPdf}
            className="px-3 py-1.5 rounded-lg bg-slate-900 hover:bg-slate-800 text-[11px] font-bold text-white inline-flex items-center gap-1.5 transition-colors"
            title="In hoặc Lưu thành file PDF chuẩn A4"
          >
            <Printer className="w-3.5 h-3.5" />
            <span>In / Lưu PDF</span>
          </button>
        </div>
      </div>

      {/* Paper Canvas */}
      <div className="p-4 sm:p-6 lg:p-8 bg-slate-100/70 overflow-x-auto print:p-0 print:bg-white print:overflow-visible">
        <div
          style={{ fontSize: `${zoom}%` }}
          className="mx-auto max-w-[760px] bg-white rounded-xl border border-slate-200 shadow-sm p-6 sm:p-10 text-slate-800 space-y-5 transition-all print:max-w-none print:w-full print:border-0 print:shadow-none print:rounded-none print:px-[14mm] print:py-[10mm] print:space-y-2.5 print:[font-size:100%!important]"
        >
          {/* National Header */}
          <div className="text-center space-y-0.5">
            <div className="text-xs font-extrabold uppercase tracking-wide text-slate-900">
              CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM
            </div>
            <div className="text-[11px] font-semibold text-slate-600">Độc lập - Tự do - Hạnh phúc</div>
            <div className="w-24 h-px bg-slate-400 mx-auto my-1.5" />
            <h2 className="text-lg sm:text-xl print:text-[16px] font-black text-blue-700 uppercase tracking-tight pt-1">
              HỢP ĐỒNG THUÊ NHÀ Ở / PHÒNG TRỌ ĐIỆN TỬ
            </h2>
            <div className="text-[11px] text-slate-600 font-medium">
              Số: <span className="font-mono font-bold text-slate-900">{detail.contractCode}</span>
            </div>
            <div className="text-[11px] italic text-slate-500">Lập trên nền tảng HomeMatch, {createdText}</div>
          </div>

          <p className="text-xs print:text-[10.5px] italic text-slate-600 leading-relaxed">
            Căn cứ Bộ luật Dân sự số 91/2015/QH13 (Điều 472–482); Luật Nhà ở số 27/2023/QH15 (Điều 163, 170–172); Luật
            Cư trú số 68/2020/QH14; và Luật Giao dịch điện tử số 20/2023/QH15; hai bên tự nguyện thỏa thuận giao kết hợp
            đồng thuê nhà ở với các nội dung sau:
          </p>

          {/* Party A & Party B */}
          <div className="grid grid-cols-1 gap-2.5 print:gap-2">
            <div className="p-3.5 print:p-2.5 rounded-xl bg-slate-50/90 border border-slate-200/80 print:border-slate-300 space-y-1 text-xs break-inside-avoid">
              <div className="font-extrabold text-slate-900 uppercase tracking-wide flex items-center gap-1.5 text-[11px]">
                <span className="w-2 h-2 rounded-full bg-blue-600" />
                <span>BÊN CHO THUÊ (BÊN A):</span>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 print:grid-cols-2 gap-x-6 gap-y-1 text-[11px] print:text-[10.5px]">
                <div>
                  Họ và tên: <strong className="text-slate-900">{landlordName}</strong>
                </div>
                <div>
                  Số điện thoại:{" "}
                  <strong className="text-blue-700">{snapshot.landlord?.phone || "Chưa cập nhật SĐT"}</strong>
                </div>
                <div>
                  Email: <strong>{snapshot.landlord?.email || "—"}</strong>
                </div>
                <div>
                  CCCD/Định danh:{" "}
                  <strong className="font-mono">{snapshot.landlord?.cccd || "Xác thực tài khoản HomeMatch"}</strong>
                </div>
                {snapshot.landlord?.address && (
                  <div className="sm:col-span-2 print:col-span-2">
                    Địa chỉ liên hệ: <strong>{snapshot.landlord.address}</strong>
                  </div>
                )}
                {snapshot.landlord?.bankAccount && (
                  <div className="sm:col-span-2 print:col-span-2">
                    Tài khoản nhận thanh toán: <strong>{snapshot.landlord.bankAccount}</strong>
                  </div>
                )}
              </div>
            </div>

            <div className="p-3.5 print:p-2.5 rounded-xl bg-slate-50/90 border border-slate-200/80 print:border-slate-300 space-y-1 text-xs break-inside-avoid">
              <div className="font-extrabold text-slate-900 uppercase tracking-wide flex items-center gap-1.5 text-[11px]">
                <span className="w-2 h-2 rounded-full bg-indigo-600" />
                <span>BÊN THUÊ (BÊN B):</span>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 print:grid-cols-2 gap-x-6 gap-y-1 text-[11px] print:text-[10.5px]">
                <div>
                  Họ và tên: <strong className="text-slate-900">{tenantName}</strong>
                </div>
                <div>
                  Số điện thoại:{" "}
                  <strong className="text-blue-700">{snapshot.tenant?.phone || "Chưa cập nhật SĐT"}</strong>
                </div>
                <div>
                  Email ký số: <strong>{snapshot.tenant?.email || "—"}</strong>
                </div>
                <div>
                  CCCD/Định danh:{" "}
                  <strong className="font-mono">{snapshot.tenant?.cccd || "Xác thực tài khoản HomeMatch"}</strong>
                </div>
                {snapshot.tenant?.address && (
                  <div className="sm:col-span-2 print:col-span-2">
                    Địa chỉ thường trú: <strong>{snapshot.tenant.address}</strong>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Article 1 */}
          <div className="space-y-1 text-xs print:text-[10.5px] leading-relaxed break-inside-avoid">
            <h3 className="font-extrabold text-blue-700 uppercase">
              ĐIỀU 1: ĐỐI TƯỢNG, MỤC ĐÍCH VÀ THỜI HẠN THUÊ
            </h3>
            <p>
              1.1. <strong>Đặc điểm phòng cho thuê:</strong> Bên A cho Bên B thuê{" "}
              <strong>
                {roomDisplay}
                {snapshot.floor ? ` (Tầng ${snapshot.floor})` : ""}
                {snapshot.areaSqm ? `, diện tích ${snapshot.areaSqm} m²` : ""}
                {snapshot.maxOccupants ? `, ở tối đa ${snapshot.maxOccupants} người` : ""}
              </strong>{" "}
              tại <strong>{propertyDisplay}</strong>, địa chỉ: <strong>{addressDisplay}</strong>. Mục đích sử dụng: để
              ở hợp pháp.
            </p>
            {snapshot.amenities && snapshot.amenities.length > 0 && (
              <p>
                1.2. <strong>Trang thiết bị & tiện nghi bàn giao:</strong> {snapshot.amenities.join(", ")}. Hai bên xác
                nhận kiểm tra hiện trạng thực tế khi nhận phòng.
              </p>
            )}
            <p>
              {snapshot.amenities && snapshot.amenities.length > 0 ? "1.3." : "1.2."}{" "}
              <strong>Thời hạn thuê & giao nhận phòng:</strong> <strong>{months} tháng</strong>, từ ngày{" "}
              <strong>{formatDate(detail.startDate)}</strong> đến hết ngày <strong>{formatDate(detail.endDate)}</strong>
              . Thời điểm bàn giao phòng trùng với ngày bắt đầu thuê trừ khi có thỏa thuận khác.
            </p>
          </div>

          {/* Article 2 */}
          <div className="space-y-1.5 text-xs print:text-[10.5px] leading-relaxed break-inside-avoid">
            <h3 className="font-extrabold text-blue-700 uppercase">
              ĐIỀU 2: GIÁ THUÊ, TIỀN ĐẶT CỌC VÀ BIỂU PHÍ DỊCH VỤ
            </h3>

            <div className="rounded-xl border border-slate-200 print:border-slate-300 overflow-hidden">
              <table className="w-full text-[11px] print:text-[10px] text-left border-collapse">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-200 print:border-slate-300 text-slate-600 font-bold">
                    <th className="px-3 py-1.5">Hạng mục chi phí</th>
                    <th className="px-3 py-1.5">Đơn giá áp dụng</th>
                    <th className="px-3 py-1.5">Phương thức & Kỳ thanh toán</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 print:divide-slate-200">
                  <tr>
                    <td className="px-3 py-1.5 font-semibold text-slate-800">Tiền thuê phòng cố định</td>
                    <td className="px-3 py-1.5 font-black text-blue-600">{formatVnd(detail.monthlyRent)}/tháng</td>
                    <td className="px-3 py-1.5 text-slate-600">
                      Chuyển khoản/VNPay trước ngày {detail.billingDueDay} hàng tháng
                    </td>
                  </tr>
                  <tr>
                    <td className="px-3 py-1.5 font-semibold text-slate-800">Tiền điện sinh hoạt</td>
                    <td className="px-3 py-1.5 font-bold text-slate-900">
                      {snapshot.electricityPrice || "Theo biểu giá công bố của tòa nhà"}
                    </td>
                    <td className="px-3 py-1.5 text-slate-600">Theo chỉ số công tơ thực tế hàng tháng</td>
                  </tr>
                  <tr>
                    <td className="px-3 py-1.5 font-semibold text-slate-800">Nước sinh hoạt</td>
                    <td className="px-3 py-1.5 font-bold text-slate-900">
                      {snapshot.waterPrice || "Theo biểu giá công bố của tòa nhà"}
                    </td>
                    <td className="px-3 py-1.5 text-slate-600">Đối soát cuối tháng cùng hóa đơn phòng</td>
                  </tr>
                  <tr>
                    <td className="px-3 py-1.5 font-semibold text-slate-800">Internet / Dịch vụ chung</td>
                    <td className="px-3 py-1.5 font-bold text-slate-900">
                      {[snapshot.internetPrice, snapshot.serviceFee, snapshot.parkingFee].filter(Boolean).join(" · ") ||
                        "Theo thỏa thuận dịch vụ tòa nhà"}
                    </td>
                    <td className="px-3 py-1.5 text-slate-600">Thanh toán định kỳ hàng tháng</td>
                  </tr>
                </tbody>
              </table>
            </div>

            <p>
              2.2. <strong>Tiền đặt cọc bảo đảm:</strong> Bên B đặt cọc cho Bên A số tiền{" "}
              <strong className="text-slate-900">{formatVnd(detail.depositAmount)}</strong>. Mọi giao dịch nhận cọc,
              khấu trừ thiệt hại/nợ phí (nếu có chứng từ) và hoàn trả được ghi nhận công khai trên{" "}
              <strong>Sổ cái tiền cọc HomeMatch</strong> và hoàn trả phần còn lại khi hoàn tất bàn giao phòng.
            </p>
          </div>

          {/* Article 3 & 4 */}
          <div className="space-y-1 text-xs print:text-[10.5px] leading-relaxed break-inside-avoid">
            <h3 className="font-extrabold text-blue-700 uppercase">ĐIỀU 3: QUYỀN VÀ NGHĨA VỤ CỦA HAI BÊN</h3>
            <p>
              3.1. <strong>Bên A (Cho thuê):</strong> Bàn giao phòng và trang thiết bị đúng thỏa thuận; bảo đảm quyền sử
              dụng ổn định cho Bên B; hỗ trợ cung cấp giấy tờ để Bên B đăng ký tạm trú theo Luật Cư trú 2020; kịp thời
              sửa chữa hư hỏng kết cấu chính không do lỗi của Bên B.
            </p>
            <p>
              3.2. <strong>Bên B (Thuê):</strong> Thanh toán đủ tiền thuê và phí dịch vụ đúng hạn; giữ gìn tài sản, tuân
              thủ quy định PCCC, an ninh trật tự và số người ở tối đa; không tự ý cải tạo kết cấu, không cho thuê lại
              hoặc chuyển nhượng hợp đồng khi chưa có sự đồng ý bằng văn bản của Bên A.
            </p>
          </div>

          {/* Article 4: Termination & Disputes */}
          <div className="space-y-1 text-xs print:text-[10.5px] leading-relaxed break-inside-avoid">
            <h3 className="font-extrabold text-blue-700 uppercase">
              ĐIỀU 4: CHẤM DỨT TRƯỚC HẠN, GIẢI QUYẾT TRANH CHẤP & HIỆU LỰC
            </h3>
            <p>
              4.1. <strong>Đơn phương chấm dứt hợp đồng (Điều 172 Luật Nhà ở 2023):</strong> Bên muốn chấm dứt trước hạn
              phải thông báo trước tối thiểu <strong>30 ngày</strong> (trừ trường hợp vi phạm nghiêm trọng). Nếu Bên B tự
              ý bỏ phòng trái thỏa thuận sẽ bị xử lý tiền cọc; nếu Bên A chấm dứt trái thỏa thuận phải hoàn cọc và bồi
              thường tương ứng theo pháp luật.
            </p>
            <p>
              4.2. <strong>Tranh chấp & Hiệu lực điện tử:</strong> Mọi tranh chấp ưu tiên giải quyết bằng thương lượng;
              nếu không đạt được thỏa thuận sẽ đưa ra Tòa án nhân dân có thẩm quyền giải quyết theo pháp luật Việt Nam.
              Hợp đồng điện tử này được xác lập theo <strong>Luật Giao dịch điện tử số 20/2023/QH15</strong>, có giá trị
              pháp lý tương đương bản giấy kể từ thời điểm cả hai bên hoàn tất xác thực OTP và ký tên điện tử.
            </p>
            {snapshot.terms && (
              <p className="p-2 rounded-lg bg-blue-50/50 border border-blue-100 print:border-slate-300 text-[10.5px] text-slate-700">
                <strong>4.3. Thỏa thuận bổ sung:</strong> {snapshot.terms}
              </p>
            )}
          </div>

          {/* Signature Stamps & Footer */}
          <div className="space-y-3 print:space-y-2 pt-1 break-inside-avoid">
            <div className="grid grid-cols-1 sm:grid-cols-2 print:grid-cols-2 gap-6 pt-2.5 border-t border-slate-200 items-stretch">
              <SignatureStamp
                heading="ĐẠI DIỆN BÊN A (CHO THUÊ)"
                sub="(Ký số & ghi rõ họ tên)"
                name={landlordName}
                signedAt={detail.landlordSignedAt}
                signatureData={detail.landlordSignatureData}
                certCode={`HM-L-${detail.id}-${detail.landlordId}`}
              />
              <SignatureStamp
                heading="ĐẠI DIỆN BÊN B (NGƯỜI THUÊ)"
                sub="(Ký số & ghi rõ họ tên)"
                name={tenantName}
                signedAt={detail.tenantSignedAt}
                signatureData={detail.tenantSignatureData}
                certCode={`HM-T-${detail.id}-${detail.tenantId}`}
              />
            </div>

            <div className="pt-2 border-t border-slate-200 flex flex-col sm:flex-row print:flex-row sm:items-center print:items-center justify-between gap-2 text-[9.5px] text-slate-500">
              <div className="flex items-center gap-1.5 text-emerald-700 font-semibold">
                <ShieldCheck className="w-3.5 h-3.5 shrink-0" />
                <span>Xác thực toàn vẹn trên hệ thống HomeMatch · Luật Giao dịch điện tử 2023</span>
              </div>
              {detail.signedDocumentHash && (
                <button
                  type="button"
                  onClick={() => onCopyHash(detail.signedDocumentHash!)}
                  className="inline-flex items-center gap-1 font-mono bg-slate-100 hover:bg-slate-200 px-2 py-0.5 rounded text-slate-600 print:bg-transparent print:px-0"
                  title="Sao chép mã SHA-256"
                >
                  <span>
                    SHA-256:{" "}
                    {detail.signedDocumentHash.length > 24
                      ? `${detail.signedDocumentHash.slice(0, 24)}...`
                      : detail.signedDocumentHash}
                  </span>
                  <Copy className="w-3 h-3 shrink-0 print:hidden" />
                </button>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function SignatureStamp({
  heading,
  sub,
  name,
  signedAt,
  signatureData,
  certCode,
}: {
  heading: string;
  sub: string;
  name: string;
  signedAt?: string | null;
  signatureData?: string | null;
  certCode: string;
}) {
  const signed = Boolean(signedAt);
  return (
    <div className="text-center flex flex-col gap-1.5 h-full">
      <div>
        <div className="text-[11px] font-extrabold text-slate-900 uppercase">{heading}</div>
        <div className="text-[10px] italic text-slate-500">{sub}</div>
      </div>

      {signed ? (
        <div className="flex-1 min-h-[124px] print:min-h-[104px] p-2.5 print:p-2 rounded-xl bg-emerald-50 border border-emerald-300 text-emerald-800 flex flex-col items-center justify-center gap-0.5">
          {signatureData ? (
            <img
              src={signatureData}
              alt={`Chữ ký ${name}`}
              className="h-11 print:h-9 mx-auto object-contain select-none"
            />
          ) : (
            <CheckCircle2 className="w-4 h-4 text-emerald-600 mx-auto" />
          )}
          <div className="text-[10px] font-black tracking-wide uppercase inline-flex items-center justify-center gap-1">
            <CheckCircle2 className="w-3 h-3 text-emerald-600" />
            <span>ĐÃ KÝ SỐ ĐIỆN TỬ</span>
          </div>
          <div className="text-xs font-bold text-slate-900">{name}</div>
          <div className="text-[9px] text-emerald-700 font-mono">
            Timestamp: {formatDateTime(signedAt)} · Cert: {certCode}
          </div>
        </div>
      ) : (
        <div className="flex-1 min-h-[124px] print:min-h-[104px] p-3 print:p-2 rounded-xl border-2 border-dashed border-slate-300 bg-blue-50/40 text-blue-700 flex flex-col items-center justify-center gap-1">
          <Clock className="w-4 h-4 text-blue-500 mx-auto" />
          <div className="text-[11px] font-bold">Chờ xác thực OTP</div>
          <div className="text-[11px] text-slate-700 font-semibold">{name}</div>
        </div>
      )}
    </div>
  );
}
