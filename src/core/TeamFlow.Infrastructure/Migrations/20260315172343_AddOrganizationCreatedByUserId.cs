using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationCreatedByUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "organizations",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "organizations");
        }
    }
}
