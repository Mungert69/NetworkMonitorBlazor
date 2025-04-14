using NetworkMonitorBlazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Server.Circuits; // Add this for CircuitHandler
using Microsoft.Extensions.Logging; // For ILogger

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = true;  // Enable detailed errors
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
});

// Register services with proper lifecycle management
builder.Services.AddScoped<ChatStateService>();
builder.Services.AddScoped<WebSocketService>();
builder.Services.AddScoped<AudioService>();
builder.Services.AddScoped<ILLMService, LLMService>();
builder.Services.AddScoped<LLMService>();
builder.Services.AddScoped<CircuitHandler, CircuitHandlerService>();

// Configure Kestrel ports
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5225); // HTTP
   
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub(options =>
{
    options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(30);
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(30);
});
app.MapFallbackToPage("/_Host");

app.Run();

// Circuit handler service
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