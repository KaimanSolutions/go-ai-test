using Xunit;

namespace FormBuilder.Backend.Tests;

public class WorkflowServiceTests
{
    // ── Save — create ─────────────────────────────────────────────────────────

    [Fact]
    public void Save_NewWorkflow_CreatesWithStagesAndTasks()
    {
        using var db = TestDb.Create();
        var svc = new WorkflowService(db);

        var (success, _, id) = svc.Save(null, new WorkflowSaveRequest
        {
            Name = "Mortgage Review",
            Stages =
            [
                new WorkflowStageSaveRequest
                {
                    TempId    = "t1",
                    Name      = "Initial Assessment",
                    Order     = 0,
                    IsInitial = true,
                    IsFinal   = false,
                    Tasks     = [new WorkflowTaskSaveRequest { Title = "Review docs", Required = true }]
                }
            ]
        });

        Assert.True(success);
        Assert.True(id > 0);
        Assert.Single(db.WorkflowStages.ToList());
        Assert.Single(db.WorkflowTasks.ToList());
    }

    [Fact]
    public void Save_NewWorkflow_CreatesTransitionsBetweenStages()
    {
        using var db = TestDb.Create();
        var svc = new WorkflowService(db);

        var (success, _, id) = svc.Save(null, new WorkflowSaveRequest
        {
            Name = "Two-Stage",
            Stages =
            [
                new WorkflowStageSaveRequest
                {
                    TempId    = "s1",
                    Name      = "Stage 1",
                    Order     = 0,
                    IsInitial = true,
                    TransitionsOut = [new WorkflowTransitionSaveRequest { ToStageTempId = "s2", Label = "Approve" }]
                },
                new WorkflowStageSaveRequest
                {
                    TempId  = "s2",
                    Name    = "Stage 2",
                    Order   = 1,
                    IsFinal = true
                }
            ]
        });

        Assert.True(success);
        var transitions = db.WorkflowTransitions.ToList();
        Assert.Single(transitions);
        Assert.Equal("Approve", transitions[0].Label);
    }

    [Fact]
    public void Save_EmptyName_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new WorkflowService(db);

        var (success, error, _) = svc.Save(null, new WorkflowSaveRequest { Name = "  " });

        Assert.False(success);
        Assert.NotEmpty(error);
    }

    // ── Save — update ─────────────────────────────────────────────────────────

    [Fact]
    public void Save_UpdateExisting_ChangesNameAndDescription()
    {
        using var db = TestDb.Create();
        var (wf, _)  = TestDb.SeedWorkflow(db, "Original Name");
        var svc      = new WorkflowService(db);

        var (success, _, _) = svc.Save(wf.Id, new WorkflowSaveRequest
        {
            Name        = "Updated Name",
            Description = "New description",
            Stages      = [new WorkflowStageSaveRequest { TempId = "t1", Name = "Stage", Order = 0, DbId = db.WorkflowStages.First().Id }]
        });

        Assert.True(success);
        var updated = db.Workflows.Find(wf.Id)!;
        Assert.Equal("Updated Name",    updated.Name);
        Assert.Equal("New description", updated.Description);
    }

    [Fact]
    public void Save_UpdateExisting_NonExistentId_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new WorkflowService(db);

        var (success, error, _) = svc.Save(9999, new WorkflowSaveRequest
        {
            Name   = "Whatever",
            Stages = []
        });

        Assert.False(success);
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Save_UpdateExisting_RemovedStage_NullsApplicationCurrentStageId()
    {
        using var db = TestDb.Create();

        // Set up: workflow with two stages, application on stage 2
        var (wf, _) = TestDb.SeedWorkflow(db, "WF");
        var stage2 = new WorkflowStage { Name = "Stage 2", Order = 1, WorkflowId = wf.Id };
        db.WorkflowStages.Add(stage2);
        db.SaveChanges();

        TestDb.SeedSchema(db, "s1");
        var app = new Application
        {
            FormSchemaId      = "s1",
            FormSchemaVersion = 1,
            Version           = 1,
            WorkflowId        = wf.Id,
            CurrentStageId    = stage2.Id,
            CreatedAt         = DateTime.UtcNow,
            UpdatedAt         = DateTime.UtcNow
        };
        db.Applications.Add(app);
        db.SaveChanges();

        var svc         = new WorkflowService(db);
        var initialId   = db.WorkflowStages.First(s => s.Order == 0).Id;

        // Update: keep only stage 1, drop stage 2
        var (success, _, _) = svc.Save(wf.Id, new WorkflowSaveRequest
        {
            Name   = "WF",
            Stages = [new WorkflowStageSaveRequest { TempId = "t1", DbId = initialId, Name = "Initial", Order = 0, IsInitial = true }]
        });

        Assert.True(success);

        db.Entry(app).Reload();
        Assert.Null(app.CurrentStageId);
        Assert.Equal(0, db.WorkflowStages.Count(s => s.Id == stage2.Id));
    }

    [Fact]
    public void Save_UpdateExisting_AddsNewStage()
    {
        using var db = TestDb.Create();
        var (wf, stage1) = TestDb.SeedWorkflow(db, "WF");
        var svc          = new WorkflowService(db);

        svc.Save(wf.Id, new WorkflowSaveRequest
        {
            Name = "WF",
            Stages =
            [
                new WorkflowStageSaveRequest { TempId = "t1", DbId = stage1.Id, Name = "Initial", Order = 0, IsInitial = true },
                new WorkflowStageSaveRequest { TempId = "t2", Name = "Final",   Order = 1, IsFinal  = true }
            ]
        });

        Assert.Equal(2, db.WorkflowStages.Count(s => s.WorkflowId == wf.Id));
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_ExistingWorkflow_Succeeds()
    {
        using var db = TestDb.Create();
        var (wf, _)  = TestDb.SeedWorkflow(db);
        var svc      = new WorkflowService(db);

        var (success, _) = svc.Delete(wf.Id);

        Assert.True(success);
        Assert.Null(db.Workflows.Find(wf.Id));
    }

    [Fact]
    public void Delete_NonExistentWorkflow_ReturnsError()
    {
        using var db = TestDb.Create();
        var svc = new WorkflowService(db);

        var (success, error) = svc.Delete(9999);

        Assert.False(success);
        Assert.NotEmpty(error);
    }
}
