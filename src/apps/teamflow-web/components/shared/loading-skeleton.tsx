interface LoadingSkeletonProps {
  rows?: number;
  type?: "card" | "list-row" | "detail";
}

function SkeletonBlock({
  width = "100%",
  height = 12,
  borderRadius = 4,
  style,
}: {
  width?: string | number;
  height?: string | number;
  borderRadius?: number;
  style?: React.CSSProperties;
}) {
  return (
    <div
      style={{
        width,
        height,
        borderRadius,
        background: "var(--tf-bg3)",
        animation: "pulse 1.5s ease-in-out infinite",
        ...style,
      }}
    />
  );
}

function CardSkeleton() {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "flex-start",
        gap: 8,
        padding: "9px 10px",
        borderRadius: "var(--tf-radius)",
        border: "1px solid var(--tf-border)",
        background: "var(--tf-bg2)",
        marginBottom: 5,
      }}
    >
      <SkeletonBlock width={18} height={18} borderRadius={3} />
      <div style={{ flex: 1, minWidth: 0, display: "flex", flexDirection: "column", gap: 5 }}>
        <SkeletonBlock width="60%" height={10} />
        <SkeletonBlock width="85%" height={12} />
        <div style={{ display: "flex", gap: 4, marginTop: 2 }}>
          <SkeletonBlock width={40} height={16} borderRadius={3} />
          <SkeletonBlock width={60} height={16} borderRadius={3} />
        </div>
      </div>
      <SkeletonBlock width={22} height={22} borderRadius={5} />
    </div>
  );
}

function ListRowSkeleton() {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 8,
        padding: "8px 10px",
        borderBottom: "1px solid var(--tf-border)",
      }}
    >
      <SkeletonBlock width={18} height={18} borderRadius={3} />
      <SkeletonBlock width="40%" height={11} />
      <div style={{ marginLeft: "auto", display: "flex", gap: 6 }}>
        <SkeletonBlock width={50} height={16} borderRadius={100} />
        <SkeletonBlock width={22} height={22} borderRadius={100} />
      </div>
    </div>
  );
}

function DetailSkeleton() {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 16, padding: 20 }}>
      <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
        <SkeletonBlock width={18} height={18} borderRadius={3} />
        <SkeletonBlock width="50%" height={20} borderRadius={6} />
        <div style={{ marginLeft: "auto" }}>
          <SkeletonBlock width={80} height={22} borderRadius={100} />
        </div>
      </div>
      <SkeletonBlock width="100%" height={80} borderRadius={6} />
      <SkeletonBlock width="100%" height={120} borderRadius={6} />
    </div>
  );
}

export function LoadingSkeleton({ rows = 5, type = "list-row" }: LoadingSkeletonProps) {
  if (type === "detail") return <DetailSkeleton />;

  const RowComponent = type === "card" ? CardSkeleton : ListRowSkeleton;

  return (
    <div>
      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.4; }
        }
      `}</style>
      {Array.from({ length: rows }).map((_, i) => (
        <RowComponent key={i} />
      ))}
    </div>
  );
}
