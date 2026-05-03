namespace FormBuilder.Backend.Models;

public sealed class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
