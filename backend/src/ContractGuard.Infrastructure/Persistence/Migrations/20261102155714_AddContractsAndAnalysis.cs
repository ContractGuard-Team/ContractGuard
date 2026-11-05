using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractGuard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractsAndAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contract",
                columns: table => new
                {
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    approval_status = table.Column<string>(type: "text", nullable: false),
                    status_before_analysis = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract", x => x.contract_id);
                    table.ForeignKey(
                        name: "fk_contract_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "organization_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_user_account_owner_user_account_id",
                        column: x => x.owner_user_account_id,
                        principalTable: "user_account",
                        principalColumn: "user_account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contract_version",
                columns: table => new
                {
                    contract_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    source_file_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_type = table.Column<string>(type: "text", nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    page_count = table.Column<int>(type: "integer", nullable: true),
                    uploaded_by_user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_version", x => x.contract_version_id);
                    table.ForeignKey(
                        name: "fk_contract_version_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contract_version_user_account_uploaded_by_user_account_id",
                        column: x => x.uploaded_by_user_account_id,
                        principalTable: "user_account",
                        principalColumn: "user_account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "analysis_run",
                columns: table => new
                {
                    analysis_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_status = table.Column<string>(type: "text", nullable: false),
                    current_step = table.Column<string>(type: "text", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    prompt_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    rule_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_event_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analysis_run", x => x.analysis_run_id);
                    table.ForeignKey(
                        name: "fk_analysis_run_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_analysis_run_contract_version_contract_version_id",
                        column: x => x.contract_version_id,
                        principalTable: "contract_version",
                        principalColumn: "contract_version_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_context",
                columns: table => new
                {
                    contract_context_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    own_party = table.Column<string>(type: "text", nullable: false),
                    counterparty_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    contract_type = table.Column<string>(type: "text", nullable: false),
                    counterparty_offering = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    has_personal_data = table.Column<bool>(type: "boolean", nullable: false),
                    service_criticality = table.Column<string>(type: "text", nullable: true),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_context", x => x.contract_context_id);
                    table.ForeignKey(
                        name: "fk_contract_context_contract_version_contract_version_id",
                        column: x => x.contract_version_id,
                        principalTable: "contract_version",
                        principalColumn: "contract_version_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clause",
                columns: table => new
                {
                    clause_id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clause_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    clause_order = table.Column<int>(type: "integer", nullable: false),
                    clause_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    heading = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    clause_text = table.Column<string>(type: "text", nullable: false),
                    clause_type = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    location = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clause", x => x.clause_id);
                    table.ForeignKey(
                        name: "fk_clause_analysis_run_analysis_run_id",
                        column: x => x.analysis_run_id,
                        principalTable: "analysis_run",
                        principalColumn: "analysis_run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_analysis_run_contract_id",
                table: "analysis_run",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_analysis_run_contract_version_id",
                table: "analysis_run",
                column: "contract_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_analysis_run_run_status_last_event_at",
                table: "analysis_run",
                columns: new[] { "run_status", "last_event_at" });

            migrationBuilder.CreateIndex(
                name: "ix_clause_analysis_run_id_clause_key",
                table: "clause",
                columns: new[] { "analysis_run_id", "clause_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_organization_id",
                table: "contract",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_owner_user_account_id",
                table: "contract",
                column: "owner_user_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_context_contract_version_id",
                table: "contract_context",
                column: "contract_version_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_version_contract_id_version_number",
                table: "contract_version",
                columns: new[] { "contract_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_version_uploaded_by_user_account_id",
                table: "contract_version",
                column: "uploaded_by_user_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clause");

            migrationBuilder.DropTable(
                name: "contract_context");

            migrationBuilder.DropTable(
                name: "analysis_run");

            migrationBuilder.DropTable(
                name: "contract_version");

            migrationBuilder.DropTable(
                name: "contract");
        }
    }
}
