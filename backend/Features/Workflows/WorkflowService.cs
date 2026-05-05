using FormBuilder.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Backend;

public sealed class WorkflowService
{
    private readonly FormBuilderDbContext _context;

    public WorkflowService(FormBuilderDbContext context) => _context = context;

    public IEnumerable<object> GetAll() =>
        _context.Workflows
            .AsNoTracking()
            .Include(w => w.Stages)
            .OrderBy(w => w.Name)
            .Select(w => (object)new
            {
                w.Id,
                w.Name,
                w.Description,
                w.CreatedAt,
                StageCount = w.Stages.Count
            })
            .ToList();

    public object? GetDetail(int id)
    {
        var w = _context.Workflows
            .Include(w => w.Stages.OrderBy(s => s.Order))
                .ThenInclude(s => s.Tasks.OrderBy(t => t.Order))
            .Include(w => w.Stages)
                .ThenInclude(s => s.TransitionsOut)
                    .ThenInclude(t => t.ToStage)
            .FirstOrDefault(w => w.Id == id);

        if (w is null) return null;

        return new
        {
            w.Id,
            w.Name,
            w.Description,
            w.CreatedAt,
            Stages = w.Stages.OrderBy(s => s.Order).Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                s.Order,
                s.IsInitial,
                s.IsFinal,
                Tasks = s.Tasks.OrderBy(t => t.Order).Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.Required,
                    t.Order
                }).ToList(),
                TransitionsOut = s.TransitionsOut.Select(tr => new
                {
                    tr.Id,
                    tr.ToStageId,
                    ToStageName = tr.ToStage.Name,
                    tr.Label,
                    tr.Condition
                }).ToList()
            }).ToList()
        };
    }

    public (bool Success, string Error, int Id) Save(int? existingId, WorkflowSaveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "Workflow name is required.", 0);

        if (existingId.HasValue)
        {
            var existing = _context.Workflows.Find(existingId.Value);
            if (existing is null) return (false, "Workflow not found.", 0);
            _context.Workflows.Remove(existing);
            _context.SaveChanges();
        }

        var workflow = new Workflow
        {
            Name        = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt   = DateTime.UtcNow
        };

        // Map temp IDs → stage entities so transitions can reference them
        var stageMap = new Dictionary<string, WorkflowStage>();

        foreach (var sr in request.Stages)
        {
            var stage = new WorkflowStage
            {
                Name        = sr.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(sr.Description) ? null : sr.Description.Trim(),
                Order       = sr.Order,
                IsInitial   = sr.IsInitial,
                IsFinal     = sr.IsFinal,
                Tasks       = sr.Tasks.Select((t, i) => new WorkflowTask
                {
                    Title       = t.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(t.Description) ? null : t.Description.Trim(),
                    Required    = t.Required,
                    Order       = i
                }).ToList()
            };
            workflow.Stages.Add(stage);
            stageMap[sr.TempId] = stage;
        }

        _context.Workflows.Add(workflow);
        _context.SaveChanges(); // flushes stage IDs

        // Create transitions now that all stages have real DB IDs
        foreach (var sr in request.Stages)
        {
            if (!stageMap.TryGetValue(sr.TempId, out var fromStage)) continue;

            foreach (var tr in sr.TransitionsOut)
            {
                if (!stageMap.TryGetValue(tr.ToStageTempId, out var toStage)) continue;
                if (string.IsNullOrWhiteSpace(tr.Label)) continue;

                _context.WorkflowTransitions.Add(new WorkflowTransition
                {
                    WorkflowId  = workflow.Id,
                    FromStageId = fromStage.Id,
                    ToStageId   = toStage.Id,
                    Label       = tr.Label.Trim(),
                    Condition   = string.IsNullOrWhiteSpace(tr.Condition) ? null : tr.Condition.Trim()
                });
            }
        }

        _context.SaveChanges();
        return (true, string.Empty, workflow.Id);
    }

    public (bool Success, string Error) Delete(int id)
    {
        var workflow = _context.Workflows.Find(id);
        if (workflow is null) return (false, "Workflow not found.");
        _context.Workflows.Remove(workflow);
        _context.SaveChanges();
        return (true, string.Empty);
    }
}
