using FormBuilder.Backend.Models;
using FormBuilder.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Backend.Services;

public sealed class FormSchemaService
{
    private readonly FormBuilderDbContext _context;

    public FormSchemaService(FormBuilderDbContext context)
    {
        _context = context;
    }

    public void EnsureSampleData()
    {
        if (_context.FormSchemas.Any(s => s.Id == "loan-application"))
            return;

        var sample = new FormSchema
        {
            Id = "loan-application",
            Title = "Mortgage Loan Application",
            Description = "A sample mortgage originations application form.",
            Steps = new List<FormStep>
            {
                new FormStep
                {
                    Title = "Personal Information",
                    Fields = new List<FormField>
                    {
                        new FormField { Name = "applicantName", Label = "Applicant Name", Type = "text", Required = true },
                        new FormField { Name = "email", Label = "Email Address", Type = "email", Required = true },
                        new FormField { Name = "phone", Label = "Phone Number", Type = "tel", Required = true }
                    }
                },
                new FormStep
                {
                    Title = "Loan Details",
                    Fields = new List<FormField>
                    {
                        new FormField { Name = "loanAmount", Label = "Loan Amount", Type = "number", Required = true, Validators = new List<ValidationRule>{ new ValidationRule { RuleType = "min", Value = 10000, Message = "Loan amount must be at least 10,000." } } },
                        new FormField { Name = "loanPurpose", Label = "Loan Purpose", Type = "select", Options = new List<string>{ "Purchase", "Refinance", "Construction" }, Required = true },
                        new FormField { Name = "propertyValue", Label = "Property Value", Type = "number", Required = true }
                    }
                },
                new FormStep
                {
                    Title = "Additional Borrowers",
                    Fields = new List<FormField>
                    {
                        new FormField { Name = "hasCoBorrower", Label = "Add Co-Borrower?", Type = "checkbox" },
                        new FormField { Name = "coborrowerName", Label = "Co-Borrower Name", Type = "text", Conditions = new List<FieldCondition>{ new FieldCondition { FieldName = "hasCoBorrower", Operator = "equals", Value = true } }, Required = false },
                        new FormField { Name = "coborrowerEmail", Label = "Co-Borrower Email", Type = "email", Conditions = new List<FieldCondition>{ new FieldCondition { FieldName = "hasCoBorrower", Operator = "equals", Value = true } }, Required = false }
                    }
                }
            }
        };

        _context.FormSchemas.Add(sample);
        _context.SaveChanges();
    }

    public void SaveSchema(FormSchema schema)
    {
        var existing = LoadFullSchema(schema.Id);
        if (existing != null)
        {
            _context.FormSchemas.Remove(existing);
            _context.SaveChanges();
        }

        // Reset all nested IDs so EF generates new ones after the delete
        foreach (var step in schema.Steps)
        {
            step.Id = 0;
            step.FormSchemaId = schema.Id;
            foreach (var field in step.Fields)
            {
                field.Id = 0;
                field.FormStepId = null;
                foreach (var condition in field.Conditions)
                    condition.Id = 0;
                foreach (var validator in field.Validators)
                {
                    validator.Id = 0;
                    validator.FormFieldId = null;
                    foreach (var vc in validator.Conditions)
                        vc.Id = 0;
                }
            }
        }

        _context.FormSchemas.Add(schema);
        _context.SaveChanges();
    }

    public void DeleteSchema(string id)
    {
        var existing = LoadFullSchema(id);
        if (existing != null)
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
