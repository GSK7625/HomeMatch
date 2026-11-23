"use client";

import React, { Suspense } from "react";
import { RefreshCw } from "lucide-react";
import { useLandlordAnalytics } from "@/features/billing/useLandlordAnalytics";
import { LandlordAnalyticsShell } from "@/features/billing/LandlordAnalyticsShell";
import { DashboardOverviewSection } from "@/features/billing/DashboardOverviewSection";
import { MarketingAndViewingsSection } from "@/features/billing/MarketingAndViewingsSection";

function LandlordDashboardContent() {
  const analytics = useLandlordAnalytics();
  const filterQuery = analytics.buildFilterQuery();

  return (
    <LandlordAnalyticsShell
      activeSection="dashboard"
      title="Dashboard Tổng Quan Chủ Nhà"
      subtitle="Theo dõi số căn trống, căn đang thuê, tin đang hiển thị, phễu khách quan tâm và lịch xem phòng trong ngày."
      redirectPath="/landlord/dashboard"
      user={analytics.user}
      role={analytics.role}
      properties={analytics.properties}
      periodOptions={analytics.periodOptions}
      selectedBuilding={analytics.selectedBuilding}
      onBuildingChange={analytics.setSelectedBuilding}
      selectedPeriod={analytics.selectedPeriod}
      onPeriodChange={analytics.setSelectedPeriod}
      loading={analytics.loading}
      error={analytics.error}
      data={analytics.data}
      onRefresh={analytics.refresh}
      filterQuery={filterQuery}
    >
      {(data) => (
        <div className="space-y-6">
          <DashboardOverviewSection
            data={data}
            selectedPeriod={analytics.selectedPeriod}
            filterQuery={filterQuery}
          />
          <MarketingAndViewingsSection data={data} filterQuery={filterQuery} />
        </div>
      )}
    </LandlordAnalyticsShell>
  );
}

export default function LandlordDashboardPage() {
  return (
    <Suspense
      fallback={
        <div className="max-w-7xl mx-auto px-4 py-20 text-center space-y-3">
          <RefreshCw className="w-8 h-8 text-blue-600 animate-spin mx-auto" />
          <p className="text-xs text-slate-500 font-medium">Đang tải Dashboard Chủ nhà...</p>
        </div>
      }
    >
      <LandlordDashboardContent />
    </Suspense>
  );
}
