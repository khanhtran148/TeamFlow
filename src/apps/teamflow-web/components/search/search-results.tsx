"use client";

import { useFullTextSearch } from "@/lib/hooks/use-search";
import { useSearchStore } from "@/lib/stores/search-store";

export function SearchResults({ projectId }: { projectId: string }) {
  const { q, statusFilter, priorityFilter, typeFilter, assigneeId, sprintId, releaseId, page, pageSize } =
    useSearchStore();
  const setPage = useSearchStore((s) => s.setPage);

  const { data, isLoading } = useFullTextSearch({
    projectId,
    q: q || undefined,
    status: statusFilter.length > 0 ? statusFilter : undefined,
    priority: priorityFilter.length > 0 ? priorityFilter : undefined,
    type: typeFilter.length > 0 ? typeFilter : undefined,
    assigneeId: assigneeId ?? undefined,
    sprintId: sprintId ?? undefined,
    releaseId: releaseId ?? undefined,
    page,
    pageSize,
  });

  if (isLoading) return <div className="text-gray-500">Searching...</div>;

  return (
    <div className="space-y-2">
      <p className="text-sm text-gray-500">
        {data?.totalCount ?? 0} results found
      </p>
      <div className="space-y-1">
        {data?.items.map((item) => (
          <div
            key={item.id}
            className="p-3 bg-white border rounded flex items-center justify-between"
          >
            <div>
              <span className="text-xs text-gray-400 mr-2">{item.type}</span>
              <span className="font-medium">{item.title}</span>
            </div>
            <div className="flex items-center gap-2 text-xs text-gray-500">
              <span>{item.status}</span>
              {item.priority && <span>{item.priority}</span>}
              {item.assigneeName && <span>{item.assigneeName}</span>}
            </div>
          </div>
        ))}
      </div>
      {data && data.totalCount > pageSize && (
        <div className="flex gap-2 justify-center mt-4">
          <button
            disabled={page <= 1}
            onClick={() => setPage(page - 1)}
            className="px-3 py-1 text-sm border rounded disabled:opacity-50"
          >
            Previous
          </button>
          <span className="px-3 py-1 text-sm">Page {page}</span>
          <button
            disabled={!data || page * pageSize >= data.totalCount}
            onClick={() => setPage(page + 1)}
            className="px-3 py-1 text-sm border rounded disabled:opacity-50"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
