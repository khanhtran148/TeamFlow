import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  getAdminUsers,
  getAdminOrganizations,
  resetUserPassword,
  changeUserStatus,
  changeOrgStatus,
  updateAdminOrg,
  transferOrgOwnership,
} from "@/lib/api/admin";
import type {
  AdminListParams,
  AdminResetPasswordRequest,
  ChangeStatusRequest,
  AdminUpdateOrgRequest,
  TransferOwnershipRequest,
} from "@/lib/api/types";

// ---- Query keys ----

export const adminKeys = {
  all: ["admin"] as const,
  users: (params?: AdminListParams) =>
    ["admin", "users", params ?? {}] as const,
  organizations: (params?: AdminListParams) =>
    ["admin", "organizations", params ?? {}] as const,
};

// ---- Queries ----

export function useAdminUsers(params?: AdminListParams) {
  return useQuery({
    queryKey: adminKeys.users(params),
    queryFn: () => getAdminUsers(params),
  });
}

export function useAdminOrganizations(params?: AdminListParams) {
  return useQuery({
    queryKey: adminKeys.organizations(params),
    queryFn: () => getAdminOrganizations(params),
  });
}

// ---- User mutations ----

export function useResetUserPassword() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      userId,
      body,
    }: {
      userId: string;
      body: AdminResetPasswordRequest;
    }) => resetUserPassword(userId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.all });
    },
  });
}

export function useChangeUserStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      userId,
      body,
    }: {
      userId: string;
      body: ChangeStatusRequest;
    }) => changeUserStatus(userId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.all });
    },
  });
}

// ---- Organization mutations ----

export function useChangeOrgStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      orgId,
      body,
    }: {
      orgId: string;
      body: ChangeStatusRequest;
    }) => changeOrgStatus(orgId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.all });
    },
  });
}

export function useUpdateAdminOrg() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      orgId,
      body,
    }: {
      orgId: string;
      body: AdminUpdateOrgRequest;
    }) => updateAdminOrg(orgId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.all });
    },
  });
}

export function useTransferOrgOwnership() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      orgId,
      body,
    }: {
      orgId: string;
      body: TransferOwnershipRequest;
    }) => transferOrgOwnership(orgId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.all });
    },
  });
}
