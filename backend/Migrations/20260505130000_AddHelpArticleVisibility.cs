using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddHelpArticleVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowInAdminPortal",
                table: "HelpArticles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInBrokerPortal",
                table: "HelpArticles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInCustomerPortal",
                table: "HelpArticles",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ShowInAdminPortal",    table: "HelpArticles");
            migrationBuilder.DropColumn(name: "ShowInBrokerPortal",   table: "HelpArticles");
            migrationBuilder.DropColumn(name: "ShowInCustomerPortal", table: "HelpArticles");
        }
    }
}
