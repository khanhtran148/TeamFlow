import { apiClient } from "./client";
import type {
  AdminOrganizationDto,
  AdminUserDto,
  AdminListParams,
  AdminResetPasswordRequest,
  ChangeStatusRequest,
  AdminUpdateOrgRequest,
  TransferOwnershipRequest,
  PagedResult,
} from "./types";

// ---- Users ----

export async function getAdminUsers(
  params?: AdminListParams,
): Promise<PagedResult<AdminUserDto>> {
  const response = await apiClient.get<PagedResult<AdminUserDto>>(
    "/admin/users",
    { params },
  );
  return response.data;
}

export async function resetUserPassword(
  userId: string,
  body: AdminResetPasswordRequest,
): Promise<void> {
  await apiClient.post(`/admin/users/${userId}/reset-password`, body);
}

export async function changeUserStatus(
  userId: string,
  body: ChangeStatusRequest,
): Promise<void> {
  await apiClient.put(`/admin/users/${userId}/status`, body);
}

// ---- Organizations ----

export async function getAdminOrganizations(
  params?: AdminListParams,
): Promise<PagedResult<AdminOrganizationDto>> {
  const response = await apiClient.get<PagedResult<AdminOrganizationDto>>(
    "/admin/organizations",
    { params },
  );
  return response.data;
}

export async function changeOrgStatus(
  orgId: string,
  body: ChangeStatusRequest,
): Promise<void> {
  await apiClient.put(`/admin/organizations/${orgId}/status`, body);
}

export async function updateAdminOrg(
  orgId: string,
  body: AdminUpdateOrgRequest,
): Promise<void> {
  await apiClient.put(`/admin/organizations/${orgId}`, body);
}

export async function transferOrgOwnership(
  orgId: string,
  body: TransferOwnershipRequest,
): Promise<void> {
  await apiClient.put(`/admin/organizations/${orgId}/owner`, body);
}
