using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleReference     = table.Column<string>(type: "nvarchar(50)",  maxLength: 50,  nullable: false),
                    Name              = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description       = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrokerDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive          = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FormSchemaId      = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt         = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessRules_FormSchemas_FormSchemaId",
                        column: x => x.FormSchemaId,
                        principalTable: "FormSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RuleConditions",
                columns: table => new
                {
                    Id              = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessRuleId  = table.Column<int>(type: "int", nullable: false),
                    LeftExpression  = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Operator        = table.Column<string>(type: "nvarchar(10)",  maxLength: 10,  nullable: false),
                    RightExpression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailMessage     = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order           = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleConditions_BusinessRules_BusinessRuleId",
                        column: x => x.BusinessRuleId,
                        principalTable: "BusinessRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRules_FormSchemaId",
                table: "BusinessRules",
                column: "FormSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRules_RuleReference",
                table: "BusinessRules",
                column: "RuleReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleConditions_BusinessRuleId",
                table: "RuleConditions",
                column: "BusinessRuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RuleConditions");
            migrationBuilder.DropTable(name: "BusinessRules");
        }
    }
}
