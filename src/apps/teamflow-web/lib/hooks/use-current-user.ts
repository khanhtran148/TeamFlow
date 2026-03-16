import { useQuery } from "@tanstack/react-query";
import { getCurrentUser } from "@/lib/api/users";
import { useAuthStore } from "@/lib/stores/auth-store";

export function useCurrentUser() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  return useQuery({
    queryKey: ["current-user"],
    queryFn: getCurrentUser,
    enabled: isAuthenticated,
    staleTime: 5 * 60_000,
  });
}

export function useCurrentUserOrgs() {
  const { data, ...rest } = useCurrentUser();
  return {
    ...rest,
    data: data?.organizations ?? [],
    defaultOrgId: data?.organizations?.[0]?.orgId ?? null,
  };
}
