namespace FormBuilder.Backend;

// Statuses: Outstanding → Pending Review → Approved | More Info Needed | Rejected
// More Info Needed → (user re-submits) → Pending Review → Approved | Rejected
public sealed class ApplicationChecklistItem
{
    public int       Id                  { get; set; }
    public int       ApplicationId       { get; set; }
    public int       ChecklistItemId     { get; set; }
    public string    Status              { get; set; } = "Outstanding";
    public string?   TextResponse        { get; set; }
    public string?   DocumentName        { get; set; }
    public string?   DocumentPath        { get; set; }
    public string?   DocumentContentType { get; set; }
    public DateTime  GeneratedAt         { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt         { get; set; }

    public ChecklistItem                      ChecklistItem { get; set; } = null!;
    public List<ApplicationChecklistComment>  Comments      { get; set; } = [];
}

public sealed class ApplicationChecklistComment
{
    public int      Id                        { get; set; }
    public int      ApplicationChecklistItemId { get; set; }
    public string   Comment                   { get; set; } = string.Empty;
    public string   AuthorName                { get; set; } = string.Empty;
    public DateTime CreatedAt                 { get; set; } = DateTime.UtcNow;
}

public sealed class ChecklistRespondRequest
{
    public string? Text { get; set; }
}

public sealed class ChecklistStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public sealed class ChecklistCommentRequest
{
    public string Comment { get; set; } = string.Empty;
}
