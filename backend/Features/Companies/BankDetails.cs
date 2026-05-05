using System.ComponentModel.DataAnnotations;

namespace FormBuilder.Backend;

public sealed class BankDetails
{
    public int     Id            { get; set; }
    public int     CompanyId     { get; set; }
    public Company Company       { get; set; } = null!;

    [MaxLength(100)] public string AccountName   { get; set; } = string.Empty;
    [MaxLength(100)] public string BankName       { get; set; } = string.Empty;
    [MaxLength(8)]   public string AccountNumber  { get; set; } = string.Empty;
    [MaxLength(6)]   public string SortCode       { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
