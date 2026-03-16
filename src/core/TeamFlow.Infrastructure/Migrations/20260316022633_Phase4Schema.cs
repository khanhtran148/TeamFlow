using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase4Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_ready_for_sprint",
                table: "work_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "release_notes",
                table: "releases",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    edited_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comments", x => x.id);
                    table.ForeignKey(
                        name: "FK_comments_comments_parent_comment_id",
                        column: x => x.parent_comment_id,
                        principalTable: "comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comments_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comments_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "in_app_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_in_app_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_in_app_notifications_users_recipient_id",
                        column: x => x.recipient_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "planning_poker_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facilitator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_revealed = table.Column<bool>(type: "boolean", nullable: false),
                    final_estimate = table.Column<decimal>(type: "numeric(5,1)", nullable: true),
                    confirmed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planning_poker_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_planning_poker_sessions_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_planning_poker_sessions_users_confirmed_by_id",
                        column: x => x.confirmed_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_planning_poker_sessions_users_facilitator_id",
                        column: x => x.facilitator_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_planning_poker_sessions_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "planning_poker_votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<decimal>(type: "numeric(5,1)", nullable: false),
                    voted_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planning_poker_votes", x => x.id);
                    table.ForeignKey(
                        name: "FK_planning_poker_votes_planning_poker_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "planning_poker_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_planning_poker_votes_users_voter_id",
                        column: x => x.voter_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_comments_author_id",
                table: "comments",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_comments_parent_comment_id",
                table: "comments",
                column: "parent_comment_id");

            migrationBuilder.CreateIndex(
                name: "IX_comments_work_item_id_created_at",
                table: "comments",
                columns: new[] { "work_item_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_in_app_notifications_recipient_id_is_read_created_at",
                table: "in_app_notifications",
                columns: new[] { "recipient_id", "is_read", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_planning_poker_sessions_confirmed_by_id",
                table: "planning_poker_sessions",
                column: "confirmed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_planning_poker_sessions_facilitator_id",
                table: "planning_poker_sessions",
                column: "facilitator_id");

            migrationBuilder.CreateIndex(
                name: "IX_planning_poker_sessions_project_id",
                table: "planning_poker_sessions",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_planning_poker_sessions_work_item_id_active",
                table: "planning_poker_sessions",
                column: "work_item_id",
                unique: true,
                filter: "closed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_planning_poker_votes_session_id_voter_id",
                table: "planning_poker_votes",
                columns: new[] { "session_id", "voter_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_planning_poker_votes_voter_id",
                table: "planning_poker_votes",
                column: "voter_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comments");

            migrationBuilder.DropTable(
                name: "in_app_notifications");

            migrationBuilder.DropTable(
                name: "planning_poker_votes");

            migrationBuilder.DropTable(
                name: "planning_poker_sessions");

            migrationBuilder.DropColumn(
                name: "is_ready_for_sprint",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "release_notes",
                table: "releases");
        }
    }
}
