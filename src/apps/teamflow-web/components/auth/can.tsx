"use client";

import type { ReactNode } from "react";
import { useHasPermission } from "@/lib/hooks/use-permission";

interface CanProps {
  projectId: string | undefined;
  permission: string;
  children: ReactNode;
  fallback?: ReactNode;
}

/**
 * Conditionally renders children based on whether the current user
 * has the specified permission on the project.
 *
 * Usage:
 * <Can projectId={projectId} permission="WorkItem_Create">
 *   <button>Create Work Item</button>
 * </Can>
 */
export function Can({ projectId, permission, children, fallback = null }: CanProps) {
  const allowed = useHasPermission(projectId, permission);

  if (!allowed) return <>{fallback}</>;
  return <>{children}</>;
}
