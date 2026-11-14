using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace servicio_alertas;

public class ServicioAlertas
{
    private readonly ILogger<ServicioAlertas> _logger;
    private readonly HttpClient _pacientes;

    public ServicioAlertas(ILogger<ServicioAlertas> logger, IHttpClientFactory factory)
    {
        _logger = logger;
        _pacientes = factory.CreateClient("servicioGestionPacientesUrl");
    }

    [Function("EnviarAlerta")]
    public async Task Alertar([QueueTrigger("alertar")] string mensaje)
    {
        using var doc = JsonDocument.Parse(mensaje);
        var m = doc.RootElement;
        var paciente = m.GetProperty("pacienteId").GetString()!;

        // 1) Grupo del paciente (CareTeam con el mismo id que el Patient)
        using var rc = await _pacientes.GetAsync($"CareTeam/{Uri.EscapeDataString(paciente)}");
        if (rc.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("El paciente {Paciente} no tiene grupo (CareTeam); no hay a quién avisar", paciente);
            return;
        }
        rc.EnsureSuccessStatusCode();

        using var careTeam = JsonDocument.Parse(await rc.Content.ReadAsStringAsync());

        // 2) Email de cada miembro (Patient, Practitioner, RelatedPerson)
        var correos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (careTeam.RootElement.TryGetProperty("participant", out var participantes))
        {
            foreach (var p in participantes.EnumerateArray())
            {
                var referencia = p.GetProperty("member").GetProperty("reference").GetString()!;
                using var rm = await _pacientes.GetAsync(referencia);
                if (!rm.IsSuccessStatusCode) continue;

                using var miembro = JsonDocument.Parse(await rm.Content.ReadAsStringAsync());
                if (!miembro.RootElement.TryGetProperty("telecom", out var telecom)) continue;

                foreach (var t in telecom.EnumerateArray())
                {
                    if (t.TryGetProperty("system", out var s) && s.GetString() == "email"
                        && t.TryGetProperty("value", out var v) && !string.IsNullOrWhiteSpace(v.GetString()))
                        correos.Add(v.GetString()!);
                }
            }
        }

        if (correos.Count == 0)
        {
            // Ningún miembro tiene email: es un error (la cola reintenta y luego a poison)
            throw new InvalidOperationException($"Ningún miembro del grupo de {paciente} tiene email en telecom; alerta no enviada");
        }

        // 3) Un solo correo a todos; si falla se lanza la excepción y la cola reintenta
        var u = m.GetProperty("umbrales");
        var cuerpo =
            $"Alerta de salud para el paciente {paciente}\n\n" +
            $"{m.GetProperty("medicionesFueraDeRango").GetInt32()} mediciones fuera de rango en los últimos 3 minutos.\n" +
            $"Última medición ({m.GetProperty("fecha").GetString()}):\n" +
            $"  - SpO2: {m.GetProperty("spo2").GetDouble()} % (mínimo {u.GetProperty("spo2Min").GetDouble()})\n" +
            $"  - Frecuencia cardiaca: {m.GetProperty("frecuenciaCardiaca").GetDouble()} lpm " +
            $"(rango {u.GetProperty("fcMin").GetDouble()} - {u.GetProperty("fcMax").GetDouble()})\n";

        var from = Environment.GetEnvironmentVariable("SMTP_USER")!;
        using var correo = new MailMessage { From = new MailAddress(from), Subject = $"Alerta de salud: {paciente}", Body = cuerpo };
        foreach (var c in correos) correo.To.Add(c);

        using var smtp = new SmtpClient(
            Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com",
            int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587"))
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(from, (Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "").Replace(" ", ""))
        };
        await smtp.SendMailAsync(correo);

        _logger.LogInformation("Alerta de {Paciente} enviada a {Cantidad} destinatario(s)", paciente, correos.Count);
    }
}
