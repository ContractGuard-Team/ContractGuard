using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractGuard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractTrash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "app",
                table: "contract",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_account_id",
                schema: "app",
                table: "contract",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_deleted_at",
                schema: "app",
                table: "contract",
                column: "deleted_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_contract_deleted_at",
                schema: "app",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                schema: "app",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_account_id",
                schema: "app",
                table: "contract");
        }
    }
}
