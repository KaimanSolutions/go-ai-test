using System.ComponentModel.DataAnnotations;

namespace FormBuilder.Backend;

// ── Entities ─────────────────────────────────────────────────────────────────

public sealed class Workflow
{
    public int      Id          { get; set; }
    [MaxLength(200)] public string  Name        { get; set; } = string.Empty;
    public string?  Description { get; set; }
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    public ICollection<WorkflowStage> Stages { get; set; } = [];
}

public sealed class WorkflowStage
{
    public int      Id          { get; set; }
    public int      WorkflowId  { get; set; }
    public Workflow Workflow    { get; set; } = null!;
    [MaxLength(200)] public string  Name        { get; set; } = string.Empty;
    public string?  Description { get; set; }
    public int      Order       { get; set; }
    public bool     IsInitial   { get; set; }
    public bool     IsFinal     { get; set; }

    public ICollection<WorkflowTask>       Tasks          { get; set; } = [];
    public ICollection<WorkflowTransition> TransitionsOut { get; set; } = [];
}

public sealed class WorkflowTask
{
    public int           Id          { get; set; }
    public int           StageId     { get; set; }
    public WorkflowStage Stage       { get; set; } = null!;
    [MaxLength(200)] public string   Title       { get; set; } = string.Empty;
    public string?       Description { get; set; }
    public bool          Required    { get; set; } = true;
    public int           Order       { get; set; }
}

public sealed class WorkflowTransition
{
    public int           Id          { get; set; }
    public int           WorkflowId  { get; set; }
    public int           FromStageId { get; set; }
    public WorkflowStage FromStage   { get; set; } = null!;
    public int           ToStageId   { get; set; }
    public WorkflowStage ToStage     { get; set; } = null!;
    [MaxLength(200)] public string  Label       { get; set; } = string.Empty;
    public string?       Condition   { get; set; }
}

// ── Request models ────────────────────────────────────────────────────────────

public sealed class WorkflowSaveRequest
{
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<WorkflowStageSaveRequest> Stages { get; set; } = [];
}

public sealed class WorkflowStageSaveRequest
{
    public int?    DbId        { get; set; }
    public string  TempId      { get; set; } = string.Empty;
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int     Order       { get; set; }
    public bool    IsInitial   { get; set; }
    public bool    IsFinal     { get; set; }
    public List<WorkflowTaskSaveRequest>       Tasks          { get; set; } = [];
    public List<WorkflowTransitionSaveRequest> TransitionsOut { get; set; } = [];
}

public sealed class WorkflowTaskSaveRequest
{
    public string  Title       { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool    Required    { get; set; } = true;
    public int     Order       { get; set; }
}

public sealed class WorkflowTransitionSaveRequest
{
    public string  ToStageTempId { get; set; } = string.Empty;
    public string  Label         { get; set; } = string.Empty;
    public string? Condition     { get; set; }
}
