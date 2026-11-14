using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractGuard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSharingOrganizationsAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password_reset_emails_sent_at",
                table: "user_account",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "verification_emails_sent_at",
                table: "user_account",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "overall_risk_level",
                table: "analysis_run",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "overall_score",
                table: "analysis_run",
                type: "integer",
                nullable: true);

            // Runs completed before these columns existed keep their score in the jsonb column only.
            migrationBuilder.Sql("UPDATE analysis_run SET overall_score = (score->>'overall_score')::int, overall_risk_level = score->>'risk_level' WHERE score IS NOT NULL;");

            migrationBuilder.CreateTable(
                name: "audit_event",
                columns: table => new
                {
                    audit_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    detail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_event", x => x.audit_event_id);
                    table.ForeignKey(
                        name: "fk_audit_event_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_share",
                columns: table => new
                {
                    contract_share_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shared_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shared_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_share", x => x.contract_share_id);
                    table.ForeignKey(
                        name: "fk_contract_share_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contract_share_user_account_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_account",
                        principalColumn: "user_account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invitation",
                columns: table => new
                {
                    invitation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    invited_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitation", x => x.invitation_id);
                    table.ForeignKey(
                        name: "fk_invitation_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "organization_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_event_contract_id_occurred_at",
                table: "audit_event",
                columns: new[] { "contract_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_share_contract_id_user_account_id",
                table: "contract_share",
                columns: new[] { "contract_id", "user_account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_share_user_account_id",
                table: "contract_share",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitation_organization_id_email",
                table: "invitation",
                columns: new[] { "organization_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invitation_token_hash",
                table: "invitation",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_event");

            migrationBuilder.DropTable(
                name: "contract_share");

            migrationBuilder.DropTable(
                name: "invitation");

            migrationBuilder.DropColumn(
                name: "password_reset_emails_sent_at",
                table: "user_account");

            migrationBuilder.DropColumn(
                name: "verification_emails_sent_at",
                table: "user_account");

            migrationBuilder.DropColumn(
                name: "overall_risk_level",
                table: "analysis_run");

            migrationBuilder.DropColumn(
                name: "overall_score",
                table: "analysis_run");
        }
    }
}
