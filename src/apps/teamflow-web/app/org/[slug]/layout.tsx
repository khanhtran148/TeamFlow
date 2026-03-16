import type { ReactNode } from "react";
import { OrgLayoutClient } from "./org-layout-client";

interface OrgLayoutProps {
  children: ReactNode;
  params: Promise<{ slug: string }>;
}

export default async function OrgLayout({ children, params }: OrgLayoutProps) {
  const { slug } = await params;

  return (
    <OrgLayoutClient slug={slug}>
      {children}
    </OrgLayoutClient>
  );
}
