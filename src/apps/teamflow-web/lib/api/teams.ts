import { apiClient } from "./client";
import type { PaginatedResponse } from "./types";

// ---- Team DTOs ----

export interface TeamDto {
  id: string;
  orgId: string;
  name: string;
  description: string | null;
  memberCount: number;
  createdAt: string;
}

export interface TeamMemberDto {
  id: string;
  userId: string;
  userName: string;
  userEmail: string;
  role: string;
  joinedAt: string;
}

export interface TeamDetailDto extends TeamDto {
  members: TeamMemberDto[];
}

export interface ProjectMembershipDto {
  id: string;
  projectId: string;
  memberId: string;
  memberType: string;
  memberName: string;
  role: string;
  createdAt: string;
}

// ---- Team API functions ----

export async function getTeams(
  orgId: string,
  page = 1,
  pageSize = 20,
): Promise<PaginatedResponse<TeamDto>> {
  const { data } = await apiClient.get<PaginatedResponse<TeamDto>>("/teams", {
    params: { orgId, page, pageSize },
  });
  return data;
}

export async function getTeam(teamId: string): Promise<TeamDetailDto> {
  const { data } = await apiClient.get<TeamDetailDto>(`/teams/${teamId}`);
  return data;
}

export async function createTeam(body: {
  orgId: string;
  name: string;
  description?: string;
}): Promise<TeamDto> {
  const { data } = await apiClient.post<TeamDto>("/teams", body);
  return data;
}

export async function updateTeam(
  teamId: string,
  body: { name: string; description?: string },
): Promise<TeamDto> {
  const { data } = await apiClient.put<TeamDto>(`/teams/${teamId}`, body);
  return data;
}

export async function deleteTeam(teamId: string): Promise<void> {
  await apiClient.delete(`/teams/${teamId}`);
}

export async function addTeamMember(
  teamId: string,
  body: { userId: string; role: string },
): Promise<void> {
  await apiClient.post(`/teams/${teamId}/members`, body);
}

export async function removeTeamMember(
  teamId: string,
  userId: string,
): Promise<void> {
  await apiClient.delete(`/teams/${teamId}/members/${userId}`);
}

export async function changeTeamMemberRole(
  teamId: string,
  userId: string,
  body: { newRole: string },
): Promise<void> {
  await apiClient.put(`/teams/${teamId}/members/${userId}/role`, body);
}

// ---- Project Membership API functions ----

export async function getProjectMemberships(
  projectId: string,
): Promise<ProjectMembershipDto[]> {
  const { data } = await apiClient.get<ProjectMembershipDto[]>(
    `/projects/${projectId}/memberships`,
  );
  return data;
}

export async function addProjectMember(
  projectId: string,
  body: { memberId: string; memberType: string; role: string },
): Promise<void> {
  await apiClient.post(`/projects/${projectId}/memberships`, body);
}

export async function removeProjectMember(
  projectId: string,
  membershipId: string,
): Promise<void> {
  await apiClient.delete(
    `/projects/${projectId}/memberships/${membershipId}`,
  );
}
