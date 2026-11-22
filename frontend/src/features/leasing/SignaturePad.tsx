"use client";

import React, { useCallback, useEffect, useRef, useState } from "react";
import { cn } from "@/lib/utils";
import { Eraser, PenTool, Type } from "lucide-react";

interface SignaturePadProps {
  onChange: (dataUrl: string | null) => void;
}

const INK_COLORS = [
  { label: "Xanh mực", value: "#1e3a8a" },
  { label: "Đen", value: "#0f172a" },
];

export function SignaturePad({ onChange }: SignaturePadProps) {
  const [mode, setMode] = useState<"draw" | "type">("draw");
  const [inkColor, setInkColor] = useState("#1e3a8a");
  const [typedName, setTypedName] = useState("");
  const [typedPreviewUrl, setTypedPreviewUrl] = useState<string | null>(null);
  const [hasStroke, setHasStroke] = useState(false);

  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const drawingRef = useRef(false);

  const setupCanvas = useCallback(
    (canvas: HTMLCanvasElement) => {
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      const width = 360;
      const height = 115;
      canvas.width = width * dpr;
      canvas.height = height * dpr;
      const ctx = canvas.getContext("2d");
      if (ctx) {
        ctx.scale(dpr, dpr);
        ctx.lineCap = "round";
        ctx.lineJoin = "round";
        ctx.lineWidth = 2.4;
        ctx.strokeStyle = inkColor;
      }
      return ctx;
    },
    [inkColor]
  );

  const clearDraw = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = setupCanvas(canvas);
    ctx?.clearRect(0, 0, canvas.width, canvas.height);
    setHasStroke(false);
    onChange(null);
  }, [setupCanvas, onChange]);

  useEffect(() => {
    if (mode === "draw") {
      clearDraw();
    }
  }, [mode, clearDraw]);

  useEffect(() => {
    const ctx = canvasRef.current?.getContext("2d");
    if (ctx) ctx.strokeStyle = inkColor;
  }, [inkColor]);

  // Render Vietnamese-safe handwriting signature onto offscreen canvas, auto-fitting width cleanly
  useEffect(() => {
    if (mode !== "type") return;
    const text = typedName.trim();
    if (!text) {
      setTypedPreviewUrl(null);
      onChange(null);
      return;
    }

    let cancelled = false;
    const renderSignature = async () => {
      const sigFontFamily =
        getComputedStyle(document.documentElement).getPropertyValue("--font-signature").trim() ||
        '"Dancing Script", Georgia, serif';

      // Ensure Vietnamese webfont is loaded before painting to canvas
      if (document.fonts) {
        try {
          await document.fonts.load(`700 48px ${sigFontFamily}`, text);
        } catch {
          // fallback gracefully
        }
      }
      if (cancelled) return;

      const canvas = document.createElement("canvas");
      const width = 560;
      const height = 150;
      canvas.width = width;
      canvas.height = height;
      const ctx = canvas.getContext("2d");
      if (!ctx) return;

      // Shrink font size smoothly until the entire Vietnamese name fits with generous margins
      let fontSize = 54;
      const maxTextWidth = width - 48;
      while (fontSize > 24) {
        ctx.font = `700 ${fontSize}px ${sigFontFamily}`;
        if (ctx.measureText(text).width <= maxTextWidth) break;
        fontSize -= 2;
      }

      ctx.clearRect(0, 0, width, height);
      ctx.fillStyle = inkColor;
      ctx.textAlign = "center";
      ctx.textBaseline = "middle";
      ctx.font = `700 ${fontSize}px ${sigFontFamily}`;
      ctx.fillText(text, width / 2, height / 2);

      const dataUrl = canvas.toDataURL("image/png");
      setTypedPreviewUrl(dataUrl);
      onChange(dataUrl);
    };

    renderSignature();
    return () => {
      cancelled = true;
    };
  }, [mode, typedName, inkColor, onChange]);

  const getPoint = (e: React.PointerEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current!;
    const rect = canvas.getBoundingClientRect();
    return {
      x: ((e.clientX - rect.left) / rect.width) * 360,
      y: ((e.clientY - rect.top) / rect.height) * 115,
    };
  };

  const handlePointerDown = (e: React.PointerEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    const ctx = canvas?.getContext("2d");
    if (!canvas || !ctx) return;
    canvas.setPointerCapture(e.pointerId);
    drawingRef.current = true;
    const pt = getPoint(e);
    ctx.strokeStyle = inkColor;
    ctx.lineWidth = 2.4;
    ctx.beginPath();
    ctx.moveTo(pt.x, pt.y);
  };

  const handlePointerMove = (e: React.PointerEvent<HTMLCanvasElement>) => {
    if (!drawingRef.current) return;
    const ctx = canvasRef.current?.getContext("2d");
    if (!ctx) return;
    const pt = getPoint(e);
    ctx.lineTo(pt.x, pt.y);
    ctx.stroke();
    if (!hasStroke) setHasStroke(true);
  };

  const handlePointerUp = (e: React.PointerEvent<HTMLCanvasElement>) => {
    if (!drawingRef.current) return;
    drawingRef.current = false;
    const canvas = canvasRef.current;
    if (canvas) {
      canvas.releasePointerCapture(e.pointerId);
      onChange(canvas.toDataURL("image/png"));
    }
  };

  return (
    <div className="space-y-2.5">
      <div className="flex items-center justify-between gap-2">
        <span className="text-[11px] font-bold text-slate-700">Chữ ký tên điện tử:</span>

        <div className="flex items-center gap-1 bg-slate-100 p-0.5 rounded-lg">
          <button
            type="button"
            onClick={() => setMode("draw")}
            className={cn(
              "px-2 py-1 rounded-md text-[10px] font-bold inline-flex items-center gap-1 transition-colors",
              mode === "draw" ? "bg-white text-blue-700 shadow-xs" : "text-slate-500 hover:text-slate-700"
            )}
          >
            <PenTool className="w-3 h-3" />
            <span>Ký tay</span>
          </button>
          <button
            type="button"
            onClick={() => setMode("type")}
            className={cn(
              "px-2 py-1 rounded-md text-[10px] font-bold inline-flex items-center gap-1 transition-colors",
              mode === "type" ? "bg-white text-blue-700 shadow-xs" : "text-slate-500 hover:text-slate-700"
            )}
          >
            <Type className="w-3 h-3" />
            <span>Theo tên</span>
          </button>
        </div>
      </div>

      {mode === "draw" ? (
        <div className="relative rounded-xl border-2 border-dashed border-slate-300 bg-white overflow-hidden">
          <canvas
            ref={canvasRef}
            onPointerDown={handlePointerDown}
            onPointerMove={handlePointerMove}
            onPointerUp={handlePointerUp}
            onPointerCancel={handlePointerUp}
            className="w-full h-[115px] touch-none cursor-crosshair block"
          />
          {!hasStroke && (
            <div className="pointer-events-none absolute inset-0 flex items-center justify-center text-[11px] text-slate-400">
              Dùng chuột hoặc ngón tay ký tên vào khung này
            </div>
          )}
          <div className="px-2.5 py-1.5 border-t border-slate-100 bg-slate-50/70 flex items-center justify-between">
            <div className="flex items-center gap-1.5">
              {INK_COLORS.map((c) => (
                <button
                  key={c.value}
                  type="button"
                  onClick={() => setInkColor(c.value)}
                  title={c.label}
                  className={cn(
                    "w-4 h-4 rounded-full border-2 transition-transform",
                    inkColor === c.value ? "border-blue-500 scale-110" : "border-white shadow-xs"
                  )}
                  style={{ backgroundColor: c.value }}
                />
              ))}
            </div>
            <button
              type="button"
              onClick={clearDraw}
              className="text-[10px] font-bold text-slate-500 hover:text-rose-600 inline-flex items-center gap-1"
            >
              <Eraser className="w-3 h-3" />
              <span>Xóa ký lại</span>
            </button>
          </div>
        </div>
      ) : (
        <div className="space-y-2">
          <div className="flex items-center gap-2">
            <input
              type="text"
              value={typedName}
              onChange={(e) => setTypedName(e.target.value)}
              placeholder="Nhập họ tên để tạo chữ ký..."
              className="flex-1 px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white focus:outline-none focus:border-blue-500"
            />
            <div className="flex items-center gap-1.5 px-2">
              {INK_COLORS.map((c) => (
                <button
                  key={c.value}
                  type="button"
                  onClick={() => setInkColor(c.value)}
                  title={c.label}
                  className={cn(
                    "w-4 h-4 rounded-full border-2 transition-transform",
                    inkColor === c.value ? "border-blue-500 scale-110" : "border-white shadow-xs"
                  )}
                  style={{ backgroundColor: c.value }}
                />
              ))}
            </div>
          </div>

          <div className="h-[92px] rounded-xl border-2 border-dashed border-slate-300 bg-white flex items-center justify-center px-3 overflow-hidden">
            {typedPreviewUrl ? (
              <img
                src={typedPreviewUrl}
                alt="Xem trước chữ ký"
                className="max-h-[76px] max-w-full object-contain select-none"
              />
            ) : (
              <span className="text-[11px] text-slate-400">Chữ ký mẫu sẽ hiển thị tại đây</span>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
