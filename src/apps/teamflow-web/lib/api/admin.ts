import { apiClient } from "./client";
import type { AdminOrganizationDto, AdminUserDto } from "./types";

export async function getAdminOrganizations(): Promise<AdminOrganizationDto[]> {
  const response = await apiClient.get<AdminOrganizationDto[]>("/admin/organizations");
  return response.data;
}

export async function getAdminUsers(): Promise<AdminUserDto[]> {
  const response = await apiClient.get<AdminUserDto[]>("/admin/users");
  return response.data;
}
