"use client";

import { createContext, useContext, type ReactNode } from "react";
import type { ProjectDto } from "@/lib/api/types";

interface ProjectContextValue {
  project: ProjectDto;
}

const ProjectContext = createContext<ProjectContextValue | null>(null);

export function ProjectProvider({
  project,
  children,
}: {
  project: ProjectDto;
  children: ReactNode;
}) {
  return (
    <ProjectContext.Provider value={{ project }}>
      {children}
    </ProjectContext.Provider>
  );
}

export function useProjectContext(): ProjectContextValue {
  const ctx = useContext(ProjectContext);
  if (!ctx) {
    throw new Error("useProjectContext must be used within a ProjectProvider");
  }
  return ctx;
}
