"use client";

import { useTeamHealthSummary } from "@/lib/hooks/use-reports";

export function TeamHealthCard({ projectId }: { projectId: string }) {
  const { data, isLoading, isError } = useTeamHealthSummary(projectId);

  if (isLoading) return <div className="p-4 bg-white border rounded-lg">Loading health...</div>;
  if (isError || !data)
    return <div className="p-4 bg-white border rounded-lg text-gray-400">No team health data yet</div>;

  const summaryData = data.summaryData as Record<string, unknown>;

  return (
    <div className="p-4 bg-white border rounded-lg">
      <h3 className="text-sm font-semibold mb-3">Team Health Summary</h3>
      <p className="text-xs text-gray-500 mb-2">
        {data.periodStart} - {data.periodEnd}
      </p>
      <div className="grid grid-cols-2 gap-2 text-sm">
        {Object.entries(summaryData).map(([key, value]) => (
          <div key={key} className="flex justify-between">
            <span className="text-gray-600">{key}</span>
            <span className="font-medium">{String(value)}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
