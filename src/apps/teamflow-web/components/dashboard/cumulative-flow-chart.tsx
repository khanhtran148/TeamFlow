"use client";

import { useCumulativeFlow } from "@/lib/hooks/use-dashboard";
import { useMemo } from "react";

export function CumulativeFlowChart({ projectId }: { projectId: string }) {
  const today = useMemo(() => new Date().toISOString().split("T")[0], []);
  const thirtyDaysAgo = useMemo(() => {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return d.toISOString().split("T")[0];
  }, []);

  const { data, isLoading } = useCumulativeFlow(projectId, thirtyDaysAgo, today);

  if (isLoading) return <div className="p-4 bg-white border rounded-lg">Loading flow...</div>;
  if (!data || data.dataPoints.length === 0)
    return <div className="p-4 bg-white border rounded-lg text-gray-400">No cumulative flow data</div>;

  return (
    <div className="p-4 bg-white border rounded-lg">
      <h3 className="text-sm font-semibold mb-3">Cumulative Flow (30 days)</h3>
      <div className="overflow-x-auto">
        <table className="text-xs w-full">
          <thead>
            <tr className="text-gray-500">
              <th className="text-left">Date</th>
              <th className="text-right">To Do</th>
              <th className="text-right">In Progress</th>
              <th className="text-right">In Review</th>
              <th className="text-right">Done</th>
            </tr>
          </thead>
          <tbody>
            {data.dataPoints.slice(-10).map((point) => (
              <tr key={point.date} className="border-t">
                <td>{point.date}</td>
                <td className="text-right">{point.toDo}</td>
                <td className="text-right">{point.inProgress}</td>
                <td className="text-right">{point.inReview}</td>
                <td className="text-right">{point.done}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
