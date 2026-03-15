"use client";

import { useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import { Plus, Search, FolderOpen } from "lucide-react";
import { toast } from "sonner";
import { TopBar } from "@/components/layout/top-bar";
import { ProjectCard } from "@/components/projects/project-card";
import { CreateProjectDialog } from "@/components/projects/create-project-dialog";
import { EditProjectDialog } from "@/components/projects/edit-project-dialog";
import { ConfirmDialog } from "@/components/projects/confirm-dialog";
import { EmptyState } from "@/components/shared/empty-state";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import { Pagination } from "@/components/shared/pagination";
import { useProjects, useArchiveProject, useDeleteProject } from "@/lib/hooks/use-projects";
import type { ProjectDto, ProjectStatus } from "@/lib/api/types";
import type { ApiError } from "@/lib/api/client";
import { useDebounce } from "@/lib/hooks/use-debounce";

type FilterStatus = "All" | ProjectStatus;

const FILTER_CHIPS: { label: string; value: FilterStatus }[] = [
  { label: "All", value: "All" },
  { label: "Active", value: "Active" },
  { label: "Archived", value: "Archived" },
];

const PAGE_SIZE = 12;

export default function ProjectsPage() {
  const router = useRouter();

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebounce(search, 300);

  const [statusFilter, setStatusFilter] = useState<FilterStatus>("All");
  const [page, setPage] = useState(1);

  const [createOpen, setCreateOpen] = useState(false);
  const [editProject, setEditProject] = useState<ProjectDto | null>(null);
  const [archiveTarget, setArchiveTarget] = useState<ProjectDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ProjectDto | null>(null);

  const { data, isLoading, isError } = useProjects({
    search: debouncedSearch || undefined,
    status: statusFilter === "All" ? undefined : statusFilter,
    page,
    pageSize: PAGE_SIZE,
  });

  const { mutate: archiveProject, isPending: archiving } = useArchiveProject();
  const { mutate: deleteProject, isPending: deleting } = useDeleteProject();

  function handleFilterChange(value: FilterStatus) {
    setStatusFilter(value);
    setPage(1);
  }

  function handleSearch(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleProjectClick(project: ProjectDto) {
    router.push(`/projects/${project.id}/backlog`);
  }

  function handleArchiveConfirm() {
    if (!archiveTarget) return;
    archiveProject(archiveTarget.id, {
      onSuccess: () => {
        toast.success(`"${archiveTarget.name}" archived.`);
        setArchiveTarget(null);
      },
      onError: (err) => {
        const apiErr = err as ApiError;
        toast.error(apiErr.message ?? "Failed to archive project.");
        setArchiveTarget(null);
      },
    });
  }

  function handleDeleteConfirm() {
    if (!deleteTarget) return;
    deleteProject(deleteTarget.id, {
      onSuccess: () => {
        toast.success(`"${deleteTarget.name}" deleted.`);
        setDeleteTarget(null);
      },
      onError: (err) => {
        const apiErr = err as ApiError;
        toast.error(apiErr.message ?? "Failed to delete project.");
        setDeleteTarget(null);
      },
    });
  }

  const projects = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const isEmpty = !isLoading && !isError && projects.length === 0;

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

      <main style={{ flex: 1, overflow: "auto", padding: "24px 20px" }}>
        <div style={{ maxWidth: 960, margin: "0 auto" }}>

          {/* Page header */}
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              marginBottom: 20,
              gap: 12,
              flexWrap: "wrap",
            }}
          >
            <h1
              style={{
                fontFamily: "var(--tf-font-head)",
                fontWeight: 700,
                fontSize: 22,
                color: "var(--tf-text)",
              }}
            >
              Projects
            </h1>
            <button
              onClick={() => setCreateOpen(true)}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 6,
                padding: "7px 14px",
                borderRadius: 6,
                border: "1px solid var(--tf-accent)",
                background: "var(--tf-accent)",
                color: "#0a0a0b",
                fontSize: 12,
                fontWeight: 600,
                cursor: "pointer",
                fontFamily: "var(--tf-font-body)",
                transition: "opacity var(--tf-tr)",
              }}
              onMouseEnter={(e) => ((e.currentTarget as HTMLButtonElement).style.opacity = "0.85")}
              onMouseLeave={(e) => ((e.currentTarget as HTMLButtonElement).style.opacity = "1")}
            >
              <Plus size={13} />
              New Project
            </button>
          </div>

          {/* Toolbar: search + filter chips */}
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 10,
              marginBottom: 16,
              flexWrap: "wrap",
            }}
          >
            {/* Search */}
            <div
              style={{
                display: "flex",
                alignItems: "center",
                gap: 7,
                padding: "6px 10px",
                borderRadius: 6,
                border: "1px solid var(--tf-border)",
                background: "var(--tf-bg2)",
                flex: "1 1 200px",
                maxWidth: 280,
              }}
            >
              <Search size={12} color="var(--tf-text3)" />
              <input
                type="text"
                value={search}
                onChange={(e) => handleSearch(e.target.value)}
                placeholder="Search projects..."
                style={{
                  border: "none",
                  background: "transparent",
                  color: "var(--tf-text)",
                  fontSize: 12,
                  outline: "none",
                  width: "100%",
                  fontFamily: "var(--tf-font-body)",
                }}
              />
            </div>

            {/* Status filter chips */}
            <div style={{ display: "flex", gap: 5 }}>
              {FILTER_CHIPS.map((chip) => (
                <FilterChip
                  key={chip.value}
                  label={chip.label}
                  active={statusFilter === chip.value}
                  onClick={() => handleFilterChange(chip.value)}
                />
              ))}
            </div>

            {/* Total count */}
            {!isLoading && totalCount > 0 && (
              <span
                style={{
                  marginLeft: "auto",
                  fontSize: 11,
                  color: "var(--tf-text3)",
                  fontFamily: "var(--tf-font-mono)",
                }}
              >
                {totalCount} project{totalCount !== 1 ? "s" : ""}
              </span>
            )}
          </div>

          {/* Content area */}
          <div
            style={{
              background: "var(--tf-bg2)",
              border: "1px solid var(--tf-border)",
              borderRadius: "var(--tf-radius)",
              overflow: "hidden",
            }}
          >
            {isLoading ? (
              <div style={{ padding: 12 }}>
                <ProjectListSkeleton />
              </div>
            ) : isError ? (
              <EmptyState
                title="Failed to load projects"
                description="Check your connection and try again."
              />
            ) : isEmpty ? (
              <EmptyState
                icon={FolderOpen}
                title={
                  debouncedSearch || statusFilter !== "All"
                    ? "No projects match your filters"
                    : "No projects yet"
                }
                description={
                  debouncedSearch || statusFilter !== "All"
                    ? "Try adjusting your search or filter."
                    : "Create your first project to get started."
                }
                action={
                  !debouncedSearch && statusFilter === "All" ? (
                    <button
                      onClick={() => setCreateOpen(true)}
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: 5,
                        padding: "7px 14px",
                        borderRadius: 6,
                        border: "1px solid var(--tf-accent)",
                        background: "var(--tf-accent-dim)",
                        color: "var(--tf-accent)",
                        fontSize: 12,
                        fontWeight: 500,
                        cursor: "pointer",
                        fontFamily: "var(--tf-font-body)",
                      }}
                    >
                      <Plus size={12} />
                      New Project
                    </button>
                  ) : undefined
                }
              />
            ) : (
              <div style={{ padding: 12 }}>
                <div
                  style={{
                    display: "grid",
                    gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))",
                    gap: 10,
                  }}
                >
                  {projects.map((project) => (
                    <ProjectCard
                      key={project.id}
                      project={project}
                      onClick={handleProjectClick}
                      onEdit={setEditProject}
                      onArchive={setArchiveTarget}
                      onDelete={setDeleteTarget}
                    />
                  ))}
                </div>
              </div>
            )}

            {/* Pagination */}
            {!isLoading && totalCount > PAGE_SIZE && (
              <Pagination
                page={page}
                pageSize={PAGE_SIZE}
                totalCount={totalCount}
                onPageChange={setPage}
              />
            )}
          </div>
        </div>
      </main>

      {/* Dialogs */}
      <CreateProjectDialog open={createOpen} onClose={() => setCreateOpen(false)} />
      <EditProjectDialog project={editProject} onClose={() => setEditProject(null)} />
      <ConfirmDialog
        open={!!archiveTarget}
        title="Archive Project"
        message={`Are you sure you want to archive "${archiveTarget?.name}"? It will be hidden from the active projects list.`}
        confirmLabel="Archive"
        isPending={archiving}
        onConfirm={handleArchiveConfirm}
        onCancel={() => setArchiveTarget(null)}
      />
      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete Project"
        message={`Are you sure you want to delete "${deleteTarget?.name}"? This action cannot be undone and will remove all work items.`}
        confirmLabel="Delete"
        destructive
        isPending={deleting}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  );
}

function FilterChip({
  label,
  active,
  onClick,
}: {
  label: string;
  active: boolean;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      style={{
        padding: "4px 10px",
        borderRadius: 100,
        border: `1px solid ${active ? "var(--tf-accent)" : "var(--tf-border)"}`,
        background: active ? "var(--tf-accent-dim2)" : "transparent",
        color: active ? "var(--tf-accent)" : "var(--tf-text3)",
        fontSize: 11,
        fontWeight: active ? 600 : 400,
        cursor: "pointer",
        fontFamily: "var(--tf-font-mono)",
        transition: "all var(--tf-tr)",
      }}
    >
      {label}
    </button>
  );
}

function ProjectListSkeleton() {
  return (
    <div
      style={{
        display: "grid",
        gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))",
        gap: 10,
      }}
    >
      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.4; }
        }
      `}</style>
      {Array.from({ length: 6 }).map((_, i) => (
        <ProjectCardSkeleton key={i} />
      ))}
    </div>
  );
}

function ProjectCardSkeleton() {
  function Block({
    w,
    h,
    r = 4,
  }: {
    w: string | number;
    h: number;
    r?: number;
  }) {
    return (
      <div
        style={{
          width: w,
          height: h,
          borderRadius: r,
          background: "var(--tf-bg3)",
          animation: "pulse 1.5s ease-in-out infinite",
        }}
      />
    );
  }

  return (
    <div
      style={{
        background: "var(--tf-bg2)",
        border: "1px solid var(--tf-border)",
        borderRadius: "var(--tf-radius)",
        padding: "14px 16px",
        display: "flex",
        flexDirection: "column",
        gap: 10,
      }}
    >
      <div style={{ display: "flex", alignItems: "flex-start", gap: 8 }}>
        <Block w={8} h={8} r={4} />
        <div style={{ flex: 1, display: "flex", flexDirection: "column", gap: 6 }}>
          <Block w="50%" h={13} r={4} />
          <Block w="85%" h={11} r={4} />
          <Block w="70%" h={10} r={4} />
        </div>
        <Block w={26} h={26} r={5} />
      </div>
      <div
        style={{
          borderTop: "1px solid var(--tf-border)",
          paddingTop: 8,
          display: "flex",
          gap: 12,
        }}
      >
        <Block w={60} h={11} r={3} />
        <Block w={55} h={11} r={3} />
        <div style={{ marginLeft: "auto" }}>
          <Block w={90} h={11} r={3} />
        </div>
      </div>
    </div>
  );
}
