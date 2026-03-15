"use client";

import { useQuery } from "@tanstack/react-query";
import { getMyPermissions } from "@/lib/api/permissions";

export function useMyPermissions(projectId: string | undefined) {
  return useQuery({
    queryKey: ["my-permissions", projectId],
    queryFn: () => getMyPermissions(projectId!),
    enabled: !!projectId,
    staleTime: 60_000, // Cache permissions for 1 minute
  });
}

export function useHasPermission(
  projectId: string | undefined,
  permission: string,
): boolean {
  const { data } = useMyPermissions(projectId);
  if (!data) return false;
  return data.permissions.includes(permission);
}

export function useRole(projectId: string | undefined): string | null {
  const { data } = useMyPermissions(projectId);
  return data?.role ?? null;
}
