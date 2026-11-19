"use client";

import React from "react";
import { useParams } from "next/navigation";
import { ContractSigningRoom } from "@/features/leasing/ContractSigningRoom";

export default function TenantContractDetailPage() {
  const params = useParams();
  const id = Number(params?.id);
  return <ContractSigningRoom contractId={Number.isFinite(id) ? id : 0} role="TENANT" />;
}
