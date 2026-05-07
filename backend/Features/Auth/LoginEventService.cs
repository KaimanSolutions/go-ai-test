using FormBuilder.Backend.Data;

namespace FormBuilder.Backend;

public sealed class LoginEventService(FormBuilderDbContext db)
{
    public void Record(int? userId, string userType, string? email, string? userAgent, string authMethod)
    {
        db.LoginEvents.Add(new LoginEvent
        {
            UserId       = userId,
            UserType     = userType,
            Email        = email,
            TimestampUtc = DateTimeOffset.UtcNow,
            DeviceType   = DetectDevice(userAgent),
            AuthMethod   = authMethod,
        });
        db.SaveChanges();
    }

    public (IEnumerable<object> Items, int Total) GetAll(
        int page, int pageSize, string? userType, string? deviceType, string? authMethod,
        DateTimeOffset? from, DateTimeOffset? to)
    {
        var q = db.LoginEvents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userType))   q = q.Where(e => e.UserType   == userType);
        if (!string.IsNullOrWhiteSpace(deviceType)) q = q.Where(e => e.DeviceType == deviceType);
        if (!string.IsNullOrWhiteSpace(authMethod)) q = q.Where(e => e.AuthMethod == authMethod);
        if (from.HasValue) q = q.Where(e => e.TimestampUtc >= from.Value);
        if (to.HasValue)   q = q.Where(e => e.TimestampUtc <= to.Value);

        var total = q.Count();
        var items = q.OrderByDescending(e => e.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => (object)new
            {
                e.Id, e.UserId, e.UserType, e.Email,
                e.TimestampUtc, e.DeviceType, e.AuthMethod
            })
            .ToList();

        return (items, total);
    }

    public object GetSummary()
    {
        var now    = DateTimeOffset.UtcNow;
        var day7   = now.AddDays(-7);
        var day30  = now.AddDays(-30);

        var all   = db.LoginEvents.ToList();
        var last30 = all.Where(e => e.TimestampUtc >= day30).ToList();
        var last7  = all.Where(e => e.TimestampUtc >= day7).ToList();

        return new
        {
            TotalAllTime  = all.Count,
            TotalLast30   = last30.Count,
            TotalLast7    = last7.Count,
            ByUserType    = last30.GroupBy(e => e.UserType)
                                  .ToDictionary(g => g.Key, g => g.Count()),
            ByDeviceType  = last30.GroupBy(e => e.DeviceType)
                                  .ToDictionary(g => g.Key, g => g.Count()),
            ByAuthMethod  = last30.GroupBy(e => e.AuthMethod)
                                  .ToDictionary(g => g.Key, g => g.Count()),
            DailyLast30   = Enumerable.Range(0, 30)
                                      .Select(i => now.AddDays(-29 + i).Date)
                                      .Select(d => new
                                      {
                                          Date  = d.ToString("yyyy-MM-dd"),
                                          Count = last30.Count(e => e.TimestampUtc.Date == d)
                                      })
                                      .ToList(),
        };
    }

    private static string DetectDevice(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua)) return "Unknown";
        ua = ua.ToLowerInvariant();
        return ua.Contains("mobile") || ua.Contains("android") ||
               ua.Contains("iphone") || ua.Contains("ipad")
            ? "Mobile" : "Desktop";
    }
}
