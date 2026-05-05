using System.Diagnostics;
using System.Text;
using FormBuilder.Backend.Data;

namespace FormBuilder.Backend;

public sealed class ApiLoggingHandler : DelegatingHandler
{
    private readonly IServiceScopeFactory          _scopeFactory;
    private readonly ILogger<ApiLoggingHandler>    _logger;

    public ApiLoggingHandler(IServiceScopeFactory scopeFactory, ILogger<ApiLoggingHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var headers = new StringBuilder();
        foreach (var h in request.Headers)
        {
            var isAuth = h.Key.StartsWith("X-Auth-", StringComparison.OrdinalIgnoreCase)
                      || h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase);
            headers.AppendLine($"{h.Key}: {(isAuth ? "[redacted]" : string.Join(", ", h.Value))}");
        }

        string? requestBody = null;
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync();
            requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        var log = new ApiRequestLog
        {
            Integration    = ResolveIntegration(request.RequestUri),
            Method         = request.Method.Method,
            Endpoint       = request.RequestUri?.ToString() ?? string.Empty,
            RequestHeaders = headers.Length > 0 ? headers.ToString().TrimEnd() : null,
            RequestBody    = requestBody
        };

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            await response.Content.LoadIntoBufferAsync();
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            log.StatusCode   = (int)response.StatusCode;
            log.ResponseBody = responseBody.Length > 0 ? responseBody : null;
            log.DurationMs   = sw.ElapsedMilliseconds;

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            log.StatusCode   = null;
            log.ErrorMessage = ex.Message;
            log.DurationMs   = sw.ElapsedMilliseconds;
            throw;
        }
        finally
        {
            await PersistAsync(log);
        }
    }

    private static string ResolveIntegration(Uri? uri)
    {
        if (uri is null) return "Unknown";
        if (uri.Host.Contains("fca.org.uk",                       StringComparison.OrdinalIgnoreCase)) return "FCA Register";
        if (uri.Host.Contains("get-energy-performance-data",      StringComparison.OrdinalIgnoreCase)) return "EPB Data";
        return uri.Host;
    }

    private async Task PersistAsync(ApiRequestLog log)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FormBuilderDbContext>();
            db.ApiRequestLogs.Add(log);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to persist API request log: {Msg}", ex.Message);
        }
    }
}
