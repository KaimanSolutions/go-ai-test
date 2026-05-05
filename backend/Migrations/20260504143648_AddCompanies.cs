using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "UserAccounts");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "UserAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FCANumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_CompanyId",
                table: "UserAccounts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_FCANumber",
                table: "Companies",
                column: "FCANumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccounts_Companies_CompanyId",
                table: "UserAccounts",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAccounts_Companies_CompanyId",
                table: "UserAccounts");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_UserAccounts_CompanyId",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "UserAccounts");

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "UserAccounts",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
