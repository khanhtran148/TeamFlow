import type { ReactNode } from "react";
import { ProjectLayoutClient } from "./project-layout-client";

interface ProjectLayoutProps {
  children: ReactNode;
  params: Promise<{ projectId: string }>;
}

export default async function ProjectLayout({ children, params }: ProjectLayoutProps) {
  const { projectId } = await params;

  return (
    <ProjectLayoutClient projectId={projectId}>
      {children}
    </ProjectLayoutClient>
  );
}
