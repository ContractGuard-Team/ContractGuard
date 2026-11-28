using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractGuard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "app",
                table: "contract_version",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_account_id",
                schema: "app",
                table: "contract_version",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at",
                schema: "app",
                table: "contract_version");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_account_id",
                schema: "app",
                table: "contract_version");
        }
    }
}
