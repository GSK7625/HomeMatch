import { API_BASE, apiRequest } from "@/lib/api-client";
import {
  BillingDebtSummaryDto,
  BillingInvoiceDetailDto,
  BillingPagedDebtInvoicesDto,
  BillingPagedInvoicesDto,
  BillingPaymentDto,
  VNPayIpnResult,
} from "@/types";

const BASE = "/api/v1/billing";

export interface GetBillingInvoicesParams {
  contractId?: number;
  propertyId?: number;
  status?: string;
  month?: number;
  year?: number;
  page?: number;
  pageSize?: number;
}

export interface GetBillingDebtParams {
  propertyId?: number;
  contractId?: number;
  agingGroup?: string;
  page?: number;
  pageSize?: number;
}

export const billingApi = {
  getInvoices: (params?: GetBillingInvoicesParams) => {
    const q = new URLSearchParams();
    if (params?.contractId) q.set("contractId", String(params.contractId));
    if (params?.propertyId) q.set("propertyId", String(params.propertyId));
    if (params?.status && params.status !== "ALL") q.set("status", params.status);
    if (params?.month) q.set("month", String(params.month));
    if (params?.year) q.set("year", String(params.year));
    if (params?.page) q.set("page", String(params.page));
    if (params?.pageSize) q.set("pageSize", String(params.pageSize));
    const qs = q.toString();
    return apiRequest<BillingPagedInvoicesDto>(`${BASE}/invoices${qs ? `?${qs}` : ""}`, {
      method: "GET",
    });
  },

  getInvoiceById: (id: number) =>
    apiRequest<BillingInvoiceDetailDto>(`${BASE}/invoices/${id}`, {
      method: "GET",
    }),

  getDebtSummary: (params?: { propertyId?: number; contractId?: number }) => {
    const q = new URLSearchParams();
    if (params?.propertyId) q.set("propertyId", String(params.propertyId));
    if (params?.contractId) q.set("contractId", String(params.contractId));
    const qs = q.toString();
    return apiRequest<BillingDebtSummaryDto>(`${BASE}/debts/summary${qs ? `?${qs}` : ""}`, {
      method: "GET",
    });
  },

  getDebtInvoices: (params?: GetBillingDebtParams) => {
    const q = new URLSearchParams();
    if (params?.propertyId) q.set("propertyId", String(params.propertyId));
    if (params?.contractId) q.set("contractId", String(params.contractId));
    if (params?.agingGroup && params.agingGroup !== "ALL") q.set("agingGroup", params.agingGroup);
    if (params?.page) q.set("page", String(params.page));
    if (params?.pageSize) q.set("pageSize", String(params.pageSize));
    const qs = q.toString();
    return apiRequest<BillingPagedDebtInvoicesDto>(`${BASE}/debts/invoices${qs ? `?${qs}` : ""}`, {
      method: "GET",
    });
  },

  createPayment: (invoiceId: number) =>
    apiRequest<BillingPaymentDto>(`${BASE}/invoices/${invoiceId}/payments`, {
      method: "POST",
    }),

  getPaymentById: (id: number) =>
    apiRequest<BillingPaymentDto>(`${BASE}/payments/${id}`, {
      method: "GET",
    }),

  /**
   * Chuyển tiếp tham số vnp_* về Backend IPN endpoint để xác thực chữ ký và đồng bộ trạng thái thanh toán.
   * Endpoint IPN trả JSON trực tiếp dạng { rspCode, message } thay vì bọc ApiResponse<T>.
   */
  forwardVnpayIpn: async (queryString: string): Promise<VNPayIpnResult> => {
    const cleanQuery = queryString.startsWith("?") ? queryString.slice(1) : queryString;
    const res = await fetch(`${API_BASE}${BASE}/payments/vnpay-ipn?${cleanQuery}`, {
      method: "GET",
      credentials: "include",
    });
    const data = await res.json().catch(() => null);
    return {
      rspCode: String(data?.rspCode ?? data?.RspCode ?? (res.ok ? "99" : "99")),
      message: String(data?.message ?? data?.Message ?? "Không thể xác thực phản hồi VNPay."),
    };
  },
};
