"use client";

import { useParams } from "next/navigation";
import { SearchInput } from "@/components/search/search-input";
import { FilterPanel } from "@/components/search/filter-panel";
import { SavedFilterList } from "@/components/search/saved-filter-list";
import { SearchResults } from "@/components/search/search-results";

export default function SearchPage() {
  const { projectId } = useParams<{ projectId: string }>();

  return (
    <div className="flex gap-6">
      <aside className="w-64 shrink-0">
        <SavedFilterList projectId={projectId} />
      </aside>
      <div className="flex-1 space-y-4">
        <SearchInput />
        <FilterPanel projectId={projectId} />
        <SearchResults projectId={projectId} />
      </div>
    </div>
  );
}
