import type { ReactNode } from "react";
import { OrgProjectLayoutClient } from "./org-project-layout-client";

interface OrgProjectLayoutProps {
  children: ReactNode;
  params: Promise<{ slug: string; projectId: string }>;
}

export default async function OrgProjectLayout({ children, params }: OrgProjectLayoutProps) {
  const { slug, projectId } = await params;

  return (
    <OrgProjectLayoutClient slug={slug} projectId={projectId}>
      {children}
    </OrgProjectLayoutClient>
  );
}
