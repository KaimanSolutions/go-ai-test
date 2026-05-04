namespace FormBuilder.Backend.Models;

public sealed class BrandingSettings
{
    public int Id { get; set; }
    public string PrimaryColor { get; set; } = "#0ea5e9";
    public string AccentColor { get; set; } = "#7c3aed";
    public string BackgroundColor { get; set; } = "#0f172a";
    public string SurfaceColor { get; set; } = "#111827";
    public string CardColor { get; set; } = "#1f2937";
    public string TextColor { get; set; } = "#e2e8f0";
    public string MutedTextColor { get; set; } = "#94a3b8";
    public string FontBody { get; set; } = "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif";
    public string FontHeading { get; set; } = "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif";
}
