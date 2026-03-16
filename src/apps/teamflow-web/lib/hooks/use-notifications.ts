import {
  useQuery,
  useMutation,
  useQueryClient,
  type UseQueryOptions,
} from "@tanstack/react-query";
import {
  getNotifications,
  getUnreadCount,
  markAsRead,
  markAllAsRead,
  getNotificationPreferences,
  updateNotificationPreferences,
} from "@/lib/api/notifications";
import type {
  PaginatedResponse,
  InAppNotificationDto,
  NotificationPreferenceDto,
  UnreadCountDto,
} from "@/lib/api/types";

export const notificationKeys = {
  all: ["notifications"] as const,
  list: (params: { isRead?: boolean; page?: number; pageSize?: number }) =>
    ["notifications", params] as const,
  unreadCount: ["notifications", "unread-count"] as const,
  preferences: ["notifications", "preferences"] as const,
};

export function useNotifications(
  params: { isRead?: boolean; page?: number; pageSize?: number } = {},
  options?: Partial<UseQueryOptions<PaginatedResponse<InAppNotificationDto>>>,
) {
  return useQuery({
    queryKey: notificationKeys.list(params),
    queryFn: () => getNotifications(params),
    ...options,
  });
}

export function useUnreadCount(
  options?: Partial<UseQueryOptions<UnreadCountDto>>,
) {
  return useQuery({
    queryKey: notificationKeys.unreadCount,
    queryFn: () => getUnreadCount(),
    refetchInterval: 30_000,
    ...options,
  });
}

export function useMarkAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => markAsRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
}

export function useMarkAllAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => markAllAsRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
}

export function useNotificationPreferences(
  options?: Partial<UseQueryOptions<NotificationPreferenceDto[]>>,
) {
  return useQuery({
    queryKey: notificationKeys.preferences,
    queryFn: () => getNotificationPreferences(),
    ...options,
  });
}

export function useUpdatePreferences() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (preferences: NotificationPreferenceDto[]) =>
      updateNotificationPreferences(preferences),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: notificationKeys.preferences,
      });
    },
  });
}
