using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Cosmos;

var builder = FunctionsApplication.CreateBuilder(args);

// 🧠 Register the CosmosClient
builder.Services.AddSingleton(s =>
{
    var connectionString = Environment.GetEnvironmentVariable("COSMOS_DB_CONNECTION_STRING");
    return new CosmosClient(connectionString);
});

// Optional: add your own services here later
// builder.Services.AddSingleton<IUserService, UserService>();

// ⚡ This sets up the Function runtime with HTTP triggers, logging, DI, etc.
builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
