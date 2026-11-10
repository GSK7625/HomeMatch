import {
  PropertyListing,
  ViewingAppointment,
  LeaseContract,
  MonthlyBill,
  ChatMessage,
  ChatConversation,
  SystemReport,
} from "@/types";

const STORAGE_KEYS = {
  LISTINGS: "homematch_listings",
  FAVORITES: "homematch_favorites",
  COMPARE: "homematch_compare",
  VIEWINGS: "homematch_viewings",
  CONTRACTS: "homematch_contracts",
  BILLS: "homematch_bills",
  CONVERSATIONS: "homematch_conversations",
  MESSAGES: "homematch_messages",
  REPORTS: "homematch_reports",
  RECENT_VIEWED: "homematch_recent_viewed",
};

// Dọn dẹp triệt để các dữ liệu mock cũ còn sót lại trong localStorage của người dùng
if (typeof window !== "undefined") {
  try {
    const rawListings = localStorage.getItem(STORAGE_KEYS.LISTINGS);
    if (rawListings && (rawListings.includes("lst-001") || rawListings.includes("lst-002"))) {
      localStorage.removeItem(STORAGE_KEYS.LISTINGS);
      localStorage.removeItem(STORAGE_KEYS.VIEWINGS);
      localStorage.removeItem(STORAGE_KEYS.CONTRACTS);
      localStorage.removeItem(STORAGE_KEYS.BILLS);
      localStorage.removeItem(STORAGE_KEYS.CONVERSATIONS);
      localStorage.removeItem(STORAGE_KEYS.MESSAGES);
      localStorage.removeItem(STORAGE_KEYS.REPORTS);
    }
  } catch {
    // Ignore storage errors in restricted iframe/private mode
  }
}

function getItem<T>(key: string, defaultValue: T): T {
  if (typeof window === "undefined") return defaultValue;
  try {
    const raw = localStorage.getItem(key);
    return raw ? JSON.parse(raw) : defaultValue;
  } catch {
    return defaultValue;
  }
}

function setItem<T>(key: string, value: T): void {
  if (typeof window === "undefined") return;
  try {
    localStorage.setItem(key, JSON.stringify(value));
  } catch (e) {
    console.error("Failed to save to localStorage", e);
  }
}

export const listingService = {
  getAll: (): PropertyListing[] => {
    return getItem<PropertyListing[]>(STORAGE_KEYS.LISTINGS, []);
  },

  getById: (id: string): PropertyListing | undefined => {
    const list = listingService.getAll();
    return list.find((item) => item.id === id);
  },

  create: (item: Omit<PropertyListing, "id" | "createdAt" | "viewCount">): PropertyListing => {
    const list = listingService.getAll();
    const newItem: PropertyListing = {
      ...item,
      id: `lst-${Date.now()}`,
      createdAt: new Date().toISOString(),
      viewCount: 0,
    };
    const updated = [newItem, ...list];
    setItem(STORAGE_KEYS.LISTINGS, updated);
    return newItem;
  },

  update: (id: string, updates: Partial<PropertyListing>): PropertyListing | null => {
    const list = listingService.getAll();
    const index = list.findIndex((item) => item.id === id);
    if (index === -1) return null;
    list[index] = { ...list[index], ...updates };
    setItem(STORAGE_KEYS.LISTINGS, list);
    return list[index];
  },

  delete: (id: string): boolean => {
    const list = listingService.getAll();
    const filtered = list.filter((item) => item.id !== id);
    setItem(STORAGE_KEYS.LISTINGS, filtered);
    return true;
  },

  incrementView: (id: string): void => {
    const list = listingService.getAll();
    const item = list.find((x) => x.id === id);
    if (item) {
      item.viewCount = (item.viewCount || 0) + 1;
      setItem(STORAGE_KEYS.LISTINGS, list);
      listingService.addRecent(id);
    }
  },

  // Recent viewed
  getRecentIds: (): string[] => getItem<string[]>(STORAGE_KEYS.RECENT_VIEWED, []),
  addRecent: (id: string): void => {
    const recents = getItem<string[]>(STORAGE_KEYS.RECENT_VIEWED, []).filter((x) => x !== id);
    setItem(STORAGE_KEYS.RECENT_VIEWED, [id, ...recents].slice(0, 10));
  },

  // Favorites
  getFavorites: (): string[] => {
    return getItem<string[]>(STORAGE_KEYS.FAVORITES, []);
  },
  toggleFavorite: (id: string): boolean => {
    const favs = listingService.getFavorites();
    const exists = favs.includes(id);
    const updated = exists ? favs.filter((x) => x !== id) : [...favs, id];
    setItem(STORAGE_KEYS.FAVORITES, updated);
    return !exists;
  },
  isFavorite: (id: string): boolean => listingService.getFavorites().includes(id),

  // Comparison (max 3 items as per Section 4.1)
  getCompareList: (): string[] => {
    return getItem<string[]>(STORAGE_KEYS.COMPARE, []);
  },
  addToCompare: (id: string): { success: boolean; message: string } => {
    const current = listingService.getCompareList();
    if (current.includes(id)) {
      return { success: false, message: "Phòng này đã có trong danh sách so sánh!" };
    }
    if (current.length >= 3) {
      return { success: false, message: "Chỉ được so sánh tối đa 3 phòng cùng lúc theo quy định!" };
    }
    setItem(STORAGE_KEYS.COMPARE, [...current, id]);
    return { success: true, message: "Đã thêm vào danh sách so sánh." };
  },
  removeFromCompare: (id: string): void => {
    const current = listingService.getCompareList().filter((x) => x !== id);
    setItem(STORAGE_KEYS.COMPARE, current);
  },
  clearCompare: (): void => setItem(STORAGE_KEYS.COMPARE, []),
};

export const viewingService = {
  getAll: (): ViewingAppointment[] => {
    return getItem<ViewingAppointment[]>(STORAGE_KEYS.VIEWINGS, []);
  },

  book: (appointment: Omit<ViewingAppointment, "id" | "status" | "createdAt">): ViewingAppointment => {
    const list = viewingService.getAll();
    const newItem: ViewingAppointment = {
      ...appointment,
      id: `vw-${Date.now()}`,
      status: "Chờ xác nhận",
      createdAt: new Date().toISOString(),
    };
    setItem(STORAGE_KEYS.VIEWINGS, [newItem, ...list]);
    return newItem;
  },

  updateStatus: (id: string, status: ViewingAppointment["status"]): boolean => {
    const list = viewingService.getAll();
    const item = list.find((x) => x.id === id);
    if (!item) return false;
    item.status = status;
    setItem(STORAGE_KEYS.VIEWINGS, list);
    return true;
  },
};

export const leasingService = {
  getAll: (): LeaseContract[] => {
    return getItem<LeaseContract[]>(STORAGE_KEYS.CONTRACTS, []);
  },

  create: (contract: Omit<LeaseContract, "id" | "status" | "daysRemaining">): LeaseContract => {
    const list = leasingService.getAll();
    const end = new Date(contract.endDate).getTime();
    const now = new Date().getTime();
    const daysRemaining = Math.max(0, Math.ceil((end - now) / (1000 * 60 * 60 * 24)));
    const status: LeaseContract["status"] = daysRemaining <= 30 ? "Sắp hết hạn" : "Còn hiệu lực";

    const newItem: LeaseContract = {
      ...contract,
      id: `ctr-${Date.now()}`,
      status,
      daysRemaining,
    };
    setItem(STORAGE_KEYS.CONTRACTS, [newItem, ...list]);
    return newItem;
  },
};

export const billingService = {
  getAll: (): MonthlyBill[] => {
    return getItem<MonthlyBill[]>(STORAGE_KEYS.BILLS, []);
  },

  createFromReading: (params: {
    contractId: string;
    roomName: string;
    tenantName: string;
    period: string;
    rentAmount: number;
    electricityUsage: number;
    electricityUnitPrice: number;
    waterUsage: number;
    waterUnitPrice: number;
    serviceCost: number;
    dueDate: string;
  }): MonthlyBill => {
    const electricityCost = params.electricityUsage * params.electricityUnitPrice;
    const waterCost = params.waterUsage * params.waterUnitPrice;
    const totalDue = params.rentAmount + electricityCost + waterCost + params.serviceCost;
    const paidAmount = 0;
    const remainingDebt = totalDue - paidAmount;

    const newBill: MonthlyBill = {
      id: `bil-${Date.now()}`,
      contractId: params.contractId,
      roomName: params.roomName,
      tenantName: params.tenantName,
      period: params.period,
      rentAmount: params.rentAmount,
      electricityUsage: params.electricityUsage,
      electricityCost,
      waterUsage: params.waterUsage,
      waterCost,
      serviceCost: params.serviceCost,
      totalDue,
      paidAmount,
      remainingDebt,
      dueDate: params.dueDate,
      status: "Chưa thanh toán",
    };

    const list = billingService.getAll();
    setItem(STORAGE_KEYS.BILLS, [newBill, ...list]);
    return newBill;
  },

  payBill: (id: string, amount: number): boolean => {
    const list = billingService.getAll();
    const bill = list.find((b) => b.id === id);
    if (!bill) return false;

    bill.paidAmount = Math.min(bill.totalDue, bill.paidAmount + amount);
    bill.remainingDebt = bill.totalDue - bill.paidAmount;
    bill.status = bill.remainingDebt === 0 ? "Đã thanh toán" : "Thanh toán một phần";

    setItem(STORAGE_KEYS.BILLS, list);
    return true;
  },
};

export const messagingService = {
  getConversations: (): ChatConversation[] => {
    return getItem<ChatConversation[]>(STORAGE_KEYS.CONVERSATIONS, []);
  },

  getMessages: (conversationId: string): ChatMessage[] => {
    const map = getItem<Record<string, ChatMessage[]>>(STORAGE_KEYS.MESSAGES, {});
    return map[conversationId] || [];
  },

  sendMessage: (
    conversationId: string,
    content: string,
    senderId: string,
    senderName: string,
    receiverId: string
  ): ChatMessage => {
    const map = getItem<Record<string, ChatMessage[]>>(STORAGE_KEYS.MESSAGES, {});
    const msgs = map[conversationId] || [];
    const newMsg: ChatMessage = {
      id: `msg-${Date.now()}`,
      senderId,
      senderName,
      receiverId,
      content,
      timestamp: new Date().toISOString(),
      isRead: true,
    };
    map[conversationId] = [...msgs, newMsg];
    setItem(STORAGE_KEYS.MESSAGES, map);

    const convs = messagingService.getConversations();
    const c = convs.find((x) => x.id === conversationId);
    if (c) {
      c.lastMessage = content;
      c.lastMessageTime = newMsg.timestamp;
      setItem(STORAGE_KEYS.CONVERSATIONS, convs);
    }
    return newMsg;
  },
};

export const adminService = {
  getReports: (): SystemReport[] => {
    return getItem<SystemReport[]>(STORAGE_KEYS.REPORTS, []);
  },

  resolveReport: (id: string, status: "Đã xử lý" | "Đã bác bỏ"): void => {
    const list = adminService.getReports();
    const item = list.find((x) => x.id === id);
    if (item) {
      item.status = status;
      setItem(STORAGE_KEYS.REPORTS, list);
    }
  },
};
