namespace FormBuilder.Backend.Models;

public sealed class FormSchema
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<FormStep> Steps { get; set; } = new();
}

public sealed class FormStep
{
    public string Title { get; set; } = string.Empty;
    public List<FormField> Fields { get; set; } = new();
}

public sealed class FormField
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public bool Required { get; set; }
    public bool ReadOnly { get; set; }
    public object? DefaultValue { get; set; }
    public List<string> Options { get; set; } = new();
    public List<FieldCondition> Conditions { get; set; } = new();
    public List<ValidationRule> Validators { get; set; } = new();
}

public sealed class FieldCondition
{
    public string FieldName { get; set; } = string.Empty;
    public string Operator { get; set; } = "equals";
    public object? Value { get; set; }
}

public sealed class ValidationRule
{
    public string RuleType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Value { get; set; }
    public List<FieldCondition> Conditions { get; set; } = new();
}

public sealed class FormSubmissionRequest
{
    public FormSchema? Schema { get; set; }
    public Dictionary<string, object?> Values { get; set; } = new();
}

public sealed class FormValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public Dictionary<string, List<string>> Errors { get; set; } = new();
}
