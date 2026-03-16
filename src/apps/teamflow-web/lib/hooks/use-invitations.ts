import {
  useQuery,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import {
  createInvitation,
  listInvitations,
  acceptInvitation,
  revokeInvitation,
  listPendingInvitations,
  type CreateInvitationBody,
} from "@/lib/api/invitations";

// ---- Query keys ----

export const invitationKeys = {
  all: ["invitations"] as const,
  byOrg: (orgId: string) => ["invitations", "org", orgId] as const,
  pending: () => ["invitations", "pending"] as const,
};

// ---- Queries ----

export function useInvitations(orgId: string) {
  return useQuery({
    queryKey: invitationKeys.byOrg(orgId),
    queryFn: () => listInvitations(orgId),
    enabled: !!orgId,
  });
}

export function usePendingInvitations() {
  return useQuery({
    queryKey: invitationKeys.pending(),
    queryFn: listPendingInvitations,
  });
}

// ---- Mutations ----

export function useCreateInvitation(orgId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateInvitationBody) => createInvitation(orgId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: invitationKeys.byOrg(orgId) });
    },
  });
}

export function useAcceptInvitation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (token: string) => acceptInvitation(token),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: invitationKeys.all });
      queryClient.invalidateQueries({ queryKey: ["organizations"] });
    },
  });
}

export function useRevokeInvitation(orgId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => revokeInvitation(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: invitationKeys.byOrg(orgId) });
    },
  });
}
