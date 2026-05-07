using FormBuilder.Backend;
using FormBuilder.Backend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<FormBuilderDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<FormSchemaService>();
builder.Services.AddScoped<RuleEngineService>();
builder.Services.AddScoped<JwtAuthService>();
builder.Services.AddScoped<ExternalAuthService>();
builder.Services.AddScoped<BrandingSettingsService>();
builder.Services.AddScoped<HelpArticleService>();
builder.Services.AddScoped<UserAccountService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<IntegrationSettingsService>();
builder.Services.AddScoped<WorkflowService>();
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddSingleton<ExpressionEvaluator>();
builder.Services.AddScoped<BusinessRuleService>();
builder.Services.AddScoped<ChecklistService>();
builder.Services.AddScoped<TemplateService>();
builder.Services.AddTransient<ApiLoggingHandler>();
builder.Services.AddHttpClient<EpcService>()
    .AddHttpMessageHandler<ApiLoggingHandler>();
builder.Services.AddHttpClient<FcaLookupService>()
    .AddHttpMessageHandler<ApiLoggingHandler>();
builder.Services.AddHttpClient<CompaniesHouseService>()
    .AddHttpMessageHandler<ApiLoggingHandler>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"] ?? string.Empty;
var issuer = jwtSettings["Issuer"] ?? string.Empty;
var audience = jwtSettings["Audience"] ?? string.Empty;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
    };
})
.AddOpenIdConnect("SSO", options =>
{
    var oidcSettings = builder.Configuration.GetSection("OpenIdConnect");
    options.Authority = oidcSettings["Authority"] ?? string.Empty;
    options.ClientId = oidcSettings["ClientId"] ?? string.Empty;
    options.ClientSecret = oidcSettings["ClientSecret"] ?? string.Empty;
    options.ResponseType = oidcSettings["ResponseType"] ?? string.Empty;
    options.Scope.Clear();
    foreach (var scope in oidcSettings["Scope"]?.Split(' ') ?? Array.Empty<string>())
    {
        options.Scope.Add(scope);
    }
    options.SaveTokens = true;
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FormBuilderDbContext>();
    db.Database.Migrate();

    var brandingService = scope.ServiceProvider.GetRequiredService<BrandingSettingsService>();
    brandingService.EnsureDefaultSettings();

    var formService = scope.ServiceProvider.GetRequiredService<FormSchemaService>();
    formService.EnsureSampleData();

}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
