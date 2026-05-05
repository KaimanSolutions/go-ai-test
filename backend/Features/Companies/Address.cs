using System.ComponentModel.DataAnnotations;

namespace FormBuilder.Backend;

public sealed class Address
{
    public int     Id        { get; set; }
    public int     CompanyId { get; set; }
    public Company Company   { get; set; } = null!;
    public AddressType Type  { get; set; } = AddressType.Registered;

    // Organisation
    [MaxLength(60)] public string? OrganisationName { get; set; }
    [MaxLength(60)] public string? DepartmentName   { get; set; }

    // Premises
    [MaxLength(30)] public string? SubBuildingName { get; set; }
    [MaxLength(50)] public string? BuildingName    { get; set; }
    [MaxLength(12)] public string? BuildingNumber  { get; set; }

    // Thoroughfare
    [MaxLength(60)] public string? DependentThoroughfareName       { get; set; }
    [MaxLength(20)] public string? DependentThoroughfareDescriptor { get; set; }
    [MaxLength(60)] public string? ThoroughfareName                { get; set; }
    [MaxLength(20)] public string? ThoroughfareDescriptor          { get; set; }

    // Locality
    [MaxLength(35)] public string? DoubleDependentLocality { get; set; }
    [MaxLength(35)] public string? DependentLocality       { get; set; }
    [MaxLength(30)] public string? PostTown                { get; set; }

    [MaxLength(8)] public string? Postcode { get; set; }
    [MaxLength(6)] public string? POBox    { get; set; }
    [MaxLength(60)] public string? Country  { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum AddressType
{
    Registered,
    Trading,
    Correspondence,
    Branch,
    Complaints
}
