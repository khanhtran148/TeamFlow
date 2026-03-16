"use client";

import {
  useNotificationPreferences,
  useUpdatePreferences,
} from "@/lib/hooks/use-notifications";
import type { NotificationPreferenceDto } from "@/lib/api/types";
import { useState, useEffect } from "react";

export function NotificationPreferences() {
  const { data: prefs, isLoading } = useNotificationPreferences();
  const updatePrefs = useUpdatePreferences();
  const [localPrefs, setLocalPrefs] = useState<NotificationPreferenceDto[]>([]);

  useEffect(() => {
    if (prefs) setLocalPrefs(prefs);
  }, [prefs]);

  const toggle = (type: string, field: "emailEnabled" | "inAppEnabled") => {
    setLocalPrefs((prev) =>
      prev.map((p) =>
        p.notificationType === type ? { ...p, [field]: !p[field] } : p,
      ),
    );
  };

  const save = () => {
    updatePrefs.mutate(localPrefs);
  };

  if (isLoading) return <p className="text-gray-500">Loading preferences...</p>;

  return (
    <div className="space-y-4">
      <table className="w-full text-sm">
        <thead>
          <tr className="text-gray-500 text-xs border-b">
            <th className="text-left py-2">Notification Type</th>
            <th className="text-center py-2">Email</th>
            <th className="text-center py-2">In-App</th>
          </tr>
        </thead>
        <tbody>
          {localPrefs.map((p) => (
            <tr key={p.notificationType} className="border-b">
              <td className="py-2">{p.notificationType}</td>
              <td className="text-center">
                <input
                  type="checkbox"
                  checked={p.emailEnabled}
                  onChange={() => toggle(p.notificationType, "emailEnabled")}
                />
              </td>
              <td className="text-center">
                <input
                  type="checkbox"
                  checked={p.inAppEnabled}
                  onChange={() => toggle(p.notificationType, "inAppEnabled")}
                />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <button
        onClick={save}
        disabled={updatePrefs.isPending}
        className="px-4 py-2 bg-blue-600 text-white rounded text-sm hover:bg-blue-700 disabled:opacity-50"
      >
        {updatePrefs.isPending ? "Saving..." : "Save Preferences"}
      </button>
    </div>
  );
}
