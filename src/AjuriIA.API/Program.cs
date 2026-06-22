using AjuriIA.API.Middleware;
using AjuriIA.API.Services;
using AjuriIA.API.Validators;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

const string LogTemplate =
    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .WriteTo.Console(outputTemplate: LogTemplate));

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
