using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_execution_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    job_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    records_processed = table.Column<int>(type: "integer", nullable: false),
                    records_failed = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<JsonDocument>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_execution_metrics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                    table.ForeignKey(
                        name: "FK_projects_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "domain_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    metadata = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_domain_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_domain_events_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    custom_permissions = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_project_memberships_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_memberships_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "releases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    release_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    released_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    released_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes_locked = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_releases", x => x.id);
                    table.ForeignKey(
                        name: "FK_releases_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_releases_users_released_by_id",
                        column: x => x.released_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "sprints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    goal = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    capacity_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprints", x => x.id);
                    table.ForeignKey(
                        name: "FK_sprints_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_members_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "burndown_data_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sprint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_date = table.Column<DateOnly>(type: "date", nullable: false),
                    remaining_points = table.Column<int>(type: "integer", nullable: false),
                    completed_points = table.Column<int>(type: "integer", nullable: false),
                    added_points = table.Column<int>(type: "integer", nullable: false),
                    is_weekend = table.Column<bool>(type: "boolean", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_burndown_data_points", x => x.id);
                    table.ForeignKey(
                        name: "FK_burndown_data_points_sprints_sprint_id",
                        column: x => x.sprint_id,
                        principalTable: "sprints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "retro_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sprint_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facilitator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    anonymity_mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ai_summary = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retro_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_retro_sessions_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_retro_sessions_sprints_sprint_id",
                        column: x => x.sprint_id,
                        principalTable: "sprints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_retro_sessions_users_facilitator_id",
                        column: x => x.facilitator_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sprint_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sprint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_final = table.Column<bool>(type: "boolean", nullable: false),
                    payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    captured_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprint_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_sprint_snapshots_sprints_sprint_id",
                        column: x => x.sprint_id,
                        principalTable: "sprints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_velocity_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sprint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planned_points = table.Column<int>(type: "integer", nullable: false),
                    completed_points = table.Column<int>(type: "integer", nullable: false),
                    velocity = table.Column<int>(type: "integer", nullable: false),
                    velocity_3sprint_avg = table.Column<double>(type: "double precision", nullable: true),
                    velocity_6sprint_avg = table.Column<double>(type: "double precision", nullable: true),
                    velocity_trend = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ai_adjusted_velocity = table.Column<double>(type: "double precision", nullable: true),
                    confidence_interval = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_velocity_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_velocity_history_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_velocity_history_sprints_sprint_id",
                        column: x => x.sprint_id,
                        principalTable: "sprints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "retro_cards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    is_discussed = table.Column<bool>(type: "boolean", nullable: false),
                    sentiment = table.Column<double>(type: "double precision", nullable: true),
                    theme_tags = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retro_cards", x => x.id);
                    table.ForeignKey(
                        name: "FK_retro_cards_retro_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "retro_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_retro_cards_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "retro_votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vote_count = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retro_votes", x => x.id);
                    table.CheckConstraint("CK_retro_votes_vote_count", "vote_count BETWEEN 1 AND 2");
                    table.ForeignKey(
                        name: "FK_retro_votes_retro_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "retro_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_retro_votes_users_voter_id",
                        column: x => x.voter_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_interactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sprint_id = table.Column<Guid>(type: "uuid", nullable: true),
                    model_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    input_context = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    ai_output = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    user_action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    user_modified = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    latency_ms = table.Column<int>(type: "integer", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_interactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_interactions_sprints_sprint_id",
                        column: x => x.sprint_id,
                        principalTable: "sprints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ai_interactions_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "retro_action_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    assignee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    linked_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retro_action_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_retro_action_items_retro_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "retro_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_retro_action_items_retro_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "retro_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_retro_action_items_users_assignee_id",
                        column: x => x.assignee_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "work_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<string>(type: "text", nullable: true),
                    estimation_value = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    estimation_unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    estimation_confidence = table.Column<double>(type: "double precision", nullable: true),
                    estimation_source = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    estimation_history = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    assignee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sprint_id = table.Column<Guid>(type: "uuid", nullable: true),
                    release_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    retro_action_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    custom_fields = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    ai_metadata = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    external_refs = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_items_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_work_items_releases_release_id",
                        column: x => x.release_id,
                        principalTable: "releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_work_items_retro_action_items_retro_action_item_id",
                        column: x => x.retro_action_item_id,
                        principalTable: "retro_action_items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_work_items_sprints_sprint_id",
                        column: x => x.sprint_id,
                        principalTable: "sprints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_work_items_users_assignee_id",
                        column: x => x.assignee_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_work_items_work_items_parent_id",
                        column: x => x.parent_id,
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_item_embeddings",
                columns: table => new
                {
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    embedding = table.Column<float[]>(type: "float4[]", nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    generated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    is_stale = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_item_embeddings", x => x.work_item_id);
                    table.ForeignKey(
                        name: "FK_work_item_embeddings_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_item_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    action_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    field_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    old_value = table.Column<string>(type: "text", nullable: true),
                    new_value = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_item_histories", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_item_histories_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_work_item_histories_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_item_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    link_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_item_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_item_links_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_item_links_work_items_source_id",
                        column: x => x.source_id,
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_item_links_work_items_target_id",
                        column: x => x.target_id,
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_interactions_actor_id",
                table: "ai_interactions",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_interactions_sprint_id",
                table: "ai_interactions",
                column: "sprint_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_interactions_work_item_id",
                table: "ai_interactions",
                column: "work_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_burndown_data_points_sprint_id_recorded_date",
                table: "burndown_data_points",
                columns: new[] { "sprint_id", "recorded_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_domain_events_actor_id_event_type_occurred_at",
                table: "domain_events",
                columns: new[] { "actor_id", "event_type", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_domain_events_aggregate_type_aggregate_id_occurred_at",
                table: "domain_events",
                columns: new[] { "aggregate_type", "aggregate_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_domain_events_occurred_at",
                table: "domain_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "IX_project_memberships_project_id_member_id_member_type",
                table: "project_memberships",
                columns: new[] { "project_id", "member_id", "member_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_memberships_UserId",
                table: "project_memberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_org_id",
                table: "projects",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_releases_project_id",
                table: "releases",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_releases_released_by_id",
                table: "releases",
                column: "released_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_retro_action_items_assignee_id",
                table: "retro_action_items",
                column: "assignee_id");

            migrationBuilder.CreateIndex(
                name: "IX_retro_action_items_card_id",
                table: "retro_action_items",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "IX_retro_action_items_linked_task_id",
                table: "retro_action_items",
                column: "linked_task_id");

            migrationBuilder.CreateIndex(
                name: "IX_retro_action_items_session_id",
                table: "retro_action_items",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_retro_cards_author_id",
                table: "retro_cards",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_retro_cards_session_id",
                table: "retro_cards",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_retro_sessions_facilitator_id",
                table: "retro_sessions",
                column: "facilitator_id");

            migrationBuilder.CreateIndex(
                name: "IX_retro_sessions_project_id",
                table: "retro_sessions",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_retro_sessions_sprint_id",
                table: "retro_sessions",
                column: "sprint_id");

            migrationBuilder.CreateIndex(
                name: "IX_retro_votes_card_id_voter_id",
                table: "retro_votes",
                columns: new[] { "card_id", "voter_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_retro_votes_voter_id",
                table: "retro_votes",
                column: "voter_id");

            migrationBuilder.CreateIndex(
                name: "IX_sprint_snapshots_sprint_id_snapshot_type",
                table: "sprint_snapshots",
                columns: new[] { "sprint_id", "snapshot_type" });

            migrationBuilder.CreateIndex(
                name: "IX_sprints_project_id",
                table: "sprints",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_members_team_id_user_id",
                table: "team_members",
                columns: new[] { "team_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_members_user_id",
                table: "team_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_velocity_history_project_id",
                table: "team_velocity_history",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_velocity_history_sprint_id",
                table: "team_velocity_history",
                column: "sprint_id");

            migrationBuilder.CreateIndex(
                name: "IX_teams_org_id",
                table: "teams",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_item_histories_actor_id_created_at",
                table: "work_item_histories",
                columns: new[] { "actor_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_work_item_histories_work_item_id_created_at",
                table: "work_item_histories",
                columns: new[] { "work_item_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_work_item_links_created_by",
                table: "work_item_links",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_work_item_links_source_id",
                table: "work_item_links",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_item_links_source_id_target_id_link_type",
                table: "work_item_links",
                columns: new[] { "source_id", "target_id", "link_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_item_links_target_id",
                table: "work_item_links",
                column: "target_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_items_assignee_id",
                table: "work_items",
                column: "assignee_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_items_parent_id",
                table: "work_items",
                column: "parent_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_work_items_project_id",
                table: "work_items",
                column: "project_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_work_items_release_id",
                table: "work_items",
                column: "release_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_items_retro_action_item_id",
                table: "work_items",
                column: "retro_action_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_items_sprint_id",
                table: "work_items",
                column: "sprint_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ai_interactions_work_items_work_item_id",
                table: "ai_interactions",
                column: "work_item_id",
                principalTable: "work_items",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_retro_action_items_work_items_linked_task_id",
                table: "retro_action_items",
                column: "linked_task_id",
                principalTable: "work_items",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_retro_sessions_sprints_sprint_id",
                table: "retro_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_work_items_sprints_sprint_id",
                table: "work_items");

            migrationBuilder.DropForeignKey(
                name: "FK_releases_users_released_by_id",
                table: "releases");

            migrationBuilder.DropForeignKey(
                name: "FK_retro_action_items_users_assignee_id",
                table: "retro_action_items");

            migrationBuilder.DropForeignKey(
                name: "FK_retro_cards_users_author_id",
                table: "retro_cards");

            migrationBuilder.DropForeignKey(
                name: "FK_retro_sessions_users_facilitator_id",
                table: "retro_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_work_items_users_assignee_id",
                table: "work_items");

            migrationBuilder.DropForeignKey(
                name: "FK_retro_action_items_work_items_linked_task_id",
                table: "retro_action_items");

            migrationBuilder.DropTable(
                name: "ai_interactions");

            migrationBuilder.DropTable(
                name: "burndown_data_points");

            migrationBuilder.DropTable(
                name: "domain_events");

            migrationBuilder.DropTable(
                name: "job_execution_metrics");

            migrationBuilder.DropTable(
                name: "project_memberships");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "retro_votes");

            migrationBuilder.DropTable(
                name: "sprint_snapshots");

            migrationBuilder.DropTable(
                name: "team_members");

            migrationBuilder.DropTable(
                name: "team_velocity_history");

            migrationBuilder.DropTable(
                name: "work_item_embeddings");

            migrationBuilder.DropTable(
                name: "work_item_histories");

            migrationBuilder.DropTable(
                name: "work_item_links");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "sprints");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "work_items");

            migrationBuilder.DropTable(
                name: "releases");

            migrationBuilder.DropTable(
                name: "retro_action_items");

            migrationBuilder.DropTable(
                name: "retro_cards");

            migrationBuilder.DropTable(
                name: "retro_sessions");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "organizations");
        }
    }
}
