using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class AddChecklistStatusAndComments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Change the default status from 'Pending' to 'Outstanding'
            // and update any existing 'Pending' rows to 'Outstanding'
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ApplicationChecklistItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Outstanding",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Pending");

            migrationBuilder.Sql(
                "UPDATE ApplicationChecklistItems SET Status = 'Outstanding' WHERE Status = 'Pending'");

            migrationBuilder.CreateTable(
                name: "ApplicationChecklistComments",
                columns: table => new
                {
                    Id                        = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationChecklistItemId = table.Column<int>(nullable: false),
                    Comment                   = table.Column<string>(nullable: false),
                    AuthorName                = table.Column<string>(nullable: false),
                    CreatedAt                 = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationChecklistComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationChecklistComments_ApplicationChecklistItems_ApplicationChecklistItemId",
                        column: x => x.ApplicationChecklistItemId,
                        principalTable: "ApplicationChecklistItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationChecklistComments_ApplicationChecklistItemId",
                table: "ApplicationChecklistComments",
                column: "ApplicationChecklistItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ApplicationChecklistComments");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ApplicationChecklistItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Outstanding");
        }
    }
}
