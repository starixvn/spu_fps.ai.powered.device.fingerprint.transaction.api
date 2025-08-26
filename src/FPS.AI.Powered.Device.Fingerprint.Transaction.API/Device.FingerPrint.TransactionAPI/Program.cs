using Device.FingerPrint.TransactionAPI;
using Device.FingerPrint.TransactionAPI.JWT;
using Device.FingerPrint.TransactionAPI.Middleware;
using Device.FingerPrint.TransactionAPI.Queue;
using Device.FingerPrint.TransactionAPI.Services;
using Fanex.Logging;
using Fanex.Logging.Sentry;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using ONEbook.UM.AspNetCore;
using ONEbook.UM.Configurations;
using ONEbook.UM.Helpers;
using ONEbook.UM.Services;
using ONEbook.UM.Transport;
using Prometheus;
using Sentry;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Core.Implementations;
using StackExchange.Redis.Extensions.MsgPack;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs
#if DEBUG
builder.WebHost.UseUrls("http://*:5250");
#else
builder.WebHost.UseUrls("http://*:5000");
#endif

// Basic configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Essential services
var configuration = builder.Configuration;
var services = builder.Services;

var appSettings = new AppSettings(configuration);
services.AddSingleton<IAppSettings>(appSettings);
var gatewaySetting = new GatewaySetting(configuration);
services.AddSingleton<IGatewaySetting>(gatewaySetting);

RegisterServiceStatic(services, configuration);
RegisterRedisStatic(services, configuration);
RegisterMassTransitStatic(services, configuration);

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Transaction API",
        Version = "v1",
        Description = "This is transaction API",
        Contact = new OpenApiContact()
        {
            Name = "API Support",
            Email = "support@fps.ai"
        }
    });
});

builder.Services.AddCors(p => p.AddPolicy("corsapp", builder =>
{
    builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));


LogManager.SetDefaultLogCategory("FPS-API")
           .Use(
           [
                    new SentryLogging(new SentryEngineOptions
                    {
                        Dsn = new Dsn(appSettings.SentryDsn)
                    })
           ]);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Log startup information
LogService.Info($"TransactionAPI {appSettings.Environment} Start!");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

// Use the Prometheus middleware
app.UseMetricServer();
app.UseHttpMetrics();

MetricHttpService.PublicDataToGateway(gatewaySetting.GatewayInfo);

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<CustomJWTMiddleware>(appSettings.JWTEnable, appSettings.JWTPassword);
app.UseCors("corsapp");
app.MapControllers();

//var gatewaySetting = app.ApplicationServices.GetRequiredService<IGatewaySetting>();

//MetricHttpService.PublicDataToGateway(gatewaySetting.GatewayInfo);

// Health check endpoint
app.UseHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => true
});

if (app.Environment.IsDevelopment() || 
    app.Configuration.GetValue("Environment", "LOCAL") == "LOCAL" || 
    app.Configuration.GetValue("Environment", "UAT") == "UAT")
{
    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transaction API V1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.Run();

static void RegisterServiceStatic(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<Fanex.Logging.ILogger, Logger>();
    services.AddSingleton<ISubscriberService, SubscriberService>();
    services.AddSingleton<IMassTransitProducer, MassTransitProducer>();
    services.AddSingleton(new UMClientConfiguration().Initialize(configuration));
    services.AddSingleton<IUMCacheService, UMCacheService>();
    services.AddSingleton<IUMRestClient, UMRestClient>();
    services.AddSingleton<IMaintenanceService, MaintenanceService>();
}

static void RegisterRedisStatic(IServiceCollection services, IConfiguration configuration)
{
    var redisConfiguration = configuration.GetSection("Redis").Get<RedisConfiguration>();

    services.AddSingleton(redisConfiguration);

    services.AddSingleton<IRedisClient, RedisClient>();
    services.AddSingleton<IRedisConnectionPoolManager, RedisConnectionPoolManager>();
    services.AddSingleton<ISerializer, MsgPackObjectSerializer>();
    services.AddSingleton<IRedisService, RedisService>();
}

static void RegisterMassTransitStatic(IServiceCollection services, IConfiguration configuration)
{
    services.AddMassTransit(busConfigurator =>
    {
        busConfigurator.SetKebabCaseEndpointNameFormatter();

        busConfigurator.UsingRabbitMq((context, cfx) =>
        {
            cfx.ConfigureEndpoints(context);
        });
    });

    services.AddOptions<RabbitMqTransportOptions>()
        .Configure(options =>
        {
            options.Host = configuration.GetValue<string>("RabbitMQ:Host");
            options.Port = configuration.GetValue<ushort>("RabbitMQ:Port");
            options.User = configuration.GetValue<string>("RabbitMQ:UserName");
            options.Pass = configuration.GetValue<string>("RabbitMQ:Password");
            options.VHost = configuration.GetValue<string>("RabbitMQ:VirtualHost");
        });
}