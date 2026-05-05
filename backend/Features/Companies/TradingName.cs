namespace FormBuilder.Backend;

public sealed class TradingName
{
    public int     Id            { get; set; }
    public int     CompanyId     { get; set; }
    public Company Company       { get; set; } = null!;
    public string  Name          { get; set; } = string.Empty;
    public string? Status        { get; set; }
    public string? EffectiveFrom { get; set; }
    public string? EffectiveTo   { get; set; }
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
}
