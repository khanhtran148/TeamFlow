using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgMembershipModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "organizations",
                newName: "created_by_user_id");

            // Step 1: Add slug as nullable to allow backfill before unique constraint
            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "organizations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "organizations",
                type: "timestamptz",
                nullable: false,
                defaultValue: DateTime.UtcNow);

            // Step 2: Backfill slugs from organization names
            // Generates URL-safe slug: lowercase, spaces to hyphens, strip special chars
            migrationBuilder.Sql(@"
                UPDATE organizations
                SET slug = LOWER(
                    REGEXP_REPLACE(
                        REGEXP_REPLACE(
                            REGEXP_REPLACE(
                                TRIM(name),
                                '\s+', '-', 'g'
                            ),
                            '[^a-z0-9\-]', '', 'g'
                        ),
                        '-{2,}', '-', 'g'
                    )
                )
                WHERE slug IS NULL OR slug = '';
            ");

            // Handle duplicate slugs by appending row number
            migrationBuilder.Sql(@"
                WITH duplicates AS (
                    SELECT id,
                           slug,
                           ROW_NUMBER() OVER (PARTITION BY slug ORDER BY created_at) AS rn
                    FROM organizations
                )
                UPDATE organizations
                SET slug = duplicates.slug || '-' || duplicates.rn::text
                FROM duplicates
                WHERE organizations.id = duplicates.id
                  AND duplicates.rn > 1;
            ");

            // Step 3: Make slug NOT NULL after backfill
            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "organizations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            // Step 4: Create organization_members table
            migrationBuilder.CreateTable(
                name: "organization_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_members_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organization_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Step 5: Backfill OrganizationMember(Owner) from existing OrgAdmin project memberships
            // For each user with ProjectRole.OrgAdmin on any project in an org,
            // create an OrganizationMember with Owner role (if they are not already a member).
            migrationBuilder.Sql(@"
                INSERT INTO organization_members (id, organization_id, user_id, role, joined_at)
                SELECT
                    gen_random_uuid(),
                    p.org_id,
                    pm.member_id,
                    'Owner',
                    NOW()
                FROM project_memberships pm
                INNER JOIN projects p ON pm.project_id = p.id
                WHERE pm.member_type = 'User'
                  AND pm.role = 'OrgAdmin'
                ON CONFLICT DO NOTHING;
            ");

            // Step 6: Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_organizations_slug",
                table: "organizations",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_members_organization_id_user_id",
                table: "organization_members",
                columns: new[] { "organization_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_members_user_id",
                table: "organization_members",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_members");

            migrationBuilder.DropIndex(
                name: "IX_organizations_slug",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "organizations");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "organizations",
                newName: "CreatedByUserId");
        }
    }
}
