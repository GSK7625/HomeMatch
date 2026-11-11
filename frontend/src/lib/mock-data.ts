import {
  PropertyListing,
  ViewingAppointment,
  LeaseContract,
  MonthlyBill,
  ChatMessage,
  ChatConversation,
  SystemReport,
} from "@/types";

// Toàn bộ dữ liệu mock đã được gỡ bỏ để đưa hệ thống vào hoạt động thật với API backend.
export const INITIAL_LISTINGS: PropertyListing[] = [];
export const INITIAL_VIEWINGS: ViewingAppointment[] = [];
export const INITIAL_CONTRACTS: LeaseContract[] = [];
export const INITIAL_BILLS: MonthlyBill[] = [];
export const INITIAL_CONVERSATIONS: ChatConversation[] = [];
export const INITIAL_MESSAGES: Record<string, ChatMessage[]> = {};
export const INITIAL_REPORTS: SystemReport[] = [];
