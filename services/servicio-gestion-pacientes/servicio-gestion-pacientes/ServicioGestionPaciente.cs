using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace servicio_gestion_pacientes;

public class ServicioGestionPaciente
{
    private readonly ILogger<ServicioGestionPaciente> _logger;
    private readonly HttpClient _fhir;

    public ServicioGestionPaciente(ILogger<ServicioGestionPaciente> logger, IHttpClientFactory factory)
    {
        _logger = logger;
        _fhir = factory.CreateClient("fhir");
    }

    [Function("GetAllOrganization")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        var resp = await _fhir.GetAsync($"Organization");
        var contenido = await resp.Content.ReadAsStringAsync();

        return new ContentResult
        {
            Content = contenido,
            ContentType = "application/fhir+json",
            StatusCode = (int)resp.StatusCode
        };
    }
}