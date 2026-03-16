"use client";

// Redirect to org-scoped teams route
import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useMyOrganizations } from "@/lib/hooks/use-organizations";

export default function TeamsRedirectPage() {
  const router = useRouter();
  const { data: orgs, isLoading } = useMyOrganizations();

  useEffect(() => {
    if (isLoading) return;
    if (!orgs || orgs.length === 0) {
      router.replace("/onboarding");
    } else if (orgs.length === 1) {
      router.replace(`/org/${orgs[0].slug}/teams`);
    } else {
      router.replace("/onboarding/pick-org");
    }
  }, [orgs, isLoading, router]);

  return null;
}
