using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using System.Net.Http.Headers;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

var fhirUrl = Environment.GetEnvironmentVariable("FHIR_BASE_URL")
    ?? throw new InvalidOperationException("Falta FHIR_BASE_URL");

builder.Services.AddHttpClient("fhir", c =>
{
    c.BaseAddress = new Uri(fhirUrl.TrimEnd('/') + "/");   // la barra final es necesaria
    c.Timeout = TimeSpan.FromSeconds(10);
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
});

builder.Build().Run();
