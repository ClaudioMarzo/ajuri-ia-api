using AjuriIA.API.Middleware;
using AjuriIA.API.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ProfileService>();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlerMiddleware>();
app.MapControllers();
app.MapOpenApi();

app.Run();

public partial class Program { }
