using Amazon.CognitoIdentityProvider;
using Amazon.Runtime;
using IssueTrackerApi.Data;
using IssueTrackerApi.Middleware;
using IssueTrackerApi.Repositories;
using IssueTrackerApi.Repositories.Interfaces;
using IssueTrackerApi.Services;
using IssueTrackerApi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

var region = config["Aws:Region"]!;
var userPoolId = config["Aws:CognitoUserPoolId"]!;
var clientId = config["Aws:CognitoAppClientId"]!;
var issuer = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";

// Wire up JWT bearer so [Authorize] knows how to challenge unauthenticated requests.
// The actual token validation and User population is done by CognitoJwtMiddleware —
// this registration is needed purely so the authorization pipeline is active.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = issuer;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = clientId,
            ValidateLifetime = true,
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IAmazonCognitoIdentityProvider>(_ =>
    new AmazonCognitoIdentityProviderClient(
        new AnonymousAWSCredentials(),
        Amazon.RegionEndpoint.GetBySystemName(region)));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IIssueRepository, IssueRepository>();
builder.Services.AddScoped<IIssueService, IssueService>();
builder.Services.AddScoped<IS3Service, S3Service>();

builder.Services.AddControllers();

var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste your Cognito ID token here"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52_428_800;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API");
    c.RoutePrefix = "swagger";
});

app.UseCors();

// Must run before UseAuthentication so HttpContext.User is populated from our
// Cognito-validated token before ASP.NET's authorization checks fire.
app.UseMiddleware<CognitoJwtMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
