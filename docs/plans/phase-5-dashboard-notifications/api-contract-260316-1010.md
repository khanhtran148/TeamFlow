# API Contract: Phase 5 — Insights & Automation

**Created:** 2026-03-16
**Version:** 1.0

---

## 5.1 Search Endpoints

### GET /api/v1/search
- **Auth:** Bearer JWT
- **Rate Limit:** Search
- **Query Params:** `projectId` (required), `q`, `status[]`, `priority[]`, `type[]`, `assigneeId`, `sprintId`, `releaseId`, `fromDate`, `toDate`, `page`, `pageSize`
- **Response 200:** `PagedResult<WorkItemDto>`
- **Response 400:** ProblemDetails (missing projectId)
- **Response 403:** ProblemDetails (no project membership)

### POST /api/v1/projects/{projectId}/saved-filters
- **Auth:** Bearer JWT
- **Rate Limit:** Write
- **Body:** `{ name: string, filterJson: object, isDefault: boolean }`
- **Response 201:** `SavedFilterDto`
- **Response 400:** ProblemDetails (validation)
- **Response 409:** ProblemDetails (duplicate name)

### GET /api/v1/projects/{projectId}/saved-filters
- **Auth:** Bearer JWT
- **Rate Limit:** General
- **Response 200:** `SavedFilterDto[]`

### PUT /api/v1/projects/{projectId}/saved-filters/{id}
- **Auth:** Bearer JWT
- **Rate Limit:** Write
- **Body:** `{ name?: string, filterJson?: object, isDefault?: boolean }`
- **Response 200:** `SavedFilterDto`
- **Response 404:** ProblemDetails

### DELETE /api/v1/projects/{projectId}/saved-filters/{id}
- **Auth:** Bearer JWT
- **Rate Limit:** Write
- **Response 204:** No Content
- **Response 403:** ProblemDetails (not owner)

---

## 5.2 Dashboard Endpoints

### GET /api/v1/projects/{projectId}/dashboard/summary
- **Response 200:** `DashboardSummaryDto`

### GET /api/v1/projects/{projectId}/dashboard/velocity?sprintCount=10
- **Response 200:** `VelocityChartDto`

### GET /api/v1/projects/{projectId}/dashboard/cumulative-flow?fromDate={}&toDate={}
- **Response 200:** `CumulativeFlowDto`

### GET /api/v1/projects/{projectId}/dashboard/cycle-time?fromDate={}&toDate={}
- **Response 200:** `CycleTimeDto`

### GET /api/v1/projects/{projectId}/dashboard/workload
- **Response 200:** `WorkloadHeatmapDto`

### GET /api/v1/releases/{releaseId}/progress
- **Response 200:** `ReleaseProgressDto`

All dashboard endpoints: Auth Bearer JWT, Rate Limit General, 403 on no membership.

---

## 5.3 Notification Endpoints

### GET /api/v1/notifications?isRead={bool}&page=1&pageSize=20
- **Response 200:** `PagedResult<NotificationDto>`

### GET /api/v1/notifications/unread-count
- **Response 200:** `{ count: number }`

### POST /api/v1/notifications/{id}/read
- **Response 200:** OK

### POST /api/v1/notifications/read-all
- **Response 200:** OK

### GET /api/v1/notifications/preferences
- **Response 200:** `{ preferences: NotificationPreferenceDto[] }`

### PUT /api/v1/notifications/preferences
- **Body:** `{ preferences: NotificationPreferenceDto[] }`
- **Response 200:** OK

---

## 5.4 Report Endpoints

### GET /api/v1/sprints/{sprintId}/report
- **Response 200:** `SprintReportDto`
- **Response 404:** ProblemDetails (no report)

### GET /api/v1/projects/{projectId}/reports/sprints?page=1&pageSize=10
- **Response 200:** `PagedResult<SprintReportDto>`

### GET /api/v1/projects/{projectId}/reports/team-health/latest
- **Response 200:** `TeamHealthSummaryDto`

### GET /api/v1/projects/{projectId}/reports/team-health?page=1&pageSize=10
- **Response 200:** `PagedResult<TeamHealthSummaryDto>`

---

## Shared TypeScript Interfaces

```typescript
interface SavedFilterDto {
  id: string;
  name: string;
  filterJson: Record<string, unknown>;
  isDefault: boolean;
  createdAt: string;
}

interface DashboardSummaryDto {
  activeSprintId: string | null;
  activeSprintName: string | null;
  totalItems: number;
  openItems: number;
  completionPct: number;
  overdueReleases: number;
  staleItems: number;
  velocity3SprintAvg: number;
}

interface VelocityChartDto {
  sprints: VelocitySprintDto[];
}

interface VelocitySprintDto {
  sprintId: string;
  sprintName: string;
  plannedPoints: number;
  completedPoints: number;
  velocity: number;
  avg3Sprint: number;
  avg6Sprint: number;
}

interface CumulativeFlowDto {
  dataPoints: CumulativeFlowPointDto[];
}

interface CumulativeFlowPointDto {
  date: string;
  toDo: number;
  inProgress: number;
  inReview: number;
  done: number;
}

interface CycleTimeDto {
  byType: CycleTimeByTypeDto[];
}

interface CycleTimeByTypeDto {
  itemType: string;
  avgDays: number;
  medianDays: number;
  p90Days: number;
  sampleSize: number;
}

interface WorkloadHeatmapDto {
  members: WorkloadMemberDto[];
}

interface WorkloadMemberDto {
  userId: string;
  name: string;
  assignedCount: number;
  inProgressCount: number;
  pointsAssigned: number;
}

interface ReleaseProgressDto {
  doneCount: number;
  inProgressCount: number;
  todoCount: number;
  donePoints: number;
  totalPoints: number;
  completionPct: number;
}

interface NotificationDto {
  id: string;
  type: string;
  title: string;
  body: string | null;
  referenceId: string | null;
  referenceType: string | null;
  isRead: boolean;
  createdAt: string;
}

interface NotificationPreferenceDto {
  notificationType: string;
  emailEnabled: boolean;
  inAppEnabled: boolean;
}

interface SprintReportDto {
  id: string;
  sprintId: string;
  projectId: string;
  reportData: Record<string, unknown>;
  generatedAt: string;
  generatedBy: string;
}

interface TeamHealthSummaryDto {
  id: string;
  projectId: string;
  periodStart: string;
  periodEnd: string;
  summaryData: Record<string, unknown>;
  generatedAt: string;
}
```
