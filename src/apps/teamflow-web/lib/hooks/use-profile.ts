import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getProfile, updateProfile, getActivityLog } from "@/lib/api/users";
import type { UserProfileDto, UpdateProfileBody, ActivityLogItemDto, PagedResult } from "@/lib/api/types";

export const profileKeys = {
  profile: ["profile"] as const,
  activity: (page: number, pageSize: number) =>
    ["profile", "activity", page, pageSize] as const,
};

export function useProfile() {
  return useQuery<UserProfileDto>({
    queryKey: profileKeys.profile,
    queryFn: () => getProfile(),
  });
}

export function useUpdateProfile() {
  const queryClient = useQueryClient();

  return useMutation<UserProfileDto, Error, UpdateProfileBody>({
    mutationFn: (body: UpdateProfileBody) => updateProfile(body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.profile });
    },
  });
}

export function useActivityLog(page: number = 1, pageSize: number = 20) {
  return useQuery<PagedResult<ActivityLogItemDto>>({
    queryKey: profileKeys.activity(page, pageSize),
    queryFn: () => getActivityLog({ page, pageSize }),
  });
}
