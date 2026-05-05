using FormBuilder.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Backend;

public sealed class CompanyService
{
    private readonly FormBuilderDbContext _context;

    public CompanyService(FormBuilderDbContext context) => _context = context;

    public IEnumerable<object> GetAll() =>
        _context.Companies
            .Include(c => c.Brokers)
            .OrderBy(c => c.Name)
            .Select(c => (object)new
            {
                c.Id,
                c.Name,
                c.FCANumber,
                c.Email,
                c.Phone,
                c.Website,
                c.FcaStatus,
                Type        = c.Type.ToString(),
                BrokerCount = c.Brokers.Count
            })
            .ToList();

    public IEnumerable<object> GetBrokers() =>
        _context.Companies
            .Where(c => c.Type == CompanyType.Broker)
            .OrderBy(c => c.Name)
            .Select(c => (object)new { c.Id, c.Name, c.FCANumber, c.Email })
            .ToList();

    public IEnumerable<object> GetNetworks() =>
        _context.Companies
            .Where(c => c.Type == CompanyType.Network)
            .Include(c => c.ParentCompany)
            .OrderBy(c => c.Name)
            .Select(c => (object)new { c.Id, c.Name, c.FCANumber, c.Email, ParentBroker = c.ParentCompany!.Name })
            .ToList();

    public IEnumerable<object> GetClubs() =>
        _context.Companies
            .Where(c => c.Type == CompanyType.Club)
            .OrderBy(c => c.Name)
            .Select(c => (object)new { c.Id, c.Name, c.Email })
            .ToList();

    public object? GetDetail(int id)
    {
        var c = _context.Companies
            .Include(c => c.Brokers)
            .Include(c => c.Addresses)
            .Include(c => c.TradingNames)
            .Include(c => c.BankAccounts)
            .Include(c => c.ParentCompany)
            .Include(c => c.ChildCompanies)
            .FirstOrDefault(c => c.Id == id);

        if (c is null) return null;

        return new
        {
            c.Id,
            c.Name,
            c.FCANumber,
            c.Phone,
            c.Email,
            c.Website,
            c.FcaStatus,
            Type      = c.Type.ToString(),
            c.CreatedAt,
            Addresses = c.Addresses.Select(a => new
            {
                a.Id,
                Type = a.Type.ToString(),
                a.OrganisationName,
                a.DepartmentName,
                a.SubBuildingName,
                a.BuildingName,
                a.BuildingNumber,
                a.DependentThoroughfareName,
                a.DependentThoroughfareDescriptor,
                a.ThoroughfareName,
                a.ThoroughfareDescriptor,
                a.DoubleDependentLocality,
                a.DependentLocality,
                a.PostTown,
                a.Postcode,
                a.POBox,
                a.Country
            }).ToList(),
            TradingNames = c.TradingNames
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name, t.Status, t.EffectiveFrom, t.EffectiveTo })
                .ToList(),
            BankAccounts = c.BankAccounts
                .OrderBy(b => b.CreatedAt)
                .Select(b => new { b.Id, b.AccountName, b.BankName, b.AccountNumber, b.SortCode })
                .ToList(),
            ParentCompany = c.ParentCompany is null ? null : new
            {
                c.ParentCompany.Id,
                c.ParentCompany.Name,
                c.ParentCompany.FCANumber,
                Type    = c.ParentCompany.Type.ToString(),
                c.ParentCompany.Email,
                c.ParentCompany.Phone,
                c.ParentCompany.Website
            },
            ChildCompanies = c.ChildCompanies
                .Select(ch => new { ch.Id, ch.Name, Type = ch.Type.ToString() }).ToList(),
            Brokers = c.Brokers
                .Select(b => new { b.Id, b.FirstName, b.LastName, b.Email, b.Phone }).ToList()
        };
    }

    public (bool Success, string Error, Company? Company) Register(CompanyRegistrationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "Company name is required.", null);

        if (string.IsNullOrWhiteSpace(request.FCANumber))
            return (false, "FCA reference number is required.", null);

        if (string.IsNullOrWhiteSpace(request.Email))
            return (false, "Company email address is required.", null);

        if (_context.Companies.Any(c => c.FCANumber == request.FCANumber))
            return (false, "A company with this FCA number is already registered.", null);

        if (request.ParentCompanyId.HasValue && !_context.Companies.Any(c => c.Id == request.ParentCompanyId))
            return (false, "Parent company does not exist.", null);

        var company = new Company
        {
            Name            = request.Name,
            FCANumber       = request.FCANumber,
            Phone           = request.Phone,
            Email           = request.Email,
            Website         = request.Website,
            FcaStatus       = request.FcaStatus,
            Type            = request.Type,
            ParentCompanyId = request.ParentCompanyId,
            CreatedAt       = DateTime.UtcNow
        };

        foreach (var addrReq in request.Addresses)
        {
            company.Addresses.Add(new Address
            {
                Type                            = Enum.TryParse<AddressType>(addrReq.Type, out var at) ? at : AddressType.Registered,
                OrganisationName                = addrReq.OrganisationName,
                DepartmentName                  = addrReq.DepartmentName,
                SubBuildingName                 = addrReq.SubBuildingName,
                BuildingName                    = addrReq.BuildingName,
                BuildingNumber                  = addrReq.BuildingNumber,
                DependentThoroughfareName       = addrReq.DependentThoroughfareName,
                DependentThoroughfareDescriptor = addrReq.DependentThoroughfareDescriptor,
                ThoroughfareName                = addrReq.ThoroughfareName,
                ThoroughfareDescriptor          = addrReq.ThoroughfareDescriptor,
                DoubleDependentLocality         = addrReq.DoubleDependentLocality,
                DependentLocality               = addrReq.DependentLocality,
                PostTown                        = addrReq.PostTown,
                Postcode                        = addrReq.Postcode,
                POBox                           = addrReq.POBox,
                Country                         = addrReq.Country,
                CreatedAt                       = DateTime.UtcNow
            });
        }

        foreach (var nameReq in request.TradingNames)
        {
            if (string.IsNullOrWhiteSpace(nameReq.Name)) continue;
            company.TradingNames.Add(new TradingName
            {
                Name          = nameReq.Name.Trim(),
                Status        = nameReq.Status,
                EffectiveFrom = nameReq.EffectiveFrom,
                EffectiveTo   = nameReq.EffectiveTo
            });
        }

        _context.Companies.Add(company);
        _context.SaveChanges();
        return (true, string.Empty, company);
    }

    public (bool Success, string Error) AddBankDetails(int companyId, BankDetailsRequest request)
    {
        if (!_context.Companies.Any(c => c.Id == companyId))
            return (false, "Company not found.");

        if (string.IsNullOrWhiteSpace(request.AccountName))
            return (false, "Account name is required.");

        if (string.IsNullOrWhiteSpace(request.BankName))
            return (false, "Bank name is required.");

        if (string.IsNullOrWhiteSpace(request.AccountNumber))
            return (false, "Account number is required.");

        if (string.IsNullOrWhiteSpace(request.SortCode))
            return (false, "Sort code is required.");

        _context.BankDetails.Add(new BankDetails
        {
            CompanyId     = companyId,
            AccountName   = request.AccountName.Trim(),
            BankName      = request.BankName.Trim(),
            AccountNumber = request.AccountNumber.Trim().Replace("-", "").Replace(" ", ""),
            SortCode      = request.SortCode.Trim().Replace("-", "").Replace(" ", ""),
            CreatedAt     = DateTime.UtcNow
        });

        _context.SaveChanges();
        return (true, string.Empty);
    }

    public (bool Success, string Error) DeleteBankDetails(int companyId, int bankDetailsId)
    {
        var entry = _context.BankDetails.FirstOrDefault(b => b.Id == bankDetailsId && b.CompanyId == companyId);
        if (entry is null) return (false, "Bank account not found.");
        _context.BankDetails.Remove(entry);
        _context.SaveChanges();
        return (true, string.Empty);
    }
}
