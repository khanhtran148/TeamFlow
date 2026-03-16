import { apiClient } from "./client";
import type {
  DashboardSummaryDto,
  VelocityChartDto,
  CumulativeFlowDto,
  CycleTimeDto,
  WorkloadHeatmapDto,
  ReleaseProgressDto,
} from "./types";

export async function getDashboardSummary(
  projectId: string,
): Promise<DashboardSummaryDto> {
  const response = await apiClient.get<DashboardSummaryDto>(
    `/projects/${projectId}/dashboard/summary`,
  );
  return response.data;
}

export async function getVelocityChart(
  projectId: string,
  sprintCount = 10,
): Promise<VelocityChartDto> {
  const response = await apiClient.get<VelocityChartDto>(
    `/projects/${projectId}/dashboard/velocity`,
    { params: { sprintCount } },
  );
  return response.data;
}

export async function getCumulativeFlow(
  projectId: string,
  fromDate: string,
  toDate: string,
): Promise<CumulativeFlowDto> {
  const response = await apiClient.get<CumulativeFlowDto>(
    `/projects/${projectId}/dashboard/cumulative-flow`,
    { params: { fromDate, toDate } },
  );
  return response.data;
}

export async function getCycleTime(
  projectId: string,
  fromDate?: string,
  toDate?: string,
): Promise<CycleTimeDto> {
  const response = await apiClient.get<CycleTimeDto>(
    `/projects/${projectId}/dashboard/cycle-time`,
    { params: { fromDate, toDate } },
  );
  return response.data;
}

export async function getWorkloadHeatmap(
  projectId: string,
): Promise<WorkloadHeatmapDto> {
  const response = await apiClient.get<WorkloadHeatmapDto>(
    `/projects/${projectId}/dashboard/workload`,
  );
  return response.data;
}

export async function getReleaseProgress(
  releaseId: string,
  projectId: string,
): Promise<ReleaseProgressDto> {
  const response = await apiClient.get<ReleaseProgressDto>(
    `/releases/${releaseId}/progress`,
    { params: { projectId } },
  );
  return response.data;
}
