import { FolderX } from "lucide-react";
import Link from "next/link";

export default function ProjectNotFound() {
  return (
    <div
      style={{
        flex: 1,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: "40px 20px",
      }}
    >
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          gap: 16,
          maxWidth: 380,
          textAlign: "center",
        }}
      >
        <div
          style={{
            width: 48,
            height: 48,
            borderRadius: 12,
            background: "var(--tf-bg3)",
            border: "1px solid var(--tf-border)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <FolderX size={22} color="var(--tf-text3)" />
        </div>

        <div>
          <h2
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 700,
              fontSize: 18,
              color: "var(--tf-text)",
              marginBottom: 8,
            }}
          >
            Project not found
          </h2>
          <p
            style={{
              fontSize: 13,
              color: "var(--tf-text3)",
              lineHeight: 1.6,
            }}
          >
            This project does not exist or has been deleted. Check the URL or
            return to the projects list.
          </p>
        </div>

        <Link
          href="/projects"
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 6,
            padding: "7px 16px",
            borderRadius: 6,
            border: "1px solid var(--tf-accent)",
            background: "var(--tf-accent-dim2)",
            color: "var(--tf-accent)",
            fontSize: 12,
            fontWeight: 600,
            textDecoration: "none",
            fontFamily: "var(--tf-font-body)",
            transition: "all var(--tf-tr)",
          }}
        >
          Back to Projects
        </Link>
      </div>
    </div>
  );
}
