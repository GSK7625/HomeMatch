import { ApiResponse, HealthCheck, SystemStatus } from "@/types";

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

export async function fetchHealth(): Promise<HealthCheck> {
  const res = await fetch(`${API_BASE}/health`, { cache: "no-store" });
  if (!res.ok) {
    throw new Error(`Failed to fetch health: ${res.statusText}`);
  }
  return res.json();
}

export async function fetchSystemStatus(): Promise<SystemStatus> {
  const res = await fetch(`${API_BASE}/api/v1/system/status`, { cache: "no-store" });
  if (!res.ok) {
    throw new Error(`Failed to fetch system status: ${res.statusText}`);
  }
  return res.json();
}

export async function pingModule(moduleName: string): Promise<Record<string, unknown>> {
  const res = await fetch(`${API_BASE}/api/v1/${moduleName.toLowerCase()}/ping`, { cache: "no-store" });
  if (!res.ok) {
    throw new Error(`Failed to ping module ${moduleName}: ${res.statusText}`);
  }
  return res.json();
}
