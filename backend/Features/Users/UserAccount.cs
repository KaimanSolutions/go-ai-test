namespace FormBuilder.Backend;

public sealed class UserAccount
{
    public int     Id            { get; set; }
    public string  Username      { get; set; } = string.Empty;
    public string  Email         { get; set; } = string.Empty;
    public string  PasswordHash  { get; set; } = string.Empty;
    public string  Role          { get; set; } = UserRoles.Client;
    public string  FirstName     { get; set; } = string.Empty;
    public string  LastName      { get; set; } = string.Empty;
    public string  Phone         { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public string? JobTitle      { get; set; }
    public string? Department    { get; set; }
    public int?    CompanyId     { get; set; }
    public Company? Company      { get; set; }
    public bool    IsLockedOut   { get; set; } = false;
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
}

public static class UserRoles
{
    public const string Admin  = "Admin";
    public const string Broker = "Broker";
    public const string Client = "Client";
}
