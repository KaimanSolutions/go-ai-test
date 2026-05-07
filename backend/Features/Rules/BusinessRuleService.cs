using FormBuilder.Backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FormBuilder.Backend;

public sealed class BusinessRuleService(FormBuilderDbContext context, ExpressionEvaluator evaluator)
{
    // ── List / detail ─────────────────────────────────────────────────────────

    public IEnumerable<object> GetAll(string? formSchemaId = null)
    {
        var query = context.BusinessRules
            .Include(r => r.Conditions)
            .Include(r => r.FormSchema)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(formSchemaId))
            query = query.Where(r => r.FormSchemaId == formSchemaId);

        return query.OrderBy(r => r.RuleReference).Select(r => (object)new
        {
            r.Id, r.RuleReference, r.Name, r.Description,
            r.ClientDescription, r.BrokerDescription,
            r.IsActive, r.IsClientVisible, r.IsBrokerVisible, r.DecisionType,
            r.FormSchemaId, r.CreatedAt,
            FormTitle  = r.FormSchema != null ? r.FormSchema.Title : null,
            Conditions = r.Conditions.OrderBy(c => c.Order).Select(c => new
            {
                c.Id, c.ConditionGroup, c.LeftExpression, c.Operator,
                c.RightExpression, c.FailMessage, c.Order
            })
        }).ToList();
    }

    public object? GetById(int id)
    {
        var rule = context.BusinessRules
            .Include(r => r.Conditions)
            .Include(r => r.FormSchema)
            .FirstOrDefault(r => r.Id == id);

        if (rule is null) return null;

        return new
        {
            rule.Id, rule.RuleReference, rule.Name, rule.Description,
            rule.ClientDescription, rule.BrokerDescription,
            rule.IsActive, rule.IsClientVisible, rule.IsBrokerVisible, rule.DecisionType,
            rule.FormSchemaId, rule.CreatedAt,
            FormTitle  = rule.FormSchema?.Title,
            Conditions = rule.Conditions.OrderBy(c => c.Order).Select(c => new
            {
                c.Id, c.ConditionGroup, c.LeftExpression, c.Operator,
                c.RightExpression, c.FailMessage, c.Order
            })
        };
    }

    // ── Save (create / update) ────────────────────────────────────────────────

    public (bool Success, string Error, int Id) Save(int? existingId, BusinessRuleSaveRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RuleReference))
            return (false, "Rule reference is required.", 0);

        if (string.IsNullOrWhiteSpace(req.Name))
            return (false, "Rule name is required.", 0);

        var duplicateRef = context.BusinessRules.Any(r =>
            r.RuleReference == req.RuleReference && r.Id != (existingId ?? 0));
        if (duplicateRef)
            return (false, $"Rule reference '{req.RuleReference}' is already in use.", 0);

        if (string.IsNullOrWhiteSpace(req.FormSchemaId))
            return (false, "A form must be selected for this rule.", 0);

        if (!context.FormSchemas.Any(s => s.Id == req.FormSchemaId))
            return (false, $"Form schema '{req.FormSchemaId}' not found.", 0);

        BusinessRule rule;

        if (existingId.HasValue)
        {
            rule = context.BusinessRules
                .Include(r => r.Conditions)
                .FirstOrDefault(r => r.Id == existingId.Value)!;
            if (rule is null) return (false, "Rule not found.", 0);
            context.RuleConditions.RemoveRange(rule.Conditions);
        }
        else
        {
            rule = new BusinessRule { CreatedAt = DateTime.UtcNow };
            context.BusinessRules.Add(rule);
        }

        rule.RuleReference     = req.RuleReference.Trim();
        rule.Name              = req.Name.Trim();
        rule.Description       = req.Description?.Trim();
        rule.ClientDescription = req.ClientDescription?.Trim();
        rule.BrokerDescription = req.BrokerDescription?.Trim();
        rule.IsActive          = req.IsActive;
        rule.IsClientVisible   = req.IsClientVisible;
        rule.IsBrokerVisible   = req.IsBrokerVisible;
        rule.DecisionType      = req.DecisionType is "Decline" or "Refer" ? req.DecisionType : "Decline";
        rule.FormSchemaId      = req.FormSchemaId;

        rule.Conditions = req.Conditions.Select((c, i) => new RuleCondition
        {
            ConditionGroup  = c.ConditionGroup,
            LeftExpression  = c.LeftExpression.Trim(),
            Operator        = c.Operator,
            RightExpression = c.RightExpression.Trim(),
            FailMessage     = c.FailMessage?.Trim(),
            Order           = i
        }).ToList();

        context.SaveChanges();
        return (true, string.Empty, rule.Id);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    public (bool Success, string Error) Delete(int id)
    {
        var rule = context.BusinessRules.Find(id);
        if (rule is null) return (false, "Rule not found.");
        context.BusinessRules.Remove(rule);
        context.SaveChanges();
        return (true, string.Empty);
    }

    // ── Evaluate (and optionally persist changed outcomes) ────────────────────

    public IEnumerable<RuleEvaluationResult> Evaluate(EvaluateRulesRequest req)
    {
        Dictionary<string, JsonElement> formValues;
        try
        {
            formValues = string.IsNullOrWhiteSpace(req.FormData)
                ? []
                : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(req.FormData) ?? [];
        }
        catch { formValues = []; }

        var rules = context.BusinessRules
            .Include(r => r.Conditions)
            .Where(r => r.IsActive && r.FormSchemaId == req.FormSchemaId)
            .OrderBy(r => r.RuleReference)
            .ToList();

        // Resolve stage name once (snapshot for history record)
        string? stageName = null;
        if (req.CurrentStageId.HasValue)
            stageName = context.WorkflowStages.Find(req.CurrentStageId.Value)?.Name;

        // Load latest persisted outcomes for this application so we can detect changes
        Dictionary<int, RuleOutcome> latestOutcomes = [];
        if (req.ApplicationId.HasValue)
        {
            latestOutcomes = context.RuleOutcomes
                .Where(o => o.ApplicationId == req.ApplicationId.Value)
                .GroupBy(o => o.BusinessRuleId)
                .Select(g => g.OrderByDescending(o => o.RecordedAt).First())
                .ToDictionary(o => o.BusinessRuleId);
        }

        var results = new List<RuleEvaluationResult>();

        foreach (var rule in rules)
        {
            var (rulePassed, failReasons) = RunRule(rule, formValues);

            // Persist a new outcome record when the result differs from the last known outcome
            if (req.ApplicationId.HasValue)
            {
                var hasExisting = latestOutcomes.TryGetValue(rule.Id, out var latest);
                if (!hasExisting || latest!.Passed != rulePassed)
                {
                    context.RuleOutcomes.Add(new RuleOutcome
                    {
                        ApplicationId  = req.ApplicationId.Value,
                        BusinessRuleId = rule.Id,
                        Passed         = rulePassed,
                        FailReasons    = JsonSerializer.Serialize(failReasons),
                        StageId        = req.CurrentStageId,
                        StageName      = stageName,
                        RecordedAt     = DateTime.UtcNow
                    });
                }
            }

            results.Add(new RuleEvaluationResult
            {
                RuleId            = rule.Id,
                RuleReference     = rule.RuleReference,
                RuleName          = rule.Name,
                Passed            = rulePassed,
                DecisionType      = rule.DecisionType,
                IsClientVisible   = rule.IsClientVisible,
                IsBrokerVisible   = rule.IsBrokerVisible,
                ClientDescription = rule.ClientDescription,
                BrokerDescription = rule.BrokerDescription,
                FailReasons       = rulePassed ? [] : failReasons
            });
        }

        if (req.ApplicationId.HasValue)
            context.SaveChanges();

        return results;
    }

    // ── Outcome history ────────────────────────────────────────────────────────

    public IEnumerable<object> GetOutcomes(int applicationId, string userRole)
    {
        var outcomes = context.RuleOutcomes
            .Include(o => o.BusinessRule)
            .Where(o => o.ApplicationId == applicationId)
            .OrderBy(o => o.BusinessRuleId)
            .ThenByDescending(o => o.RecordedAt)
            .ToList();

        // Filter by portal visibility (admins see everything)
        if (userRole == UserRoles.Client)
            outcomes = outcomes.Where(o => o.BusinessRule.IsClientVisible).ToList();
        else if (userRole == UserRoles.Broker)
            outcomes = outcomes.Where(o => o.BusinessRule.IsBrokerVisible).ToList();

        return outcomes
            .GroupBy(o => o.BusinessRuleId)
            .Select(g =>
            {
                var current = g.First();
                var rule    = current.BusinessRule;
                return (object)new
                {
                    RuleId            = rule.Id,
                    rule.RuleReference,
                    RuleName          = rule.Name,
                    rule.IsClientVisible,
                    rule.IsBrokerVisible,
                    rule.DecisionType,
                    rule.ClientDescription,
                    rule.BrokerDescription,
                    Current = new
                    {
                        current.Passed,
                        FailReasons = JsonSerializer.Deserialize<List<string>>(current.FailReasons) ?? [],
                        current.StageName,
                        current.RecordedAt
                    },
                    History = g.Skip(1).Select(o => new
                    {
                        o.Passed,
                        FailReasons = JsonSerializer.Deserialize<List<string>>(o.FailReasons) ?? [],
                        o.StageName,
                        o.RecordedAt
                    }).ToList()
                };
            }).ToList();
    }

    // ── Shared rule evaluation logic ──────────────────────────────────────────

    private (bool Passed, List<string> FailReasons) RunRule(
        BusinessRule rule, Dictionary<string, JsonElement> formValues)
    {
        var groups = rule.Conditions
            .OrderBy(c => c.ConditionGroup).ThenBy(c => c.Order)
            .GroupBy(c => c.ConditionGroup)
            .ToList();

        if (!groups.Any()) return (true, []);

        var allFailReasons = new List<string>();

        foreach (var group in groups)
        {
            var groupFails = new List<string>();

            foreach (var cond in group)
            {
                if (string.IsNullOrWhiteSpace(cond.LeftExpression)) continue;

                var left  = evaluator.Evaluate(cond.LeftExpression,  formValues);
                var right = evaluator.Evaluate(cond.RightExpression, formValues);
                if (left is null || right is null) continue;

                var passed = cond.Operator switch
                {
                    "==" => left == right,
                    "!=" => left != right,
                    "<"  => left <  right,
                    ">"  => left >  right,
                    "<=" => left <= right,
                    ">=" => left >= right,
                    _    => true
                };

                if (!passed)
                    groupFails.Add(cond.FailMessage
                        ?? $"{cond.LeftExpression} {cond.Operator} {cond.RightExpression} not satisfied");
            }

            if (groupFails.Count == 0) return (true, []); // short-circuit OR
            allFailReasons.AddRange(groupFails);
        }

        return (false, allFailReasons);
    }
}
