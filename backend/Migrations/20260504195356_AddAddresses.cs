using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    OrganisationName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    DepartmentName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    SubBuildingName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BuildingName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BuildingNumber = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    DependentThoroughfareName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    DependentThoroughfareDescriptor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ThoroughfareName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ThoroughfareDescriptor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DoubleDependentLocality = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    DependentLocality = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    PostTown = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Postcode = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    POBox = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Addresses_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_CompanyId",
                table: "Addresses",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Addresses");
        }
    }
}
