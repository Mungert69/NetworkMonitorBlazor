using NetworkMonitorChat;
using NetworkMonitor.Connection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Build configuration first
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var builder = WebApplication.CreateBuilder(args);

// Add the pre-built configuration to the builder
builder.Configuration.AddConfiguration(configuration);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = true;
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
    options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(60);
});

// Configure NetConnectConfig with proper file loading
builder.Services.AddSingleton<NetConnectConfig>(provider =>
{
    // Get the configuration from DI
    var config = provider.GetRequiredService<IConfiguration>();

    // For Blazor Server, use ContentRootPath instead of AppDataDirectory
    var dataPath = Path.Combine(builder.Environment.ContentRootPath, "Data");

    // Ensure directory exists
    Directory.CreateDirectory(dataPath);

    return new NetConnectConfig(config, dataPath);
});

builder.Services.AddScoped<ILLMService, LLMService>();

builder.Services.AddScoped<ChatStateService>(provider =>
    new ChatStateService(provider.GetRequiredService<IJSRuntime>()));

builder.Services.AddScoped<AudioService>(provider =>
    new AudioService(provider.GetRequiredService<IJSRuntime>(), provider.GetRequiredService<NetConnectConfig>()));

builder.Services.AddScoped<WebSocketService>(provider =>
    new WebSocketService(
        provider.GetRequiredService<ChatStateService>(),
        provider.GetRequiredService<IJSRuntime>(),
        provider.GetRequiredService<AudioService>(),
        provider.GetRequiredService<ILLMService>(),
        provider.GetRequiredService<NetConnectConfig>()));

builder.Services.AddScoped<CircuitHandler, CircuitHandlerService>();

// Configure Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(7860);
});

var app = builder.Build();

// Configure the HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

var configuredPathBase = builder.Configuration["PATH_BASE"]
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_PATHBASE");
if (!string.IsNullOrWhiteSpace(configuredPathBase))
{
    app.UsePathBase(configuredPathBase);
}

app.Use((context, next) =>
{
    if (!context.Request.PathBase.HasValue &&
        context.Request.Headers.TryGetValue("X-Forwarded-Prefix", out var forwardedPrefix) &&
        !string.IsNullOrWhiteSpace(forwardedPrefix))
    {
        context.Request.PathBase = forwardedPrefix.ToString();
    }

    return next();
});

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub(options =>
{
    options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(30);
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(30);
});
app.MapFallbackToPage("/_Host");

app.Run();

// CircuitHandlerService implementation remains the same
public class CircuitHandlerService : CircuitHandler
{
    private readonly ILogger<CircuitHandlerService> _logger;

    public CircuitHandlerService(ILogger<CircuitHandlerService> logger)
    {
        _logger = logger;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Circuit opened: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Connection down: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Circuit closed: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }
}
