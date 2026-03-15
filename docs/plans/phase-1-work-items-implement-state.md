# Phase 1 Work Items — Implementation State

## Topic
Phase 1: Work Item Management — Project CRUD, Work Item CRUD + Hierarchy, Assignment, Item Linking, Release Basics, Backlog/Kanban Queries, Realtime Broadcast

## Discovery Context
- **branch:** feat/phase-1-work-items
- **requirements:** Backend-only, fullstack Phase 1 scope from docs/process/phases.md
- **test_db_strategy:** In-memory/mocks for unit tests, Testcontainers PostgreSQL for integration tests
- **task_type:** feature

## Phase-Specific Context
- **plan_dir:** docs/plans/phase-1-work-items
- **plan_source:** docs/plans/phase-1-work-items/plan.md
- **scope:** backend-only (38 tasks, 9 phases)
- **approach:** Test-First Development, sealed classes, vertical slice architecture
- **key_decisions:**
  - No auth enforcement — FakeCurrentUser + AlwaysAllowPermissionChecker stubs
  - All handlers return Result<T>
  - Controllers only call Sender.Send()
  - History via IHistoryService only (append-only)
  - Circular link detection via BFS
  - Bidirectional links in single transaction
  - Soft-delete cascade via recursive approach
