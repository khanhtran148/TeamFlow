"use client";

import { useState } from "react";
import { useProfile, useUpdateProfile } from "@/lib/hooks/use-profile";
import { useAuthStore } from "@/lib/stores/auth-store";
import { ApiError } from "@/lib/api/client";

function getInitials(name: string): string {
  return name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString("en-AU", {
    year: "numeric",
    month: "long",
    day: "numeric",
  });
}

export function ProfileDetails() {
  const { data: profile, isLoading, isError } = useProfile();
  const updateProfile = useUpdateProfile();
  const setAuth = useAuthStore((s) => s.setAuth);
  const authState = useAuthStore((s) => s);

  const [editing, setEditing] = useState(false);
  const [name, setName] = useState("");
  const [avatarUrl, setAvatarUrl] = useState("");
  const [saveError, setSaveError] = useState<string | null>(null);

  function startEdit() {
    if (!profile) return;
    setName(profile.name);
    setAvatarUrl(profile.avatarUrl ?? "");
    setSaveError(null);
    setEditing(true);
  }

  function cancelEdit() {
    setEditing(false);
    setSaveError(null);
  }

  async function handleSave() {
    if (!profile) return;
    setSaveError(null);
    updateProfile.mutate(
      { name: name.trim(), avatarUrl: avatarUrl.trim() || null },
      {
        onSuccess: (updated) => {
          // Sync name back into auth store
          if (authState.user && authState.accessToken && authState.refreshToken && authState.expiresAt) {
            setAuth({
              user: { ...authState.user, name: updated.name },
              accessToken: authState.accessToken,
              refreshToken: authState.refreshToken,
              expiresAt: authState.expiresAt,
            });
          }
          setEditing(false);
        },
        onError: (err) => {
          if (err instanceof ApiError) {
            setSaveError(err.message);
          } else {
            setSaveError("Failed to save profile. Please try again.");
          }
        },
      },
    );
  }

  if (isLoading) {
    return (
      <div style={{ color: "var(--tf-text3)", fontSize: 13, padding: "24px 0" }}>
        Loading profile...
      </div>
    );
  }

  if (isError || !profile) {
    return (
      <div
        style={{
          color: "#ef4444",
          fontSize: 13,
          background: "var(--tf-bg2)",
          borderRadius: 8,
          padding: "12px 16px",
        }}
      >
        Failed to load profile. Please refresh the page.
      </div>
    );
  }

  const initials = getInitials(profile.name);

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 24 }}>
      {/* Avatar + identity */}
      <div
        style={{
          display: "flex",
          alignItems: "flex-start",
          gap: 20,
          flexWrap: "wrap",
        }}
      >
        {/* Avatar */}
        <div
          aria-hidden="true"
          style={{
            width: 72,
            height: 72,
            borderRadius: "50%",
            background: profile.avatarUrl ? "transparent" : "var(--tf-accent)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            flexShrink: 0,
            overflow: "hidden",
            border: "2px solid var(--tf-border)",
          }}
        >
          {profile.avatarUrl ? (
            <img
              src={profile.avatarUrl}
              alt={`${profile.name} avatar`}
              style={{ width: "100%", height: "100%", objectFit: "cover" }}
            />
          ) : (
            <span
              style={{
                color: "#fff",
                fontFamily: "var(--tf-font-head)",
                fontWeight: 700,
                fontSize: 22,
              }}
            >
              {initials}
            </span>
          )}
        </div>

        {/* Identity info */}
        <div style={{ flex: 1, minWidth: 200 }}>
          {editing ? (
            <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
              <label style={{ display: "flex", flexDirection: "column", gap: 4 }}>
                <span style={{ fontSize: 11, fontWeight: 600, color: "var(--tf-text3)", textTransform: "uppercase", letterSpacing: "0.05em" }}>
                  Display name
                </span>
                <input
                  aria-label="Display name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  maxLength={100}
                  style={{
                    background: "var(--tf-bg3)",
                    border: "1px solid var(--tf-border)",
                    borderRadius: 6,
                    padding: "7px 10px",
                    fontSize: 14,
                    color: "var(--tf-text)",
                    outline: "none",
                    width: "100%",
                    maxWidth: 320,
                    boxSizing: "border-box",
                  }}
                />
              </label>
              <label style={{ display: "flex", flexDirection: "column", gap: 4 }}>
                <span style={{ fontSize: 11, fontWeight: 600, color: "var(--tf-text3)", textTransform: "uppercase", letterSpacing: "0.05em" }}>
                  Avatar URL
                </span>
                <input
                  aria-label="Avatar URL"
                  value={avatarUrl}
                  onChange={(e) => setAvatarUrl(e.target.value)}
                  maxLength={2048}
                  placeholder="https://example.com/avatar.jpg"
                  style={{
                    background: "var(--tf-bg3)",
                    border: "1px solid var(--tf-border)",
                    borderRadius: 6,
                    padding: "7px 10px",
                    fontSize: 14,
                    color: "var(--tf-text)",
                    outline: "none",
                    width: "100%",
                    maxWidth: 320,
                    boxSizing: "border-box",
                  }}
                />
              </label>
              {saveError && (
                <p style={{ color: "#ef4444", fontSize: 12, margin: 0 }}>
                  {saveError}
                </p>
              )}
              <div style={{ display: "flex", gap: 8, marginTop: 4 }}>
                <button
                  onClick={handleSave}
                  disabled={updateProfile.isPending || !name.trim()}
                  aria-label="Save profile changes"
                  style={{
                    padding: "7px 16px",
                    borderRadius: 6,
                    border: "none",
                    background: "var(--tf-accent)",
                    color: "#fff",
                    fontSize: 13,
                    fontWeight: 600,
                    cursor: updateProfile.isPending ? "not-allowed" : "pointer",
                    opacity: updateProfile.isPending || !name.trim() ? 0.6 : 1,
                    minWidth: 44,
                    minHeight: 44,
                  }}
                >
                  {updateProfile.isPending ? "Saving..." : "Save"}
                </button>
                <button
                  onClick={cancelEdit}
                  disabled={updateProfile.isPending}
                  aria-label="Cancel editing profile"
                  style={{
                    padding: "7px 16px",
                    borderRadius: 6,
                    border: "1px solid var(--tf-border)",
                    background: "transparent",
                    color: "var(--tf-text2)",
                    fontSize: 13,
                    cursor: "pointer",
                    minWidth: 44,
                    minHeight: 44,
                  }}
                >
                  Cancel
                </button>
              </div>
            </div>
          ) : (
            <div>
              <div style={{ display: "flex", alignItems: "center", gap: 10, flexWrap: "wrap", marginBottom: 6 }}>
                <h2
                  style={{
                    fontFamily: "var(--tf-font-head)",
                    fontWeight: 700,
                    fontSize: 20,
                    color: "var(--tf-text)",
                    margin: 0,
                  }}
                >
                  {profile.name}
                </h2>
                <span
                  aria-label={`System role: ${profile.systemRole}`}
                  style={{
                    fontSize: 11,
                    fontWeight: 600,
                    padding: "2px 8px",
                    borderRadius: 99,
                    background:
                      profile.systemRole === "SystemAdmin"
                        ? "rgba(245,158,11,0.15)"
                        : "rgba(99,102,241,0.12)",
                    color:
                      profile.systemRole === "SystemAdmin"
                        ? "#f59e0b"
                        : "var(--tf-accent)",
                    letterSpacing: "0.04em",
                    textTransform: "uppercase",
                  }}
                >
                  {profile.systemRole === "SystemAdmin" ? "Admin" : "User"}
                </span>
              </div>
              <p style={{ fontSize: 13, color: "var(--tf-text2)", margin: "0 0 4px 0" }}>
                {profile.email}
              </p>
              <p style={{ fontSize: 12, color: "var(--tf-text3)", margin: "0 0 12px 0" }}>
                Member since {formatDate(profile.createdAt)}
              </p>
              <button
                onClick={startEdit}
                aria-label="Edit profile"
                style={{
                  padding: "7px 16px",
                  borderRadius: 6,
                  border: "1px solid var(--tf-border)",
                  background: "transparent",
                  color: "var(--tf-text2)",
                  fontSize: 13,
                  cursor: "pointer",
                  minWidth: 44,
                  minHeight: 44,
                }}
              >
                Edit profile
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Organizations */}
      <section aria-labelledby="profile-orgs-heading">
        <h3
          id="profile-orgs-heading"
          style={{
            fontFamily: "var(--tf-font-head)",
            fontWeight: 600,
            fontSize: 14,
            color: "var(--tf-text)",
            marginBottom: 10,
          }}
        >
          Organizations
        </h3>
        {profile.organizations.length === 0 ? (
          <p style={{ fontSize: 13, color: "var(--tf-text3)" }}>
            Not a member of any organization.
          </p>
        ) : (
          <ul style={{ listStyle: "none", padding: 0, margin: 0, display: "flex", flexDirection: "column", gap: 8 }}>
            {profile.organizations.map((org) => (
              <li
                key={org.orgId}
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  padding: "10px 14px",
                  background: "var(--tf-bg2)",
                  border: "1px solid var(--tf-border)",
                  borderRadius: 8,
                  flexWrap: "wrap",
                  gap: 6,
                }}
              >
                <div>
                  <div style={{ fontSize: 13, fontWeight: 600, color: "var(--tf-text)" }}>
                    {org.orgName}
                  </div>
                  <div style={{ fontSize: 11, color: "var(--tf-text3)", marginTop: 2 }}>
                    Joined {formatDate(org.joinedAt)}
                  </div>
                </div>
                <span
                  style={{
                    fontSize: 11,
                    fontWeight: 600,
                    padding: "2px 8px",
                    borderRadius: 99,
                    background: "rgba(99,102,241,0.12)",
                    color: "var(--tf-accent)",
                    textTransform: "capitalize",
                  }}
                >
                  {org.role}
                </span>
              </li>
            ))}
          </ul>
        )}
      </section>

      {/* Teams */}
      <section aria-labelledby="profile-teams-heading">
        <h3
          id="profile-teams-heading"
          style={{
            fontFamily: "var(--tf-font-head)",
            fontWeight: 600,
            fontSize: 14,
            color: "var(--tf-text)",
            marginBottom: 10,
          }}
        >
          Teams
        </h3>
        {profile.teams.length === 0 ? (
          <p style={{ fontSize: 13, color: "var(--tf-text3)" }}>
            Not a member of any team.
          </p>
        ) : (
          <ul style={{ listStyle: "none", padding: 0, margin: 0, display: "flex", flexDirection: "column", gap: 8 }}>
            {profile.teams.map((team) => (
              <li
                key={team.teamId}
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  padding: "10px 14px",
                  background: "var(--tf-bg2)",
                  border: "1px solid var(--tf-border)",
                  borderRadius: 8,
                  flexWrap: "wrap",
                  gap: 6,
                }}
              >
                <div>
                  <div style={{ fontSize: 13, fontWeight: 600, color: "var(--tf-text)" }}>
                    {team.teamName}
                  </div>
                  <div style={{ fontSize: 11, color: "var(--tf-text3)", marginTop: 2 }}>
                    {team.orgName} &middot; Joined {formatDate(team.joinedAt)}
                  </div>
                </div>
                <span
                  style={{
                    fontSize: 11,
                    fontWeight: 600,
                    padding: "2px 8px",
                    borderRadius: 99,
                    background: "rgba(99,102,241,0.12)",
                    color: "var(--tf-accent)",
                    textTransform: "capitalize",
                  }}
                >
                  {team.role}
                </span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
