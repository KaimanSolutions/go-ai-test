using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class AddApplicationChecklistItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationChecklistItems",
                columns: table => new
                {
                    Id                  = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId       = table.Column<int>(nullable: false),
                    ChecklistItemId     = table.Column<int>(nullable: false),
                    Status              = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "Pending"),
                    TextResponse        = table.Column<string>(nullable: true),
                    DocumentName        = table.Column<string>(nullable: true),
                    DocumentPath        = table.Column<string>(nullable: true),
                    DocumentContentType = table.Column<string>(nullable: true),
                    GeneratedAt         = table.Column<DateTime>(nullable: false),
                    CompletedAt         = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationChecklistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationChecklistItems_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationChecklistItems_ChecklistItems_ChecklistItemId",
                        column: x => x.ChecklistItemId,
                        principalTable: "ChecklistItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationChecklistItems_ApplicationId",
                table: "ApplicationChecklistItems",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationChecklistItems_ChecklistItemId",
                table: "ApplicationChecklistItems",
                column: "ChecklistItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ApplicationChecklistItems");
        }
    }
}
