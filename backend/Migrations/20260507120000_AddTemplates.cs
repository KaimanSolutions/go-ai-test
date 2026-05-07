using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class AddTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id           = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Name         = table.Column<string>(nullable: false),
                    Description  = table.Column<string>(nullable: false),
                    TemplateType = table.Column<string>(maxLength: 20, nullable: false),
                    Subject      = table.Column<string>(nullable: false),
                    Content      = table.Column<string>(nullable: false),
                    IsActive     = table.Column<bool>(nullable: false),
                    CreatedAt    = table.Column<DateTime>(nullable: false),
                    UpdatedAt    = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Templates");
        }
    }
}
