const COMPARE_SESSION_KEY = "homematch_compare_room_ids";

function readIds(): string[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = window.sessionStorage.getItem(COMPARE_SESSION_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed)
      ? parsed.map(String).filter((id) => /^\d+$/.test(id)).slice(0, 3)
      : [];
  } catch {
    return [];
  }
}

function writeIds(ids: string[]): void {
  if (typeof window === "undefined") return;
  try {
    window.sessionStorage.setItem(COMPARE_SESSION_KEY, JSON.stringify(ids));
  } catch {
    // Ignore storage errors in restricted environments
  }
  window.dispatchEvent(new CustomEvent("homematch:compare-updated", { detail: { ids } }));
}

export const compareSelection = {
  getIds: (): string[] => readIds(),

  add: (roomId: string | number): { success: boolean; message: string } => {
    const normalizedId = String(roomId);
    if (!/^\d+$/.test(normalizedId) || Number(normalizedId) <= 0) {
      return { success: false, message: "Phòng này không hợp lệ để so sánh." };
    }
    const current = readIds();
    if (current.includes(normalizedId)) {
      return { success: false, message: "Phòng này đã có trong danh sách so sánh!" };
    }
    if (current.length >= 3) {
      return { success: false, message: "Chỉ được so sánh tối đa 3 phòng cùng lúc theo quy định!" };
    }
    writeIds([...current, normalizedId]);
    return { success: true, message: "Đã thêm vào danh sách so sánh." };
  },

  remove: (roomId: string | number): void => {
    const normalizedId = String(roomId);
    writeIds(readIds().filter((x) => x !== normalizedId));
  },

  clear: (): void => {
    writeIds([]);
  },
};
