using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Workflows", x => x.Id));

            migrationBuilder.CreateTable(
                name: "WorkflowStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowId  = table.Column<int>(type: "int",          nullable: false),
                    Name        = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order       = table.Column<int>(type: "int",  nullable: false),
                    IsInitial   = table.Column<bool>(type: "bit", nullable: false),
                    IsFinal     = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStages_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StageId     = table.Column<int>(type: "int",          nullable: false),
                    Title       = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Required    = table.Column<bool>(type: "bit", nullable: false),
                    Order       = table.Column<int>(type: "int",  nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTasks_WorkflowStages_StageId",
                        column: x => x.StageId,
                        principalTable: "WorkflowStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTransitions",
                columns: table => new
                {
                    Id          = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowId  = table.Column<int>(type: "int", nullable: false),
                    FromStageId = table.Column<int>(type: "int", nullable: false),
                    ToStageId   = table.Column<int>(type: "int", nullable: false),
                    Label       = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Condition   = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_WorkflowStages_FromStageId",
                        column: x => x.FromStageId,
                        principalTable: "WorkflowStages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_WorkflowStages_ToStageId",
                        column: x => x.ToStageId,
                        principalTable: "WorkflowStages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(name: "IX_WorkflowStages_WorkflowId",      table: "WorkflowStages",      column: "WorkflowId");
            migrationBuilder.CreateIndex(name: "IX_WorkflowTasks_StageId",          table: "WorkflowTasks",        column: "StageId");
            migrationBuilder.CreateIndex(name: "IX_WorkflowTransitions_FromStageId", table: "WorkflowTransitions", column: "FromStageId");
            migrationBuilder.CreateIndex(name: "IX_WorkflowTransitions_ToStageId",   table: "WorkflowTransitions", column: "ToStageId");
            migrationBuilder.CreateIndex(name: "IX_WorkflowTransitions_WorkflowId",  table: "WorkflowTransitions", column: "WorkflowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WorkflowTransitions");
            migrationBuilder.DropTable(name: "WorkflowTasks");
            migrationBuilder.DropTable(name: "WorkflowStages");
            migrationBuilder.DropTable(name: "Workflows");
        }
    }
}
