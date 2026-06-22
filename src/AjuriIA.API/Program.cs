using AjuriIA.API.Middleware;
using AjuriIA.API.Services;
using AjuriIA.API.Validators;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

const string DevLogTemplate =
    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration);

    if (ctx.HostingEnvironment.IsProduction())
        lc.WriteTo.Console(new CompactJsonFormatter());
    else
        lc.WriteTo.Console(outputTemplate: DevLogTemplate);
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Profiles
builder.Services.AddSingleton<ProfileService>();

// Validators
builder.Services.AddScoped<ChatRequestValidator>();

// LLM Services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ILLMService, ClaudeService>();
builder.Services.AddSingleton<ILLMService, OpenAIService>();
builder.Services.AddSingleton<ILLMService, GeminiService>();
builder.Services.AddScoped<LLMOrchestratorService>();

// CORS
var allowedOrigin = builder.Configuration["AllowedOrigin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// Startup validation
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation(
    "AjuriIA iniciando — Environment: {Environment}",
    app.Environment.EnvironmentName);

void LogApiKey(string key)
{
    if (string.IsNullOrEmpty(app.Configuration[key]))
        startupLogger.LogWarning("{Key}: ✗ não configurada — perfis que dependem desta chave usarão fallback", key);
    else
        startupLogger.LogInformation("{Key}: ✓", key);
}

LogApiKey("CLAUDE_API_KEY");
LogApiKey("OPENAI_API_KEY");
LogApiKey("GEMINI_API_KEY");

app.UseMiddleware<RequestEnrichmentMiddleware>();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0}ms)";
});
app.UseCors();
app.MapControllers();
app.MapOpenApi();
app.MapScalarApiReference(opt =>
{
    opt.Title = "AjuriIA API";
    opt.Theme = ScalarTheme.DeepSpace;
});

app.Run();

public partial class Program { }
