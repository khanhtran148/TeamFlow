# 09 — Future Roadmap & AI Outlook

## v1 Status — All 5 Phases Complete (2026-03-16)

| Phase | Name | Status | Completed |
|---|---|---|---|
| 0 | Foundation & Design Ready | COMPLETE | 2026-03-15 |
| 1 | Work Item Management | COMPLETE | 2026-03-15 |
| 2 | Authentication & Authorization | COMPLETE | 2026-03-15 |
| 3 | Hardening + Sprint Planning + MVP Release | COMPLETE | 2026-03-15 |
| 4 | Collaboration & Planning | COMPLETE | 2026-03-16 |
| 5 | Insights & Automation | COMPLETE | 2026-03-16 |

TeamFlow v1 is feature-complete. All 5 phases delivered. Post-v1 org management and admin improvements also complete. Total: 1015 tests passing.

---

## Out of Scope — v1

Features intentionally excluded from the 5-phase plan:

| Feature | Reason for Deferral |
|---|---|
| AI task breakdown from User Story | Needs stable domain model + historical sprint data |
| AI story point estimation | Needs velocity history from ≥5 sprints to be useful |
| AI sprint risk detection | Same data maturity dependency |
| Git integration (commit → Work Item) | Significant complexity. Interim: commit message convention `#SX-101` |
| Time tracking (log actual hours) | Story points sufficient for now |
| Mobile app (iOS / Android) | Responsive web first |
| Multi-language i18n (vi/en) | Internal tool — consistent team language |
| SSO / OAuth2 (Google, Microsoft, GitHub) | JWT sufficient for internal use |
| Event Sourcing | Audit table sufficient; Event Sourcing when replay needed |
| pgvector semantic search | Full-text covers v1 needs |
| Custom workflow per project | Fixed workflow covers current processes |
| Webhook outbound (Slack, Teams) | Manual notifications sufficient initially |

---

## AI Impact by Time Horizon

### 6 Months — Stable, minor adjustments

TeamFlow v1 remains 100% relevant. Claude Code makes development faster but workflow unchanged.

**What to watch:**
- Story point estimation accuracy decreasing as AI coding accelerates velocity
- Team may start paste User Stories into Claude for task breakdown outside TeamFlow → friction point

**Action:** Start collecting baseline metrics — velocity, cycle time, estimation accuracy. This is training data for future AI features.

---

### 1 Year — Augmentation needed

| Feature | Current | 1-Year Adjustment |
|---|---|---|
| Planning Poker | Manual voting | AI suggests estimate, team reviews in 5 min |
| Acceptance Criteria | Empty textarea | AI drafts from brief description, PO approves |
| Sprint Capacity Warning | Simple threshold | AI-adjusted velocity based on AI coding assist level |
| Daily Standup prep | Manual | AI summary from commit + PR + item update activity |

**Features to add in this window:**
- AI estimate suggestion button (uses `AIInteractions` table already in schema)
- Smart sprint recommendation: `recommended_points = ai_adjusted_velocity * 0.85`
- Release notes auto-generation button (uses sprint DomainEvents)

---

### 2 Years — Fundamental rethink

| Feature | Risk Level | Direction |
|---|---|---|
| Story Points (Fibonacci) | High | Replace with flexible unit — `EstimationSchema` table already supports this |
| Manual Status Updates | High | Auto-detect from IDE/Git activity if integrated |
| Retrospective format | Medium | AI pre-populates cards from sprint data; human discusses why |
| Work Item History as sole source | Medium | Expand to multi-source event stream (Git, CI/CD, PR reviews) |
| Backlog Grooming sessions | Medium | AI drafts AC + estimates; PO reviews in shorter sessions |

**Architecture decisions made in Phase 0 that future-proof for this:**

| Decision | Benefit |
|---|---|
| `estimation_unit` + `estimation_value` not hardcoded | Can switch to time-based, confidence-based, or custom units without migration |
| `DomainEvents` partitioned table | Already structured for AI training queries |
| `WorkItemEmbeddings` table created empty | pgvector ready — just populate with AI service |
| `AIInteractions` table | Human feedback loop data accumulating from day AI features launch |
| `ai_metadata` JSONB on WorkItems | AI can populate `risk_score`, `sprint_fit_probability` without schema changes |
| `SprintSnapshots` + `BurndownDataPoints` | Time-series data for sprint completion prediction |
| `external_refs` JSONB on WorkItems | Git/CI/CD integration adds data without migration |

---

## AI Feature Roadmap (Post v1)

### When to start each AI feature

| Feature | Start When |
|---|---|
| Estimation suggestion | ≥10 completed sprints, ≥100 estimated items |
| Sprint completion prediction | ≥10 completed sprints with full DomainEvent data |
| Release risk detection | ≥5 completed releases with outcome data |
| Retro card pre-population | ≥5 completed retros with `sprint_metrics_snapshot` |
| Semantic search | `WorkItemEmbeddings` populated for ≥500 active items |
| Auto task breakdown | Stable domain model + team validates output quality |

**Data quality check before launching AI features:**
```sql
-- Check if enough sprint history exists
SELECT COUNT(*) FROM sprint_snapshots WHERE is_final = true;
-- Need: ≥10

-- Check estimation change data quality
SELECT COUNT(*) FROM domain_events 
WHERE event_type = 'WorkItem.EstimationChanged';
-- Need: ≥100

-- Check retro health trend data
SELECT COUNT(*) FROM domain_events 
WHERE event_type = 'Retro.SessionClosed';
-- Need: ≥5
```

---

## The Real Strategic Question

> It's not "which AI features to build" — it's "what data does TeamFlow need to collect today so AI can work well in 2 years."

**Answer:** TeamFlow is already collecting the right data from Phase 0:
- Every state change → DomainEvent with full context
- Every sprint → SprintSnapshot + BurndownDataPoints
- Every estimation change → who changed what, from/to, all votes
- Every retro → cards, votes, sentiment, action items, sprint metrics snapshot
- Every AI interaction → input context, output, human action (for RLHF)

The data collection starts on day 1 of Phase 1. The AI features use that data — but only when there's enough of it to be accurate and trustworthy.
