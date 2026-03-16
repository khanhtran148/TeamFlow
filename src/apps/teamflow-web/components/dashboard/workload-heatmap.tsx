"use client";

import { useWorkloadHeatmap } from "@/lib/hooks/use-dashboard";

export function WorkloadHeatmap({ projectId }: { projectId: string }) {
  const { data, isLoading } = useWorkloadHeatmap(projectId);

  if (isLoading) return <div className="p-4 bg-white border rounded-lg">Loading workload...</div>;
  if (!data || data.members.length === 0)
    return <div className="p-4 bg-white border rounded-lg text-gray-400">No workload data</div>;

  const maxAssigned = Math.max(...data.members.map((m) => m.assignedCount));

  return (
    <div className="p-4 bg-white border rounded-lg">
      <h3 className="text-sm font-semibold mb-3">Team Workload</h3>
      <div className="space-y-2">
        {data.members.map((m) => (
          <div key={m.userId} className="flex items-center gap-3">
            <span className="text-sm w-24 truncate">{m.name}</span>
            <div className="flex-1 bg-gray-100 rounded-full h-4 relative">
              <div
                className="bg-blue-500 h-4 rounded-full"
                style={{ width: `${maxAssigned > 0 ? (m.assignedCount / maxAssigned) * 100 : 0}%` }}
              />
            </div>
            <span className="text-xs text-gray-500 w-16 text-right">
              {m.assignedCount} items
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
