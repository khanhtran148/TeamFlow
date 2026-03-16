"use client";

import { useCycleTime } from "@/lib/hooks/use-dashboard";

export function CycleTimeChart({ projectId }: { projectId: string }) {
  const { data, isLoading } = useCycleTime(projectId);

  if (isLoading) return <div className="p-4 bg-white border rounded-lg">Loading cycle time...</div>;
  if (!data || data.byType.length === 0)
    return <div className="p-4 bg-white border rounded-lg text-gray-400">No cycle time data</div>;

  return (
    <div className="p-4 bg-white border rounded-lg">
      <h3 className="text-sm font-semibold mb-3">Cycle Time by Type</h3>
      <table className="text-sm w-full">
        <thead>
          <tr className="text-gray-500 text-xs">
            <th className="text-left">Type</th>
            <th className="text-right">Avg (days)</th>
            <th className="text-right">Median</th>
            <th className="text-right">P90</th>
            <th className="text-right">Samples</th>
          </tr>
        </thead>
        <tbody>
          {data.byType.map((t) => (
            <tr key={t.itemType} className="border-t">
              <td>{t.itemType}</td>
              <td className="text-right">{t.avgDays}</td>
              <td className="text-right">{t.medianDays}</td>
              <td className="text-right">{t.p90Days}</td>
              <td className="text-right">{t.sampleSize}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
