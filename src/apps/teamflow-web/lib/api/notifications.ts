import { apiClient } from "./client";
import type {
  PaginatedResponse,
  InAppNotificationDto,
  NotificationPreferenceDto,
  UnreadCountDto,
} from "./types";

export async function getNotifications(params: {
  isRead?: boolean;
  page?: number;
  pageSize?: number;
}): Promise<PaginatedResponse<InAppNotificationDto>> {
  const response = await apiClient.get<
    PaginatedResponse<InAppNotificationDto>
  >("/notifications", { params });
  return response.data;
}

export async function getUnreadCount(): Promise<UnreadCountDto> {
  const response =
    await apiClient.get<UnreadCountDto>("/notifications/unread-count");
  return response.data;
}

export async function markAsRead(id: string): Promise<void> {
  await apiClient.post(`/notifications/${id}/read`);
}

export async function markAllAsRead(): Promise<void> {
  await apiClient.post("/notifications/read-all");
}

export async function getNotificationPreferences(): Promise<
  NotificationPreferenceDto[]
> {
  const response = await apiClient.get<{
    preferences: NotificationPreferenceDto[];
  }>("/notifications/preferences");
  return response.data.preferences ?? response.data;
}

export async function updateNotificationPreferences(
  preferences: NotificationPreferenceDto[],
): Promise<void> {
  await apiClient.put("/notifications/preferences", { preferences });
}
