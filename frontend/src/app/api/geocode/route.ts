import { NextRequest, NextResponse } from "next/server";

export async function GET(req: NextRequest) {
  const { searchParams } = new URL(req.url);
  const q = searchParams.get("q");

  if (!q || !q.trim()) {
    return NextResponse.json(
      { error: "Tham số tìm kiếm 'q' là bắt buộc." },
      { status: 400 }
    );
  }

  try {
    const endpoint = `https://nominatim.openstreetmap.org/search?format=json&countrycodes=vn&limit=5&q=${encodeURIComponent(
      q.trim()
    )}`;

    const response = await fetch(endpoint, {
      headers: {
        "User-Agent": "HomeMatch-Platform/1.0 (contact@homematch.vn)",
        "Accept-Language": "vi,en;q=0.8",
      },
      next: { revalidate: 3600 },
    });

    if (!response.ok) {
      return NextResponse.json(
        { error: "Lỗi kết nối đến dịch vụ bản đồ OpenStreetMap" },
        { status: response.status }
      );
    }

    const data = await response.json();
    return NextResponse.json(data);
  } catch (error) {
    console.error("Geocoding proxy error:", error);
    return NextResponse.json(
      { error: "Không thể kết nối đến máy chủ bản đồ." },
      { status: 502 }
    );
  }
}
