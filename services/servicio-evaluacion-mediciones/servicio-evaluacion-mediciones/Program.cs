using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using System.Net.Http.Headers;
using Azure.Storage.Queues;
using StackExchange.Redis;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

var redisConexion = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";
var servicioGestionPacientesUrl = Environment.GetEnvironmentVariable("SERVICIO_GESTION_PACIENTES")
    ?? throw new InvalidOperationException("Falta SERVICIO_GESTION_PACIENTES");

builder.Services.AddHttpClient("servicioGestionPacientesUrl", c =>
{
    c.BaseAddress = new Uri(servicioGestionPacientesUrl.TrimEnd('/') + "/");   // la barra final es necesaria
    c.Timeout = TimeSpan.FromSeconds(10);
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
    c.DefaultRequestHeaders.Add("x-functions-key", Environment.GetEnvironmentVariable("PACIENTES_FUNCTIONS_KEY"));
});

builder.Services.AddSingleton(_ => new QueueClient(
    Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
    "alertar",
    new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var opciones = ConfigurationOptions.Parse(redisConexion);
    opciones.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(opciones);
});

builder.Build().Run();
