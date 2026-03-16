import { apiClient } from "./client";
import type {
  InvitationDto,
  CreateInvitationResponse,
  AcceptInvitationResponse,
  OrgRole,
} from "./types";

export interface CreateInvitationBody {
  email?: string;
  role: OrgRole;
}

export async function createInvitation(
  orgId: string,
  data: CreateInvitationBody,
): Promise<CreateInvitationResponse> {
  const response = await apiClient.post<CreateInvitationResponse>(
    `/organizations/${orgId}/invitations`,
    data,
  );
  return response.data;
}

export async function listInvitations(orgId: string): Promise<InvitationDto[]> {
  const response = await apiClient.get<InvitationDto[]>(
    `/organizations/${orgId}/invitations`,
  );
  return response.data;
}

export async function acceptInvitation(token: string): Promise<AcceptInvitationResponse> {
  const response = await apiClient.post<AcceptInvitationResponse>(
    `/invitations/${token}/accept`,
  );
  return response.data;
}

export async function revokeInvitation(id: string): Promise<void> {
  await apiClient.delete(`/invitations/${id}`);
}

export async function listPendingInvitations(): Promise<InvitationDto[]> {
  const response = await apiClient.get<InvitationDto[]>("/invitations/pending");
  return response.data;
}
