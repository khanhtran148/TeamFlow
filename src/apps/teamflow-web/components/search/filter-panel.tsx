"use client";

import { useSearchStore } from "@/lib/stores/search-store";
import type { WorkItemStatus, Priority, WorkItemType } from "@/lib/api/types";

const statuses: WorkItemStatus[] = ["ToDo", "InProgress", "InReview", "Done", "Rejected"];
const priorities: Priority[] = ["Critical", "High", "Medium", "Low"];
const types: WorkItemType[] = ["Epic", "UserStory", "Task", "Bug", "Spike"];

export function FilterPanel({ projectId: _projectId }: { projectId: string }) {
  const { statusFilter, priorityFilter, typeFilter, setStatusFilter, setPriorityFilter, setTypeFilter, resetFilters } =
    useSearchStore();

  const toggleStatus = (s: WorkItemStatus) => {
    setStatusFilter(
      statusFilter.includes(s)
        ? statusFilter.filter((x) => x !== s)
        : [...statusFilter, s],
    );
  };

  const togglePriority = (p: Priority) => {
    setPriorityFilter(
      priorityFilter.includes(p)
        ? priorityFilter.filter((x) => x !== p)
        : [...priorityFilter, p],
    );
  };

  const toggleType = (t: WorkItemType) => {
    setTypeFilter(
      typeFilter.includes(t)
        ? typeFilter.filter((x) => x !== t)
        : [...typeFilter, t],
    );
  };

  return (
    <div className="flex flex-wrap gap-4 p-3 bg-gray-50 rounded-lg">
      <div>
        <span className="text-xs font-medium text-gray-500">Status</span>
        <div className="flex gap-1 mt-1">
          {statuses.map((s) => (
            <button
              key={s}
              onClick={() => toggleStatus(s)}
              className={`px-2 py-1 text-xs rounded ${statusFilter.includes(s) ? "bg-blue-600 text-white" : "bg-white border"}`}
            >
              {s}
            </button>
          ))}
        </div>
      </div>
      <div>
        <span className="text-xs font-medium text-gray-500">Priority</span>
        <div className="flex gap-1 mt-1">
          {priorities.map((p) => (
            <button
              key={p}
              onClick={() => togglePriority(p)}
              className={`px-2 py-1 text-xs rounded ${priorityFilter.includes(p) ? "bg-blue-600 text-white" : "bg-white border"}`}
            >
              {p}
            </button>
          ))}
        </div>
      </div>
      <div>
        <span className="text-xs font-medium text-gray-500">Type</span>
        <div className="flex gap-1 mt-1">
          {types.map((t) => (
            <button
              key={t}
              onClick={() => toggleType(t)}
              className={`px-2 py-1 text-xs rounded ${typeFilter.includes(t) ? "bg-blue-600 text-white" : "bg-white border"}`}
            >
              {t}
            </button>
          ))}
        </div>
      </div>
      <button
        onClick={resetFilters}
        className="self-end px-3 py-1 text-xs text-gray-600 hover:text-gray-900"
      >
        Clear all
      </button>
    </div>
  );
}
