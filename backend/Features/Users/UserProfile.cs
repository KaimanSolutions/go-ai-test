namespace FormBuilder.Backend;

public sealed class UserProfile
{
    public int     Id            { get; set; }
    public string  UserName      { get; set; } = string.Empty;
    public string  Email         { get; set; } = string.Empty;
    public string  Role          { get; set; } = string.Empty;
    public string  FirstName     { get; set; } = string.Empty;
    public string  LastName      { get; set; } = string.Empty;
    public string? Phone         { get; set; }
    public string? LicenseNumber { get; set; }
    public int?    CompanyId     { get; set; }
    public string? CompanyName   { get; set; }
    public DateTime CreatedAt    { get; set; }
}
