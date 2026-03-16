import { apiClient } from "./client";
import type { OrganizationDto, MyOrganizationDto } from "./types";

export interface CreateOrganizationBody {
  name: string;
  slug?: string;
}

export interface UpdateOrganizationBody {
  name: string;
  slug: string;
}

export async function listMyOrgs(): Promise<MyOrganizationDto[]> {
  const response = await apiClient.get<MyOrganizationDto[]>("/me/organizations");
  return response.data;
}

export async function getOrgBySlug(slug: string): Promise<OrganizationDto> {
  const response = await apiClient.get<OrganizationDto>(`/organizations/by-slug/${slug}`);
  return response.data;
}

export async function createOrg(data: CreateOrganizationBody): Promise<OrganizationDto> {
  const response = await apiClient.post<OrganizationDto>("/organizations", data);
  return response.data;
}

export async function updateOrg(id: string, data: UpdateOrganizationBody): Promise<OrganizationDto> {
  const response = await apiClient.put<OrganizationDto>(`/organizations/${id}`, data);
  return response.data;
}
