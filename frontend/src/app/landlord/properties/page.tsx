"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { listingService } from "@/lib/services";
import { PropertyListing, RoomStatus } from "@/types";
import { useToast } from "@/components/Toast";
import { Modal } from "@/components/Modal";
import {
  Home,
  Plus,
  Edit2,
  Trash2,
  MapPin,
  Maximize2,
  Zap,
  Droplet,
  CheckCircle2,
  Eye,
  ArrowRight,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function LandlordPropertiesPage() {
  const { showToast } = useToast();
  const [rooms, setRooms] = useState<PropertyListing[]>([]);
  const [editingRoom, setEditingRoom] = useState<PropertyListing | null>(null);

  const loadData = () => {
    setRooms(listingService.getAll());
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleStatusChange = (id: string, newStatus: RoomStatus) => {
    listingService.update(id, { status: newStatus });
    loadData();
    showToast(`Đã chuyển trạng thái phòng sang "${newStatus}"!`, "success");
  };

  const handleSaveEdit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingRoom) return;

    listingService.update(editingRoom.id, {
      title: editingRoom.title,
      price: Number(editingRoom.price),
      deposit: Number(editingRoom.deposit),
      area: Number(editingRoom.area),
      electricityPrice: Number(editingRoom.electricityPrice),
      waterPrice: Number(editingRoom.waterPrice),
      serviceFee: Number(editingRoom.serviceFee),
    });

    setEditingRoom(null);
    loadData();
    showToast("Cập nhật thông tin phòng thành công!", "success");
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full flex-1 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div>
          <div className="flex items-center gap-2 text-blue-600 text-xs font-bold uppercase tracking-wider mb-1">
            <Home className="w-4 h-4" />
            <span>Nghiệp vụ căn / phòng (Module 3.6)</span>
          </div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Quản Lý Danh Sách Căn & Phòng Cho Thuê
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Xem phòng và chuyển đổi trạng thái: Còn trống, Đã thuê hoặc Tạm ngừng.
          </p>
        </div>

        <Link
          href="/landlord/listings"
          className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold shadow-md shadow-blue-500/20"
        >
          <Plus className="w-4 h-4" />
          <span>Tạo tin đăng phòng mới</span>
        </Link>
      </div>

      {/* Table of rooms */}
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-x-auto">
        <table className="w-full text-xs text-left border-collapse min-w-[800px]">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50/70 text-slate-500 uppercase tracking-wider">
              <th className="p-4">Hình ảnh & Tên phòng</th>
              <th className="p-4">Khu vực</th>
              <th className="p-4">Giá thuê / tháng</th>
              <th className="p-4">Biểu phí điện / nước</th>
              <th className="p-4">Trạng thái hiện tại</th>
              <th className="p-4 text-right">Thao tác</th>
            </tr>
          </thead>

          <tbody className="divide-y divide-slate-100">
            {rooms.map((room) => (
              <tr key={room.id} className="hover:bg-slate-50/60 transition-colors">
                {/* Photo & Title */}
                <td className="p-4">
                  <div className="flex items-center gap-3">
                    <img
                      src={room.images[0]}
                      alt={room.title}
                      className="w-14 h-10 rounded-lg object-cover bg-slate-100 shrink-0"
                    />
                    <div className="max-w-xs">
                      <h4 className="font-bold text-slate-900 line-clamp-1">{room.title}</h4>
                      <span className="text-[11px] text-slate-400">
                        {room.roomType} • {room.area} m²
                      </span>
                    </div>
                  </div>
                </td>

                {/* Location */}
                <td className="p-4 font-medium text-slate-700">
                  {room.district}, {room.city}
                </td>

                {/* Price */}
                <td className="p-4 font-black text-blue-600 text-sm">
                  {room.price.toLocaleString()} đ
                </td>

                {/* Rates */}
                <td className="p-4 text-slate-600 space-y-0.5 text-[11px]">
                  <div>Điện: {room.electricityPrice.toLocaleString()} đ/kWh</div>
                  <div>Nước: {room.waterPrice.toLocaleString()} đ/m³</div>
                </td>

                {/* Status Toggle (Section 3.6) */}
                <td className="p-4">
                  <select
                    value={room.status}
                    onChange={(e) => handleStatusChange(room.id, e.target.value as RoomStatus)}
                    className={cn(
                      "px-3 py-1.5 rounded-xl text-xs font-bold border focus:outline-none cursor-pointer",
                      room.status === "Còn trống" && "bg-emerald-50 text-emerald-700 border-emerald-300",
                      room.status === "Đã thuê" && "bg-slate-100 text-slate-700 border-slate-300",
                      room.status === "Tạm ngừng" && "bg-rose-50 text-rose-700 border-rose-300"
                    )}
                  >
                    <option value="Còn trống">Còn trống</option>
                    <option value="Đã thuê">Đã thuê</option>
                    <option value="Tạm ngừng">Tạm ngừng</option>
                  </select>
                </td>

                {/* Actions */}
                <td className="p-4 text-right">
                  <div className="flex items-center justify-end gap-2">
                    <button
                      onClick={() => setEditingRoom(room)}
                      className="p-1.5 text-slate-500 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors"
                      title="Chỉnh sửa thông số"
                    >
                      <Edit2 className="w-4 h-4" />
                    </button>

                    <Link
                      href={`/listings/${room.id}`}
                      className="p-1.5 text-slate-500 hover:text-slate-800 hover:bg-slate-100 rounded-lg transition-colors"
                      title="Xem trang công khai"
                    >
                      <Eye className="w-4 h-4" />
                    </Link>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Edit Room Modal */}
      {editingRoom && (
        <Modal
          isOpen={true}
          onClose={() => setEditingRoom(null)}
          title="Chỉnh Sửa Thông Số Phòng Cho Thuê"
          maxWidth="lg"
        >
          <form onSubmit={handleSaveEdit} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                Tiêu đề phòng
              </label>
              <input
                type="text"
                required
                value={editingRoom.title}
                onChange={(e) => setEditingRoom({ ...editingRoom, title: e.target.value })}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
              />
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Giá thuê (VNĐ)
                </label>
                <input
                  type="number"
                  required
                  value={editingRoom.price}
                  onChange={(e) => setEditingRoom({ ...editingRoom, price: Number(e.target.value) })}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Tiền cọc (VNĐ)
                </label>
                <input
                  type="number"
                  required
                  value={editingRoom.deposit}
                  onChange={(e) => setEditingRoom({ ...editingRoom, deposit: Number(e.target.value) })}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Diện tích (m²)
                </label>
                <input
                  type="number"
                  required
                  value={editingRoom.area}
                  onChange={(e) => setEditingRoom({ ...editingRoom, area: Number(e.target.value) })}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
                />
              </div>
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Đơn giá điện (đ/kWh)
                </label>
                <input
                  type="number"
                  required
                  value={editingRoom.electricityPrice}
                  onChange={(e) => setEditingRoom({ ...editingRoom, electricityPrice: Number(e.target.value) })}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Đơn giá nước (đ/m³)
                </label>
                <input
                  type="number"
                  required
                  value={editingRoom.waterPrice}
                  onChange={(e) => setEditingRoom({ ...editingRoom, waterPrice: Number(e.target.value) })}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                  Phí dịch vụ (đ/tháng)
                </label>
                <input
                  type="number"
                  required
                  value={editingRoom.serviceFee}
                  onChange={(e) => setEditingRoom({ ...editingRoom, serviceFee: Number(e.target.value) })}
                  className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200"
                />
              </div>
            </div>

            <div className="flex gap-3 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setEditingRoom(null)}
                className="flex-1 py-2.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50"
              >
                Hủy
              </button>
              <button
                type="submit"
                className="flex-1 py-2.5 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 shadow-md shadow-blue-500/20"
              >
                Lưu thay đổi
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
