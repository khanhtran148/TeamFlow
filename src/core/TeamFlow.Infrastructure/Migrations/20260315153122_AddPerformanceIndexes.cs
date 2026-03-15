using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "idx_wi_project_status_priority",
                table: "work_items",
                columns: new[] { "project_id", "status", "priority" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_wi_sprint_status",
                table: "work_items",
                columns: new[] { "sprint_id", "status" },
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_wi_project_status_priority",
                table: "work_items");

            migrationBuilder.DropIndex(
                name: "idx_wi_sprint_status",
                table: "work_items");
        }
    }
}
