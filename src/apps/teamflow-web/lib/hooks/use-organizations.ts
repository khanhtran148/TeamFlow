import {
  useQuery,
  useMutation,
  useQueryClient,
  type UseQueryOptions,
} from "@tanstack/react-query";
import {
  listMyOrgs,
  getOrgBySlug,
  createOrg,
  updateOrg,
  type CreateOrganizationBody,
  type UpdateOrganizationBody,
} from "@/lib/api/organizations";
import type { MyOrganizationDto, OrganizationDto } from "@/lib/api/types";

// ---- Query keys ----

export const orgKeys = {
  all: ["organizations"] as const,
  myOrgs: () => ["organizations", "my"] as const,
  bySlug: (slug: string) => ["organizations", "slug", slug] as const,
};

// ---- Queries ----

export function useMyOrganizations(
  options?: Partial<UseQueryOptions<MyOrganizationDto[]>>,
) {
  return useQuery({
    queryKey: orgKeys.myOrgs(),
    queryFn: listMyOrgs,
    ...options,
  });
}

export function useOrganizationBySlug(
  slug: string,
  options?: Partial<UseQueryOptions<OrganizationDto>>,
) {
  return useQuery({
    queryKey: orgKeys.bySlug(slug),
    queryFn: () => getOrgBySlug(slug),
    enabled: !!slug,
    ...options,
  });
}

// ---- Mutations ----

export function useCreateOrganization() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateOrganizationBody) => createOrg(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: orgKeys.all });
    },
  });
}

export function useUpdateOrganization() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateOrganizationBody }) =>
      updateOrg(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: orgKeys.all });
    },
  });
}
