"use client";

import { useParams } from "next/navigation";
import { DashboardSummaryCard } from "@/components/dashboard/dashboard-summary-card";
import { VelocityChart } from "@/components/dashboard/velocity-chart";
import { CumulativeFlowChart } from "@/components/dashboard/cumulative-flow-chart";
import { CycleTimeChart } from "@/components/dashboard/cycle-time-chart";
import { WorkloadHeatmap } from "@/components/dashboard/workload-heatmap";

export default function DashboardPage() {
  const { projectId } = useParams<{ projectId: string }>();

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold">Dashboard</h1>
      <DashboardSummaryCard projectId={projectId} />
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <VelocityChart projectId={projectId} />
        <CumulativeFlowChart projectId={projectId} />
        <CycleTimeChart projectId={projectId} />
        <WorkloadHeatmap projectId={projectId} />
      </div>
    </div>
  );
}
