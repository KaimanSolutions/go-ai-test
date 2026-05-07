using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentStageToApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentStageId",
                table: "Applications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CurrentStageId",
                table: "Applications",
                column: "CurrentStageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_WorkflowStages_CurrentStageId",
                table: "Applications",
                column: "CurrentStageId",
                principalTable: "WorkflowStages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_WorkflowStages_CurrentStageId",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_CurrentStageId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "CurrentStageId",
                table: "Applications");
        }
    }
}
