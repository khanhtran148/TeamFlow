"use client";

import { useVelocityChart } from "@/lib/hooks/use-dashboard";

export function VelocityChart({ projectId }: { projectId: string }) {
  const { data, isLoading } = useVelocityChart(projectId);

  if (isLoading) return <div className="p-4 bg-white border rounded-lg">Loading velocity...</div>;
  if (!data || data.sprints.length === 0)
    return <div className="p-4 bg-white border rounded-lg text-gray-400">No velocity data</div>;

  const maxPoints = Math.max(...data.sprints.map((s) => Math.max(s.plannedPoints, s.completedPoints)));

  return (
    <div className="p-4 bg-white border rounded-lg">
      <h3 className="text-sm font-semibold mb-3">Velocity Chart</h3>
      <div className="flex items-end gap-2 h-40">
        {data.sprints.map((sprint) => (
          <div key={sprint.sprintId} className="flex-1 flex flex-col items-center gap-1">
            <div className="flex gap-0.5 items-end w-full" style={{ height: "100%" }}>
              <div
                className="flex-1 bg-blue-200 rounded-t"
                style={{ height: `${maxPoints > 0 ? (sprint.plannedPoints / maxPoints) * 100 : 0}%` }}
                title={`Planned: ${sprint.plannedPoints}`}
              />
              <div
                className="flex-1 bg-blue-600 rounded-t"
                style={{ height: `${maxPoints > 0 ? (sprint.completedPoints / maxPoints) * 100 : 0}%` }}
                title={`Completed: ${sprint.completedPoints}`}
              />
            </div>
            <span className="text-[10px] text-gray-500 truncate max-w-full">{sprint.sprintName}</span>
          </div>
        ))}
      </div>
      <div className="flex gap-4 mt-2 text-xs text-gray-500">
        <span className="flex items-center gap-1"><span className="w-3 h-3 bg-blue-200 rounded" /> Planned</span>
        <span className="flex items-center gap-1"><span className="w-3 h-3 bg-blue-600 rounded" /> Completed</span>
      </div>
    </div>
  );
}
