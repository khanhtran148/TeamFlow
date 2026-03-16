"use client";

import { useParams } from "next/navigation";
import { SprintReportCard } from "@/components/reports/sprint-report-card";
import { TeamHealthCard } from "@/components/reports/team-health-card";

export default function ReportsPage() {
  const { projectId } = useParams<{ projectId: string }>();

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold">Reports</h1>
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <SprintReportCard projectId={projectId} />
        <TeamHealthCard projectId={projectId} />
      </div>
    </div>
  );
}
