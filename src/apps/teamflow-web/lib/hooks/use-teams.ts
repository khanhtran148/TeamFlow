"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  getTeams,
  getTeam,
  createTeam,
  updateTeam,
  deleteTeam,
  addTeamMember,
  removeTeamMember,
  changeTeamMemberRole,
} from "@/lib/api/teams";

export function useTeams(orgId: string | undefined, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ["teams", orgId, page, pageSize],
    queryFn: () => getTeams(orgId!, page, pageSize),
    enabled: !!orgId,
  });
}

export function useTeam(teamId: string | undefined) {
  return useQuery({
    queryKey: ["team", teamId],
    queryFn: () => getTeam(teamId!),
    enabled: !!teamId,
  });
}

export function useCreateTeam() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createTeam,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["teams"] });
    },
  });
}

export function useUpdateTeam() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ teamId, ...body }: { teamId: string; name: string; description?: string }) =>
      updateTeam(teamId, body),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ["teams"] });
      queryClient.invalidateQueries({ queryKey: ["team", vars.teamId] });
    },
  });
}

export function useDeleteTeam() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: deleteTeam,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["teams"] });
    },
  });
}

export function useAddTeamMember() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ teamId, ...body }: { teamId: string; userId: string; role: string }) =>
      addTeamMember(teamId, body),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ["team", vars.teamId] });
      queryClient.invalidateQueries({ queryKey: ["teams"] });
    },
  });
}

export function useRemoveTeamMember() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ teamId, userId }: { teamId: string; userId: string }) =>
      removeTeamMember(teamId, userId),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ["team", vars.teamId] });
      queryClient.invalidateQueries({ queryKey: ["teams"] });
    },
  });
}

export function useChangeTeamMemberRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      teamId,
      userId,
      newRole,
    }: {
      teamId: string;
      userId: string;
      newRole: string;
    }) => changeTeamMemberRole(teamId, userId, { newRole }),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ["team", vars.teamId] });
    },
  });
}
