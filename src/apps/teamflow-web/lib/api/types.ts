// ============================================================
// Shared DTO types matching the TeamFlow API contracts
// Source: docs/architecture/api-contracts.md
// ============================================================

// ---- Enums ----

export type WorkItemType = "Epic" | "UserStory" | "Task" | "Bug" | "Spike";

export type WorkItemStatus =
  | "ToDo"
  | "InProgress"
  | "InReview"
  | "NeedsClarification"
  | "Done"
  | "Rejected";

export type Priority = "Critical" | "High" | "Medium" | "Low";

export type LinkType =
  | "Blocks"
  | "RelatesTo"
  | "Duplicates"
  | "DependsOn"
  | "Causes"
  | "Clones";

export type LinkScope = "SameProject" | "CrossProject";

export type ProjectStatus = "Active" | "Archived";

export type ReleaseStatus = "Unreleased" | "Overdue" | "Released";

// ---- Pagination ----

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ---- ProblemDetails (RFC 7807) ----

export interface ProblemDetails {
  status: number;
  title: string;
  detail?: string;
  instance?: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
}

// ---- Project DTOs ----

export interface ProjectDto {
  id: string;
  orgId: string;
  name: string;
  description: string | null;
  status: ProjectStatus;
  epicCount: number;
  openItemCount: number;
  createdAt: string;
  updatedAt: string;
}

// ---- Work Item DTOs ----

export interface WorkItemDto {
  id: string;
  projectId: string;
  parentId: string | null;
  type: WorkItemType;
  title: string;
  description: string | null;
  acceptanceCriteria: string | null;
  status: WorkItemStatus;
  priority: Priority | null;
  estimationValue: number | null;
  assigneeId: string | null;
  assigneeName: string | null;
  sprintId: string | null;
  releaseId: string | null;
  childCount: number;
  linkCount: number;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface WorkItemSummaryDto {
  id: string;
  type: WorkItemType;
  title: string;
  status: WorkItemStatus;
  priority: Priority | null;
  assigneeId: string | null;
  assigneeName: string | null;
  parentId: string | null;
  parentTitle: string | null;
  isBlocked: boolean;
  releaseId: string | null;
}

export interface BacklogItemDto extends WorkItemDto {
  isBlocked: boolean;
  releaseName: string | null;
}

// ---- Work Item Link DTOs ----

export interface WorkItemLinkItemDto {
  id: string;
  title: string;
  type: WorkItemType;
  status: WorkItemStatus;
  scope: LinkScope;
}

export interface WorkItemLinkGroupDto {
  linkType: LinkType;
  items: WorkItemLinkItemDto[];
}

export interface WorkItemLinksDto {
  workItemId: string;
  groups: WorkItemLinkGroupDto[];
}

// ---- Blockers ----

export interface BlockerItemDto {
  blockerId: string;
  title: string;
  status: WorkItemStatus;
}

export interface BlockersDto {
  workItemId: string;
  hasUnresolvedBlockers: boolean;
  blockers: BlockerItemDto[];
}

// ---- Kanban DTOs ----

export interface KanbanItemDto {
  id: string;
  type: WorkItemType;
  title: string;
  status: WorkItemStatus;
  priority: Priority | null;
  assigneeId: string | null;
  assigneeName: string | null;
  parentId: string | null;
  parentTitle: string | null;
  isBlocked: boolean;
  releaseId: string | null;
}

export interface KanbanColumnDto {
  status: WorkItemStatus;
  itemCount: number;
  items: KanbanItemDto[];
}

export interface KanbanBoardDto {
  projectId: string;
  columns: KanbanColumnDto[];
}

// ---- Release DTOs ----

export interface ReleaseDto {
  id: string;
  projectId: string;
  name: string;
  description: string | null;
  releaseDate: string | null;
  status: ReleaseStatus;
  notesLocked: boolean;
  totalItems: number;
  itemCountsByStatus: Partial<Record<WorkItemStatus, number>>;
  createdAt: string;
}

// ---- API Request bodies ----

export interface CreateProjectBody {
  orgId: string;
  name: string;
  description?: string;
}

export interface UpdateProjectBody {
  name: string;
  description?: string;
}

export interface CreateWorkItemBody {
  projectId: string;
  parentId?: string;
  type: WorkItemType;
  title: string;
  description?: string;
  priority?: Priority;
  acceptanceCriteria?: string;
}

export interface UpdateWorkItemBody {
  title: string;
  description?: string;
  priority?: Priority;
  estimationValue?: number;
  acceptanceCriteria?: string;
}

export interface ChangeStatusBody {
  status: WorkItemStatus;
}

export interface AssignBody {
  assigneeId: string;
}

export interface MoveWorkItemBody {
  newParentId?: string;
}

export interface AddLinkBody {
  targetId: string;
  linkType: LinkType;
}

export interface CreateReleaseBody {
  projectId: string;
  name: string;
  description?: string;
  releaseDate?: string;
}

export interface UpdateReleaseBody {
  name: string;
  description?: string;
  releaseDate?: string;
}

export interface WorkItemSortOrder {
  workItemId: string;
  sortOrder: number;
}

export interface ReorderBacklogBody {
  projectId: string;
  items: WorkItemSortOrder[];
}

// ---- API Query params ----

export interface GetProjectsParams {
  orgId?: string;
  status?: ProjectStatus;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface GetBacklogParams {
  projectId: string;
  status?: WorkItemStatus;
  priority?: Priority;
  assigneeId?: string;
  type?: WorkItemType;
  sprintId?: string;
  releaseId?: string;
  unscheduled?: boolean;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface GetKanbanParams {
  projectId: string;
  assigneeId?: string;
  type?: WorkItemType;
  priority?: Priority;
  sprintId?: string;
  releaseId?: string;
  swimlane?: "assignee" | "epic";
}

export interface GetReleasesParams {
  projectId: string;
  page?: number;
  pageSize?: number;
}
