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
  isReady?: boolean;
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

// ---- Sprint Enums ----

export type SprintStatus = "Planning" | "Active" | "Completed";

// ---- Sprint DTOs ----

export interface SprintDto {
  id: string;
  projectId: string;
  name: string;
  goal: string | null;
  startDate: string | null;
  endDate: string | null;
  status: SprintStatus;
  totalPoints: number;
  completedPoints: number;
  itemCount: number;
  capacityUtilization: number | null;
  createdAt: string;
}

export interface SprintCapacityMemberDto {
  memberId: string;
  memberName: string;
  capacityPoints: number;
  assignedPoints: number;
}

export interface SprintDetailDto extends SprintDto {
  items: WorkItemDto[];
  capacity: SprintCapacityMemberDto[];
}

export interface BurndownDataPointDto {
  date: string;
  points: number;
}

export interface BurndownActualPointDto {
  date: string;
  remainingPoints: number;
  completedPoints: number;
  addedPoints: number;
}

export interface BurndownDto {
  sprintId: string;
  idealLine: BurndownDataPointDto[];
  actualLine: BurndownActualPointDto[];
}

// ---- Sprint Request Bodies ----

export interface CreateSprintBody {
  projectId: string;
  name: string;
  goal?: string;
  startDate?: string;
  endDate?: string;
}

export interface UpdateSprintBody {
  name: string;
  goal?: string;
  startDate?: string;
  endDate?: string;
}

export interface UpdateCapacityBody {
  capacity: { memberId: string; points: number }[];
}

// ---- Sprint Query Params ----

export interface GetSprintsParams {
  projectId: string;
  page?: number;
  pageSize?: number;
}

// ---- Comment DTOs ----

export interface CommentDto {
  id: string;
  workItemId: string;
  authorId: string;
  authorName: string;
  parentCommentId: string | null;
  content: string;
  editedAt: string | null;
  createdAt: string;
  replies: CommentDto[];
}

export interface GetCommentsResponse {
  items: CommentDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateCommentBody {
  content: string;
  parentCommentId?: string;
}

export interface UpdateCommentBody {
  content: string;
}

// ---- Retro Enums ----

export type RetroSessionStatus = "Draft" | "Open" | "Voting" | "Discussing" | "Closed";
export type RetroCardCategory = "WentWell" | "NeedsImprovement" | "ActionItem";

// ---- Retro DTOs ----

export interface RetroSessionDto {
  id: string;
  projectId: string;
  sprintId: string | null;
  facilitatorId: string;
  facilitatorName: string;
  anonymityMode: string;
  status: RetroSessionStatus;
  aiSummary: Record<string, unknown> | null;
  cards: RetroCardDto[];
  actionItems: RetroActionItemDto[];
  createdAt: string;
}

export interface RetroCardDto {
  id: string;
  authorId: string | null;
  authorName: string | null;
  category: RetroCardCategory;
  content: string;
  isDiscussed: boolean;
  totalVotes: number;
  createdAt: string;
}

export interface RetroActionItemDto {
  id: string;
  cardId: string | null;
  title: string;
  description: string | null;
  assigneeId: string | null;
  assigneeName: string | null;
  dueDate: string | null;
  linkedTaskId: string | null;
  createdAt: string;
}

export interface RetroSessionSummaryDto {
  id: string;
  projectId: string;
  sprintId: string | null;
  facilitatorName: string;
  anonymityMode: string;
  status: RetroSessionStatus;
  cardCount: number;
  actionItemCount: number;
  createdAt: string;
}

export interface ListRetroSessionsResponse {
  items: RetroSessionSummaryDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateRetroSessionBody {
  projectId: string;
  sprintId?: string;
  anonymityMode: string;
}

export interface SubmitRetroCardBody {
  category: RetroCardCategory;
  content: string;
}

export interface CastRetroVoteBody {
  voteCount: number;
}

export interface TransitionRetroBody {
  targetStatus: RetroSessionStatus;
}

export interface CreateRetroActionItemBody {
  sessionId: string;
  cardId?: string;
  title: string;
  description?: string;
  assigneeId?: string;
  dueDate?: string;
  linkToBacklog?: boolean;
}

// ---- Planning Poker DTOs ----

export interface PokerSessionDto {
  id: string;
  workItemId: string;
  projectId: string;
  facilitatorId: string;
  facilitatorName: string;
  isRevealed: boolean;
  finalEstimate: number | null;
  confirmedById: string | null;
  voteCount: number;
  votes: PokerVoteDto[];
  createdAt: string;
  closedAt: string | null;
}

export interface PokerVoteDto {
  id: string;
  voterId: string;
  voterName: string;
  value: number | null;
}

export interface CreatePokerSessionBody {
  workItemId: string;
}

export interface CastPokerVoteBody {
  value: number;
}

export interface ConfirmPokerEstimateBody {
  finalEstimate: number;
}

// ---- Release Detail DTOs ----

export interface ReleaseDetailDto {
  id: string;
  name: string;
  description: string | null;
  releaseNotes: string | null;
  releaseDate: string | null;
  status: ReleaseStatus;
  notesLocked: boolean;
  isOverdue: boolean;
  progress: ReleaseProgressDto;
  byEpic: ReleaseGroupDto[];
  byAssignee: ReleaseGroupDto[];
  bySprint: ReleaseGroupDto[];
  createdAt: string;
}

export interface ReleaseProgressDto {
  totalItems: number;
  doneItems: number;
  inProgressItems: number;
  toDoItems: number;
  totalPoints: number;
  donePoints: number;
  inProgressPoints: number;
  toDoPoints: number;
}

export interface ReleaseGroupDto {
  groupName: string;
  groupId: string | null;
  itemCount: number;
  doneCount: number;
}

export interface ShipReleaseResultDto {
  shipped: boolean;
  incompleteItems: IncompleteItemDto[] | null;
}

export interface IncompleteItemDto {
  id: string;
  title: string;
  status: WorkItemStatus;
}

export interface UpdateReleaseNotesBody {
  notes: string;
}

export interface ShipReleaseBody {
  confirmOpenItems: boolean;
}

// ---- Backlog Refinement ----

export interface ToggleReadyBody {
  isReady: boolean;
}

export interface PriorityUpdate {
  workItemId: string;
  priority: Priority;
}

export interface BulkUpdatePriorityBody {
  items: PriorityUpdate[];
}

// ---- Notification DTOs ----

export interface InAppNotificationDto {
  id: string;
  recipientId: string;
  type: string;
  title: string;
  body: string | null;
  referenceId: string | null;
  referenceType: string | null;
  isRead: boolean;
  createdAt: string;
}
