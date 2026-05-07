using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class AddLoginEventColumnstoreIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A clustered columnstore index replaces the default clustered PK index,
            // turning it into a non-clustered unique constraint. This gives batch-mode
            // vectorised execution and high compression for the analytics queries on
            // LoginEvents, which is an append-only, read-heavy table.
            // The default clustered PK must become non-clustered before a
            // clustered columnstore index can be created on the same table.
            migrationBuilder.Sql(
                "ALTER TABLE [dbo].[LoginEvents] DROP CONSTRAINT [PK_LoginEvents];");
            migrationBuilder.Sql(
                "ALTER TABLE [dbo].[LoginEvents] ADD CONSTRAINT [PK_LoginEvents] PRIMARY KEY NONCLUSTERED ([Id]);");
            migrationBuilder.Sql(
                "CREATE CLUSTERED COLUMNSTORE INDEX [CCI_LoginEvents] ON [dbo].[LoginEvents];");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX [CCI_LoginEvents] ON [dbo].[LoginEvents];");
            migrationBuilder.Sql(
                "ALTER TABLE [dbo].[LoginEvents] DROP CONSTRAINT [PK_LoginEvents];");
            migrationBuilder.Sql(
                "ALTER TABLE [dbo].[LoginEvents] ADD CONSTRAINT [PK_LoginEvents] PRIMARY KEY CLUSTERED ([Id]);");
        }
    }
}
