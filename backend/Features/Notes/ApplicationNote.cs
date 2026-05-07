namespace FormBuilder.Backend;

public sealed class ApplicationNote
{
    public int             Id              { get; set; }
    public int             ApplicationId   { get; set; }
    public string          Content         { get; set; } = string.Empty;
    public string          AuthorName      { get; set; } = string.Empty;
    public string          AuthorRole      { get; set; } = string.Empty;
    public string?         Stage           { get; set; }
    public string?         Category        { get; set; }
    public bool            IsClientVisible { get; set; }
    public bool            IsBrokerVisible { get; set; }
    public DateTimeOffset  CreatedAt       { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AddNoteRequest
{
    public string  Content         { get; set; } = string.Empty;
    public string? Category        { get; set; }
    public bool    IsClientVisible { get; set; }
    public bool    IsBrokerVisible { get; set; }
}

public sealed class UpdateNoteVisibilityRequest
{
    public bool IsClientVisible { get; set; }
    public bool IsBrokerVisible { get; set; }
}
