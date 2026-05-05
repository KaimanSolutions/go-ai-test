namespace FormBuilder.Backend;

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RegistrationRequest
{
    public string Email         { get; set; } = string.Empty;
    public string Password      { get; set; } = string.Empty;
    public string FirstName     { get; set; } = string.Empty;
    public string LastName      { get; set; } = string.Empty;
    public string Phone         { get; set; } = string.Empty;
    public string Role          { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public int?   CompanyId     { get; set; }
}

public sealed class AuthResponse
{
    public string  AccessToken { get; set; } = string.Empty;
    public string  UserName    { get; set; } = string.Empty;
    public string  Email       { get; set; } = string.Empty;
    public string  Role        { get; set; } = string.Empty;
    public int?    CompanyId   { get; set; }
    public string? CompanyName { get; set; }
}
