import { create } from "zustand";
import type {
  WorkItemStatus,
  Priority,
  WorkItemType,
} from "@/lib/api/types";

interface SearchState {
  q: string;
  statusFilter: WorkItemStatus[];
  priorityFilter: Priority[];
  typeFilter: WorkItemType[];
  assigneeId: string | null;
  sprintId: string | null;
  releaseId: string | null;
  page: number;
  pageSize: number;

  setQuery: (q: string) => void;
  setStatusFilter: (status: WorkItemStatus[]) => void;
  setPriorityFilter: (priority: Priority[]) => void;
  setTypeFilter: (type: WorkItemType[]) => void;
  setAssigneeId: (id: string | null) => void;
  setSprintId: (id: string | null) => void;
  setReleaseId: (id: string | null) => void;
  setPage: (page: number) => void;
  resetFilters: () => void;
}

const initialState = {
  q: "",
  statusFilter: [] as WorkItemStatus[],
  priorityFilter: [] as Priority[],
  typeFilter: [] as WorkItemType[],
  assigneeId: null as string | null,
  sprintId: null as string | null,
  releaseId: null as string | null,
  page: 1,
  pageSize: 20,
};

export const useSearchStore = create<SearchState>((set) => ({
  ...initialState,

  setQuery: (q) => set({ q, page: 1 }),
  setStatusFilter: (statusFilter) => set({ statusFilter, page: 1 }),
  setPriorityFilter: (priorityFilter) => set({ priorityFilter, page: 1 }),
  setTypeFilter: (typeFilter) => set({ typeFilter, page: 1 }),
  setAssigneeId: (assigneeId) => set({ assigneeId, page: 1 }),
  setSprintId: (sprintId) => set({ sprintId, page: 1 }),
  setReleaseId: (releaseId) => set({ releaseId, page: 1 }),
  setPage: (page) => set({ page }),
  resetFilters: () => set(initialState),
}));
