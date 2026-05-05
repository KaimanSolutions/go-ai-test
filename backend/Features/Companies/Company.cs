namespace FormBuilder.Backend;

public sealed class Company
{
    public int     Id              { get; set; }
    public string  Name            { get; set; } = string.Empty;
    public string  FCANumber       { get; set; } = string.Empty;
    public string? Phone           { get; set; }
    public string  Email           { get; set; } = string.Empty;
    public string? Website         { get; set; }
    public string? FcaStatus       { get; set; }
    public CompanyType Type        { get; set; } = CompanyType.Broker;
    public int?    ParentCompanyId { get; set; }
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;

    public ICollection<UserAccount> Brokers         { get; set; } = [];
    public Company?                  ParentCompany   { get; set; }
    public ICollection<Company>      ChildCompanies  { get; set; } = [];
    public ICollection<Address>      Addresses       { get; set; } = [];
    public ICollection<TradingName>  TradingNames    { get; set; } = [];
    public ICollection<BankDetails>  BankAccounts    { get; set; } = [];
}

public enum CompanyType
{
    Broker,
    Network,
    Club,
    Lender,
    Other
}
