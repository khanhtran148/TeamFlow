"use client";

import { use } from "react";
import { RetroSessionList } from "@/components/retros/retro-session-list";

interface RetrosPageProps {
  params: Promise<{ projectId: string }>;
}

export default function RetrosPage({ params }: RetrosPageProps) {
  const { projectId } = use(params);

  return <RetroSessionList projectId={projectId} />;
}
