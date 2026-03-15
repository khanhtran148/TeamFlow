# 03 — Data Model & Schema

## Design Philosophy

> Every table designed today is an investment or technical debt for AI tomorrow.

Three principles:
1. **Never hardcode estimation units** — support future AI-adjusted velocity
2. **Event log from day one** — AI needs history to learn patterns
3. **AI metadata columns pre-created** — populate later without migration

---

## Core Tables

### Users
```sql
CREATE TABLE users (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email         VARCHAR(255) UNIQUE NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  name          VARCHAR(100) NOT NULL,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Organizations
```sql
CREATE TABLE organizations (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name       VARCHAR(100) NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Teams
```sql
CREATE TABLE teams (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  org_id      UUID NOT NULL REFERENCES organizations(id),
  name        VARCHAR(100) NOT NULL,
  description TEXT,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### TeamMembers
```sql
CREATE TABLE team_members (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  team_id    UUID NOT NULL REFERENCES teams(id),
  user_id    UUID NOT NULL REFERENCES users(id),
  role       project_role NOT NULL,
  joined_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  UNIQUE (team_id, user_id)
);
```

### Projects
```sql
CREATE TABLE projects (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  org_id      UUID NOT NULL REFERENCES organizations(id),
  name        VARCHAR(100) NOT NULL,
  description TEXT,
  status      VARCHAR(20) NOT NULL DEFAULT 'Active', -- Active, Archived
  created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### ProjectMemberships — 3-level permission
```sql
CREATE TABLE project_memberships (
  id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  project_id          UUID NOT NULL REFERENCES projects(id),
  member_id           UUID NOT NULL,
  member_type         VARCHAR(10) NOT NULL, -- User, Team
  role                project_role NOT NULL,
  custom_permissions  JSONB, -- Individual-level overrides
  created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  UNIQUE (project_id, member_id, member_type)
);
```

---

## Work Items

### WorkItems — Unified table with hierarchy
```sql
CREATE TABLE work_items (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  project_id      UUID NOT NULL REFERENCES projects(id),
  parent_id       UUID REFERENCES work_items(id), -- NULL for Epic
  type            work_item_type NOT NULL, -- Epic, UserStory, Task, Bug, Spike
  title           VARCHAR(500) NOT NULL,
  description     TEXT,
  status          work_item_status NOT NULL DEFAULT 'ToDo',
  priority        VARCHAR(20), -- Critical, High, Medium, Low

  -- Estimation (flexible, AI-ready)
  estimation_value      DECIMAL(6,2),
  estimation_unit       VARCHAR(20) DEFAULT 'StoryPoint',
  estimation_confidence FLOAT,                    -- 0.0-1.0, null until AI
  estimation_source     VARCHAR(10),              -- Human, AI, Hybrid
  estimation_history    JSONB DEFAULT '[]'::jsonb, -- [{value, source, actor, ts}]

  -- Assignments
  assignee_id     UUID REFERENCES users(id),
  sprint_id       UUID REFERENCES sprints(id),
  release_id      UUID REFERENCES releases(id),

  -- Retrospective link
  retro_action_item_id UUID REFERENCES retro_action_items(id),

  -- Flexible fields
  custom_fields   JSONB DEFAULT '{}'::jsonb,
  ai_metadata     JSONB DEFAULT '{
    "suggested_epic_id": null,
    "risk_score": null,
    "complexity_indicators": [],
    "similar_item_ids": [],
    "auto_generated": false,
    "sprint_fit_probability": null,
    "stale_flag": false
  }'::jsonb,
  external_refs   JSONB DEFAULT '{}'::jsonb, -- {github_pr, notion_page, figma}

  -- Search
  search_vector   TSVECTOR,

  -- Timestamps
  created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  deleted_at  TIMESTAMPTZ -- soft delete
);

-- Indexes
CREATE INDEX idx_wi_project    ON work_items(project_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_wi_parent     ON work_items(parent_id)  WHERE deleted_at IS NULL;
CREATE INDEX idx_wi_assignee   ON work_items(assignee_id);
CREATE INDEX idx_wi_sprint     ON work_items(sprint_id);
CREATE INDEX idx_wi_release    ON work_items(release_id);
CREATE INDEX idx_wi_search     ON work_items USING GIN(search_vector);
CREATE INDEX idx_wi_custom     ON work_items USING GIN(custom_fields jsonb_path_ops);
CREATE INDEX idx_wi_ai         ON work_items USING GIN(ai_metadata jsonb_path_ops);
```

### WorkItemHistories — Append-only, never updated or deleted
```sql
CREATE TABLE work_item_histories (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_item_id  UUID NOT NULL REFERENCES work_items(id),
  actor_id      UUID REFERENCES users(id),
  actor_type    VARCHAR(10) NOT NULL DEFAULT 'User', -- User, System, AI
  action_type   VARCHAR(50) NOT NULL,
  field_name    VARCHAR(100),
  old_value     TEXT,
  new_value     TEXT,
  metadata      JSONB DEFAULT '{}'::jsonb,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_wih_item ON work_item_histories(work_item_id, created_at DESC);
CREATE INDEX idx_wih_actor ON work_item_histories(actor_id, created_at DESC);
```

### WorkItemLinks
```sql
CREATE TABLE work_item_links (
  id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  source_id    UUID NOT NULL REFERENCES work_items(id),
  target_id    UUID NOT NULL REFERENCES work_items(id),
  link_type    VARCHAR(30) NOT NULL, -- blocks, relates_to, duplicates, depends_on, causes, clones
  scope        VARCHAR(20) NOT NULL DEFAULT 'SameProject', -- SameProject, CrossProject
  created_by   UUID NOT NULL REFERENCES users(id),
  created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  UNIQUE (source_id, target_id, link_type)
);

CREATE INDEX idx_wil_source ON work_item_links(source_id);
CREATE INDEX idx_wil_target ON work_item_links(target_id);
```

---

## Sprint & Release

### Sprints
```sql
CREATE TABLE sprints (
  id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  project_id   UUID NOT NULL REFERENCES projects(id),
  name         VARCHAR(100) NOT NULL,
  goal         TEXT,
  start_date   DATE,
  end_date     DATE,
  status       VARCHAR(20) NOT NULL DEFAULT 'Planning', -- Planning, Active, Completed
  capacity_json JSONB DEFAULT '{}'::jsonb, -- {member_id: points}
  created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Releases
```sql
CREATE TABLE releases (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  project_id       UUID NOT NULL REFERENCES projects(id),
  name             VARCHAR(100) NOT NULL,
  description      TEXT,
  release_date     DATE,
  status           VARCHAR(20) NOT NULL DEFAULT 'Unreleased', -- Unreleased, Overdue, Released
  released_at      TIMESTAMPTZ,
  released_by_id   UUID REFERENCES users(id),
  notes_locked     BOOLEAN NOT NULL DEFAULT FALSE,
  created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

## Retrospective

### RetroSessions
```sql
CREATE TABLE retro_sessions (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  sprint_id        UUID REFERENCES sprints(id),
  project_id       UUID NOT NULL REFERENCES projects(id),
  facilitator_id   UUID NOT NULL REFERENCES users(id),
  anonymity_mode   VARCHAR(10) NOT NULL DEFAULT 'Public', -- Anonymous, Public
  status           VARCHAR(20) NOT NULL DEFAULT 'Draft',
  -- Draft, Open, Voting, Discussing, Closed
  ai_summary       JSONB, -- populated after session close
  created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### RetroCards
```sql
CREATE TABLE retro_cards (
  id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  session_id   UUID NOT NULL REFERENCES retro_sessions(id),
  author_id    UUID NOT NULL REFERENCES users(id), -- never exposed in API when anonymous
  category     VARCHAR(30) NOT NULL, -- WentWell, NeedsImprovement, ActionItem
  content      TEXT NOT NULL,
  is_discussed BOOLEAN NOT NULL DEFAULT FALSE,
  sentiment    FLOAT, -- AI-analyzed, null until populated
  theme_tags   JSONB DEFAULT '[]'::jsonb,
  created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### RetroVotes
```sql
CREATE TABLE retro_votes (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  card_id    UUID NOT NULL REFERENCES retro_cards(id),
  voter_id   UUID NOT NULL REFERENCES users(id),
  vote_count SMALLINT NOT NULL DEFAULT 1 CHECK (vote_count BETWEEN 1 AND 2),
  UNIQUE (card_id, voter_id)
);
```

### RetroActionItems
```sql
CREATE TABLE retro_action_items (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  session_id     UUID NOT NULL REFERENCES retro_sessions(id),
  card_id        UUID REFERENCES retro_cards(id),
  title          VARCHAR(500) NOT NULL,
  description    TEXT,
  assignee_id    UUID REFERENCES users(id),
  due_date       DATE,
  linked_task_id UUID REFERENCES work_items(id), -- bidirectional link
  created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

## AI-Ready Tables (created Phase 0, populated later)

### DomainEvents — Event log for AI training
```sql
CREATE TABLE domain_events (
  id              UUID NOT NULL,
  event_type      VARCHAR(100) NOT NULL,
  aggregate_type  VARCHAR(50) NOT NULL,
  aggregate_id    UUID NOT NULL,
  actor_id        UUID REFERENCES users(id),
  actor_type      VARCHAR(10) NOT NULL DEFAULT 'User',
  payload         JSONB NOT NULL,
  metadata        JSONB DEFAULT '{}'::jsonb,
  occurred_at     TIMESTAMPTZ NOT NULL,
  recorded_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  schema_version  INTEGER NOT NULL DEFAULT 1,
  session_id      UUID
) PARTITION BY RANGE (occurred_at);

-- Create first partition
CREATE TABLE domain_events_2026_03
PARTITION OF domain_events
FOR VALUES FROM ('2026-03-01') TO ('2026-04-01');

-- Indexes on parent table (inherited by partitions)
CREATE INDEX idx_de_aggregate ON domain_events(aggregate_type, aggregate_id, occurred_at DESC);
CREATE INDEX idx_de_actor     ON domain_events(actor_id, event_type, occurred_at DESC);
CREATE INDEX idx_de_time      ON domain_events(occurred_at DESC);
CREATE INDEX idx_de_payload   ON domain_events USING GIN(payload jsonb_path_ops);
```

### SprintSnapshots — Immutable once sprint closes
```sql
CREATE TABLE sprint_snapshots (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  sprint_id      UUID NOT NULL REFERENCES sprints(id),
  snapshot_type  VARCHAR(20) NOT NULL, -- OnStart, Daily, OnClose
  is_final       BOOLEAN NOT NULL DEFAULT FALSE,
  payload        JSONB NOT NULL,
  captured_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_ss_sprint ON sprint_snapshots(sprint_id, snapshot_type);
```

### BurndownDataPoints — Daily snapshots per sprint
```sql
CREATE TABLE burndown_data_points (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  sprint_id        UUID NOT NULL REFERENCES sprints(id),
  recorded_date    DATE NOT NULL,
  remaining_points INTEGER NOT NULL,
  completed_points INTEGER NOT NULL,
  added_points     INTEGER NOT NULL DEFAULT 0,
  is_weekend       BOOLEAN NOT NULL DEFAULT FALSE,
  recorded_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  UNIQUE (sprint_id, recorded_date)
);
```

### TeamVelocityHistory
```sql
CREATE TABLE team_velocity_history (
  id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  project_id              UUID NOT NULL REFERENCES projects(id),
  sprint_id               UUID NOT NULL REFERENCES sprints(id),
  planned_points          INTEGER NOT NULL,
  completed_points        INTEGER NOT NULL,
  velocity                INTEGER NOT NULL,
  velocity_3sprint_avg    FLOAT,
  velocity_6sprint_avg    FLOAT,
  velocity_trend          VARCHAR(20), -- Increasing, Decreasing, Stable
  ai_adjusted_velocity    FLOAT,       -- null until AI populates
  confidence_interval     JSONB,       -- {lower, upper}
  recorded_at             TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### WorkItemEmbeddings — Semantic search (pgvector)
```sql
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE work_item_embeddings (
  work_item_id  UUID PRIMARY KEY REFERENCES work_items(id),
  embedding     VECTOR(1536),           -- null until AI service populates
  model         VARCHAR(100),
  generated_at  TIMESTAMPTZ,
  is_stale      BOOLEAN NOT NULL DEFAULT TRUE
);

-- Index created but only useful once embeddings populated
CREATE INDEX ON work_item_embeddings
USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);
```

### AIInteractions — Human feedback loop
```sql
CREATE TABLE ai_interactions (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  feature_type    VARCHAR(50) NOT NULL,  -- EstimationSuggest, TaskBreakdown, etc.
  work_item_id    UUID REFERENCES work_items(id),
  sprint_id       UUID REFERENCES sprints(id),
  model_version   VARCHAR(100),
  input_context   JSONB NOT NULL,
  ai_output       JSONB NOT NULL,
  user_action     VARCHAR(20) NOT NULL,  -- Accepted, Modified, Rejected, Ignored
  user_modified   JSONB,
  actor_id        UUID NOT NULL REFERENCES users(id),
  latency_ms      INTEGER,
  occurred_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

## Enums (PostgreSQL)

```sql
CREATE TYPE project_role AS ENUM (
  'OrgAdmin', 'ProductOwner', 'TechnicalLeader',
  'TeamManager', 'Developer', 'Viewer'
);

CREATE TYPE work_item_type AS ENUM (
  'Epic', 'UserStory', 'Task', 'Bug', 'Spike'
);

CREATE TYPE work_item_status AS ENUM (
  'ToDo', 'InProgress', 'InReview',
  'NeedsClarification', 'Done', 'Rejected'
);
```

---

## Data Retention Strategy

```
Hot    (PostgreSQL main)      Active data + 6 months
Warm   (PostgreSQL partition) 6–36 months — slower queries acceptable
Cold   (S3 object storage)    36+ months — JSON export, compressed
```

- `domain_events` — partitioned by month, archived after 36 months
- `work_item_histories` — never deleted (compliance)
- Soft-deleted work items — hard deleted after 30 days (cleanup job)
- Retro cards — kept permanently for team health trend analysis
