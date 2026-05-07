namespace FormBuilder.Backend;

public sealed class LoginEvent
{
    public int              Id           { get; set; }
    public int?             UserId       { get; set; }
    public string           UserType     { get; set; } = string.Empty;
    public string?          Email        { get; set; }
    public DateTimeOffset   TimestampUtc { get; set; }
    public string           DeviceType   { get; set; } = string.Empty;
    public string           AuthMethod   { get; set; } = string.Empty;
}
