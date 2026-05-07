using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RuleOutcomes",
                columns: table => new
                {
                    Id            = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId  = table.Column<int>(type: "int", nullable: false),
                    BusinessRuleId = table.Column<int>(type: "int", nullable: false),
                    Passed         = table.Column<bool>(type: "bit", nullable: false),
                    FailReasons    = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    StageId        = table.Column<int>(type: "int", nullable: true),
                    StageName      = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecordedAt     = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleOutcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleOutcomes_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RuleOutcomes_BusinessRules_BusinessRuleId",
                        column: x => x.BusinessRuleId,
                        principalTable: "BusinessRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuleOutcomes_ApplicationId",
                table: "RuleOutcomes",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleOutcomes_BusinessRuleId",
                table: "RuleOutcomes",
                column: "BusinessRuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RuleOutcomes");
        }
    }
}
