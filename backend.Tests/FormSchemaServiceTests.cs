using Xunit;

namespace FormBuilder.Backend.Tests;

public class FormSchemaServiceTests
{
    // ── SaveSchema ────────────────────────────────────────────────────────────

    [Fact]
    public void SaveSchema_NewSchema_IsCreatedWithVersionOne()
    {
        using var db = TestDb.Create();
        var svc = new FormSchemaService(db);

        svc.SaveSchema(new FormSchema
        {
            Id    = "new-form",
            Title = "New Form",
            Steps = [new FormStep { Title = "Step 1", Fields = [] }]
        });

        var saved = db.FormSchemas.Find("new-form");
        Assert.NotNull(saved);
        Assert.Equal(1, saved!.Version);
        Assert.Single(db.FormSteps.ToList());
    }

    [Fact]
    public void SaveSchema_ExistingSchema_IncrementsVersion()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "form-1"); // Version = 1
        var svc = new FormSchemaService(db);

        svc.SaveSchema(new FormSchema
        {
            Id    = "form-1",
            Title = "Updated Title",
            Steps = []
        });

        var updated = db.FormSchemas.Find("form-1");
        Assert.Equal(2, updated!.Version);
        Assert.Equal("Updated Title", updated.Title);
    }

    [Fact]
    public void SaveSchema_ExistingSchema_ReplacesSteps()
    {
        using var db = TestDb.Create();
        var schema = new FormSchema
        {
            Id    = "form-1",
            Title = "Form",
            Steps = [new FormStep { Title = "Old Step", Fields = [] }]
        };
        db.FormSchemas.Add(schema);
        db.SaveChanges();

        var svc = new FormSchemaService(db);
        svc.SaveSchema(new FormSchema
        {
            Id    = "form-1",
            Title = "Form",
            Steps =
            [
                new FormStep { Title = "New Step A", Fields = [] },
                new FormStep { Title = "New Step B", Fields = [] }
            ]
        });

        var steps = db.FormSteps.Where(s => s.FormSchemaId == "form-1").ToList();
        Assert.Equal(2, steps.Count);
        Assert.DoesNotContain(steps, s => s.Title == "Old Step");
    }

    [Fact]
    public void SaveSchema_ExistingSchema_PreservesWorkflowLink()
    {
        using var db = TestDb.Create();
        var (wf, _) = TestDb.SeedWorkflow(db);
        var schema  = TestDb.SeedSchema(db, "form-wf");
        schema.WorkflowId = wf.Id;
        db.SaveChanges();

        var svc = new FormSchemaService(db);
        svc.SaveSchema(new FormSchema { Id = "form-wf", Title = "Updated", Steps = [] });

        var updated = db.FormSchemas.Find("form-wf");
        Assert.Equal(wf.Id, updated!.WorkflowId);
    }

    // ── LinkWorkflow ──────────────────────────────────────────────────────────

    [Fact]
    public void LinkWorkflow_ValidIds_SetsWorkflowId()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "form-1");
        var (wf, _) = TestDb.SeedWorkflow(db);
        var svc = new FormSchemaService(db);

        var (success, _) = svc.LinkWorkflow("form-1", wf.Id);

        Assert.True(success);
        Assert.Equal(wf.Id, db.FormSchemas.Find("form-1")!.WorkflowId);
    }

    [Fact]
    public void LinkWorkflow_NullWorkflowId_ClearsLink()
    {
        using var db = TestDb.Create();
        var (wf, _) = TestDb.SeedWorkflow(db);
        var schema  = TestDb.SeedSchema(db, "form-1");
        schema.WorkflowId = wf.Id;
        db.SaveChanges();

        var svc = new FormSchemaService(db);
        var (success, _) = svc.LinkWorkflow("form-1", null);

        Assert.True(success);
        Assert.Null(db.FormSchemas.Find("form-1")!.WorkflowId);
    }

    [Fact]
    public void LinkWorkflow_InvalidSchemaId_ReturnsError()
    {
        using var db = TestDb.Create();
        var (wf, _) = TestDb.SeedWorkflow(db);
        var svc = new FormSchemaService(db);

        var (success, error) = svc.LinkWorkflow("ghost-schema", wf.Id);

        Assert.False(success);
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LinkWorkflow_InvalidWorkflowId_ReturnsError()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "form-1");
        var svc = new FormSchemaService(db);

        var (success, error) = svc.LinkWorkflow("form-1", 9999);

        Assert.False(success);
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    // ── DeleteSchema ──────────────────────────────────────────────────────────

    [Fact]
    public void DeleteSchema_ExistingSchema_IsRemoved()
    {
        using var db = TestDb.Create();
        TestDb.SeedSchema(db, "form-1");
        var svc = new FormSchemaService(db);

        svc.DeleteSchema("form-1");

        Assert.Null(db.FormSchemas.Find("form-1"));
    }

    [Fact]
    public void DeleteSchema_NonExistentId_DoesNotThrow()
    {
        using var db = TestDb.Create();
        var svc = new FormSchemaService(db);

        var ex = Record.Exception(() => svc.DeleteSchema("ghost"));
        Assert.Null(ex);
    }
}
