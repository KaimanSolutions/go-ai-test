namespace FormBuilder.Backend;

public sealed class FormSchema
{
    public string       Id          { get; set; } = string.Empty;
    public string       Title       { get; set; } = string.Empty;
    public string       Description { get; set; } = string.Empty;
    public int          Version     { get; set; } = 1;
    public int?         WorkflowId  { get; set; }
    public Workflow?    Workflow    { get; set; }
    public List<FormStep> Steps     { get; set; } = [];
    public bool              IsArchived  { get; set; }
    public DateTimeOffset?   ArchivedAt  { get; set; }
}

public sealed class StepConditionDef
{
    public string FieldName { get; set; } = string.Empty;
    public string Operator  { get; set; } = "equals";
    public string Value     { get; set; } = string.Empty;
}

public sealed class FormStep
{
    public int           Id           { get; set; }
    public string        Title        { get; set; } = string.Empty;
    public string?       FormSchemaId { get; set; }
    public List<FormField>       Fields     { get; set; } = [];
    public List<StepConditionDef> Conditions { get; set; } = [];
}

public sealed class FormField
{
    public int      Id           { get; set; }
    public string   Name         { get; set; } = string.Empty;
    public string   Label        { get; set; } = string.Empty;
    public string   Type         { get; set; } = "text";
    public string?  InfoVariant  { get; set; }
    public bool     Required     { get; set; }
    public bool     ReadOnly     { get; set; }
    public object?  DefaultValue { get; set; }
    public int?     FormStepId   { get; set; }

    public List<string>              Options    { get; set; } = [];
    public List<SubFieldDefinition>  SubFields  { get; set; } = [];
    public List<FieldCondition>      Conditions { get; set; } = [];
    public List<ValidationRule>      Validators { get; set; } = [];
}

public sealed class SubFieldDefinition
{
    public string       Name     { get; set; } = string.Empty;
    public string       Label    { get; set; } = string.Empty;
    public string       Type     { get; set; } = "text";
    public bool         Required { get; set; }
    public List<string> Options  { get; set; } = [];
}

public sealed class FieldCondition
{
    public int     Id          { get; set; }
    public string  FieldName   { get; set; } = string.Empty;
    public string  Operator    { get; set; } = "equals";
    public string? Value       { get; set; }
    public int?    FormFieldId { get; set; }
}

public sealed class ValidationRule
{
    public int    Id          { get; set; }
    public string RuleType    { get; set; } = string.Empty;
    public string Message     { get; set; } = string.Empty;
    public string? Value      { get; set; }
    public int?   FormFieldId { get; set; }

    public List<FieldCondition> Conditions { get; set; } = [];
}

public sealed class LinkWorkflowRequest
{
    public int? WorkflowId { get; set; }
}

public sealed class FormSubmissionRequest
{
    public FormSchema?                  Schema { get; set; }
    public Dictionary<string, object?> Values { get; set; } = [];
}

public sealed class FormValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public Dictionary<string, List<string>> Errors { get; set; } = [];
}
