using FormBuilder.Backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FormBuilder.Backend;

public sealed class ChecklistService(FormBuilderDbContext context)
{
    public IEnumerable<object> GetAll(string? formSchemaId = null)
    {
        var query = context.ChecklistItems
            .Include(i => i.Conditions)
            .Include(i => i.FormSchema)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(formSchemaId))
            query = query.Where(i => i.FormSchemaId == formSchemaId);

        return query.OrderBy(i => i.ItemType).ThenBy(i => i.Name)
            .Select(i => (object)new
            {
                i.Id, i.Name, i.Description, i.ItemType,
                i.FormSchemaId, i.IsClientVisible, i.IsBrokerVisible, i.IsActive, i.CreatedAt,
                FormTitle  = i.FormSchema != null ? i.FormSchema.Title : null,
                Conditions = i.Conditions.Select(c => new { c.Id, c.FieldName, c.Operator, c.Value })
            }).ToList();
    }

    public (bool Success, string Error, int Id) Save(int? existingId, ChecklistItemRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return (false, "Name is required.", 0);

        if (req.ItemType is not ("Document" or "Information"))
            return (false, "Item type must be Document or Information.", 0);

        if (string.IsNullOrWhiteSpace(req.FormSchemaId))
            return (false, "A form must be selected.", 0);

        if (!context.FormSchemas.Any(s => s.Id == req.FormSchemaId))
            return (false, $"Form schema '{req.FormSchemaId}' not found.", 0);

        ChecklistItem item;

        if (existingId.HasValue)
        {
            item = context.ChecklistItems.Include(i => i.Conditions)
                .FirstOrDefault(i => i.Id == existingId.Value)!;
            if (item is null) return (false, "Item not found.", 0);
            context.ChecklistConditions.RemoveRange(item.Conditions);
        }
        else
        {
            item = new ChecklistItem { CreatedAt = DateTime.UtcNow };
            context.ChecklistItems.Add(item);
        }

        item.Name            = req.Name.Trim();
        item.Description     = req.Description?.Trim() ?? string.Empty;
        item.ItemType        = req.ItemType;
        item.FormSchemaId    = req.FormSchemaId;
        item.IsClientVisible = req.IsClientVisible;
        item.IsBrokerVisible = req.IsBrokerVisible;
        item.IsActive        = req.IsActive;
        item.Conditions      = req.Conditions
            .Where(c => !string.IsNullOrWhiteSpace(c.FieldName))
            .Select(c => new ChecklistCondition
            {
                FieldName = c.FieldName.Trim(),
                Operator  = c.Operator,
                Value     = c.Value.Trim()
            }).ToList();

        context.SaveChanges();
        return (true, string.Empty, item.Id);
    }

    public (bool Success, string Error) Delete(int id)
    {
        var item = context.ChecklistItems.Find(id);
        if (item is null) return (false, "Item not found.");
        context.ChecklistItems.Remove(item);
        context.SaveChanges();
        return (true, string.Empty);
    }

    // ── Application checklist ─────────────────────────────────────────────────

    public IEnumerable<object> GetForApplication(int applicationId, string userRole)
    {
        var query = context.ApplicationChecklistItems
            .Include(a => a.ChecklistItem)
            .Include(a => a.Comments)
            .Where(a => a.ApplicationId == applicationId)
            .AsQueryable();

        if (userRole == UserRoles.Client)
            query = query.Where(a => a.ChecklistItem.IsClientVisible);
        else if (userRole == UserRoles.Broker)
            query = query.Where(a => a.ChecklistItem.IsBrokerVisible);

        return query
            .OrderBy(a => a.ChecklistItem.ItemType)
            .ThenBy(a => a.ChecklistItem.Name)
            .Select(a => (object)new
            {
                a.Id, a.ApplicationId, a.ChecklistItemId, a.Status,
                a.TextResponse, a.DocumentName, a.CompletedAt, a.GeneratedAt,
                ItemName        = a.ChecklistItem.Name,
                ItemDescription = a.ChecklistItem.Description,
                ItemType        = a.ChecklistItem.ItemType,
                IsClientVisible = a.ChecklistItem.IsClientVisible,
                IsBrokerVisible = a.ChecklistItem.IsBrokerVisible,
                Comments = a.Comments.OrderBy(c => c.CreatedAt).Select(c => new
                {
                    c.Id, c.Comment, c.AuthorName, c.CreatedAt
                })
            }).ToList();
    }

    public (bool Success, string Error) GenerateForApplication(int applicationId)
    {
        var app = context.Applications.FirstOrDefault(a => a.Id == applicationId);
        if (app is null) return (false, "Application not found.");

        var templates = context.ChecklistItems
            .Include(i => i.Conditions)
            .Where(i => i.IsActive && i.FormSchemaId == app.FormSchemaId)
            .ToList();

        Dictionary<string, JsonElement> formValues = [];
        try
        {
            if (!string.IsNullOrWhiteSpace(app.FormData))
                formValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(app.FormData) ?? [];
        }
        catch { }

        var existingItems = context.ApplicationChecklistItems
            .Where(a => a.ApplicationId == applicationId)
            .ToList();

        // Upgrade any stale "Pending" rows created before the status rename
        foreach (var stale in existingItems.Where(a => a.Status == "Pending"))
            stale.Status = "Outstanding";

        var existingIds = existingItems.Select(a => a.ChecklistItemId).ToHashSet();

        foreach (var template in templates)
        {
            var applies = !template.Conditions.Any()
                || template.Conditions.All(c => EvaluateCondition(c, formValues));

            if (!applies || existingIds.Contains(template.Id)) continue;

            context.ApplicationChecklistItems.Add(new ApplicationChecklistItem
            {
                ApplicationId   = applicationId,
                ChecklistItemId = template.Id,
                Status          = "Outstanding",
                GeneratedAt     = DateTime.UtcNow
            });
        }

        context.SaveChanges();
        return (true, string.Empty);
    }

    public (bool Success, string Error) Respond(int appItemId, string text)
    {
        var item = context.ApplicationChecklistItems.Find(appItemId);
        if (item is null) return (false, "Item not found.");
        item.TextResponse = text.Trim();
        item.Status       = "Pending Review";
        item.CompletedAt  = DateTime.UtcNow;
        context.SaveChanges();
        return (true, string.Empty);
    }

    public (bool Success, string Error) RecordUpload(
        int appItemId, string docName, string docPath, string contentType)
    {
        var item = context.ApplicationChecklistItems.Find(appItemId);
        if (item is null) return (false, "Item not found.");
        item.DocumentName        = docName;
        item.DocumentPath        = docPath;
        item.DocumentContentType = contentType;
        item.Status              = "Pending Review";
        item.CompletedAt         = DateTime.UtcNow;
        context.SaveChanges();
        return (true, string.Empty);
    }

    private static readonly HashSet<string> ValidAdminStatuses =
        ["Approved", "More Info Needed", "Rejected"];

    public (bool Success, string Error) UpdateStatus(int appItemId, string newStatus)
    {
        if (!ValidAdminStatuses.Contains(newStatus))
            return (false, $"Invalid status '{newStatus}'.");

        var item = context.ApplicationChecklistItems.Find(appItemId);
        if (item is null) return (false, "Item not found.");
        item.Status = newStatus;
        context.SaveChanges();
        return (true, string.Empty);
    }

    public (bool Success, string Error) AddComment(int appItemId, string comment, string authorName)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return (false, "Comment cannot be empty.");

        if (!context.ApplicationChecklistItems.Any(a => a.Id == appItemId))
            return (false, "Item not found.");

        context.ApplicationChecklistComments.Add(new ApplicationChecklistComment
        {
            ApplicationChecklistItemId = appItemId,
            Comment    = comment.Trim(),
            AuthorName = authorName,
            CreatedAt  = DateTime.UtcNow
        });
        context.SaveChanges();
        return (true, string.Empty);
    }

    public ApplicationChecklistItem? GetAppItem(int appItemId)
        => context.ApplicationChecklistItems
            .Include(a => a.Comments)
            .FirstOrDefault(a => a.Id == appItemId);

    private static bool EvaluateCondition(
        ChecklistCondition c, Dictionary<string, JsonElement> formValues)
    {
        if (!formValues.TryGetValue(c.FieldName, out var el)) return false;
        var v = el.ValueKind == JsonValueKind.String ? el.GetString() ?? "" : el.ToString();
        return c.Operator switch
        {
            "equals"    => v.Equals(c.Value, StringComparison.OrdinalIgnoreCase),
            "notEquals" => !v.Equals(c.Value, StringComparison.OrdinalIgnoreCase),
            "contains"  => v.Contains(c.Value, StringComparison.OrdinalIgnoreCase),
            _           => true
        };
    }
}
