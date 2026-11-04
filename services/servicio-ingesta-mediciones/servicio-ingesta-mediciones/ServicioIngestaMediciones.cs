using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace servicio_ingesta_mediciones;

public class ServicioIngestaMediciones
{
    private readonly ILogger<ServicioIngestaMediciones> _logger;
    private readonly HttpClient _fhir;
    private readonly QueueClient _cola;

    private const double Spo2Min = 30, Spo2Max = 100;
    private const int FcMin = 20, FcMax = 250;

    public ServicioIngestaMediciones(ILogger<ServicioIngestaMediciones> logger, IHttpClientFactory factory, QueueClient cola)
    {
        _logger = logger;
        _fhir = factory.CreateClient("fhir");
        _cola = cola;
    }

    [Function("ConsultarMedicionesPaciente")]
    public async Task<IActionResult> GetConsultarMedicionesPaciente([HttpTrigger(AuthorizationLevel.Function, "get", Route = "Patient/{id}/mediciones")] HttpRequest req, string id)
    {
        var filtros = new List<string>();

        string? textoDesde = req.Query["desde"], textoHasta = req.Query["hasta"];
        DateTimeOffset? desde = null, hasta = null;

        if (!string.IsNullOrWhiteSpace(textoDesde))
        {
            if (!DateTimeOffset.TryParse(textoDesde, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d))
                return Responder(400, new { motivos = new[] { "desde debe ser formato 2026-11-01" } });
            desde = d;
            filtros.Add("date=ge" + Uri.EscapeDataString(d.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)));
        }

        if (!string.IsNullOrWhiteSpace(textoHasta))
        {
            if (!DateTimeOffset.TryParse(textoHasta, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var h))
                return Responder(400, new { motivos = new[] { "hasta debe tener el formato 2026-11-03)." } });
            if (textoHasta.Length <= 10) h = h.AddDays(1).AddSeconds(-1);   // solo fecha: incluye todo ese día
            hasta = h;
            filtros.Add("date=le" + Uri.EscapeDataString(h.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)));
        }

        if (desde > hasta)
            return Responder(400, new { motivos = new[] { "desde no puede ser posterior a hasta." } });

        var limite = int.TryParse(req.Query["limite"], out var l) && l is > 0 and <= 500 ? l : 100;
        filtros.Add($"_count={limite}");
        filtros.Add("_sort=-date");

        try
        {
            using var resp = await _fhir.GetAsync($"Observation?subject={Uri.EscapeDataString($"Patient/{id}")}&" + string.Join("&", filtros));
            var contenido = await resp.Content.ReadAsStringAsync();
            return new ContentResult
            {
                Content = contenido,
                ContentType = "application/fhir+json",
                StatusCode = (int)resp.StatusCode
            };
        }
        catch (Exception e) when (e is HttpRequestException)
        {
            return Responder(502, new { estado = "error_fhir", motivos = new[] { "No fue posible consultar fhir-service." } });
        }
    }

    [Function("RegistrarMedicionManual")]
    public Task<IActionResult> Manual(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mediciones/manual")] HttpRequest req) => Registrar(req, "manual");

    // Medición del dispositivo. En la nube esta función se sustituye por un trigger de Event Hubs
    [Function("RegistrarMedicionDispositivo")]
    public Task<IActionResult> Dispositivo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mediciones/dispositivo")] HttpRequest req,
        CancellationToken ct) => Registrar(req, "dispositivo");

    private async Task<IActionResult> Registrar(HttpRequest req, string origen)
    {
        JsonElement cuerpo = default;
        var motivos = new List<string>();
        try
        {
            using var doc = await JsonDocument.ParseAsync(req.Body);
            cuerpo = doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return Responder(502, new { estado = "error_fhir", motivos = new[] { "No fue posible guardar la medición en fhir-service." } });
        }

        string? pacienteId = null;
        double? spo2 = null;
        int? frecuencia = null;
        DateTimeOffset? fecha = null;

        if (cuerpo.ValueKind != JsonValueKind.Object)
            return Responder(502, new { estado = "error_fhir", motivos = new[] { "No fue posible guardar la medición en fhir-service." } });
        else
        {
            if (cuerpo.TryGetProperty("pacienteId", out var p) && p.ValueKind == JsonValueKind.String)
                pacienteId = p.GetString();
            if (cuerpo.TryGetProperty("spo2", out var s) && s.ValueKind == JsonValueKind.Number && s.TryGetDouble(out var sv))
                spo2 = sv;
            if (cuerpo.TryGetProperty("frecuenciaCardiaca", out var f) && f.ValueKind == JsonValueKind.Number && f.TryGetInt32(out var fv))
                frecuencia = fv;
            if (cuerpo.TryGetProperty("fecha", out var d) && d.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(d.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dv))
                fecha = dv;
        }


        if (string.IsNullOrWhiteSpace(pacienteId)) motivos.Add("pacienteId es obligatorio.");

        if (spo2 is null) motivos.Add("spo2 es obligatorio y debe ser numérico.");
        else if (spo2 < Spo2Min || spo2 > Spo2Max) motivos.Add($"spo2 fuera del rango plausible ({Spo2Min}-{Spo2Max} %).");

        if (frecuencia is null) motivos.Add("frecuenciaCardiaca es obligatoria y debe ser un entero.");
        else if (frecuencia < FcMin || frecuencia > FcMax) motivos.Add($"frecuenciaCardiaca fuera del rango plausible ({FcMin}-{FcMax} lpm).");

        if (fecha is null) motivos.Add("fecha es obligatoria y debe tener formato ISO 8601.");
        else if (fecha > DateTimeOffset.UtcNow.AddMinutes(5)) motivos.Add("fecha de medición en el futuro.");

        if (motivos.Count > 0)
        {
            return Responder(422, new { estado = "posiblemente_invalida", motivos });
        }

        // 3) Transformar a recursos FHIR y guardar en fhir-service
        try
        {
            using var existe = await _fhir.GetAsync($"Patient/{pacienteId}");
            if (existe.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Gone)
                return Responder(404, new { estado = "paciente_no_encontrado", motivos = new[] { "El paciente indicado no existe." } });
            if (!existe.IsSuccessStatusCode)
                throw new HttpRequestException($"fhir-service respondió {(int)existe.StatusCode} al consultar el paciente.");

            var efectiva = fecha!.Value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

            object Entrada(string loinc, string nombre, double valor, string unidad, string codigoUcum) => new
            {
                resource = new
                {
                    resourceType = "Observation",
                    status = "final",
                    meta = new { tag = new[] { new { system = "urn:cloud-health-monitor:origen", code = origen } } },
                    category = new[]
                    {
                        new { coding = new[] { new { system = "http://terminology.hl7.org/CodeSystem/observation-category", code = "vital-signs" } } }
                    },
                    code = new { coding = new[] { new { system = "http://loinc.org", code = loinc, display = nombre } } },
                    subject = new { reference = $"Patient/{pacienteId}" },
                    effectiveDateTime = efectiva,
                    valueQuantity = new { value = valor, unit = unidad, system = "http://unitsofmeasure.org", code = codigoUcum }
                },
                request = new { method = "POST", url = "Observation" }
            };

            var bundle = JsonSerializer.Serialize(new
            {
                resourceType = "Bundle",
                type = "transaction",
                entry = new[]
                {
                    Entrada("59408-5", "Oxygen saturation in Arterial blood by Pulse oximetry", spo2!.Value, "%", "%"),
                    Entrada("8867-4", "Heart rate", frecuencia!.Value, "beats/minute", "/min")
                }
            });

            using var contenido = new StringContent(bundle, Encoding.UTF8, "application/fhir+json");
            using var respuesta = await _fhir.PostAsync("", contenido);   // POST a la raíz de /fhir = transacción
            var texto = await respuesta.Content.ReadAsStringAsync();

            var ids = JsonNode.Parse(texto)!["entry"]!.AsArray().Select(e => e!["response"]!["location"]!.ToString().Split('/')[1]).ToList();

            try
            {
                await _cola.CreateIfNotExistsAsync();
                await _cola.SendMessageAsync(JsonSerializer.Serialize(new { pacienteId, observationIds = ids }));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "La medición se guardó pero no se pudo publicar para evaluación");
            }

            return Responder(201, new { estado = "guardada", recursosFhir = ids });
        }
        catch (Exception e) when (e is HttpRequestException)
        {
            return Responder(502, new { estado = "error_fhir", motivos = new[] { "No fue posible guardar la medición en fhir-service." } });
        }
    }

    private static ObjectResult Responder(int codigo, object cuerpo) => new(cuerpo) { StatusCode = codigo };
}