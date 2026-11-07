using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractGuard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOneRunningAnalysisPerContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_analysis_run_one_running_per_contract",
                table: "analysis_run",
                column: "contract_id",
                unique: true,
                filter: "run_status = 'running'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_analysis_run_one_running_per_contract",
                table: "analysis_run");
        }
    }
}
