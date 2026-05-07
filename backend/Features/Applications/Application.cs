namespace FormBuilder.Backend;

public sealed class Application
{
    public int               Id                { get; set; }
    public string            PublicReference   { get; set; } = string.Empty;
    public string            FormSchemaId      { get; set; } = string.Empty;
    public FormSchema        FormSchema        { get; set; } = null!;
    public int               FormSchemaVersion { get; set; }
    public int               Version           { get; set; } = 1;
    public string?           FormData          { get; set; }

    // Set when a client creates the application
    public int?              ClientId          { get; set; }
    public UserAccount?      Client            { get; set; }

    // Set when a broker creates the application
    public int?              BrokerId          { get; set; }
    public UserAccount?      Broker            { get; set; }

    // Broker's company (populated from broker's CompanyId)
    public int?              CompanyId         { get; set; }
    public Company?          Company           { get; set; }

    // Network the broker's company belongs to (parent company of Network type, if any)
    public int?              NetworkId         { get; set; }
    public Company?          Network           { get; set; }

    // Workflow that drives this application's status/stages
    public int?              WorkflowId        { get; set; }
    public Workflow?         Workflow          { get; set; }

    // The stage within the workflow the application is currently on
    public int?              CurrentStageId    { get; set; }
    public WorkflowStage?    CurrentStage      { get; set; }

    public DateTime?         SubmittedAt       { get; set; }
    public DateTime          CreatedAt         { get; set; } = DateTime.UtcNow;
    public DateTime          UpdatedAt         { get; set; } = DateTime.UtcNow;
}
