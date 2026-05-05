namespace FormBuilder.Backend;

public sealed class CompanyRegistrationRequest
{
    public string  Name            { get; set; } = string.Empty;
    public string  FCANumber       { get; set; } = string.Empty;
    public string? Phone           { get; set; }
    public string  Email           { get; set; } = string.Empty;
    public string? Website         { get; set; }
    public string? FcaStatus       { get; set; }
    public CompanyType Type        { get; set; } = CompanyType.Broker;
    public int?    ParentCompanyId { get; set; }
    public List<AddressRequest>     Addresses    { get; set; } = [];
    public List<TradingNameRequest> TradingNames { get; set; } = [];
}

public sealed class AddressRequest
{
    public string? Type                            { get; set; }
    public string? OrganisationName                { get; set; }
    public string? DepartmentName                  { get; set; }
    public string? SubBuildingName                 { get; set; }
    public string? BuildingName                    { get; set; }
    public string? BuildingNumber                  { get; set; }
    public string? DependentThoroughfareName       { get; set; }
    public string? DependentThoroughfareDescriptor { get; set; }
    public string? ThoroughfareName                { get; set; }
    public string? ThoroughfareDescriptor          { get; set; }
    public string? DoubleDependentLocality         { get; set; }
    public string? DependentLocality               { get; set; }
    public string? PostTown                        { get; set; }
    public string? Postcode                        { get; set; }
    public string? POBox                           { get; set; }
    public string? Country                         { get; set; }
}

public sealed class TradingNameRequest
{
    public string  Name          { get; set; } = string.Empty;
    public string? Status        { get; set; }
    public string? EffectiveFrom { get; set; }
    public string? EffectiveTo   { get; set; }
}

public sealed class BankDetailsRequest
{
    public string AccountName  { get; set; } = string.Empty;
    public string BankName     { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string SortCode     { get; set; } = string.Empty;
}
