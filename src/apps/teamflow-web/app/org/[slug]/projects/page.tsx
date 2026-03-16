"use client";

import { useState, useCallback } from "react";
import { useParams, useRouter } from "next/navigation";
import { Plus, Search, FolderOpen } from "lucide-react";
import { toast } from "sonner";
import { TopBar } from "@/components/layout/top-bar";
import { Breadcrumb } from "@/components/layout/breadcrumb";
import { OrgSwitcher } from "@/components/layout/org-switcher";
import { ProjectCard } from "@/components/projects/project-card";
import { CreateProjectDialog } from "@/components/projects/create-project-dialog";
import { EditProjectDialog } from "@/components/projects/edit-project-dialog";
import { ConfirmDialog } from "@/components/projects/confirm-dialog";
import { EmptyState } from "@/components/shared/empty-state";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import { Pagination } from "@/components/shared/pagination";
import { useProjects, useArchiveProject, useDeleteProject } from "@/lib/hooks/use-projects";
import { useOrgContext } from "@/lib/contexts/org-context";
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

export default function OrgProjectsPage() {
  const params = useParams();
  const slug = params.slug as string;
  const router = useRouter();
  const { org } = useOrgContext();

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebounce(search, 300);

  const [statusFilter, setStatusFilter] = useState<FilterStatus>("All");
  const [page, setPage] = useState(1);

  const [createOpen, setCreateOpen] = useState(false);
  const [editProject, setEditProject] = useState<ProjectDto | null>(null);
  const [archiveTarget, setArchiveTarget] = useState<ProjectDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ProjectDto | null>(null);

  const { data, isLoading, isError } = useProjects({
    orgId: org.id,
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
    router.push(`/org/${slug}/projects/${project.id}/backlog`);
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

  const breadcrumb = (
    <Breadcrumb
      segments={[
        { label: org.name, href: `/org/${slug}/projects`, bold: true },
        { label: "Projects" },
      ]}
    />
  );

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
      <TopBar breadcrumb={breadcrumb} actions={<OrgSwitcher currentSlug={slug} />} />

      <main style={{ flex: 1, overflow: "auto", padding: "24px 20px" }}>
        <div style={{ maxWidth: 960, margin: "0 auto" }}>
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
                color: "var(--primary-foreground)",
                fontSize: 13,
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

          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 10,
              marginBottom: 16,
              flexWrap: "wrap",
            }}
          >
            <div
              className="tf-search-focus"
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
                transition: "border-color var(--tf-tr), box-shadow var(--tf-tr)",
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
                  fontSize: 13,
                  outline: "none",
                  width: "100%",
                  fontFamily: "var(--tf-font-body)",
                }}
              />
            </div>

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

            {!isLoading && totalCount > 0 && (
              <span
                style={{
                  marginLeft: "auto",
                  fontSize: 13,
                  color: "var(--tf-text3)",
                  fontFamily: "var(--tf-font-mono)",
                }}
              >
                {totalCount} project{totalCount !== 1 ? "s" : ""}
              </span>
            )}
          </div>

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
                <LoadingSkeleton rows={6} />
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
                        fontSize: 13,
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
              <div>
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
            )}

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

      <CreateProjectDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        defaultOrgId={org.id}
      />
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
        fontSize: 13,
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
