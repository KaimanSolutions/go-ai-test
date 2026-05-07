using FormBuilder.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Backend;

public sealed class NoteService(FormBuilderDbContext db)
{
    public List<ApplicationNote> GetForApplication(int applicationId, string role)
    {
        var q = db.ApplicationNotes
            .Where(n => n.ApplicationId == applicationId);

        if (role == UserRoles.Client)
            q = q.Where(n => n.IsClientVisible);
        else if (role == UserRoles.Broker)
            q = q.Where(n => n.IsBrokerVisible);

        return q.OrderByDescending(n => n.CreatedAt).ToList();
    }

    public (bool Success, string? Error, int Id) Add(
        int applicationId, AddNoteRequest req, string authorName, string authorRole, string? currentStageName)
    {
        if (string.IsNullOrWhiteSpace(req.Content))
            return (false, "Note content is required.", 0);

        bool clientVisible = authorRole switch
        {
            UserRoles.Client => true,
            UserRoles.Broker => false,
            _                => req.IsClientVisible   // Admin uses request value
        };
        bool brokerVisible = authorRole switch
        {
            UserRoles.Broker => true,
            UserRoles.Client => false,
            _                => req.IsBrokerVisible
        };

        var note = new ApplicationNote
        {
            ApplicationId   = applicationId,
            Content         = req.Content.Trim(),
            AuthorName      = authorName,
            AuthorRole      = authorRole,
            Stage           = currentStageName,
            Category        = string.IsNullOrWhiteSpace(req.Category) ? null : req.Category.Trim(),
            IsClientVisible = clientVisible,
            IsBrokerVisible = brokerVisible,
            CreatedAt       = DateTimeOffset.UtcNow,
        };

        db.ApplicationNotes.Add(note);
        db.SaveChanges();
        return (true, null, note.Id);
    }

    public IEnumerable<object> GetMyRecentNotes(string userEmail, string role, string currentUserName)
    {
        var user = db.UserAccounts.FirstOrDefault(u => u.Email == userEmail);
        if (user is null) return [];

        var apps = db.Applications
            .Where(a => a.SubmittedAt != null &&
                (role == UserRoles.Client ? a.ClientId == user.Id : a.BrokerId == user.Id))
            .Select(a => new { a.Id, a.PublicReference })
            .ToList();

        if (apps.Count == 0) return [];

        var appIds  = apps.Select(a => a.Id).ToList();
        var appRefs = apps.ToDictionary(a => a.Id, a => a.PublicReference);

        var notes = db.ApplicationNotes
            .Where(n => appIds.Contains(n.ApplicationId)
                && n.AuthorName != currentUserName
                && (role == UserRoles.Client ? n.IsClientVisible : n.IsBrokerVisible))
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .ToList();

        return notes.Select(n => (object)new
        {
            n.Id, n.ApplicationId,
            ApplicationReference = appRefs.GetValueOrDefault(n.ApplicationId),
            n.Content, n.AuthorName, n.AuthorRole, n.Stage, n.Category, n.CreatedAt,
        });
    }

    public (bool Success, string? Error) UpdateVisibility(int id, bool clientVisible, bool brokerVisible)
    {
        var note = db.ApplicationNotes.Find(id);
        if (note is null) return (false, "Note not found.");
        note.IsClientVisible = clientVisible;
        note.IsBrokerVisible = brokerVisible;
        db.SaveChanges();
        return (true, null);
    }
}
