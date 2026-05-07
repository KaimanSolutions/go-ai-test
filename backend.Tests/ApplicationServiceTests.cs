using FormBuilder.Backend.Data;
using Xunit;

namespace FormBuilder.Backend.Tests;

public class ApplicationServiceTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_UnknownFormSchemaId_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new ApplicationService(db);

        var (success, error, _) = svc.Create("admin@test.com", UserRoles.Admin,
            new CreateApplicationRequest { FormSchemaId = "does-not-exist" });

        Assert.False(success);
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_AsAdmin_CreatesApplicationWithoutUserLink()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "schema-1");
        var svc = new ApplicationService(db);

        var (success, _, app) = svc.Create("admin@app.com", UserRoles.Admin,
            new CreateApplicationRequest { FormSchemaId = "schema-1" });

        Assert.True(success);
        Assert.NotNull(app);
        // Admin applications have no client or broker
        var saved = db.Applications.First();
        Assert.Null(saved.ClientId);
        Assert.Null(saved.BrokerId);
    }

    [Fact]
    public void Create_AsClient_SetsClientId()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "schema-1");
        var client = TestDb.SeedUser(db, UserRoles.Client, "client@test.com");
        var svc    = new ApplicationService(db);

        var (success, _, _) = svc.Create(client.Email, UserRoles.Client,
            new CreateApplicationRequest { FormSchemaId = "schema-1" });

        Assert.True(success);
        var saved = db.Applications.First();
        Assert.Equal(client.Id, saved.ClientId);
        Assert.Null(saved.BrokerId);
    }

    [Fact]
    public void Create_AsBroker_SetsBrokerIdAndCompanyId()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "schema-1");
        var company = TestDb.SeedCompany(db);
        var broker  = TestDb.SeedUser(db, UserRoles.Broker, "broker@test.com", company.Id);
        var svc     = new ApplicationService(db);

        var (success, _, _) = svc.Create(broker.Email, UserRoles.Broker,
            new CreateApplicationRequest { FormSchemaId = "schema-1" });

        Assert.True(success);
        var saved = db.Applications.First();
        Assert.Equal(broker.Id,  saved.BrokerId);
        Assert.Equal(company.Id, saved.CompanyId);
    }

    [Fact]
    public void Create_FormLinkedToWorkflow_AutoAssignsInitialStage()
    {
        using var db = TestDb.Create();
        var (workflow, initial) = TestDb.SeedWorkflow(db);
        var schema = TestDb.SeedSchema(db, "schema-wf");
        schema.WorkflowId = workflow.Id;
        db.SaveChanges();

        var svc = new ApplicationService(db);
        var (success, _, _) = svc.Create("admin@test.com", UserRoles.Admin,
            new CreateApplicationRequest { FormSchemaId = "schema-wf" });

        Assert.True(success);
        var saved = db.Applications.First();
        Assert.Equal(workflow.Id, saved.WorkflowId);
        Assert.Equal(initial.Id,  saved.CurrentStageId);
    }

    [Fact]
    public void Create_UnknownUserEmail_ReturnsError()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "schema-1");
        var svc = new ApplicationService(db);

        var (success, error, _) = svc.Create("nobody@nowhere.com", UserRoles.Client,
            new CreateApplicationRequest { FormSchemaId = "schema-1" });

        Assert.False(success);
        Assert.NotEmpty(error);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_NonExistentApplication_ReturnsError()
    {
        using var db  = TestDb.Create();
        var svc       = new ApplicationService(db);

        var (success, error, _) = svc.Update(999, "admin@test.com", UserRoles.Admin,
            new UpdateApplicationRequest { FormData = "{}" });

        Assert.False(success);
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Update_FormData_IsPersisted()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "s1");
        var svc = new ApplicationService(db);
        svc.Create("admin@test.com", UserRoles.Admin, new CreateApplicationRequest { FormSchemaId = "s1" });
        var id = db.Applications.First().Id;

        var (success, _, _) = svc.Update(id, "admin@test.com", UserRoles.Admin,
            new UpdateApplicationRequest { FormData = @"{""name"":""Alice""}" });

        Assert.True(success);
        Assert.Equal(@"{""name"":""Alice""}", db.Applications.First().FormData);
    }

    [Fact]
    public void Update_SubmitFlag_SetsSubmittedAt()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "s1");
        var svc = new ApplicationService(db);
        svc.Create("admin@test.com", UserRoles.Admin, new CreateApplicationRequest { FormSchemaId = "s1" });
        var id = db.Applications.First().Id;

        var before = DateTime.UtcNow;
        svc.Update(id, "admin@test.com", UserRoles.Admin, new UpdateApplicationRequest { Submit = true });
        var after = DateTime.UtcNow;

        var saved = db.Applications.First();
        Assert.NotNull(saved.SubmittedAt);
        Assert.InRange(saved.SubmittedAt!.Value, before, after);
    }

    [Fact]
    public void Update_SubmitFlag_IdempotentOnResubmit()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "s1");
        var svc = new ApplicationService(db);
        svc.Create("admin@test.com", UserRoles.Admin, new CreateApplicationRequest { FormSchemaId = "s1" });
        var id = db.Applications.First().Id;

        svc.Update(id, "admin@test.com", UserRoles.Admin, new UpdateApplicationRequest { Submit = true });
        var firstSubmit = db.Applications.First().SubmittedAt;

        svc.Update(id, "admin@test.com", UserRoles.Admin, new UpdateApplicationRequest { Submit = true });
        var secondSubmit = db.Applications.First().SubmittedAt;

        Assert.Equal(firstSubmit, secondSubmit);
    }

    [Fact]
    public void Update_ClientCannotEditAnotherClientApplication()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "s1");
        var ownerClient = TestDb.SeedUser(db, UserRoles.Client, "owner@test.com");
        var otherClient = TestDb.SeedUser(db, UserRoles.Client, "other@test.com");
        var svc = new ApplicationService(db);
        svc.Create(ownerClient.Email, UserRoles.Client, new CreateApplicationRequest { FormSchemaId = "s1" });
        var id = db.Applications.First().Id;

        var (success, error, _) = svc.Update(id, otherClient.Email, UserRoles.Client,
            new UpdateApplicationRequest { FormData = "{}" });

        Assert.False(success);
        Assert.Contains("permission", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Update_IncreasesVersionOnEachSave()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "s1");
        var svc = new ApplicationService(db);
        svc.Create("admin@test.com", UserRoles.Admin, new CreateApplicationRequest { FormSchemaId = "s1" });
        var id = db.Applications.First().Id;

        svc.Update(id, "admin@test.com", UserRoles.Admin, new UpdateApplicationRequest { FormData = "a" });
        svc.Update(id, "admin@test.com", UserRoles.Admin, new UpdateApplicationRequest { FormData = "b" });

        Assert.Equal(3, db.Applications.First().Version); // starts at 1, +1 per update
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_ExistingApplication_Succeeds()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "s1");
        var svc = new ApplicationService(db);
        svc.Create("admin@test.com", UserRoles.Admin, new CreateApplicationRequest { FormSchemaId = "s1" });
        var id = db.Applications.First().Id;

        var (success, _) = svc.Delete(id);

        Assert.True(success);
        Assert.Empty(db.Applications.ToList());
    }

    [Fact]
    public void Delete_NonExistentApplication_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new ApplicationService(db);

        var (success, error) = svc.Delete(9999);

        Assert.False(success);
        Assert.NotEmpty(error);
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public void GetAll_AsAdmin_ReturnsAllApplications()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "s1");
        var c1  = TestDb.SeedUser(db, UserRoles.Client, "c1@test.com");
        var c2  = TestDb.SeedUser(db, UserRoles.Client, "c2@test.com");
        var svc = new ApplicationService(db);
        svc.Create(c1.Email, UserRoles.Client, new CreateApplicationRequest { FormSchemaId = "s1" });
        svc.Create(c2.Email, UserRoles.Client, new CreateApplicationRequest { FormSchemaId = "s1" });

        var results = svc.GetAll("admin@test.com", UserRoles.Admin).ToList();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void GetAll_AsClient_ReturnsOnlyOwnApplications()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "s1");
        var c1  = TestDb.SeedUser(db, UserRoles.Client, "c1@test.com");
        var c2  = TestDb.SeedUser(db, UserRoles.Client, "c2@test.com");
        var svc = new ApplicationService(db);
        svc.Create(c1.Email, UserRoles.Client, new CreateApplicationRequest { FormSchemaId = "s1" });
        svc.Create(c2.Email, UserRoles.Client, new CreateApplicationRequest { FormSchemaId = "s1" });

        var results = svc.GetAll(c1.Email, UserRoles.Client).ToList();

        Assert.Single(results);
    }

    [Fact]
    public void GetAll_UnknownNonAdminUser_ReturnsEmpty()
    {
        using var db = TestDb.Create();
        var svc = new ApplicationService(db);

        var results = svc.GetAll("ghost@test.com", UserRoles.Client).ToList();

        Assert.Empty(results);
    }
}
