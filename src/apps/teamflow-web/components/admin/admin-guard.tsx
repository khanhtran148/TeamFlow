"use client";

import { useEffect, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/lib/stores/auth-store";

interface AdminGuardProps {
  children: ReactNode;
}

export function AdminGuard({ children }: AdminGuardProps) {
  const router = useRouter();
  const user = useAuthStore((s) => s.user);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  useEffect(() => {
    if (!isAuthenticated) {
      router.replace("/login");
      return;
    }
    if (user?.systemRole !== "SystemAdmin") {
      router.replace("/projects");
    }
  }, [isAuthenticated, user, router]);

  if (!isAuthenticated || user?.systemRole !== "SystemAdmin") {
    return null;
  }

  return <>{children}</>;
}
