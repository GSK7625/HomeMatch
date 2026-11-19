import { apiRequest } from "@/lib/api-client";

export interface BillingPaymentDto {
  id: number;
  paymentCode: string;
  invoiceId: number;
  amount: number;
  paymentMethod: string;
  gatewayTransactionNo?: string | null;
  bankCode?: string | null;
  cardType?: string | null;
  gatewayResponseCode?: string | null;
  paymentUrl?: string | null;
  status: string;
  expiredAt: string;
  completedAt?: string | null;
  createdAt: string;
}

export interface BillingInvoiceDto {
  id: number;
  invoiceCode: string;
  contractId: number;
  landlordId: number;
  tenantId: number;
  roomId: number;
  propertyId: number;
  billingMonth: number;
  billingYear: number;
  rentAmount: number;
  electricityAmount: number;
  waterAmount: number;
  serviceAmount: number;
  discountAmount: number;
  totalAmount: number;
  paidAmount: number;
  dueDate: string;
  paidAt?: string | null;
  status: string;
  createdAt: string;
}

export interface PagedInvoicesDto {
  items: BillingInvoiceDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface VerifyReturnResultDto {
  isSuccess: boolean;
  errorMessage?: string | null;
  contractId?: number | null;
  contractActivated: boolean;
  payment?: BillingPaymentDto | null;
}

const BASE = "/api/v1/billing";

export const billingApi = {
  getInvoices: (params?: { contractId?: number; status?: string; page?: number; pageSize?: number }) => {
    const q = new URLSearchParams();
    if (params?.contractId) q.set("contractId", String(params.contractId));
    if (params?.status) q.set("status", params.status);
    if (params?.page) q.set("page", String(params.page));
    if (params?.pageSize) q.set("pageSize", String(params.pageSize));
    const qs = q.toString();
    return apiRequest<PagedInvoicesDto>(`${BASE}/invoices${qs ? `?${qs}` : ""}`, { method: "GET" });
  },

  payInvoice: (invoiceId: number) =>
    apiRequest<BillingPaymentDto>(`${BASE}/invoices/${invoiceId}/payments`, { method: "POST" }),

  payContractDeposit: (contractId: number) =>
    apiRequest<BillingPaymentDto>(`${BASE}/contracts/${contractId}/pay-deposit`, { method: "POST" }),

  sandboxConfirm: (payload: { paymentCode: string; isSuccess: boolean; bankCode?: string }) =>
    apiRequest<{ callbackUrl: string }>(`${BASE}/payments/sandbox-confirm`, {
      method: "POST",
      body: JSON.stringify(payload),
    }),

  verifyVnpayReturn: (search: string) =>
    apiRequest<VerifyReturnResultDto>(`${BASE}/payments/verify-return${search.startsWith("?") ? search : `?${search}`}`, {
      method: "GET",
    }),
};
