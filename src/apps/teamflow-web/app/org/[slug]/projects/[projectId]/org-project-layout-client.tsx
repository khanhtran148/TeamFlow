"use client";

import { type ReactNode, useEffect } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { TopBar } from "@/components/layout/top-bar";
import { Breadcrumb } from "@/components/layout/breadcrumb";
import { OrgSwitcher } from "@/components/layout/org-switcher";
import { ProjectNav } from "@/components/projects/project-nav";
import { ProjectProvider } from "@/lib/contexts/project-context";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { useProject } from "@/lib/hooks/use-projects";
import { useProjectGroup } from "@/lib/signalr/signalr-provider";
import { useBacklogFilterStore } from "@/lib/stores/backlog-filter-store";
import { useKanbanFilterStore } from "@/lib/stores/kanban-filter-store";

interface OrgProjectLayoutClientProps {
  slug: string;
  projectId: string;
  children: ReactNode;
}

function resolveActiveTab(pathname: string, projectId: string): string {
  if (pathname.includes(`/projects/${projectId}/board`)) return "Board";
  if (pathname.includes(`/projects/${projectId}/releases`)) return "Releases";
  if (pathname.includes(`/projects/${projectId}/retros`)) return "Retros";
  if (pathname.includes(`/projects/${projectId}/sprints`)) return "Sprints";
  return "Backlog";
}

export function OrgProjectLayoutClient({
  slug,
  projectId,
  children,
}: OrgProjectLayoutClientProps) {
  const pathname = usePathname();
  const { data: project, isLoading, isError } = useProject(projectId);

  useProjectGroup(projectId);

  const resetBacklogFilters = useBacklogFilterStore((s) => s.resetFilters);
  const resetKanbanFilters = useKanbanFilterStore((s) => s.resetFilters);
  useEffect(() => {
    resetBacklogFilters();
    resetKanbanFilters();
  }, [projectId, resetBacklogFilters, resetKanbanFilters]);

  const activeTab = project ? resolveActiveTab(pathname, projectId) : "";

  const breadcrumb = project ? (
    <Breadcrumb
      segments={[
        { label: "Projects", href: `/org/${slug}/projects` },
        { label: project.name, href: `/org/${slug}/projects/${projectId}/backlog`, bold: true },
        ...(activeTab ? [{ label: activeTab }] : []),
      ]}
    />
  ) : undefined;

  if (isLoading) {
    return (
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          height: "100vh",
          overflow: "hidden",
          background: "var(--tf-bg)",
        }}
      >
        <TopBar />
        <div style={{ flex: 1, padding: 20 }}>
          <LoadingSkeleton rows={5} type="list-row" />
        </div>
      </div>
    );
  }

  if (isError || !project) {
    return (
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          height: "100vh",
          overflow: "hidden",
          background: "var(--tf-bg)",
        }}
      >
        <TopBar />
        <div
          style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center" }}
        >
          <EmptyState
            title="Project not found"
            description="This project does not exist or has been deleted."
            action={
              <Link
                href={`/org/${slug}/projects`}
                style={{ fontSize: 13, color: "var(--tf-accent)", textDecoration: "none" }}
              >
                Back to Projects
              </Link>
            }
          />
        </div>
      </div>
    );
  }

  return (
    <ProjectProvider project={project}>
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          height: "100vh",
          overflow: "hidden",
          background: "var(--tf-bg)",
        }}
      >
        <TopBar
          breadcrumb={breadcrumb}
          actions={<OrgSwitcher currentSlug={slug} />}
        />
        <ProjectNav projectId={projectId} orgSlug={slug} />
        <main style={{ flex: 1, overflow: "auto" }}>
          {children}
        </main>
      </div>
    </ProjectProvider>
  );
}
