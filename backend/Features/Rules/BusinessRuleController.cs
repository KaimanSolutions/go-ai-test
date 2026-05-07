using FormBuilder.Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/rules")]
[Authorize]
public sealed class BusinessRuleController(BusinessRuleService service, FormBuilderDbContext db) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll([FromQuery] string? formSchemaId = null)
        => Ok(service.GetAll(formSchemaId));

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var rule = service.GetById(id);
        return rule is null ? NotFound() : Ok(rule);
    }

    [HttpPost]
    public IActionResult Create([FromBody] BusinessRuleSaveRequest req)
    {
        var (success, error, id) = service.Save(null, req);
        return success ? Ok(new { id }) : BadRequest(new { error });
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] BusinessRuleSaveRequest req)
    {
        var (success, error, _) = service.Save(id, req);
        return success ? Ok(new { id }) : BadRequest(new { error });
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var (success, error) = service.Delete(id);
        return success ? Ok() : NotFound(new { error });
    }

    [HttpPost("evaluate")]
    public IActionResult Evaluate([FromBody] EvaluateRulesRequest req)
        => Ok(service.Evaluate(req));

    [HttpGet("outcomes/{applicationId:int}")]
    public IActionResult GetOutcomes(int applicationId)
        => Ok(service.GetOutcomes(applicationId, CurrentUserRole));

    private string CurrentUserRole =>
        User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
        ?? User.FindFirst("role")?.Value
        ?? string.Empty;

    /// <summary>
    /// Seeds a help centre article explaining the business rules engine.
    /// Idempotent — skips creation if the article already exists.
    /// </summary>
    [HttpPost("seed-help")]
    [Authorize(Roles = UserRoles.Admin)]
    public IActionResult SeedHelpArticle()
    {
        const string title = "Setting Up Business Rules";

        if (db.HelpArticles.Any(a => a.Title == title))
            return Ok(new { message = "Help article already exists." });

        db.HelpArticles.Add(new HelpArticle
        {
            Title             = title,
            Category          = "Rules & Policy",
            ShowInAdminPortal = true,
            ShowInBrokerPortal  = false,
            ShowInCustomerPortal = false,
            CreatedAt         = DateTime.UtcNow,
            Content           = """
## What are Business Rules?

Business rules let you define policy checks that run automatically against submitted application data. Each rule has one or more **conditions** — all conditions must pass for the rule to pass. When a rule fails, the reason is shown on the application's submitted view alongside any client- or broker-facing descriptions you configure.

---

## Creating a Rule

Navigate to **Rules** in the admin sidebar and click **+ New Rule**.

| Field | Purpose |
|---|---|
| Rule reference | A unique code for the rule, e.g. `BR-001`. Used in audit trails. |
| Rule name | A short human-readable name, e.g. `Maximum LTV Check`. |
| Internal description | Notes visible only to admins about why the rule exists. |
| Client description | Message shown to the client when this rule fails. |
| Broker description | Message shown to the broker when this rule fails. |
| Applies to form | Scope the rule to a specific form, or leave blank to run against all forms. |
| Active | Toggle a rule off without deleting it. Inactive rules are never evaluated. |

---

## Conditions

Each condition compares a **left expression** against a **right expression** using an **operator**.

### Operators

| Operator | Meaning |
|---|---|
| `<=` | Left is less than or equal to right |
| `>=` | Left is greater than or equal to right |
| `<` | Left is strictly less than right |
| `>` | Left is strictly greater than right |
| `==` | Left equals right |
| `!=` | Left does not equal right |

If a field referenced in an expression has no value (e.g. the applicant left it blank), the condition is **skipped** rather than failing. This avoids false negatives on optional fields.

### Fail message

Optionally add a plain-English fail message per condition, e.g. `Loan to value ratio must not exceed 90%`. If left blank, the system generates a generic message from the expression.

---

## Expression Syntax

Expressions are arithmetic formulas that can reference form field values and aggregation functions.

### Field references

Wrap any form field name in curly braces:

```
{fieldName}
```

The field name must exactly match the **field key** set in the Form Builder (the camelCase name generated from the field label).

### Arithmetic

Standard operators are supported inside expressions:

```
{loanAmount} / {propertyValue} * 100
({annualIncome} + {partnerIncome}) * 4.5
{monthlyRepayment} * 12
```

Use parentheses to control precedence.

### Aggregation functions

When a form contains a **Repeater** field, use these functions to aggregate across all entries:

| Function | Syntax | Description |
|---|---|---|
| `SUM` | `SUM({repeater.subField})` | Total of a numeric sub-field across all entries |
| `AVG` | `AVG({repeater.subField})` | Average of a numeric sub-field |
| `COUNT` | `COUNT({repeater})` | Number of entries in the repeater |

The part before the dot is the repeater field name; the part after is the sub-field key.

---

## Expression Examples

### Age check
Ensure the applicant is at least 18 years old.

| Left | Op | Right | Fail message |
|---|---|---|---|
| `{applicantAge}` | `>=` | `18` | Applicant must be at least 18 years old |

### Maximum loan-to-value (LTV)
Reject applications where the loan exceeds 90% of the property value.

| Left | Op | Right | Fail message |
|---|---|---|---|
| `{loanAmount} / {propertyValue} * 100` | `<=` | `90` | LTV must not exceed 90% |

### Minimum income multiple
Check the loan does not exceed 4.5× the applicant's annual income.

| Left | Op | Right | Fail message |
|---|---|---|---|
| `{loanAmount}` | `<=` | `{annualIncome} * 4.5` | Loan exceeds maximum income multiple of 4.5× |

### Combined household income (repeater)
A form with a repeater field `applicants` containing a sub-field `annualIncome`:

| Left | Op | Right | Fail message |
|---|---|---|---|
| `SUM({applicants.annualIncome})` | `>=` | `20000` | Combined household income must be at least £20,000 |

### Stress-tested affordability
Monthly repayment at a stressed rate (e.g. +3%) must not exceed 40% of net monthly income.

| Left | Op | Right | Fail message |
|---|---|---|---|
| `{monthlyRepayment} * 1.03 / ({monthlyNetIncome} / 100)` | `<=` | `40` | Stressed repayment exceeds 40% of net monthly income |

### Maximum number of applicants
Limit a repeater to no more than 2 entries.

| Left | Op | Right | Fail message |
|---|---|---|---|
| `COUNT({applicants})` | `<=` | `2` | A maximum of 2 applicants is permitted |

### Cross-field comparison
Ensure the deposit is at least 10% of the property value (using field references on both sides).

| Left | Op | Right | Fail message |
|---|---|---|---|
| `{depositAmount}` | `>=` | `{propertyValue} * 0.1` | Deposit must be at least 10% of property value |

---

## Tips

- **Field names are case-sensitive.** Check the exact key in the Form Builder by hovering over a field — it is shown below the label input.
- **Use parentheses freely.** `({a} + {b}) / {c}` is clearer than relying on operator precedence.
- **Test before activating.** Create a rule as inactive first, submit a test application, then use the **Policy Rules** panel on the submitted view to verify results before switching it on.
- **Scoping rules to a form** avoids referencing fields that don't exist on other forms, which would cause conditions to be silently skipped.
"""
        });

        db.SaveChanges();
        return Ok(new { message = "Help article created successfully." });
    }
}
