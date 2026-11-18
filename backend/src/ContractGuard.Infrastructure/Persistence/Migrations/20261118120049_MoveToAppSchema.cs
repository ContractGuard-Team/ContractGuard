using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractGuard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveToAppSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.RenameTable(
                name: "user_account_token",
                newName: "user_account_token",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "user_account_login",
                newName: "user_account_login",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "user_account_claim",
                newName: "user_account_claim",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "user_account",
                newName: "user_account",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "organization",
                newName: "organization",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "membership",
                newName: "membership",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "invitation",
                newName: "invitation",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "finding",
                newName: "finding",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "contract_version",
                newName: "contract_version",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "contract_share",
                newName: "contract_share",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "contract_context",
                newName: "contract_context",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "contract",
                newName: "contract",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "clause",
                newName: "clause",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "chat_message",
                newName: "chat_message",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "audit_event",
                newName: "audit_event",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "analysis_run",
                newName: "analysis_run",
                newSchema: "app");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "user_account_token",
                schema: "app",
                newName: "user_account_token");

            migrationBuilder.RenameTable(
                name: "user_account_login",
                schema: "app",
                newName: "user_account_login");

            migrationBuilder.RenameTable(
                name: "user_account_claim",
                schema: "app",
                newName: "user_account_claim");

            migrationBuilder.RenameTable(
                name: "user_account",
                schema: "app",
                newName: "user_account");

            migrationBuilder.RenameTable(
                name: "organization",
                schema: "app",
                newName: "organization");

            migrationBuilder.RenameTable(
                name: "membership",
                schema: "app",
                newName: "membership");

            migrationBuilder.RenameTable(
                name: "invitation",
                schema: "app",
                newName: "invitation");

            migrationBuilder.RenameTable(
                name: "finding",
                schema: "app",
                newName: "finding");

            migrationBuilder.RenameTable(
                name: "contract_version",
                schema: "app",
                newName: "contract_version");

            migrationBuilder.RenameTable(
                name: "contract_share",
                schema: "app",
                newName: "contract_share");

            migrationBuilder.RenameTable(
                name: "contract_context",
                schema: "app",
                newName: "contract_context");

            migrationBuilder.RenameTable(
                name: "contract",
                schema: "app",
                newName: "contract");

            migrationBuilder.RenameTable(
                name: "clause",
                schema: "app",
                newName: "clause");

            migrationBuilder.RenameTable(
                name: "chat_message",
                schema: "app",
                newName: "chat_message");

            migrationBuilder.RenameTable(
                name: "audit_event",
                schema: "app",
                newName: "audit_event");

            migrationBuilder.RenameTable(
                name: "analysis_run",
                schema: "app",
                newName: "analysis_run");
        }
    }
}
