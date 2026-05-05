namespace FormBuilder.Backend;

public sealed class ProfileSettings
{
    public int    Id          { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string JobTitle    { get; set; } = string.Empty;
    public string Department  { get; set; } = string.Empty;
}
