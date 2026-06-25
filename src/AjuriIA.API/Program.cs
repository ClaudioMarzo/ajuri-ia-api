using AjuriIA.API.Middleware;
using AjuriIA.API.Models;
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
var geminiOptions = builder.Configuration.GetSection("Gemini").Get<GeminiOptions>()
    ?? new GeminiOptions();
builder.Services.AddSingleton(geminiOptions);

// Retry curto: o fallback entre modelos (orquestrador) é quem resolve 503/sobrecarga.
builder.Services.AddHttpClient("gemini").AddStandardResilienceHandler(o =>
{
    o.Retry.MaxRetryAttempts = 1;
    o.Retry.Delay = TimeSpan.FromMilliseconds(500);
});

// Um ILLMService por modelo permitido; o orquestrador tenta o modelo pedido
// primeiro e cai para os demais (na ordem da lista) em caso de falha.
foreach (var m in geminiOptions.Models)
{
    var modelId = m.Id;
    builder.Services.AddSingleton<ILLMService>(sp => new GeminiService(
        sp.GetRequiredService<IHttpClientFactory>(),
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<ILogger<GeminiService>>(),
        modelId));
}
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

LogApiKey("GEMINI_API_KEY");

app.UseMiddleware<RequestEnrichmentMiddleware>();
app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0}ms)";
});
app.UseMiddleware<ExceptionHandlerMiddleware>();
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
