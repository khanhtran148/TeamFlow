"use client";

import { useSavedFilters, useDeleteSavedFilter } from "@/lib/hooks/use-search";
import { useSearchStore } from "@/lib/stores/search-store";
import type { SavedFilterDto } from "@/lib/api/types";

export function SavedFilterList({ projectId }: { projectId: string }) {
  const { data: filters, isLoading } = useSavedFilters(projectId);
  const deleteFilter = useDeleteSavedFilter(projectId);
  const { setQuery, setStatusFilter, setPriorityFilter, setTypeFilter } =
    useSearchStore();

  const applyFilter = (filter: SavedFilterDto) => {
    const json = filter.filterJson as Record<string, unknown>;
    if (json.q) setQuery(json.q as string);
    if (json.status) setStatusFilter(json.status as []);
    if (json.priority) setPriorityFilter(json.priority as []);
    if (json.type) setTypeFilter(json.type as []);
  };

  if (isLoading) return <div className="text-sm text-gray-500">Loading...</div>;

  return (
    <div className="space-y-2">
      <h3 className="text-sm font-semibold text-gray-700">Saved Filters</h3>
      {(!filters || filters.length === 0) && (
        <p className="text-xs text-gray-400">No saved filters</p>
      )}
      {filters?.map((f) => (
        <div
          key={f.id}
          className="flex items-center justify-between p-2 bg-white border rounded text-sm"
        >
          <button
            onClick={() => applyFilter(f)}
            className="text-blue-600 hover:underline truncate flex-1 text-left"
          >
            {f.name}
          </button>
          <button
            onClick={() => deleteFilter.mutate(f.id)}
            className="text-red-400 hover:text-red-600 ml-2 text-xs"
          >
            Delete
          </button>
        </div>
      ))}
    </div>
  );
}
