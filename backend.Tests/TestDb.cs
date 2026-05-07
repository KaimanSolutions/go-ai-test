using FormBuilder.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Backend.Tests;

internal static class TestDb
{
    /// <summary>
    /// Creates a fresh in-memory DbContext isolated to this test run.
    /// Each call with no argument gets its own independent database.
    /// </summary>
    public static FormBuilderDbContext Create(string? name = null)
    {
        var options = new DbContextOptionsBuilder<FormBuilderDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;

        return new FormBuilderDbContext(options);
    }

    // ── Seed helpers ──────────────────────────────────────────────────────────

    public static FormSchema SeedSchema(FormBuilderDbContext db, string id = "test-schema")
    {
        var schema = new FormSchema { Id = id, Title = "Test Form", Version = 1 };
        db.FormSchemas.Add(schema);
        db.SaveChanges();
        return schema;
    }

    public static (Workflow workflow, WorkflowStage initial) SeedWorkflow(FormBuilderDbContext db, string name = "Test Workflow")
    {
        var workflow = new Workflow { Name = name, CreatedAt = DateTime.UtcNow };
        var stage    = new WorkflowStage { Name = "Initial", Order = 0, IsInitial = true, IsFinal = false };
        workflow.Stages.Add(stage);
        db.Workflows.Add(workflow);
        db.SaveChanges();
        return (workflow, stage);
    }

    public static Company SeedCompany(FormBuilderDbContext db, string name = "Test Co", string fca = "000001")
    {
        var company = new Company { Name = name, FCANumber = fca, Type = CompanyType.Broker };
        db.Companies.Add(company);
        db.SaveChanges();
        return company;
    }

    public static UserAccount SeedUser(FormBuilderDbContext db, string role, string email = "user@test.com", int? companyId = null)
    {
        var user = new UserAccount
        {
            Username  = email,
            Email     = email,
            PasswordHash = "placeholder",
            Role      = role,
            FirstName = "Test",
            LastName  = "User",
            Phone     = string.Empty,
            CompanyId = companyId,
            CreatedAt = DateTime.UtcNow
        };
        db.UserAccounts.Add(user);
        db.SaveChanges();
        return user;
    }
}
