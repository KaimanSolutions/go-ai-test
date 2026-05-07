namespace FormBuilder.Backend;

public sealed class CreateApplicationRequest
{
    public string FormSchemaId { get; set; } = string.Empty;
}

public sealed class UpdateApplicationRequest
{
    public string? FormData       { get; set; }
    public int?    WorkflowId     { get; set; }
    public int?    CurrentStageId { get; set; }
    public bool    Submit         { get; set; }
}
