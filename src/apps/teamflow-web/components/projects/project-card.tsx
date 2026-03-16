"use client";

import { useState } from "react";
import { MoreHorizontal, Layers, CheckSquare, Archive, Pencil, Trash2 } from "lucide-react";
import type { ProjectDto } from "@/lib/api/types";

interface ProjectCardProps {
  project: ProjectDto;
  onEdit: (project: ProjectDto) => void;
  onArchive: (project: ProjectDto) => void;
  onDelete: (project: ProjectDto) => void;
  onClick: (project: ProjectDto) => void;
}

function ProjectStatusBadge({ status }: { status: "Active" | "Archived" }) {
  const isActive = status === "Active";
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        padding: "1px 7px",
        borderRadius: 100,
        fontSize: 10,
        fontWeight: 600,
        fontFamily: "var(--tf-font-mono)",
        background: isActive ? "var(--tf-accent-dim2)" : "var(--tf-bg4)",
        color: isActive ? "var(--tf-accent)" : "var(--tf-text3)",
        border: `1px solid ${isActive ? "var(--tf-accent)" : "var(--tf-border)"}`,
        whiteSpace: "nowrap",
      }}
    >
      {isActive ? "Active" : "Archived"}
    </span>
  );
}

function formatDate(dateStr: string): string {
  const d = new Date(dateStr);
  return d.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });
}

export function ProjectCard({
  project,
  onEdit,
  onArchive,
  onDelete,
  onClick,
}: ProjectCardProps) {
  const [menuOpen, setMenuOpen] = useState(false);

  function handleMenuToggle(e: React.MouseEvent) {
    e.stopPropagation();
    setMenuOpen((prev) => !prev);
  }

  function handleEdit(e: React.MouseEvent) {
    e.stopPropagation();
    setMenuOpen(false);
    onEdit(project);
  }

  function handleArchive(e: React.MouseEvent) {
    e.stopPropagation();
    setMenuOpen(false);
    onArchive(project);
  }

  function handleDelete(e: React.MouseEvent) {
    e.stopPropagation();
    setMenuOpen(false);
    onDelete(project);
  }

  return (
    <div
      onClick={() => onClick(project)}
      style={{
        display: "flex",
        alignItems: "center",
        gap: 12,
        padding: "12px 16px",
        cursor: "pointer",
        position: "relative",
        borderBottom: "1px solid var(--tf-border)",
        transition: "background var(--tf-tr)",
      }}
      onMouseEnter={(e) => {
        (e.currentTarget as HTMLDivElement).style.background = "var(--tf-bg3)";
      }}
      onMouseLeave={(e) => {
        (e.currentTarget as HTMLDivElement).style.background = "transparent";
      }}
    >
      {/* Color dot */}
      <div
        style={{
          width: 8,
          height: 8,
          borderRadius: "50%",
          background: project.status === "Active" ? "var(--tf-accent)" : "var(--tf-text3)",
          flexShrink: 0,
        }}
      />

      {/* Name + description */}
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <span
            style={{
              fontWeight: 600,
              fontSize: 14,
              color: "var(--tf-text)",
              whiteSpace: "nowrap",
              overflow: "hidden",
              textOverflow: "ellipsis",
            }}
          >
            {project.name}
          </span>
          <ProjectStatusBadge status={project.status} />
        </div>
        {project.description && (
          <span
            style={{
              fontSize: 12,
              color: "var(--tf-text3)",
              whiteSpace: "nowrap",
              overflow: "hidden",
              textOverflow: "ellipsis",
              display: "block",
              marginTop: 2,
            }}
          >
            {project.description}
          </span>
        )}
      </div>

      {/* Stats */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 12,
          flexShrink: 0,
          fontSize: 11,
          color: "var(--tf-text3)",
          fontFamily: "var(--tf-font-mono)",
        }}
      >
        <span style={{ display: "inline-flex", alignItems: "center", gap: 3 }}>
          <Layers size={11} />
          {project.epicCount}
        </span>
        <span style={{ display: "inline-flex", alignItems: "center", gap: 3 }}>
          <CheckSquare size={11} />
          {project.openItemCount}
        </span>
        <span>{formatDate(project.createdAt)}</span>
      </div>

      {/* Menu button */}
      <div style={{ position: "relative", flexShrink: 0 }}>
        <button
          onClick={handleMenuToggle}
          style={{
            width: 28,
            height: 28,
            borderRadius: 5,
            border: "1px solid transparent",
            background: "transparent",
            color: "var(--tf-text3)",
            cursor: "pointer",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            transition: "all var(--tf-tr)",
          }}
          onMouseEnter={(e) => {
            (e.currentTarget as HTMLButtonElement).style.background = "var(--tf-bg4)";
            (e.currentTarget as HTMLButtonElement).style.borderColor = "var(--tf-border)";
            (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text)";
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLButtonElement).style.background = "transparent";
            (e.currentTarget as HTMLButtonElement).style.borderColor = "transparent";
            (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text3)";
          }}
        >
          <MoreHorizontal size={14} />
        </button>

        {menuOpen && (
          <>
            <div
              style={{ position: "fixed", inset: 0, zIndex: 9 }}
              onClick={(e) => {
                e.stopPropagation();
                setMenuOpen(false);
              }}
            />
            <div
              style={{
                position: "absolute",
                top: 32,
                right: 0,
                zIndex: 10,
                background: "var(--tf-bg3)",
                border: "1px solid var(--tf-border)",
                borderRadius: "var(--tf-radius)",
                boxShadow: "var(--tf-shadow)",
                minWidth: 140,
                overflow: "hidden",
              }}
            >
              <MenuItemButton icon={<Pencil size={12} />} label="Edit" onClick={handleEdit} />
              {project.status === "Active" && (
                <MenuItemButton icon={<Archive size={12} />} label="Archive" onClick={handleArchive} />
              )}
              <MenuItemButton icon={<Trash2 size={12} />} label="Delete" onClick={handleDelete} destructive />
            </div>
          </>
        )}
      </div>
    </div>
  );
}

function MenuItemButton({
  icon,
  label,
  onClick,
  destructive,
}: {
  icon: React.ReactNode;
  label: string;
  onClick: (e: React.MouseEvent) => void;
  destructive?: boolean;
}) {
  return (
    <button
      onClick={onClick}
      style={{
        display: "flex",
        alignItems: "center",
        gap: 8,
        width: "100%",
        padding: "7px 12px",
        background: "transparent",
        border: "none",
        cursor: "pointer",
        fontSize: 12,
        color: destructive ? "var(--tf-red)" : "var(--tf-text2)",
        textAlign: "left",
        transition: "background var(--tf-tr)",
      }}
      onMouseEnter={(e) => {
        (e.currentTarget as HTMLButtonElement).style.background = "var(--tf-bg4)";
      }}
      onMouseLeave={(e) => {
        (e.currentTarget as HTMLButtonElement).style.background = "transparent";
      }}
    >
      {icon}
      {label}
    </button>
  );
}
