"use client";

import { useSearchStore } from "@/lib/stores/search-store";
import { useDebounce } from "@/lib/hooks/use-debounce";
import { useEffect, useState } from "react";

export function SearchInput() {
  const { q, setQuery } = useSearchStore();
  const [localValue, setLocalValue] = useState(q);
  const debouncedValue = useDebounce(localValue, 300);

  useEffect(() => {
    setQuery(debouncedValue);
  }, [debouncedValue, setQuery]);

  return (
    <input
      type="text"
      placeholder="Search work items..."
      value={localValue}
      onChange={(e) => setLocalValue(e.target.value)}
      className="w-full px-4 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
    />
  );
}
