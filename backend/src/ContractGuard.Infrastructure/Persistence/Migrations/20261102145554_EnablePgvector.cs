using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractGuard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnablePgvector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
