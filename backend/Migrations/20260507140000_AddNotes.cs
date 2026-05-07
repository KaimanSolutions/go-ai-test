using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class AddNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationNotes",
                columns: table => new
                {
                    Id              = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId   = table.Column<int>(nullable: false),
                    Content         = table.Column<string>(nullable: false),
                    AuthorName      = table.Column<string>(nullable: false),
                    AuthorRole      = table.Column<string>(maxLength: 20, nullable: false),
                    Stage           = table.Column<string>(nullable: true),
                    Category        = table.Column<string>(nullable: true),
                    IsClientVisible = table.Column<bool>(nullable: false, defaultValue: false),
                    IsBrokerVisible = table.Column<bool>(nullable: false, defaultValue: false),
                    CreatedAt       = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationNotes_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationNotes_ApplicationId",
                table: "ApplicationNotes",
                column: "ApplicationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ApplicationNotes");
        }
    }
}
