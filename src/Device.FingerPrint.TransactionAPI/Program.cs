using Device.FingerPrint.Model;
using Device.FingerPrint.TransactionAPI;
using Device.FingerPrint.TransactionAPI.Middleware;
using Device.FingerPrint.TransactionAPI.Queue;
using Device.FingerPrint.TransactionAPI.Services;
using Fanex.Logging;
using Fanex.Logging.Sentry;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ONEbook.UM.AspNetCore;
using ONEbook.UM.Configurations;
using ONEbook.UM.Helpers;
using ONEbook.UM.Services;
using ONEbook.UM.Transport;
using Prometheus;
using Sentry;

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

builder.Services.AddSingleton<IAppSettings, AppSettings>();
builder.Services.AddSingleton<IGatewaySetting, GatewaySetting>();
services.AddSingleton<IRedisMultiplexDbService, RedisMultiplexDbService>();

RegisterServiceStatic(services, configuration);
RegisterMassTransitStatic(services, configuration);

builder.Services.AddSwaggerGen();

builder.Services.AddCors(p => p.AddPolicy("corsapp", builder =>
{
    builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));



builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();

var app = builder.Build();

var appSettings = app.Services.GetRequiredService<IAppSettings>();
var gatewaySetting = app.Services.GetRequiredService<IGatewaySetting>();

LogManager.SetDefaultLogCategory("FPS-API")
           .Use(
           [
                    new SentryLogging(new SentryEngineOptions
                    {
                        Dsn = new Dsn(appSettings.SentryDsn)
                    })
           ]);

var redisService = app.Services.GetRequiredService<IRedisMultiplexDbService>();
var linkingService = app.Services.GetRequiredService<ILinkingService>();
redisService.ShadowDatabase.Execute("CONFIG", "SET", "notify-keyspace-events", "Ex");

var channel = $"__keyevent@{appSettings.RedisSettings.ShadowDatabase}__:expired";
var redisSubscriber = redisService.GetSubscriber();
#pragma warning disable S3257 // Declarations and initializations should be as concise as possible
redisSubscriber.Subscribe(channel, async (channel, message) => {
    await linkingService.LinkingOnExpired(message);
});
#pragma warning restore S3257 // Declarations and initializations should be as concise as possible

app.UseSwagger();
app.UseSwaggerUI();

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

// Health check endpoint
app.UseHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => true
});

app.Run();

static void RegisterServiceStatic(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<ILogger, Logger>();
    services.AddSingleton<ISubscriberService, SubscriberService>();
    services.AddSingleton<IMassTransitProducer, MassTransitProducer>();
    services.AddSingleton(new UMClientConfiguration().Initialize(configuration));
    services.AddSingleton<IUMCacheService, UMCacheService>();
    services.AddSingleton<IUMRestClient, UMRestClient>();
    services.AddSingleton<IMaintenanceService, MaintenanceService>();
    services.AddSingleton<ILinkingService, LinkingService>();
    services.AddSingleton<IDeviceService, DeviceService>();
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