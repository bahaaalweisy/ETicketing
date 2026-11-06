using System.Text.Json.Serialization;
using ETicketing.Api.Middleware;
using ETicketing.Application;
using ETicketing.Infrastructure;
using ETicketing.Infrastructure.Persistence;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// CORS comes first so that error responses also carry the headers the browser needs to read them.
app.UseCors(FrontendCorsPolicy);
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHealthChecks("/health");

await app.Services.InitialiseDatabaseAsync();

await app.RunAsync();

/// <summary>
/// Exposes the entry point to WebApplicationFactory in the integration tests.
/// </summary>
public partial class Program;
