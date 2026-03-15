import { apiClient } from "./client";

export interface MyPermissionsDto {
  role: string | null;
  permissions: string[];
}

export async function getMyPermissions(
  projectId: string,
): Promise<MyPermissionsDto> {
  const { data } = await apiClient.get<MyPermissionsDto>(
    `/projects/${projectId}/memberships/me`,
  );
  return data;
}
