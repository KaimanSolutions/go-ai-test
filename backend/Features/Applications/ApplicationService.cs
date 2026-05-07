using FormBuilder.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Backend;

public sealed class ApplicationService
{
    private readonly FormBuilderDbContext _context;

    public ApplicationService(FormBuilderDbContext context) => _context = context;

    public IEnumerable<object> GetAll(string userEmail, string userRole)
    {
        var query = _context.Applications
            .Include(a => a.FormSchema)
            .Include(a => a.Client)
            .Include(a => a.Broker)
            .Include(a => a.Company)
            .Include(a => a.Network)
            .Include(a => a.Workflow)
            .Include(a => a.CurrentStage)
            .AsQueryable();

        if (userRole != UserRoles.Admin)
        {
            var user = _context.UserAccounts.FirstOrDefault(u => u.Email == userEmail);
            if (user is null) return [];

            query = user.Role == UserRoles.Client
                ? query.Where(a => a.ClientId == user.Id)
                : query.Where(a => a.BrokerId == user.Id);
        }

        return query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => (object)new
            {
                a.Id,
                a.PublicReference,
                a.FormSchemaId,
                FormTitle      = a.FormSchema.Title,
                a.FormSchemaVersion,
                a.Version,
                a.SubmittedAt,
                a.CreatedAt,
                a.UpdatedAt,
                Workflow     = a.Workflow     == null ? null : new { a.Workflow.Id,     a.Workflow.Name },
                CurrentStage = a.CurrentStage == null ? null : new { a.CurrentStage.Id, a.CurrentStage.Name, a.CurrentStage.Order, a.CurrentStage.IsInitial, a.CurrentStage.IsFinal },
                Client       = a.Client  == null ? null : new { a.Client.Id,  a.Client.FirstName,  a.Client.LastName,  a.Client.Email },
                Broker       = a.Broker  == null ? null : new { a.Broker.Id,  a.Broker.FirstName,  a.Broker.LastName,  a.Broker.Email },
                Company      = a.Company == null ? null : new { a.Company.Id, a.Company.Name },
                Network      = a.Network == null ? null : new { a.Network.Id, a.Network.Name }
            })
            .ToList();
    }

    public object? GetById(int id, string userEmail, string userRole)
    {
        var application = _context.Applications
            .Include(a => a.FormSchema)
                .ThenInclude(f => f.Steps)
                    .ThenInclude(s => s.Fields)
                        .ThenInclude(f => f.Conditions)
            .Include(a => a.FormSchema)
                .ThenInclude(f => f.Steps)
                    .ThenInclude(s => s.Fields)
                        .ThenInclude(f => f.Validators)
            .Include(a => a.Client)
            .Include(a => a.Broker)
            .Include(a => a.Company)
            .Include(a => a.Network)
            .Include(a => a.Workflow)
                .ThenInclude(w => w!.Stages.OrderBy(s => s.Order))
                    .ThenInclude(s => s.Tasks.OrderBy(t => t.Order))
            .Include(a => a.Workflow)
                .ThenInclude(w => w!.Stages)
                    .ThenInclude(s => s.TransitionsOut)
            .Include(a => a.CurrentStage)
            .FirstOrDefault(a => a.Id == id);

        if (application is null) return null;

        if (userRole != UserRoles.Admin)
        {
            var user = _context.UserAccounts.FirstOrDefault(u => u.Email == userEmail);
            if (user is null) return null;

            var isOwner = user.Role == UserRoles.Client
                ? application.ClientId == user.Id
                : application.BrokerId == user.Id;

            if (!isOwner) return null;
        }

        var wf = application.Workflow;

        return new
        {
            application.Id,
            application.PublicReference,
            application.FormSchemaId,
            application.FormSchemaVersion,
            application.Version,
            application.FormData,
            application.SubmittedAt,
            application.CreatedAt,
            application.UpdatedAt,
            application.WorkflowId,
            application.CurrentStageId,
            FormSchema = application.FormSchema is null ? null : new
            {
                application.FormSchema.Id,
                application.FormSchema.Title,
                application.FormSchema.Description,
                application.FormSchema.Version,
                Steps = application.FormSchema.Steps.Select(step => new
                {
                    step.Id,
                    step.Title,
                    step.Conditions,
                    Fields = step.Fields.Select(f => new
                    {
                        f.Id, f.Name, f.Label, f.Type, f.Required, f.ReadOnly,
                        f.Options, f.SubFields,
                        Conditions = f.Conditions.Select(c => new { c.Id, c.FieldName, c.Operator, c.Value }),
                        Validators = f.Validators.Select(v => new
                        {
                            v.Id, v.RuleType, v.Message, v.Value,
                            Conditions = v.Conditions.Select(c => new { c.Id, c.FieldName, c.Operator, c.Value })
                        })
                    })
                })
            },
            Workflow = wf is null ? null : new
            {
                wf.Id,
                wf.Name,
                wf.Description,
                Stages = wf.Stages.OrderBy(s => s.Order).Select(s => new
                {
                    s.Id, s.Name, s.Description, s.Order, s.IsInitial, s.IsFinal,
                    Tasks           = s.Tasks.OrderBy(t => t.Order)
                                        .Select(t => new { t.Id, t.Title, t.Description, t.Required, t.Order }),
                    TransitionsOut  = s.TransitionsOut
                                        .Select(t => new { t.Id, t.ToStageId, t.Label, t.Condition })
                })
            },
            CurrentStage = application.CurrentStage is null ? null : new
            {
                application.CurrentStage.Id,
                application.CurrentStage.Name,
                application.CurrentStage.Order,
                application.CurrentStage.IsInitial,
                application.CurrentStage.IsFinal
            },
            Client  = application.Client  is null ? null : new { application.Client.Id,  application.Client.FirstName,  application.Client.LastName,  application.Client.Email,  application.Client.Phone },
            Broker  = application.Broker  is null ? null : new { application.Broker.Id,  application.Broker.FirstName,  application.Broker.LastName,  application.Broker.Email,  application.Broker.Phone },
            Company = application.Company is null ? null : new { application.Company.Id, application.Company.Name, application.Company.FCANumber },
            Network = application.Network is null ? null : new { application.Network.Id, application.Network.Name, application.Network.FCANumber }
        };
    }

    public (bool Success, string Error, object? Application) Create(string userEmail, string userRole, CreateApplicationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FormSchemaId))
            return (false, "Form schema ID is required.", null);

        var form = _context.FormSchemas
            .Include(f => f.Workflow)
                .ThenInclude(w => w!.Stages)
            .FirstOrDefault(f => f.Id == request.FormSchemaId);
        if (form is null)
            return (false, $"Form schema '{request.FormSchemaId}' not found.", null);

        // Auto-assign the workflow linked to the form
        int? autoWorkflowId    = form.WorkflowId;
        int? autoStageId       = null;
        if (form.Workflow is not null)
        {
            var initialStage = form.Workflow.Stages
                .OrderBy(s => s.Order)
                .FirstOrDefault(s => s.IsInitial)
                ?? form.Workflow.Stages.OrderBy(s => s.Order).FirstOrDefault();
            autoStageId = initialStage?.Id;
        }

        var application = new Application
        {
            FormSchemaId      = form.Id,
            FormSchemaVersion = form.Version,
            Version           = 1,
            WorkflowId        = autoWorkflowId,
            CurrentStageId    = autoStageId,
            CreatedAt         = DateTime.UtcNow,
            UpdatedAt         = DateTime.UtcNow
        };

        // Admins (hardcoded, no UserAccount row) create unlinked applications
        if (userRole != UserRoles.Admin)
        {
            var user = _context.UserAccounts
                .Include(u => u.Company)
                    .ThenInclude(c => c!.ParentCompany)
                .FirstOrDefault(u => u.Email == userEmail);

            if (user is null)
                return (false, "Authenticated user account not found.", null);

            if (user.Role == UserRoles.Client)
            {
                application.ClientId = user.Id;
            }
            else
            {
                application.BrokerId  = user.Id;
                application.CompanyId = user.CompanyId;

                if (user.Company?.ParentCompany?.Type == CompanyType.Network)
                    application.NetworkId = user.Company.ParentCompany.Id;
            }
        }

        _context.Applications.Add(application);
        _context.SaveChanges();

        application.PublicReference = $"APP-{application.CreatedAt:yyyyMMdd}-{application.Id:D5}";
        _context.SaveChanges();

        return (true, string.Empty, new
        {
            application.Id,
            application.PublicReference,
            application.FormSchemaId,
            application.FormSchemaVersion,
            application.Version,
            application.WorkflowId,
            application.CurrentStageId,
            application.SubmittedAt,
            application.CreatedAt,
            application.UpdatedAt
        });
    }

    public (bool Success, string Error, object? Application) Update(int id, string userEmail, string userRole, UpdateApplicationRequest request)
    {
        var application = _context.Applications.FirstOrDefault(a => a.Id == id);
        if (application is null)
            return (false, $"Application {id} not found.", null);

        if (userRole != UserRoles.Admin)
        {
            var user = _context.UserAccounts.FirstOrDefault(u => u.Email == userEmail);
            if (user is null) return (false, "User not found.", null);

            var isOwner = user.Role == UserRoles.Client
                ? application.ClientId == user.Id
                : application.BrokerId == user.Id;

            if (!isOwner)
                return (false, "You do not have permission to update this application.", null);
        }

        if (request.FormData is not null)
            application.FormData = request.FormData;

        if (request.WorkflowId.HasValue)
        {
            var workflow = _context.Workflows
                .Include(w => w.Stages)
                .FirstOrDefault(w => w.Id == request.WorkflowId);

            if (workflow is null)
                return (false, $"Workflow {request.WorkflowId} not found.", null);

            application.WorkflowId = workflow.Id;

            // Auto-advance to the initial stage when a workflow is first linked
            var initialStage = workflow.Stages.OrderBy(s => s.Order).FirstOrDefault(s => s.IsInitial)
                               ?? workflow.Stages.OrderBy(s => s.Order).FirstOrDefault();

            application.CurrentStageId = initialStage?.Id;
        }

        if (request.CurrentStageId.HasValue)
        {
            // Validate stage belongs to the application's workflow
            if (application.WorkflowId is null)
                return (false, "Cannot set a stage without a linked workflow.", null);

            var stageExists = _context.WorkflowStages
                .Any(s => s.Id == request.CurrentStageId && s.WorkflowId == application.WorkflowId);

            if (!stageExists)
                return (false, "Stage does not belong to the application's workflow.", null);

            application.CurrentStageId = request.CurrentStageId;
        }

        if (request.Submit && application.SubmittedAt is null)
            application.SubmittedAt = DateTime.UtcNow;

        application.Version  += 1;
        application.UpdatedAt = DateTime.UtcNow;

        _context.SaveChanges();

        return (true, string.Empty, new
        {
            application.Id,
            application.PublicReference,
            application.FormSchemaId,
            application.FormSchemaVersion,
            application.Version,
            application.WorkflowId,
            application.CurrentStageId,
            application.SubmittedAt,
            application.UpdatedAt
        });
    }

    public (bool Success, string Error) Delete(int id)
    {
        var application = _context.Applications.FirstOrDefault(a => a.Id == id);
        if (application is null)
            return (false, $"Application {id} not found.");

        _context.Applications.Remove(application);
        _context.SaveChanges();
        return (true, string.Empty);
    }
}
