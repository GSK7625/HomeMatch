import { apiRequest } from "@/lib/api-client";
import {
  CreateLeasingContractPayload,
  LeasingContractDetailDto,
  LeasingContractDocumentUrlDto,
  LeasingContractDto,
  LeasingDepositTransactionDto,
  LeasingHandoverDetailsDto,
  LeasingPagedContractsDto,
  LeasingRequestOtpDto,
} from "@/types";

const BASE = "/api/v1/leasing/contracts";

export interface LeasingTenantOption {
  id: number;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  role?: string | null;
}

export const leasingApi = {
  getTenants: () =>
    apiRequest<LeasingTenantOption[]>("/api/v1/leasing/tenants", { method: "GET" }),
  getContracts: (params?: {
    status?: string;
    roleView?: "LANDLORD" | "TENANT";
    page?: number;
    pageSize?: number;
  }) => {
    const q = new URLSearchParams();
    if (params?.status && params.status !== "ALL") q.set("status", params.status);
    if (params?.roleView) q.set("roleView", params.roleView);
    if (params?.page) q.set("page", String(params.page));
    if (params?.pageSize) q.set("pageSize", String(params.pageSize));
    const qs = q.toString();
    return apiRequest<LeasingPagedContractsDto>(`${BASE}${qs ? `?${qs}` : ""}`, { method: "GET" });
  },

  getExpiringContracts: (days = 30) =>
    apiRequest<LeasingContractDto[]>(`${BASE}/expiring?days=${days}`, { method: "GET" }),

  getContractDetails: (id: number) =>
    apiRequest<LeasingContractDetailDto>(`${BASE}/${id}/details`, { method: "GET" }),

  createContract: (payload: CreateLeasingContractPayload, file?: File | null) => {
    const q = new URLSearchParams({
      roomId: String(payload.roomId),
      tenantId: String(payload.tenantId),
      startDate: payload.startDate,
      endDate: payload.endDate,
      monthlyRent: String(payload.monthlyRent),
      depositAmount: String(payload.depositAmount),
      billingDueDay: String(payload.billingDueDay),
    });
    if (payload.contractCode?.trim()) q.set("contractCode", payload.contractCode.trim());
    if (payload.documentSnapshot?.trim()) q.set("documentSnapshot", payload.documentSnapshot.trim());
    if (payload.signatureExpiresAt) q.set("signatureExpiresAt", payload.signatureExpiresAt);
    if (payload.handoverDueAt) q.set("handoverDueAt", payload.handoverDueAt);

    const formData = new FormData();
    if (file) formData.append("file", file);

    return apiRequest<LeasingContractDto>(`${BASE}?${q.toString()}`, {
      method: "POST",
      body: formData,
    });
  },

  getDocumentUrl: (id: number) =>
    apiRequest<LeasingContractDocumentUrlDto>(`${BASE}/${id}/document`, { method: "GET" }),

  sendForSignature: (id: number, signatureExpiresAt: string, handoverDueAt?: string) =>
    apiRequest<LeasingContractDto>(`${BASE}/${id}/send-for-signature`, {
      method: "POST",
      body: JSON.stringify({ signatureExpiresAt, handoverDueAt: handoverDueAt || null }),
    }),

  requestOtp: (id: number) =>
    apiRequest<LeasingRequestOtpDto>(`${BASE}/${id}/request-otp`, { method: "POST" }),

  signContract: (id: number, documentHash: string, otpCode: string, signatureData?: string | null) =>
    apiRequest<LeasingContractDto>(`${BASE}/${id}/sign`, {
      method: "POST",
      body: JSON.stringify({
        documentHash,
        otpCode: otpCode.trim(),
        signatureMethod: "OTP_EMAIL",
        signatureData: signatureData || null,
      }),
    }),

  rejectContract: (id: number, reason: string) =>
    apiRequest<LeasingContractDto>(`${BASE}/${id}/reject`, {
      method: "POST",
      body: JSON.stringify({ reason }),
    }),

  cancelContract: (id: number, reason: string) =>
    apiRequest<LeasingContractDto>(`${BASE}/${id}/cancel`, {
      method: "POST",
      body: JSON.stringify({ reason }),
    }),

  deleteContract: (id: number) =>
    apiRequest<boolean>(`${BASE}/${id}`, { method: "DELETE" }),

  updateDraft: (
    id: number,
    payload: {
      startDate: string;
      endDate: string;
      monthlyRent: number;
      depositAmount: number;
      billingDueDay: number;
      documentSnapshot?: string;
    }
  ) =>
    apiRequest<LeasingContractDto>(`${BASE}/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),

  restoreContract: (id: number) =>
    apiRequest<LeasingContractDto>(`${BASE}/${id}/restore`, { method: "POST" }),

  activateContract: (id: number) =>
    apiRequest<LeasingContractDto>(`${BASE}/${id}/activate`, { method: "POST" }),

  terminateContract: (id: number, reason: string) =>
    apiRequest<LeasingContractDto>(`${BASE}/${id}/terminate`, {
      method: "POST",
      body: JSON.stringify({ reason }),
    }),

  getHandoverDetails: (id: number) =>
    apiRequest<LeasingHandoverDetailsDto>(`${BASE}/${id}/handover`, { method: "GET" }),

  confirmHandover: (id: number, actualOccupancyEndDate?: string) =>
    apiRequest<LeasingContractDto>(`${BASE}/${id}/confirm-handover`, {
      method: "POST",
      body: JSON.stringify({ actualOccupancyEndDate: actualOccupancyEndDate || null }),
    }),

  recordDepositReceived: (id: number, amount: number) =>
    apiRequest<LeasingDepositTransactionDto>(`${BASE}/${id}/deposits/received`, {
      method: "POST",
      body: JSON.stringify({ amount }),
    }),

  recordDepositDeduction: (id: number, amount: number, reason: string) =>
    apiRequest<LeasingDepositTransactionDto>(`${BASE}/${id}/deposits/deductions`, {
      method: "POST",
      body: JSON.stringify({ amount, reason }),
    }),

  recordDepositRefund: (id: number, amount: number, reason?: string) =>
    apiRequest<LeasingDepositTransactionDto>(`${BASE}/${id}/deposits/refunds`, {
      method: "POST",
      body: JSON.stringify({ amount, reason: reason || null }),
    }),
};
