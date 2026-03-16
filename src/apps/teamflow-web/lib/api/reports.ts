import { apiClient } from "./client";
import type {
  PaginatedResponse,
  SprintReportDto,
  TeamHealthSummaryDto,
} from "./types";

export async function getSprintReport(
  sprintId: string,
  projectId: string,
): Promise<SprintReportDto> {
  const response = await apiClient.get<SprintReportDto>(
    `/sprints/${sprintId}/report`,
    { params: { projectId } },
  );
  return response.data;
}

export async function listSprintReports(
  projectId: string,
  page = 1,
  pageSize = 10,
): Promise<PaginatedResponse<SprintReportDto>> {
  const response = await apiClient.get<PaginatedResponse<SprintReportDto>>(
    `/projects/${projectId}/reports/sprints`,
    { params: { page, pageSize } },
  );
  return response.data;
}

export async function getLatestTeamHealth(
  projectId: string,
): Promise<TeamHealthSummaryDto> {
  const response = await apiClient.get<TeamHealthSummaryDto>(
    `/projects/${projectId}/reports/team-health/latest`,
  );
  return response.data;
}

export async function listTeamHealthSummaries(
  projectId: string,
  page = 1,
  pageSize = 10,
): Promise<PaginatedResponse<TeamHealthSummaryDto>> {
  const response = await apiClient.get<
    PaginatedResponse<TeamHealthSummaryDto>
  >(`/projects/${projectId}/reports/team-health`, {
    params: { page, pageSize },
  });
  return response.data;
}
