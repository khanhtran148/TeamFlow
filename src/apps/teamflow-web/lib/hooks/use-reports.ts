import { useQuery, type UseQueryOptions } from "@tanstack/react-query";
import {
  getSprintReport,
  listSprintReports,
  getLatestTeamHealth,
  listTeamHealthSummaries,
} from "@/lib/api/reports";
import type {
  PaginatedResponse,
  SprintReportDto,
  TeamHealthSummaryDto,
} from "@/lib/api/types";

export const reportKeys = {
  sprintReport: (sprintId: string) =>
    ["reports", "sprint", sprintId] as const,
  sprintReports: (projectId: string, page: number) =>
    ["reports", "sprint-list", projectId, page] as const,
  teamHealth: (projectId: string) =>
    ["reports", "team-health", projectId] as const,
  teamHealthList: (projectId: string, page: number) =>
    ["reports", "team-health-list", projectId, page] as const,
};

export function useSprintReport(
  sprintId: string,
  projectId: string,
  options?: Partial<UseQueryOptions<SprintReportDto>>,
) {
  return useQuery({
    queryKey: reportKeys.sprintReport(sprintId),
    queryFn: () => getSprintReport(sprintId, projectId),
    enabled: !!sprintId && !!projectId,
    ...options,
  });
}

export function useSprintReports(
  projectId: string,
  page = 1,
  pageSize = 10,
  options?: Partial<UseQueryOptions<PaginatedResponse<SprintReportDto>>>,
) {
  return useQuery({
    queryKey: reportKeys.sprintReports(projectId, page),
    queryFn: () => listSprintReports(projectId, page, pageSize),
    enabled: !!projectId,
    ...options,
  });
}

export function useTeamHealthSummary(
  projectId: string,
  options?: Partial<UseQueryOptions<TeamHealthSummaryDto>>,
) {
  return useQuery({
    queryKey: reportKeys.teamHealth(projectId),
    queryFn: () => getLatestTeamHealth(projectId),
    enabled: !!projectId,
    ...options,
  });
}

export function useTeamHealthSummaries(
  projectId: string,
  page = 1,
  pageSize = 10,
  options?: Partial<UseQueryOptions<PaginatedResponse<TeamHealthSummaryDto>>>,
) {
  return useQuery({
    queryKey: reportKeys.teamHealthList(projectId, page),
    queryFn: () => listTeamHealthSummaries(projectId, page, pageSize),
    enabled: !!projectId,
    ...options,
  });
}
