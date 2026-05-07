namespace FormBuilder.Backend;

public sealed class ChecklistItem
{
    public int      Id              { get; set; }
    public string   Name            { get; set; } = string.Empty;
    public string   Description     { get; set; } = string.Empty;
    public string   ItemType        { get; set; } = "Document"; // "Document" | "Information"
    public string?  FormSchemaId    { get; set; }
    public FormSchema? FormSchema   { get; set; }
    public bool     IsClientVisible { get; set; }
    public bool     IsBrokerVisible { get; set; }
    public bool     IsActive        { get; set; } = true;
    public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;
    public List<ChecklistCondition> Conditions { get; set; } = [];
}

public sealed class ChecklistCondition
{
    public int    Id              { get; set; }
    public int    ChecklistItemId { get; set; }
    public string FieldName       { get; set; } = string.Empty;
    public string Operator        { get; set; } = "equals"; // equals | notEquals | contains
    public string Value           { get; set; } = string.Empty;
}

public sealed class ChecklistItemRequest
{
    public string   Name            { get; set; } = string.Empty;
    public string   Description     { get; set; } = string.Empty;
    public string   ItemType        { get; set; } = "Document";
    public string?  FormSchemaId    { get; set; }
    public bool     IsClientVisible { get; set; }
    public bool     IsBrokerVisible { get; set; }
    public bool     IsActive        { get; set; } = true;
    public List<ChecklistConditionRequest> Conditions { get; set; } = [];
}

public sealed class ChecklistConditionRequest
{
    public string FieldName { get; set; } = string.Empty;
    public string Operator  { get; set; } = "equals";
    public string Value     { get; set; } = string.Empty;
}
