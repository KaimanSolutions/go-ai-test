using FormBuilder.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<FormSchemaService>();
builder.Services.AddSingleton<RuleEngineService>();
builder.Services.AddSingleton<JwtAuthService>();
builder.Services.AddSingleton<ExternalAuthService>();
builder.Services.AddSingleton<BrandingSettingsService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"] ?? string.Empty;

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
    };
})
.AddOpenIdConnect("SSO", options =>
{
    var oidcSettings = builder.Configuration.GetSection("OpenIdConnect");
    options.Authority = oidcSettings["Authority"];
    options.ClientId = oidcSettings["ClientId"];
    options.ClientSecret = oidcSettings["ClientSecret"];
    options.ResponseType = oidcSettings["ResponseType"];
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
