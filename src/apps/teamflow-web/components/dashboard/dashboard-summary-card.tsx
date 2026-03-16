"use client";

import { useDashboardSummary } from "@/lib/hooks/use-dashboard";

export function DashboardSummaryCard({ projectId }: { projectId: string }) {
  const { data, isLoading } = useDashboardSummary(projectId);

  if (isLoading) return <div className="text-gray-500">Loading summary...</div>;
  if (!data) return null;

  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
      <div className="p-4 bg-white border rounded-lg">
        <p className="text-sm text-gray-500">Active Sprint</p>
        <p className="text-lg font-semibold">{data.activeSprintName ?? "None"}</p>
      </div>
      <div className="p-4 bg-white border rounded-lg">
        <p className="text-sm text-gray-500">Completion</p>
        <p className="text-lg font-semibold">{(data.completionPct * 100).toFixed(1)}%</p>
      </div>
      <div className="p-4 bg-white border rounded-lg">
        <p className="text-sm text-gray-500">Velocity (3-sprint avg)</p>
        <p className="text-lg font-semibold">{data.velocity3SprintAvg}</p>
      </div>
      <div className="p-4 bg-white border rounded-lg">
        <p className="text-sm text-gray-500">Overdue Releases</p>
        <p className={`text-lg font-semibold ${data.overdueReleases > 0 ? "text-red-600" : ""}`}>
          {data.overdueReleases}
        </p>
      </div>
    </div>
  );
}
