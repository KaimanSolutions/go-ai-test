using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class AddChecklist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChecklistItems",
                columns: table => new
                {
                    Id              = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Name            = table.Column<string>(nullable: false),
                    Description     = table.Column<string>(nullable: false),
                    ItemType        = table.Column<string>(maxLength: 20, nullable: false),
                    FormSchemaId    = table.Column<string>(maxLength: 450, nullable: true),
                    IsClientVisible = table.Column<bool>(nullable: false),
                    IsBrokerVisible = table.Column<bool>(nullable: false),
                    IsActive        = table.Column<bool>(nullable: false),
                    CreatedAt       = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistItems", x => x.Id);
                    table.ForeignKey(name: "FK_ChecklistItems_FormSchemas_FormSchemaId",
                        column: x => x.FormSchemaId, principalTable: "FormSchemas",
                        principalColumn: "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChecklistConditions",
                columns: table => new
                {
                    Id              = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ChecklistItemId = table.Column<int>(nullable: false),
                    FieldName       = table.Column<string>(nullable: false),
                    Operator        = table.Column<string>(nullable: false),
                    Value           = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistConditions", x => x.Id);
                    table.ForeignKey(name: "FK_ChecklistConditions_ChecklistItems_ChecklistItemId",
                        column: x => x.ChecklistItemId, principalTable: "ChecklistItems",
                        principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_ChecklistItems_FormSchemaId",
                table: "ChecklistItems", column: "FormSchemaId");
            migrationBuilder.CreateIndex(name: "IX_ChecklistConditions_ChecklistItemId",
                table: "ChecklistConditions", column: "ChecklistItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ChecklistConditions");
            migrationBuilder.DropTable(name: "ChecklistItems");
        }
    }
}
