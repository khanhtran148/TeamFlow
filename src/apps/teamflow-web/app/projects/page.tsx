"use client";

// This route is kept for backwards compatibility.
// Post-login, users are directed to /onboarding which handles org-based routing.
// Direct access to /projects redirects to /onboarding.
import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useMyOrganizations } from "@/lib/hooks/use-organizations";

export default function ProjectsRedirectPage() {
  const router = useRouter();
  const { data: orgs, isLoading } = useMyOrganizations();

  useEffect(() => {
    if (isLoading) return;
    if (!orgs || orgs.length === 0) {
      router.replace("/onboarding");
    } else if (orgs.length === 1) {
      router.replace(`/org/${orgs[0].slug}/projects`);
    } else {
      router.replace("/onboarding/pick-org");
    }
  }, [orgs, isLoading, router]);

  return null;
}
