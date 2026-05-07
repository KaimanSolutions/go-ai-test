namespace FormBuilder.Backend;

public sealed class BusinessRule
{
    public int        Id                { get; set; }
    public string     RuleReference     { get; set; } = string.Empty;
    public string     Name              { get; set; } = string.Empty;
    public string?    Description       { get; set; }
    public string?    ClientDescription { get; set; }
    public string?    BrokerDescription { get; set; }
    public bool       IsActive          { get; set; } = true;
    public bool       IsClientVisible   { get; set; } = false;
    public bool       IsBrokerVisible   { get; set; } = false;
    public string     DecisionType      { get; set; } = "Decline"; // "Decline" | "Refer"
    public string?    FormSchemaId      { get; set; }
    public FormSchema? FormSchema       { get; set; }
    public DateTime   CreatedAt         { get; set; } = DateTime.UtcNow;

    public List<RuleCondition> Conditions { get; set; } = [];
}

public sealed class RuleCondition
{
    public int     Id              { get; set; }
    public int     BusinessRuleId  { get; set; }
    public int     ConditionGroup  { get; set; } = 0;  // conditions in the same group are AND-ed; groups are OR-ed
    public string  LeftExpression  { get; set; } = string.Empty;
    public string  Operator        { get; set; } = "<=";
    public string  RightExpression { get; set; } = string.Empty;
    public string? FailMessage     { get; set; }
    public int     Order           { get; set; }
}

// ── Request / response models ─────────────────────────────────────────────────

public sealed class BusinessRuleSaveRequest
{
    public string  RuleReference     { get; set; } = string.Empty;
    public string  Name              { get; set; } = string.Empty;
    public string? Description       { get; set; }
    public string? ClientDescription { get; set; }
    public string? BrokerDescription { get; set; }
    public bool    IsActive          { get; set; } = true;
    public bool    IsClientVisible   { get; set; } = false;
    public bool    IsBrokerVisible   { get; set; } = false;
    public string  DecisionType      { get; set; } = "Decline"; // "Decline" | "Refer"
    public string? FormSchemaId      { get; set; }
    public List<RuleConditionRequest> Conditions { get; set; } = [];
}

public sealed class RuleConditionRequest
{
    public int     ConditionGroup  { get; set; } = 0;
    public string  LeftExpression  { get; set; } = string.Empty;
    public string  Operator        { get; set; } = "<=";
    public string  RightExpression { get; set; } = string.Empty;
    public string? FailMessage     { get; set; }
    public int     Order           { get; set; }
}

public sealed class EvaluateRulesRequest
{
    public string FormSchemaId   { get; set; } = string.Empty;
    public string FormData       { get; set; } = "{}";
    public int?   ApplicationId  { get; set; }
    public int?   CurrentStageId { get; set; }
}

public sealed class RuleEvaluationResult
{
    public int          RuleId            { get; set; }
    public string       RuleReference     { get; set; } = string.Empty;
    public string       RuleName          { get; set; } = string.Empty;
    public bool         Passed            { get; set; }
    public string       DecisionType      { get; set; } = "Decline";
    public bool         IsClientVisible   { get; set; }
    public bool         IsBrokerVisible   { get; set; }
    public string?      ClientDescription { get; set; }
    public string?      BrokerDescription { get; set; }
    public List<string> FailReasons       { get; set; } = [];
}
