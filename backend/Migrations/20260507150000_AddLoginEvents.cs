using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class AddLoginEvents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginEvents",
                columns: table => new
                {
                    Id           = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    UserId       = table.Column<int>(nullable: true),
                    UserType     = table.Column<string>(maxLength: 30,  nullable: false),
                    Email        = table.Column<string>(maxLength: 256, nullable: true),
                    TimestampUtc = table.Column<DateTimeOffset>(nullable: false),
                    DeviceType   = table.Column<string>(maxLength: 20,  nullable: false),
                    AuthMethod   = table.Column<string>(maxLength: 20,  nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoginEvents_TimestampUtc",
                table: "LoginEvents",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LoginEvents_UserType",
                table: "LoginEvents",
                column: "UserType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LoginEvents");
        }
    }
}
