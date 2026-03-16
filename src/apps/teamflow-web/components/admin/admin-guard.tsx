"use client";

import { useEffect, type ReactNode } from "react";
import { useRouter, usePathname } from "next/navigation";
import { useAuthStore } from "@/lib/stores/auth-store";

interface AdminGuardProps {
  children: ReactNode;
}

export function AdminGuard({ children }: AdminGuardProps) {
  const router = useRouter();
  const pathname = usePathname();
  const user = useAuthStore((s) => s.user);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const mustChangePassword = useAuthStore((s) => s.mustChangePassword);

  const isChangePasswordPage = pathname === "/admin/change-password";

  useEffect(() => {
    if (!isAuthenticated) {
      router.replace("/login");
      return;
    }
    if (user?.systemRole !== "SystemAdmin") {
      router.replace("/projects");
      return;
    }
    // If user must change password, redirect to change-password page
    // (unless they're already on it)
    if (mustChangePassword && !isChangePasswordPage) {
      router.replace("/admin/change-password");
    }
  }, [isAuthenticated, user, router, mustChangePassword, isChangePasswordPage]);

  if (!isAuthenticated || user?.systemRole !== "SystemAdmin") {
    return null;
  }

  return <>{children}</>;
}
