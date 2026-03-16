"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useMyOrganizations } from "@/lib/hooks/use-organizations";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";

export default function OnboardingPage() {
  const router = useRouter();
  const { data: orgs, isLoading, isError } = useMyOrganizations();

  useEffect(() => {
    if (isLoading) return;

    if (isError || !orgs) {
      router.replace("/onboarding/no-orgs");
      return;
    }

    if (orgs.length === 0) {
      router.replace("/onboarding/no-orgs");
    } else if (orgs.length === 1) {
      router.replace(`/org/${orgs[0].slug}/projects`);
    } else {
      router.replace("/onboarding/pick-org");
    }
  }, [orgs, isLoading, isError, router]);

  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        height: "100vh",
        background: "var(--tf-bg)",
      }}
    >
      <div style={{ width: 300 }}>
        <LoadingSkeleton rows={3} />
      </div>
    </div>
  );
}
