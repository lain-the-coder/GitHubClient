using FluentValidation;
using GitHubClient.Configuration;
using GitHubClient.Middleware;
using GitHubClient.Models.Requests;
using GitHubClient.Services;
using GitHubClient.Validators;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("System.Net.Http", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console(
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
        retainedFileCountLimit: 30)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// --- Configuration ---
builder.Services.Configure<ExternalApisOptions>(
    builder.Configuration.GetSection(ExternalApisOptions.SectionName));

// --- Named HttpClients ---
builder.Services.AddHttpClient("AuthClient");
builder.Services.AddHttpClient("GitHubClient");

// --- Services ---
builder.Services.AddSingleton<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IGitHubService, GitHubService>();

// --- Validators ---
builder.Services.AddScoped<IValidator<GitHubQueryParameters>, GitHubQueryParametersValidator>();
builder.Services.AddScoped<IValidator<GitHubUserRequest>, GitHubRequestValidator>();

// --- Controllers & Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "GitHub API Client", Version = "v1" });
});

var app = builder.Build();

// --- Middleware Pipeline ---
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

/// <summary>
/// Partial class declaration to enable WebApplicationFactory in integration tests.
/// </summary>
public partial class Program { }