import { useQuery, type UseQueryOptions } from "@tanstack/react-query";
import {
  getDashboardSummary,
  getVelocityChart,
  getCumulativeFlow,
  getCycleTime,
  getWorkloadHeatmap,
  getReleaseProgress,
} from "@/lib/api/dashboard";
import type {
  DashboardSummaryDto,
  VelocityChartDto,
  CumulativeFlowDto,
  CycleTimeDto,
  WorkloadHeatmapDto,
  ReleaseProgressDto,
} from "@/lib/api/types";

export const dashboardKeys = {
  all: (projectId: string) => ["dashboard", projectId] as const,
  summary: (projectId: string) =>
    ["dashboard", projectId, "summary"] as const,
  velocity: (projectId: string, sprintCount: number) =>
    ["dashboard", projectId, "velocity", sprintCount] as const,
  cumulativeFlow: (projectId: string, from: string, to: string) =>
    ["dashboard", projectId, "cumulative-flow", from, to] as const,
  cycleTime: (projectId: string, from?: string, to?: string) =>
    ["dashboard", projectId, "cycle-time", from, to] as const,
  workload: (projectId: string) =>
    ["dashboard", projectId, "workload"] as const,
  releaseProgress: (releaseId: string) =>
    ["release-progress", releaseId] as const,
};

export function useDashboardSummary(
  projectId: string,
  options?: Partial<UseQueryOptions<DashboardSummaryDto>>,
) {
  return useQuery({
    queryKey: dashboardKeys.summary(projectId),
    queryFn: () => getDashboardSummary(projectId),
    enabled: !!projectId,
    ...options,
  });
}

export function useVelocityChart(
  projectId: string,
  sprintCount = 10,
  options?: Partial<UseQueryOptions<VelocityChartDto>>,
) {
  return useQuery({
    queryKey: dashboardKeys.velocity(projectId, sprintCount),
    queryFn: () => getVelocityChart(projectId, sprintCount),
    enabled: !!projectId,
    ...options,
  });
}

export function useCumulativeFlow(
  projectId: string,
  fromDate: string,
  toDate: string,
  options?: Partial<UseQueryOptions<CumulativeFlowDto>>,
) {
  return useQuery({
    queryKey: dashboardKeys.cumulativeFlow(projectId, fromDate, toDate),
    queryFn: () => getCumulativeFlow(projectId, fromDate, toDate),
    enabled: !!projectId && !!fromDate && !!toDate,
    ...options,
  });
}

export function useCycleTime(
  projectId: string,
  fromDate?: string,
  toDate?: string,
  options?: Partial<UseQueryOptions<CycleTimeDto>>,
) {
  return useQuery({
    queryKey: dashboardKeys.cycleTime(projectId, fromDate, toDate),
    queryFn: () => getCycleTime(projectId, fromDate, toDate),
    enabled: !!projectId,
    ...options,
  });
}

export function useWorkloadHeatmap(
  projectId: string,
  options?: Partial<UseQueryOptions<WorkloadHeatmapDto>>,
) {
  return useQuery({
    queryKey: dashboardKeys.workload(projectId),
    queryFn: () => getWorkloadHeatmap(projectId),
    enabled: !!projectId,
    ...options,
  });
}

export function useReleaseProgress(
  releaseId: string,
  projectId: string,
  options?: Partial<UseQueryOptions<ReleaseProgressDto>>,
) {
  return useQuery({
    queryKey: dashboardKeys.releaseProgress(releaseId),
    queryFn: () => getReleaseProgress(releaseId, projectId),
    enabled: !!releaseId && !!projectId,
    ...options,
  });
}
