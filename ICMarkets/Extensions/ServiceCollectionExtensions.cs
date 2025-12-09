using ICMarkets.Application.Mappings;
using ICMarkets.Application.Behaviors;
using ICMarkets.Constants;
using ICMarkets.Domain.Interfaces;
using ICMarkets.Infrastructure.Data;
using ICMarkets.Infrastructure.ExternalServices;
using ICMarkets.Infrastructure.Repositories;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using FluentValidation;
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace ICMarkets.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        // Validate connection string
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string 'DefaultConnection' is not configured. " +
                "Please ensure appsettings.json contains a valid ConnectionStrings:DefaultConnection value.");
        }
        
        // Add DbContext with SQLite
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        });
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBlockchainRepository, BlockchainRepository>();
        
        // Add HttpClient with configuration
        // Note: Polly retry logic is implemented directly in BlockCypherClient
        services.AddHttpClient<IBlockCypherClient, BlockCypherClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            MaxConnectionsPerServer = 10
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(5));
        
        return services;
    }
    
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile));
        
        // Add MediatR with validation pipeline behavior
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssemblyContaining<MappingProfile>();
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        
        // Add FluentValidation validators from the Application assembly
        services.AddValidatorsFromAssemblyContaining<MappingProfile>();
        
        return services;
    }
    
    public static IServiceCollection AddCachingServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddResponseCaching();
        return services;
    }
    
    public static IServiceCollection AddCompressionServices(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<GzipCompressionProvider>();
            options.Providers.Add<BrotliCompressionProvider>();
        });
        
        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });
        
        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });
        
        return services;
    }
    
    public static IServiceCollection AddRateLimitingServices(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            
            // Fixed window rate limiter for general API calls
            options.AddFixedWindowLimiter(RateLimitPolicies.Fixed, opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
            });
            
            // Stricter rate limiter for fetch operations
            options.AddFixedWindowLimiter(RateLimitPolicies.Fetch, opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 2;
            });
        });
        
        return services;
    }
    
    public static IServiceCollection AddHealthCheckServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string 'DefaultConnection' is required for health checks.");
        }
        
        services.AddHealthChecks()
            .AddSqlite(
                connectionString,
                name: "Database",
                timeout: TimeSpan.FromSeconds(3),
                tags: new[] { "db", "sqlite" })
            .AddUrlGroup(
                new Uri("https://api.blockcypher.com/v1/btc/main"),
                name: "BlockCypher API",
                timeout: TimeSpan.FromSeconds(5),
                tags: new[] { "api", "external" });
        
        return services;
    }
    
    public static IServiceCollection AddCorsServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var corsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "*" };
        
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicies.Default, policy =>
            {
                if (corsOrigins.Contains("*"))
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
                else
                {
                    policy.WithOrigins(corsOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
            });
        });
        
        return services;
    }
}
