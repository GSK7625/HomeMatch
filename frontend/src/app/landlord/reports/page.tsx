"use client";

import React, { Suspense } from "react";
import { RefreshCw } from "lucide-react";
import { useLandlordAnalytics } from "@/features/billing/useLandlordAnalytics";
import { LandlordAnalyticsShell } from "@/features/billing/LandlordAnalyticsShell";
import { RevenueReportSection } from "@/features/billing/RevenueReportSection";
import { DebtAgingSection } from "@/features/billing/DebtAgingSection";
import { DepositTrackingSection } from "@/features/billing/DepositTrackingSection";

function LandlordReportsContent() {
  const analytics = useLandlordAnalytics();
  const filterQuery = analytics.buildFilterQuery();

  return (
    <LandlordAnalyticsShell
      activeSection="reports"
      title="Báo Cáo Tài Chính & Công Nợ"
      subtitle="Theo dõi doanh thu, khoản phải thu/đã thu trong kỳ, phân loại công nợ theo nhóm tuổi nợ và tiền cọc hợp đồng."
      redirectPath="/landlord/reports"
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
          <RevenueReportSection data={data} selectedPeriod={analytics.selectedPeriod} />
          <DebtAgingSection data={data} />
          <DepositTrackingSection data={data} />
        </div>
      )}
    </LandlordAnalyticsShell>
  );
}

export default function LandlordReportsPage() {
  return (
    <Suspense
      fallback={
        <div className="max-w-7xl mx-auto px-4 py-20 text-center space-y-3">
          <RefreshCw className="w-8 h-8 text-blue-600 animate-spin mx-auto" />
          <p className="text-xs text-slate-500 font-medium">Đang tải Báo cáo tài chính...</p>
        </div>
      }
    >
      <LandlordReportsContent />
    </Suspense>
  );
}
