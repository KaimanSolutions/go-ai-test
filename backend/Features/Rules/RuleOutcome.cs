namespace FormBuilder.Backend;

public sealed class RuleOutcome
{
    public int          Id             { get; set; }
    public int          ApplicationId  { get; set; }
    public int          BusinessRuleId { get; set; }
    public BusinessRule BusinessRule   { get; set; } = null!;
    public bool         Passed         { get; set; }
    public string       FailReasons    { get; set; } = "[]"; // JSON-serialised List<string>
    public int?         StageId        { get; set; }
    public string?      StageName      { get; set; }         // snapshot at time of recording
    public DateTime     RecordedAt     { get; set; } = DateTime.UtcNow;
}
