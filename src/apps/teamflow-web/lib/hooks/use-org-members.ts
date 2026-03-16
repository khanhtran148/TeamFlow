import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  listOrgMembers,
  changeOrgMemberRole,
  removeOrgMember,
} from "@/lib/api/members";
import type { OrgRole } from "@/lib/api/types";

// ---- Query keys ----

export const orgMemberKeys = {
  all: ["orgMembers"] as const,
  byOrg: (orgId: string) => ["orgMembers", "org", orgId] as const,
};

// ---- Query ----

export function useOrgMembers(orgId: string) {
  return useQuery({
    queryKey: orgMemberKeys.byOrg(orgId),
    queryFn: () => listOrgMembers(orgId),
    enabled: !!orgId,
  });
}

// ---- Mutations ----

export function useChangeOrgMemberRole(orgId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ userId, role }: { userId: string; role: OrgRole }) =>
      changeOrgMemberRole(orgId, userId, role),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: orgMemberKeys.byOrg(orgId) });
    },
  });
}

export function useRemoveOrgMember(orgId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (userId: string) => removeOrgMember(orgId, userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: orgMemberKeys.byOrg(orgId) });
    },
  });
}
