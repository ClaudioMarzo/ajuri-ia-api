using AjuriIA.API.Middleware;
using AjuriIA.API.Services;
using AjuriIA.API.Validators;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ProfileService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<LLMOrchestratorService>();
builder.Services.AddScoped<ChatRequestValidator>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlerMiddleware>();
app.MapControllers();
app.MapOpenApi();

app.Run();

public partial class Program { }
