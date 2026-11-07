using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractGuard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueSourceFileKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_contract_version_source_file_key",
                table: "contract_version",
                column: "source_file_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_contract_version_source_file_key",
                table: "contract_version");
        }
    }
}
