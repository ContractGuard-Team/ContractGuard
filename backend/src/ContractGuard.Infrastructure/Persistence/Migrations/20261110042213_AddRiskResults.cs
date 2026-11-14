using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractGuard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "edited_metadata",
                table: "contract_version",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "extracted_metadata",
                table: "contract_version",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "metadata_edited_at",
                table: "contract_version",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "metadata_edited_by_user_account_id",
                table: "contract_version",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "checks",
                table: "analysis_run",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "injection_warnings",
                table: "analysis_run",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "overall_recommendation",
                table: "analysis_run",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "score",
                table: "analysis_run",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "finding",
                columns: table => new
                {
                    finding_id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    finding_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    primary_domain = table.Column<string>(type: "text", nullable: false),
                    secondary_domains = table.Column<string>(type: "jsonb", nullable: false),
                    category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    severity = table.Column<string>(type: "text", nullable: false),
                    clause_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    evidence = table.Column<string>(type: "jsonb", nullable: false),
                    missing_description = table.Column<string>(type: "text", nullable: true),
                    why = table.Column<string>(type: "text", nullable: false),
                    impact = table.Column<string>(type: "text", nullable: false),
                    recommendation = table.Column<string>(type: "text", nullable: false),
                    references = table.Column<string>(type: "jsonb", nullable: false),
                    check_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    handling_status = table.Column<string>(type: "text", nullable: false),
                    handling_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    handled_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    handled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_finding", x => x.finding_id);
                    table.ForeignKey(
                        name: "fk_finding_analysis_run_analysis_run_id",
                        column: x => x.analysis_run_id,
                        principalTable: "analysis_run",
                        principalColumn: "analysis_run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_finding_analysis_run_id_finding_key",
                table: "finding",
                columns: new[] { "analysis_run_id", "finding_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "finding");

            migrationBuilder.DropColumn(
                name: "edited_metadata",
                table: "contract_version");

            migrationBuilder.DropColumn(
                name: "extracted_metadata",
                table: "contract_version");

            migrationBuilder.DropColumn(
                name: "metadata_edited_at",
                table: "contract_version");

            migrationBuilder.DropColumn(
                name: "metadata_edited_by_user_account_id",
                table: "contract_version");

            migrationBuilder.DropColumn(
                name: "checks",
                table: "analysis_run");

            migrationBuilder.DropColumn(
                name: "injection_warnings",
                table: "analysis_run");

            migrationBuilder.DropColumn(
                name: "overall_recommendation",
                table: "analysis_run");

            migrationBuilder.DropColumn(
                name: "score",
                table: "analysis_run");
        }
    }
}
