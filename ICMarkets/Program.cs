using ICMarkets.Constants;
using ICMarkets.Extensions;
using ICMarkets.Infrastructure.Data;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Scalar.AspNetCore;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/icmarkets-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting ICMarkets Web API");

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // Add application layers
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddCachingServices();
    builder.Services.AddCompressionServices();
    builder.Services.AddRateLimitingServices();
    builder.Services.AddHealthCheckServices(builder.Configuration);
    builder.Services.AddCorsServices(builder.Configuration);

    var app = builder.Build();

    // Initialize database with migrations (production-safe)
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            // Use EnsureCreated for both environments (migrations can be added later)
            await context.Database.EnsureCreatedAsync();
            
            // Enable SQLite WAL mode for better concurrency and immediate write visibility
            if (context.Database.IsSqlite())
            {
                await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
                await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
            }
            
            Log.Information("Database initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An error occurred while initializing the database");
            throw;
        }
    }

    // Global exception handler (must be first)
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            
            var error = context.Features.Get<IExceptionHandlerFeature>();
            if (error != null)
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(error.Error, "Unhandled exception: {Message}", error.Error.Message);
                
                var problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    title = "An error occurred while processing your request",
                    status = StatusCodes.Status500InternalServerError,
                    detail = app.Environment.IsDevelopment() ? error.Error.Message : "An internal server error occurred",
                    traceId = context.TraceIdentifier
                };
                
                await context.Response.WriteAsJsonAsync(problemDetails);
            }
        });
    });

    // Configure the HTTP request pipeline
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("ICMarkets Blockchain API");
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    app.UseHttpsRedirection();
    app.UseCors(CorsPolicies.Default);
    app.UseResponseCompression();
    app.UseResponseCaching();
    app.UseRateLimiter();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks(ApiEndpoints.Health, new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    Log.Information("ICMarkets Web API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
