// Phase 6 stub — member management API functions will be implemented in Phase 6
import { apiClient } from "./client";
import type { OrganizationMemberDto, OrgRole } from "./types";

export async function listOrgMembers(orgId: string): Promise<OrganizationMemberDto[]> {
  const response = await apiClient.get<OrganizationMemberDto[]>(
    `/organizations/${orgId}/members`,
  );
  return response.data;
}

export async function changeOrgMemberRole(
  orgId: string,
  userId: string,
  role: OrgRole,
): Promise<void> {
  await apiClient.put(`/organizations/${orgId}/members/${userId}/role`, { role });
}

export async function removeOrgMember(orgId: string, userId: string): Promise<void> {
  await apiClient.delete(`/organizations/${orgId}/members/${userId}`);
}
