namespace FormBuilder.Backend;

public sealed class Template
{
    public int      Id           { get; set; }
    public string   Name         { get; set; } = string.Empty;
    public string   Description  { get; set; } = string.Empty;
    public string   TemplateType { get; set; } = "Email"; // Email | SMS | Document
    public string   Subject      { get; set; } = string.Empty;
    public string   Content      { get; set; } = string.Empty;
    public bool     IsActive     { get; set; } = true;
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt    { get; set; } = DateTime.UtcNow;
}

public sealed class TemplateSaveRequest
{
    public string Name         { get; set; } = string.Empty;
    public string Description  { get; set; } = string.Empty;
    public string TemplateType { get; set; } = "Email";
    public string Subject      { get; set; } = string.Empty;
    public string Content      { get; set; } = string.Empty;
    public bool   IsActive     { get; set; } = true;
}

public sealed class MjmlCompileRequest
{
    public string Mjml { get; set; } = string.Empty;
}
