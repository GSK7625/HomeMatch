export interface ContractPartyInfo {
  fullName?: string;
  phone?: string;
  email?: string;
  cccd?: string;
  address?: string;
  bankAccount?: string;
}

export interface ContractSnapshotData {
  roomName?: string;
  roomType?: string;
  propertyName?: string;
  propertyAddress?: string;
  areaSqm?: number;
  floor?: number;
  maxOccupants?: number;
  amenities?: string[];
  landlord?: ContractPartyInfo;
  tenant?: ContractPartyInfo;
  electricityPrice?: string;
  waterPrice?: string;
  internetPrice?: string;
  serviceFee?: string;
  parkingFee?: string;
  terms?: string;
}

export function parseContractSnapshot(raw?: string | null): ContractSnapshotData {
  if (!raw?.trim()) return {};
  try {
    const parsed = JSON.parse(raw);
    if (parsed && typeof parsed === "object") {
      return parsed as ContractSnapshotData;
    }
  } catch {
    return { terms: raw };
  }
  return {};
}

export function serializeContractSnapshot(data: ContractSnapshotData): string {
  return JSON.stringify(data);
}

export function calcLeaseMonths(startDate: string, endDate: string): number {
  const s = new Date(startDate);
  const e = new Date(endDate);
  if (Number.isNaN(s.getTime()) || Number.isNaN(e.getTime()) || e <= s) return 12;
  return Math.max(1, Math.round((e.getTime() - s.getTime()) / (1000 * 60 * 60 * 24 * 30)));
}
