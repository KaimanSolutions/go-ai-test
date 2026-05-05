namespace FormBuilder.Backend;

public sealed class IntegrationSetting
{
    public int      Id          { get; set; }
    public string   Integration { get; set; } = string.Empty;
    public string   Key         { get; set; } = string.Empty;
    public string   Value       { get; set; } = string.Empty;
    public DateTime UpdatedAt   { get; set; } = DateTime.UtcNow;
}
