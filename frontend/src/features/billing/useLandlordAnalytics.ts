"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import { useSearchParams } from "next/navigation";
import { useAuth } from "@/features/auth/AuthContext";
import { propertiesApi, landlordDashboardApi } from "@/lib/api-client";
import { LandlordDashboardData, PropertyDto } from "@/types";

export interface PeriodOption {
  value: string;
  label: string;
}

export function buildRecentPeriods(count = 6): PeriodOption[] {
  const now = new Date();
  const list: PeriodOption[] = [];
  for (let i = 0; i < count; i++) {
    const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
    const mm = String(d.getMonth() + 1).padStart(2, "0");
    const yyyy = d.getFullYear();
    const val = `${mm}/${yyyy}`;
    const suffix = i === 0 ? " (Hiện tại)" : i === 1 ? " (Tháng trước)" : "";
    list.push({ value: val, label: `Kỳ ${val}${suffix}` });
  }
  list.push({ value: "all", label: "Tất cả các kỳ" });
  return list;
}

export function useLandlordAnalytics() {
  const { user, role } = useAuth();
  const searchParams = useSearchParams();
  const periodOptions = useMemo(() => buildRecentPeriods(6), []);
  const defaultPeriod = periodOptions[0].value;

  const initialBuilding = searchParams.get("buildingId") || "all";
  const initialPeriodParam = searchParams.get("period");
  const initialPeriod =
    initialPeriodParam && periodOptions.some((opt) => opt.value === initialPeriodParam)
      ? initialPeriodParam
      : defaultPeriod;

  const [properties, setProperties] = useState<PropertyDto[]>([]);
  const [data, setData] = useState<LandlordDashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedBuilding, setSelectedBuilding] = useState(initialBuilding);
  const [selectedPeriod, setSelectedPeriod] = useState(initialPeriod);

  useEffect(() => {
    if (!user || role !== "Landlord") return;
    let active = true;
    propertiesApi
      .getMyProperties()
      .then((res) => {
        if (active && res.isSuccess && res.data) {
          setProperties(res.data);
        }
      })
      .catch(() => {
        // Ignore property dropdown load errors
      });
    return () => {
      active = false;
    };
  }, [user, role]);

  const loadAnalytics = useCallback(async (building: string, period: string) => {
    setLoading(true);
    setError(null);
    try {
      const res = await landlordDashboardApi.getDashboard(building, period);
      if (!res.isSuccess || !res.data) {
        throw new Error(
          res.error?.message ||
            "Không thể kết nối đến Backend API (http://localhost:5000). Vui lòng khởi động Backend và PostgreSQL."
        );
      }
      setData(res.data);
    } catch (err) {
      setData(null);
      setError(
        err instanceof Error
          ? err.message
          : "Không thể kết nối đến Backend API (http://localhost:5000)."
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!user || role !== "Landlord") return;
    loadAnalytics(selectedBuilding, selectedPeriod);
  }, [user, role, loadAnalytics, selectedBuilding, selectedPeriod]);

  const buildFilterQuery = useCallback(() => {
    const params = new URLSearchParams();
    if (selectedBuilding && selectedBuilding !== "all") {
      params.set("buildingId", selectedBuilding);
    }
    if (selectedPeriod) {
      params.set("period", selectedPeriod);
    }
    const qs = params.toString();
    return qs ? `?${qs}` : "";
  }, [selectedBuilding, selectedPeriod]);

  const refresh = useCallback(() => {
    loadAnalytics(selectedBuilding, selectedPeriod);
  }, [loadAnalytics, selectedBuilding, selectedPeriod]);

  return {
    user,
    role,
    properties,
    periodOptions,
    data,
    loading,
    error,
    selectedBuilding,
    setSelectedBuilding,
    selectedPeriod,
    setSelectedPeriod,
    refresh,
    buildFilterQuery,
  };
}
