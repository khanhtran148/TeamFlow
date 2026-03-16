---
topic: Assignee Tooltip with Name + Assignment Date
status: complete
created: 2026-03-16
---

# Implement State: Assignee Tooltip

## Discovery Context
- **Branch:** feat/user-profile (continue on current)
- **Requirements:** Hover on assignee avatar shows full name + assignment date
- **Test DB Strategy:** Docker containers (Testcontainers)
- **Feature Scope:** Fullstack
- **Task Type:** feature

## Phase-Specific Context
- **Plan dir:** docs/plans/assignee-tooltip
- **Plan source:** docs/plans/assignee-tooltip/plan.md

### Plan Summary
2 phases:
1. **Phase 1 (Backend):** Add `AssignedAt` to WorkItem entity + migration, update Assign/Unassign handlers, update DTOs
2. **Phase 2 (Frontend):** Add `assignedAt` to TS types, enhance UserAvatar tooltip, update all assignee avatar usages
