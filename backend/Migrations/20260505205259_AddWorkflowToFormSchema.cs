using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowToFormSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkflowId",
                table: "FormSchemas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormSchemas_WorkflowId",
                table: "FormSchemas",
                column: "WorkflowId");

            migrationBuilder.AddForeignKey(
                name: "FK_FormSchemas_Workflows_WorkflowId",
                table: "FormSchemas",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormSchemas_Workflows_WorkflowId",
                table: "FormSchemas");

            migrationBuilder.DropIndex(
                name: "IX_FormSchemas_WorkflowId",
                table: "FormSchemas");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "FormSchemas");
        }
    }
}
