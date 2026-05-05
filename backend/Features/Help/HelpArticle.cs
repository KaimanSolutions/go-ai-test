namespace FormBuilder.Backend;

public sealed class HelpArticle
{
    public int      Id                   { get; set; }
    public string   Title                { get; set; } = string.Empty;
    public string   Content              { get; set; } = string.Empty;
    public string   Category             { get; set; } = "guides";
    public bool     ShowInAdminPortal    { get; set; } = true;
    public bool     ShowInBrokerPortal   { get; set; } = true;
    public bool     ShowInCustomerPortal { get; set; } = true;
    public DateTime CreatedAt            { get; set; } = DateTime.UtcNow;
}
