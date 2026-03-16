import { apiClient } from "./client";

export interface CurrentUserDto {
  id: string;
  email: string;
  name: string;
  organizations: UserOrganizationDto[];
}

export interface UserOrganizationDto {
  orgId: string;
  orgName: string;
}

export async function getCurrentUser(): Promise<CurrentUserDto> {
  const { data } = await apiClient.get<CurrentUserDto>("/users/me");
  return data;
}
