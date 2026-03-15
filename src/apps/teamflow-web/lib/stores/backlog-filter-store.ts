import { create } from "zustand";
import type { WorkItemType, Priority } from "@/lib/api/types";

export interface BacklogFilters {
  search: string;
  type: WorkItemType | "";
  priority: Priority | "";
  assigneeId: string;
  releaseId: string;
  blockedOnly: boolean;
  viewMode: "grouped" | "flat";
}

interface BacklogFilterStore {
  filters: BacklogFilters;
  setSearch: (search: string) => void;
  setType: (type: WorkItemType | "") => void;
  setPriority: (priority: Priority | "") => void;
  setAssigneeId: (assigneeId: string) => void;
  setReleaseId: (releaseId: string) => void;
  setBlockedOnly: (blockedOnly: boolean) => void;
  setViewMode: (viewMode: "grouped" | "flat") => void;
  resetFilters: () => void;
}

const DEFAULT_FILTERS: BacklogFilters = {
  search: "",
  type: "",
  priority: "",
  assigneeId: "",
  releaseId: "",
  blockedOnly: false,
  viewMode: "grouped",
};

export const useBacklogFilterStore = create<BacklogFilterStore>((set) => ({
  filters: { ...DEFAULT_FILTERS },

  setSearch: (search) =>
    set((state) => ({ filters: { ...state.filters, search } })),

  setType: (type) =>
    set((state) => ({ filters: { ...state.filters, type } })),

  setPriority: (priority) =>
    set((state) => ({ filters: { ...state.filters, priority } })),

  setAssigneeId: (assigneeId) =>
    set((state) => ({ filters: { ...state.filters, assigneeId } })),

  setReleaseId: (releaseId) =>
    set((state) => ({ filters: { ...state.filters, releaseId } })),

  setBlockedOnly: (blockedOnly) =>
    set((state) => ({ filters: { ...state.filters, blockedOnly } })),

  setViewMode: (viewMode) =>
    set((state) => ({ filters: { ...state.filters, viewMode } })),

  resetFilters: () => set({ filters: { ...DEFAULT_FILTERS } }),
}));
