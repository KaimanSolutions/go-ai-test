using FormBuilder.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Backend;

public sealed class FormSchemaService
{
    private readonly FormBuilderDbContext _context;

    public FormSchemaService(FormBuilderDbContext context) => _context = context;

    public void EnsureSampleData()
    {
        if (_context.FormSchemas.Any(s => s.Id == "loan-application")) return;

        _context.FormSchemas.Add(new FormSchema
        {
            Id          = "loan-application",
            Title       = "Mortgage Loan Application",
            Description = "A sample mortgage originations application form.",
            Steps       =
            [
                new FormStep
                {
                    Title  = "Personal Information",
                    Fields =
                    [
                        new() { Name = "applicantName", Label = "Applicant Name",  Type = "text",  Required = true },
                        new() { Name = "email",         Label = "Email Address",   Type = "email", Required = true },
                        new() { Name = "phone",         Label = "Phone Number",    Type = "tel",   Required = true }
                    ]
                },
                new FormStep
                {
                    Title  = "Loan Details",
                    Fields =
                    [
                        new()
                        {
                            Name       = "loanAmount",
                            Label      = "Loan Amount",
                            Type       = "number",
                            Required   = true,
                            Validators = [new() { RuleType = "min", Value = 10000, Message = "Loan amount must be at least 10,000." }]
                        },
                        new() { Name = "loanPurpose",   Label = "Loan Purpose",    Type = "select", Options = ["Purchase", "Refinance", "Construction"], Required = true },
                        new() { Name = "propertyValue", Label = "Property Value",  Type = "number", Required = true }
                    ]
                },
                new FormStep
                {
                    Title  = "Additional Borrowers",
                    Fields =
                    [
                        new() { Name = "hasCoBorrower",    Label = "Add Co-Borrower?",  Type = "checkbox" },
                        new() { Name = "coborrowerName",   Label = "Co-Borrower Name",  Type = "text",  Conditions = [new() { FieldName = "hasCoBorrower", Operator = "equals", Value = true }] },
                        new() { Name = "coborrowerEmail",  Label = "Co-Borrower Email", Type = "email", Conditions = [new() { FieldName = "hasCoBorrower", Operator = "equals", Value = true }] }
                    ]
                }
            ]
        });

        _context.SaveChanges();
    }

    public void SaveSchema(FormSchema schema)
    {
        var existing = LoadFullSchema(schema.Id);
        if (existing is not null)
        {
            _context.FormSchemas.Remove(existing);
            _context.SaveChanges();
        }

        foreach (var step in schema.Steps)
        {
            step.Id          = 0;
            step.FormSchemaId = schema.Id;
            foreach (var field in step.Fields)
            {
                field.Id         = 0;
                field.FormStepId = null;
                foreach (var condition in field.Conditions)  condition.Id = 0;
                foreach (var validator in field.Validators)
                {
                    validator.Id         = 0;
                    validator.FormFieldId = null;
                    foreach (var vc in validator.Conditions) vc.Id = 0;
                }
            }
        }

        _context.FormSchemas.Add(schema);
        _context.SaveChanges();
    }

    public void DeleteSchema(string id)
    {
        var existing = LoadFullSchema(id);
        if (existing is not null)
        {
            _context.FormSchemas.Remove(existing);
            _context.SaveChanges();
        }
    }

    public IEnumerable<FormSchema> GetAllSchemas() => LoadQuery().ToList();

    public FormSchema? GetSchema(string id) => LoadQuery().FirstOrDefault(s => s.Id == id);

    private IQueryable<FormSchema> LoadQuery() =>
        _context.FormSchemas
            .Include(s => s.Steps)
                .ThenInclude(s => s.Fields)
                    .ThenInclude(f => f.Conditions)
            .Include(s => s.Steps)
                .ThenInclude(s => s.Fields)
                    .ThenInclude(f => f.Validators)
                        .ThenInclude(v => v.Conditions);

    private FormSchema? LoadFullSchema(string id) => LoadQuery().FirstOrDefault(s => s.Id == id);
}
