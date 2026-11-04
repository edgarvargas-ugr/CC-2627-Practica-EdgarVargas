using System.Globalization;
using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace servicio_evaluacion_mediciones;

public class ServicioEvaluacionMediciones
{
    private const double DefSpo2Min = 92, DefFcMin = 50, DefFcMax = 110;
    private const double DesviacionUrgentePct = 5;
    private const int MedicionesParaAlertar = 3;

    private static readonly TimeSpan VentanaEstado = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan VentanaAlerta = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan EnfriamientoAlerta = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TtlEstado = TimeSpan.FromHours(24);
    private static readonly TimeSpan TtlUmbral = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RetencionHistorial = TimeSpan.FromDays(30);

    private readonly ILogger<ServicioEvaluacionMediciones> _logger;
    private readonly IDatabase _redis;
    private readonly HttpClient _servicioGestionPacientesUrl;
    private readonly QueueClient _cola;

    public ServicioEvaluacionMediciones(ILogger<ServicioEvaluacionMediciones> logger, IConnectionMultiplexer redis,
        IHttpClientFactory factory, QueueClient cola)
    {
        _logger = logger;
        _redis = redis.GetDatabase();
        _servicioGestionPacientesUrl = factory.CreateClient("servicioGestionPacientesUrl");
        _cola = cola;
    }

    [Function("EvaluarMedicion")]
    public async Task Evaluar([QueueTrigger("mediciones-ingestadas")] string mensaje)
    {
        using var doc = JsonDocument.Parse(mensaje);
        var raiz = doc.RootElement;
        var paciente = raiz.GetProperty("pacienteId").GetString()!;

        double? spo2N = null, fcN = null;
        DateTimeOffset? fechaN = null;

        foreach (var idEl in raiz.GetProperty("observationIds").EnumerateArray())
        {
            using var r = await _servicioGestionPacientesUrl.GetAsync($"Observation/{Uri.EscapeDataString(idEl.GetString()!)}");
            r.EnsureSuccessStatusCode();
            using var obs = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
            var o = obs.RootElement;

            var codigo = o.GetProperty("code").GetProperty("coding")[0].GetProperty("code").GetString();
            var valor = o.GetProperty("valueQuantity").GetProperty("value").GetDouble();
            fechaN = DateTimeOffset.Parse(o.GetProperty("effectiveDateTime").GetString()!, CultureInfo.InvariantCulture);

            if (codigo == "59408-5") spo2N = valor;
            else if (codigo == "8867-4") fcN = valor;
        }

        if (spo2N is null || fcN is null || fechaN is null)
        {
            _logger.LogWarning("El mensaje no tiene SpO2, frecuencia cardiaca o fecha; se descarta");
            return;
        }
        var spo2 = spo2N.Value;
        var fc = fcN.Value;
        var fecha = fechaN.Value;

        // Buscar si tiene umbrales personalizados en el servicio de gestión de pacientes
        double spo2Min = DefSpo2Min, fcMin = DefFcMin, fcMax = DefFcMax;

        using var rg = await _servicioGestionPacientesUrl.GetAsync($"Goal/{Uri.EscapeDataString(paciente)}");
        if (rg.IsSuccessStatusCode)
        {
            using var goal = JsonDocument.Parse(await rg.Content.ReadAsStringAsync());
            if (goal.RootElement.TryGetProperty("target", out var targets))
            {
                foreach (var t in targets.EnumerateArray())
                {
                    var codigo = t.GetProperty("measure").GetProperty("coding")[0].GetProperty("code").GetString();
                    if (!t.TryGetProperty("detailRange", out var rango)) continue;

                    if (rango.TryGetProperty("low", out var low))
                    {
                        if (codigo == "59408-5") spo2Min = low.GetProperty("value").GetDouble();
                        else if (codigo == "8867-4") fcMin = low.GetProperty("value").GetDouble();
                    }
                    if (codigo == "8867-4" && rango.TryGetProperty("high", out var high))
                        fcMax = high.GetProperty("value").GetDouble();
                }
            }
        }
        else if (rg.StatusCode != HttpStatusCode.NotFound)
        {
            rg.EnsureSuccessStatusCode();
        }

        if (spo2 >= spo2Min && fc >= fcMin && fc <= fcMax) return;

        var clave = $"fuera:{paciente}";
        var ahora = fecha.ToUnixTimeSeconds();

        await _redis.SortedSetAddAsync(clave, $"{ahora}|{Guid.NewGuid():N}", ahora);
        await _redis.SortedSetRemoveRangeByScoreAsync(clave, double.NegativeInfinity, ahora - 180);
        await _redis.KeyExpireAsync(clave, TimeSpan.FromMinutes(10));
        var cantidad = await _redis.SortedSetLengthAsync(clave);

        // 5) 3 o más en 3 minutos -> cola "alertar"
        if (cantidad >= 3)
        {
            await _cola.CreateIfNotExistsAsync();
            await _cola.SendMessageAsync(JsonSerializer.Serialize(new
            {
                pacienteId = paciente,
                spo2,
                frecuenciaCardiaca = fc,
                fecha,
                umbrales = new { spo2Min, fcMin, fcMax },
                medicionesFueraDeRango = cantidad
            }));
        }
    }
}