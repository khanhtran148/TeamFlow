"use client";

import type { ReactNode } from "react";
import Link from "next/link";
import { TopBar } from "@/components/layout/top-bar";
import { Breadcrumb } from "@/components/layout/breadcrumb";
import { ProjectNav } from "@/components/projects/project-nav";
import { ProjectProvider } from "@/lib/contexts/project-context";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { useProject } from "@/lib/hooks/use-projects";
import { usePathname } from "next/navigation";

interface ProjectLayoutClientProps {
  projectId: string;
  children: ReactNode;
}

function resolveActiveTab(pathname: string, projectId: string): string {
  if (pathname.includes(`/projects/${projectId}/board`)) return "Board";
  if (pathname.includes(`/projects/${projectId}/releases`)) return "Releases";
  return "Backlog";
}

export function ProjectLayoutClient({ projectId, children }: ProjectLayoutClientProps) {
  const pathname = usePathname();
  const { data: project, isLoading, isError } = useProject(projectId);

  const activeTab = project ? resolveActiveTab(pathname, projectId) : "";

  const breadcrumb = project ? (
    <Breadcrumb
      segments={[
        { label: "Projects", href: "/projects" },
        { label: project.name, href: `/projects/${projectId}/backlog`, bold: true },
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
        <div style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center" }}>
          <EmptyState
            title="Project not found"
            description="This project does not exist or has been deleted."
            action={
              <Link
                href="/projects"
                style={{
                  fontSize: 12,
                  color: "var(--tf-accent)",
                  textDecoration: "none",
                }}
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
        <TopBar breadcrumb={breadcrumb} />
        <ProjectNav projectId={projectId} />
        <main style={{ flex: 1, overflow: "auto" }}>
          {children}
        </main>
      </div>
    </ProjectProvider>
  );
}
