using FormBuilder.Backend.Models;

namespace FormBuilder.Backend.Services;

public sealed class FormSchemaService
{
    private readonly Dictionary<string, FormSchema> _schemas = new();

    public FormSchemaService()
    {
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

        _schemas[sample.Id] = sample;
    }

    public void SaveSchema(FormSchema schema)
    {
        _schemas[schema.Id] = schema;
    }

    public IEnumerable<FormSchema> GetAllSchemas() => _schemas.Values;
}
