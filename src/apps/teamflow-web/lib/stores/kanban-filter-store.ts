import { create } from "zustand";
import type { WorkItemType, Priority } from "@/lib/api/types";

export type KanbanSwimlane = "none" | "assignee" | "epic";

export interface KanbanFilters {
  type: WorkItemType | "";
  priority: Priority | "";
  assigneeId: string;
  releaseId: string;
  swimlane: KanbanSwimlane;
}

interface KanbanFilterStore {
  filters: KanbanFilters;
  setType: (type: WorkItemType | "") => void;
  setPriority: (priority: Priority | "") => void;
  setAssigneeId: (assigneeId: string) => void;
  setReleaseId: (releaseId: string) => void;
  setSwimlane: (swimlane: KanbanSwimlane) => void;
  resetFilters: () => void;
}

const DEFAULT_FILTERS: KanbanFilters = {
  type: "",
  priority: "",
  assigneeId: "",
  releaseId: "",
  swimlane: "none",
};

export const useKanbanFilterStore = create<KanbanFilterStore>((set) => ({
  filters: { ...DEFAULT_FILTERS },

  setType: (type) =>
    set((state) => ({ filters: { ...state.filters, type } })),

  setPriority: (priority) =>
    set((state) => ({ filters: { ...state.filters, priority } })),

  setAssigneeId: (assigneeId) =>
    set((state) => ({ filters: { ...state.filters, assigneeId } })),

  setReleaseId: (releaseId) =>
    set((state) => ({ filters: { ...state.filters, releaseId } })),

  setSwimlane: (swimlane) =>
    set((state) => ({ filters: { ...state.filters, swimlane } })),

  resetFilters: () => set({ filters: { ...DEFAULT_FILTERS } }),
}));
