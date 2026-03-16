import { apiClient } from "./client";
import type {
  UserProfileDto,
  UpdateProfileBody,
  ActivityLogItemDto,
  PagedResult,
} from "./types";

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

export async function getProfile(): Promise<UserProfileDto> {
  const { data } = await apiClient.get<UserProfileDto>("/users/me/profile");
  return data;
}

export async function updateProfile(
  body: UpdateProfileBody,
): Promise<UserProfileDto> {
  const { data } = await apiClient.put<UserProfileDto>(
    "/users/me/profile",
    body,
  );
  return data;
}

export async function getActivityLog(params: {
  page?: number;
  pageSize?: number;
}): Promise<PagedResult<ActivityLogItemDto>> {
  const { data } = await apiClient.get<PagedResult<ActivityLogItemDto>>(
    "/users/me/activity",
    { params },
  );
  return data;
}
