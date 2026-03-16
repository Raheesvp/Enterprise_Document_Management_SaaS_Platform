using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_instances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    current_stage_order = table.Column<int>(type: "integer", nullable: false),
                    initiated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_instances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_stages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage_order = table.Column<int>(type: "integer", nullable: false),
                    stage_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_to_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    comments = table.Column<string>(type: "text", nullable: true),
                    sla_deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_stages", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_stages_workflow_instances_workflow_instance_id",
                        column: x => x.workflow_instance_id,
                        principalTable: "workflow_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instances_document_id",
                table: "workflow_instances",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instances_tenant_id",
                table: "workflow_instances",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_stages_workflow_instance_id",
                table: "workflow_stages",
                column: "workflow_instance_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_definitions");

            migrationBuilder.DropTable(
                name: "workflow_stages");

            migrationBuilder.DropTable(
                name: "workflow_instances");
        }
    }
}
