namespace FormBuilder.Backend;

public sealed class ApiRequestLog
{
    public int     Id             { get; set; }
    public string  Integration    { get; set; } = string.Empty;
    public string  Method         { get; set; } = string.Empty;
    public string  Endpoint       { get; set; } = string.Empty;
    public int?    StatusCode     { get; set; }
    public string? RequestHeaders { get; set; }
    public string? RequestBody    { get; set; }
    public string? ResponseBody   { get; set; }
    public long    DurationMs     { get; set; }
    public DateTime RequestedAt   { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage   { get; set; }
}
