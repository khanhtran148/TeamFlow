"use client";

import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type { PaginatedResponse } from "@/lib/api/types";

export interface WorkItemHistoryDto {
  id: string;
  actorId: string | null;
  actorName: string | null;
  actorType: string;
  actionType: string;
  fieldName: string | null;
  oldValue: string | null;
  newValue: string | null;
  createdAt: string;
}

async function getWorkItemHistory(
  workItemId: string,
  page: number,
  pageSize: number,
): Promise<PaginatedResponse<WorkItemHistoryDto>> {
  const { data } = await apiClient.get<PaginatedResponse<WorkItemHistoryDto>>(
    `/workitems/${workItemId}/history`,
    { params: { page, pageSize } },
  );
  return data;
}

export function useWorkItemHistory(
  workItemId: string | undefined,
  page = 1,
  pageSize = 20,
) {
  return useQuery({
    queryKey: ["work-item-history", workItemId, page, pageSize],
    queryFn: () => getWorkItemHistory(workItemId!, page, pageSize),
    enabled: !!workItemId,
  });
}
