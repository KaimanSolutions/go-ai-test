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
                            Validators = [new() { RuleType = "min", Value = "10000", Message = "Loan amount must be at least 10,000." }]
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
                        new() { Name = "coborrowerName",   Label = "Co-Borrower Name",  Type = "text",  Conditions = [new() { FieldName = "hasCoBorrower", Operator = "equals", Value = "true" }] },
                        new() { Name = "coborrowerEmail",  Label = "Co-Borrower Email", Type = "email", Conditions = [new() { FieldName = "hasCoBorrower", Operator = "equals", Value = "true" }] }
                    ]
                }
            ]
        });

        _context.SaveChanges();
    }

    public void SaveSchema(FormSchema schema)
    {
        var existing = LoadFullSchema(schema.Id);

        if (existing is null)
        {
            // Brand new schema — just insert
            schema.Version = 1;
            _context.FormSchemas.Add(schema);
            _context.SaveChanges();
            return;
        }

        // Update in place so FK references from Applications are preserved
        existing.Title       = schema.Title;
        existing.Description = schema.Description;
        existing.Version    += 1;
        // WorkflowId is managed separately via LinkWorkflow — don't overwrite it here

        // Replace steps: remove old (cascades to fields/conditions/validators), add new
        _context.FormSteps.RemoveRange(existing.Steps);
        _context.SaveChanges();

        foreach (var step in schema.Steps)
        {
            step.Id           = 0;
            step.FormSchemaId = existing.Id;
            foreach (var field in step.Fields)
            {
                field.Id         = 0;
                field.FormStepId = null;
                foreach (var condition in field.Conditions) condition.Id = 0;
                foreach (var validator in field.Validators)
                {
                    validator.Id          = 0;
                    validator.FormFieldId = null;
                    foreach (var vc in validator.Conditions) vc.Id = 0;
                }
            }
            _context.FormSteps.Add(step);
        }

        _context.SaveChanges();
    }

    public (bool Success, string Error) LinkWorkflow(string schemaId, int? workflowId)
    {
        var schema = _context.FormSchemas.FirstOrDefault(s => s.Id == schemaId);
        if (schema is null) return (false, $"Form schema '{schemaId}' not found.");

        if (workflowId.HasValue && !_context.Workflows.Any(w => w.Id == workflowId))
            return (false, $"Workflow {workflowId} not found.");

        schema.WorkflowId = workflowId;
        _context.SaveChanges();
        return (true, string.Empty);
    }

    public void ArchiveSchema(string id)
    {
        var existing = _context.FormSchemas.FirstOrDefault(s => s.Id == id);
        if (existing is null) return;
        existing.IsArchived = true;
        existing.ArchivedAt = DateTimeOffset.UtcNow;
        _context.SaveChanges();
    }

    public bool UnarchiveSchema(string id)
    {
        var existing = _context.FormSchemas.FirstOrDefault(s => s.Id == id && s.IsArchived);
        if (existing is null) return false;
        existing.IsArchived = false;
        existing.ArchivedAt = null;
        _context.SaveChanges();
        return true;
    }

    public IEnumerable<object> GetAllSchemas() =>
        LoadQuery()
            .Where(s => !s.IsArchived)
            .Select(s => (object)new
            {
                s.Id,
                s.Title,
                s.Description,
                s.Version,
                s.WorkflowId,
                WorkflowName = s.Workflow != null ? s.Workflow.Name : null
            })
            .ToList();

    public IEnumerable<object> GetArchivedSchemas() =>
        LoadQuery()
            .Where(s => s.IsArchived)
            .Select(s => (object)new
            {
                s.Id,
                s.Title,
                s.Description,
                s.Version,
                s.ArchivedAt
            })
            .ToList();

    public FormSchema? GetSchema(string id) => LoadQuery().FirstOrDefault(s => s.Id == id);

    private IQueryable<FormSchema> LoadQuery() =>
        _context.FormSchemas
            .Include(s => s.Workflow)
            .Include(s => s.Steps)
                .ThenInclude(s => s.Fields)
                    .ThenInclude(f => f.Conditions)
            .Include(s => s.Steps)
                .ThenInclude(s => s.Fields)
                    .ThenInclude(f => f.Validators)
                        .ThenInclude(v => v.Conditions);

    private FormSchema? LoadFullSchema(string id) => LoadQuery().FirstOrDefault(s => s.Id == id);
}
