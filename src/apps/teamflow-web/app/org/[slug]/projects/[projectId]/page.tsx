import { redirect } from "next/navigation";

interface Props {
  params: Promise<{ slug: string; projectId: string }>;
}

export default async function OrgProjectPage({ params }: Props) {
  const { slug, projectId } = await params;
  redirect(`/org/${slug}/projects/${projectId}/backlog`);
}
