"use client";

import { useMemo } from "react";
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
} from "recharts";
import { useSprintBurndown } from "@/lib/hooks/use-sprints";
import { Skeleton } from "@/components/ui/skeleton";

interface BurndownChartProps {
  sprintId: string;
}

interface ChartDataPoint {
  date: string;
  ideal: number | null;
  actual: number | null;
}

function formatDateLabel(dateStr: string): string {
  const d = new Date(dateStr);
  return d.toLocaleDateString("en-US", { month: "short", day: "numeric" });
}

export function BurndownChart({ sprintId }: BurndownChartProps) {
  const { data, isLoading, isError } = useSprintBurndown(sprintId);

  const chartData = useMemo<ChartDataPoint[]>(() => {
    if (!data) return [];

    const dateMap = new Map<string, ChartDataPoint>();

    for (const point of data.idealLine) {
      const key = point.date.split("T")[0];
      dateMap.set(key, {
        date: key,
        ideal: point.points,
        actual: null,
      });
    }

    for (const point of data.actualLine) {
      const key = point.date.split("T")[0];
      const existing = dateMap.get(key);
      if (existing) {
        existing.actual = point.remainingPoints;
      } else {
        dateMap.set(key, {
          date: key,
          ideal: null,
          actual: point.remainingPoints,
        });
      }
    }

    return Array.from(dateMap.values()).sort((a, b) => a.date.localeCompare(b.date));
  }, [data]);

  if (isLoading) {
    return (
      <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
        <Skeleton style={{ height: 14, width: 120, borderRadius: 4 }} />
        <Skeleton style={{ height: 240, borderRadius: 8 }} />
      </div>
    );
  }

  if (isError) {
    return (
      <div
        style={{
          padding: "24px 16px",
          textAlign: "center",
          color: "var(--tf-red)",
          fontSize: 13,
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
        }}
      >
        Failed to load burndown data.
      </div>
    );
  }

  if (chartData.length === 0) {
    return (
      <div
        style={{
          padding: "24px 16px",
          textAlign: "center",
          color: "var(--tf-text3)",
          fontSize: 13,
        }}
      >
        No burndown data available yet. Data will appear after the sprint starts.
      </div>
    );
  }

  return (
    <div
      data-testid="burndown-chart"
      style={{
        display: "flex",
        flexDirection: "column",
        gap: 8,
      }}
    >
      <span
        style={{
          fontSize: 13,
          fontWeight: 600,
          color: "var(--tf-text2)",
          fontFamily: "var(--tf-font-mono)",
          textTransform: "uppercase",
          letterSpacing: "0.05em",
        }}
      >
        Burndown
      </span>
      <div
        style={{
          width: "100%",
          height: 260,
        }}
      >
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={chartData} margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="var(--tf-border)" />
            <XAxis
              dataKey="date"
              tickFormatter={formatDateLabel}
              tick={{ fill: "var(--tf-text3)", fontSize: 10 }}
              stroke="var(--tf-border)"
            />
            <YAxis
              tick={{ fill: "var(--tf-text3)", fontSize: 10 }}
              stroke="var(--tf-border)"
              allowDecimals={false}
            />
            <Tooltip
              contentStyle={{
                background: "var(--tf-bg3)",
                border: "1px solid var(--tf-border)",
                borderRadius: 6,
                fontSize: 13,
                color: "var(--tf-text)",
              }}
              labelFormatter={(label: unknown) => formatDateLabel(String(label))}
            />
            <Legend
              wrapperStyle={{ fontSize: 13, color: "var(--tf-text3)" }}
            />
            <Line
              type="monotone"
              dataKey="ideal"
              stroke="var(--tf-text3)"
              strokeDasharray="5 5"
              strokeWidth={2}
              dot={false}
              name="Ideal"
              connectNulls
            />
            <Line
              type="monotone"
              dataKey="actual"
              stroke="var(--tf-accent)"
              strokeWidth={2}
              dot={{ r: 3, fill: "var(--tf-accent)" }}
              name="Actual"
              connectNulls
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
