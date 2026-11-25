"use client";

import { forwardRef, useEffect, useImperativeHandle, useState } from "react";
import { listingsApi } from "@/lib/api-client";
import { ListingMedia } from "./ListingMedia";

type UploadResult = { status: "uploading" | "success" | "error"; message?: string };

export interface ListingMediaUploaderHandle {
  validate: () => boolean;
  upload: (listingId: number) => Promise<boolean>;
}

export const ListingMediaUploader = forwardRef<ListingMediaUploaderHandle, {
  listingId?: number;
  nextSortOrder?: number;
  onUploaded?: () => Promise<void>;
  onBusyChange?: (busy: boolean) => void;
  deferred?: boolean;
  disabled?: boolean;
}>(function ListingMediaUploader({ listingId, nextSortOrder = 0, onUploaded, onBusyChange, deferred = false, disabled = false }, ref) {
  const [files, setFiles] = useState<File[]>([]);
  const [previews, setPreviews] = useState<string[]>([]);
  const [results, setResults] = useState<Record<number, UploadResult>>({});
  const [busy, setBusy] = useState(false);
  const [refreshError, setRefreshError] = useState("");
  const [selectionError, setSelectionError] = useState("");

  useEffect(() => {
    const urls = files.map(file => URL.createObjectURL(file));
    setPreviews(urls);
    return () => urls.forEach(url => URL.revokeObjectURL(url));
  }, [files]);

  const fileError = (file: File) => {
    const video = /\.mp4$/i.test(file.name);
    return !/\.(jpe?g|png|webp|mp4)$/i.test(file.name) || file.size === 0
      || file.size > (video ? 50 : 5) * 1024 * 1024
      ? "Chọn ảnh JPG/PNG/WebP ≤ 5 MB hoặc video MP4 ≤ 50 MB." : null;
  };

  const validate = () => {
    if (files.length === 0) {
      setSelectionError("Chọn ít nhất một ảnh hoặc video cho tin đăng.");
      return false;
    }
    let valid = true;
    files.forEach((file, index) => {
      const message = fileError(file);
      if (message) {
        valid = false;
        setResults(previous => ({ ...previous, [index]: { status: "error", message } }));
      }
    });
    return valid;
  };

  const upload = async (targetId = listingId, queue = files): Promise<boolean> => {
    if (busy || !targetId || queue.length === 0) return false;
    setBusy(true);
    onBusyChange?.(true);
    setRefreshError("");
    let uploaded = false;
    let allSuccessful = true;
    const coverIndex = queue.findIndex(file => /\.(jpe?g|png|webp)$/i.test(file.name));
    const failedFiles: File[] = [];
    const failedResults: Record<number, UploadResult> = {};
    try {
      for (let index = 0; index < queue.length; index++) {
        if (results[index]?.status === "success") continue;
        const file = queue[index];
        const message = fileError(file);
        let result: UploadResult;
        if (message) {
          result = { status: "error", message };
        } else {
          setResults(previous => ({ ...previous, [index]: { status: "uploading" } }));
          try {
            const response = await listingsApi.uploadMedia(targetId, file, nextSortOrder + index, deferred && index === coverIndex);
            if (response.isSuccess) {
              uploaded = true;
              result = { status: "success" };
            } else {
              result = { status: "error", message: response.error?.message || "Upload thất bại. Vui lòng thử lại." };
            }
          } catch {
            result = { status: "error", message: "Không thể kết nối. Vui lòng thử lại." };
          }
        }
        if (result.status === "error") {
          allSuccessful = false;
          failedResults[failedFiles.length] = result;
          failedFiles.push(file);
        }
        setResults(previous => ({ ...previous, [index]: result }));
      }
      if (uploaded) {
        try { await onUploaded?.(); }
        catch { setRefreshError("File đã tải lên. Đóng và mở lại danh sách để xem media mới."); }
      }
      if (!deferred) {
        setFiles(failedFiles);
        setResults(failedResults);
      }
      return allSuccessful;
    } finally {
      setBusy(false);
      onBusyChange?.(false);
    }
  };

  useImperativeHandle(ref, () => ({ validate, upload }));

  return (
    <section className="space-y-3 border-t border-slate-200 pt-4">
      <label className="block text-xs font-bold text-slate-800">
        {deferred && <span className="block">Ảnh và video của tin đăng *</span>}
        <input type="file" multiple accept=".jpg,.jpeg,.png,.webp,.mp4,image/jpeg,image/png,image/webp,video/mp4"
          disabled={busy || disabled} className="peer sr-only"
          onChange={event => {
            if (busy || disabled) return;
            const incoming = Array.from(event.target.files || []);
            const selected = [...files, ...incoming.filter(file => !files.some(existing =>
              existing.name === file.name && existing.size === file.size && existing.lastModified === file.lastModified))];
            setFiles(selected);
            setRefreshError(""); setSelectionError(""); event.target.value = "";
            if (!deferred) void upload(listingId, selected);
          }} />
        <span className={`mt-2 inline-flex rounded-xl border border-blue-200 px-4 py-2 text-xs font-semibold text-blue-700 peer-focus-visible:ring-2 peer-focus-visible:ring-blue-500 ${busy || disabled ? "opacity-50 cursor-not-allowed" : "cursor-pointer hover:bg-blue-50"}`}>
          Chọn ảnh/video
        </span>
      </label>
      <p className="text-xs text-slate-500">Ảnh tối đa 5 MB/file · Video MP4 tối đa 50 MB/file.</p>
      {!deferred && busy && <p role="status" className="text-xs text-blue-700">Đang thêm ảnh/video vào danh sách…</p>}
      {deferred && <p className="text-xs text-slate-500">Ảnh đầu tiên sẽ là ảnh bìa. File được tải lên khi bạn lưu bản nháp.</p>}
      {selectionError && <p role="alert" className="text-xs text-rose-600">{selectionError}</p>}
      {(deferred || files.some((_, index) => results[index]?.status === "error")) && <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
        {files.map((file, index) => (!deferred && results[index]?.status !== "error" ? null : (
          <div key={`${file.name}-${index}`} className="rounded-xl border border-slate-200 p-2 space-y-1">
            {previews[index] && <ListingMedia media={{ imageUrl: previews[index], mediaType: /\.mp4$/i.test(file.name) ? "VIDEO" : "IMAGE" }} className="w-full h-24 object-contain bg-slate-100 rounded-lg" />}
            <p className="text-xs font-medium">{/\.mp4$/i.test(file.name) ? "Video" : "Ảnh"} {index + 1}</p>
            <p className="text-[11px] text-slate-500">{(file.size / 1024 / 1024).toFixed(1)} MB</p>
            {results[index]?.status !== "success" && <button type="button" disabled={busy || disabled}
              className="text-xs font-semibold text-rose-600 disabled:opacity-50"
              onClick={() => {
                setFiles(previous => previous.filter((_, position) => position !== index));
                setResults(previous => Object.fromEntries(Object.entries(previous)
                  .filter(([position]) => Number(position) !== index)
                  .map(([position, result]) => [Number(position) > index ? Number(position) - 1 : Number(position), result])));
                setSelectionError("");
              }}>Bỏ file</button>}
            {results[index] && <p role="status" className={`text-xs ${results[index].status === "error" ? "text-rose-600" : "text-emerald-700"}`}>
              {results[index].status === "uploading" ? "Đang tải lên…" : results[index].status === "success" ? "Đã tải lên" : results[index].message}
            </p>}
          </div>
        )))}
      </div>}
      {refreshError && <p role="alert" className="text-xs text-amber-700">{refreshError}</p>}
      {!deferred && files.some((_, index) => results[index]?.status === "error") && <button type="button" onClick={() => upload()}
        disabled={busy || disabled}
        className="rounded-xl bg-blue-600 text-white px-4 py-2 text-xs font-bold disabled:opacity-50">
        {busy ? "Đang thử lại…" : "Thử lại file lỗi"}
      </button>}
    </section>
  );
});
