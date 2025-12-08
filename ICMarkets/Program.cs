using ICMarkets.Application.Mappings;
using ICMarkets.Domain.Interfaces;
using ICMarkets.Infrastructure.Data;
using ICMarkets.Infrastructure.Contexts;
using ICMarkets.Infrastructure.ExternalServices;
using ICMarkets.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/icmarkets-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Add OpenAPI
builder.Services.AddOpenApi();

// Add DbContext configurations for Read/Write separation
// Write DbContext - Primary database for write operations (Commands)
builder.Services.AddDbContext<WriteDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("WriteConnection"),
        sqliteOptions => sqliteOptions.CommandTimeout(30)));

// Read DbContext - Read replica for read operations (Queries)
builder.Services.AddDbContext<ReadDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("ReadConnection"),
        sqliteOptions => sqliteOptions.CommandTimeout(30)));

// Keep ApplicationDbContext for backwards compatibility and initialization
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqliteOptions => sqliteOptions.CommandTimeout(30)));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Add HttpClient with optimized settings
builder.Services.AddHttpClient<IBlockCypherClient, BlockCypherClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    MaxConnectionsPerServer = 10
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5));

// Add Repository and UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IBlockchainRepository, BlockchainRepository>();

// Add Memory Cache for performance
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Fixed window rate limiter for general API calls
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
    
    // Stricter rate limiter for fetch operations
    options.AddFixedWindowLimiter("fetch", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddSqlite(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Add CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "*" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure database is created (using default connection)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors("DefaultCorsPolicy");

// Enable response caching
app.UseResponseCaching();

// Enable rate limiting
app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

// Map Health Checks
app.MapHealthChecks("/health");

app.Run();
