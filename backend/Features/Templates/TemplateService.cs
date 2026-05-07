using FormBuilder.Backend.Data;
using Mjml.Net;

namespace FormBuilder.Backend;

public sealed class TemplateService(FormBuilderDbContext context)
{
    private static readonly MjmlRenderer MjmlRenderer = new();

    // ── CRUD ──────────────────────────────────────────────────────────────────

    public IEnumerable<object> GetAll(string? type = null)
    {
        var query = context.Templates.AsQueryable();
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(t => t.TemplateType == type);

        return query.OrderBy(t => t.TemplateType).ThenBy(t => t.Name)
            .Select(t => (object)new
            {
                t.Id, t.Name, t.Description, t.TemplateType,
                t.Subject, t.IsActive, t.CreatedAt, t.UpdatedAt
            }).ToList();
    }

    public object? GetById(int id)
    {
        var t = context.Templates.Find(id);
        if (t is null) return null;
        return new
        {
            t.Id, t.Name, t.Description, t.TemplateType,
            t.Subject, t.Content, t.IsActive, t.CreatedAt, t.UpdatedAt
        };
    }

    public (bool Success, string Error, int Id) Save(int? existingId, TemplateSaveRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return (false, "Name is required.", 0);

        if (req.TemplateType is not ("Email" or "SMS" or "Document"))
            return (false, "Template type must be Email, SMS or Document.", 0);

        Template t;
        if (existingId.HasValue)
        {
            t = context.Templates.Find(existingId.Value)!;
            if (t is null) return (false, "Template not found.", 0);
        }
        else
        {
            t = new Template { CreatedAt = DateTime.UtcNow };
            context.Templates.Add(t);
        }

        t.Name         = req.Name.Trim();
        t.Description  = req.Description?.Trim() ?? string.Empty;
        t.TemplateType = req.TemplateType;
        t.Subject      = req.Subject?.Trim() ?? string.Empty;
        t.Content      = req.Content ?? string.Empty;
        t.IsActive     = req.IsActive;
        t.UpdatedAt    = DateTime.UtcNow;

        context.SaveChanges();
        return (true, string.Empty, t.Id);
    }

    public (bool Success, string Error) Delete(int id)
    {
        var t = context.Templates.Find(id);
        if (t is null) return (false, "Template not found.");
        context.Templates.Remove(t);
        context.SaveChanges();
        return (true, string.Empty);
    }

    // ── MJML compilation ──────────────────────────────────────────────────────

    public (bool Success, string Result) CompileMjml(string mjml)
    {
        if (string.IsNullOrWhiteSpace(mjml))
            return (false, "MJML content is empty.");

        try
        {
            var options = new MjmlOptions { Beautify = false };
            var result  = MjmlRenderer.Render(mjml, options);

            if (result.Errors.Count > 0)
            {
                var msgs = string.Join("; ", result.Errors.Select(e => e.Error));
                return (false, $"MJML errors: {msgs}");
            }

            return (true, result.Html ?? string.Empty);
        }
        catch (Exception ex)
        {
            return (false, $"Compilation failed: {ex.Message}");
        }
    }
}
