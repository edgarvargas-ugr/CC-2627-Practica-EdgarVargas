using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json.Nodes;

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

    [Function("ZCrearRecursoPersona")]
    public async Task<IActionResult> CrearRecursoPersona([HttpTrigger(AuthorizationLevel.Function, "post", Route ="{resource}")] HttpRequest req, string resource)
    {
        var body = await req.ReadFromJsonAsync<JsonObject>();
        HttpResponseMessage resp;

        var identifier = body?["identifier"]?[0]?["value"];

        if (identifier != null)
        {
            var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/fhir+json");
            resp = await _fhir.PutAsync($"{resource}/{identifier}", content);
        }
        else
        {
            resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\": \"El recurso no contiene un identificador válido.\"}", Encoding.UTF8, "application/fhir+json")
            };
        }

        return new ContentResult
        {
            Content = await resp.Content.ReadAsStringAsync(),
            ContentType = "application/fhir+json",
            StatusCode = (int)resp.StatusCode
        };
    }

    [Function("ZActualizarRecursoPersona")]
    public async Task<IActionResult> ActualizarRecursoPersona([HttpTrigger(AuthorizationLevel.Function, "put", Route = "{resource}/{id}")] HttpRequest req, string resource, string id)
    {
        var body = await req.ReadFromJsonAsync<JsonObject>();
        HttpResponseMessage resp;

        var identifier = body?["identifier"]?[0]?["value"];

        if (identifier != null)
        {
            if(id == identifier!.ToString())
            {
                var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/fhir+json");
                resp = await _fhir.PutAsync($"{resource}/{identifier}", content);
            }
            else
            {
                resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\": \"El identificador en la URL no coincide con el identificador en el cuerpo de la solicitud.\"}", Encoding.UTF8, "application/fhir+json")
                };
            }
        }
        else
        {
            resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\": \"El recurso no contiene un identificador válido.\"}", Encoding.UTF8, "application/fhir+json")
            };
        }

        return new ContentResult
        {
            Content = await resp.Content.ReadAsStringAsync(),
            ContentType = "application/fhir+json",
            StatusCode = (int)resp.StatusCode
        };
    }

    [Function("ZGetResourceID")]
    public async Task<IActionResult> GetResourceID([HttpTrigger(AuthorizationLevel.Function, "get", Route = "{resource}/{id}")] HttpRequest req, string resource, string id)
    {
        var resp = await _fhir.GetAsync($"{resource}/{id}");
        var contenido = await resp.Content.ReadAsStringAsync();

        return new ContentResult
        {
            Content = contenido,
            ContentType = "application/fhir+json",
            StatusCode = (int)resp.StatusCode
        };
    }

    [Function("AsociarGrupoPersonas")]
    public async Task<IActionResult> AsociarGrupoPersonas([HttpTrigger(AuthorizationLevel.Function, "post", Route = "Patient/{id}/related")] HttpRequest req, string id)
    {
        var referencias = await req.ReadFromJsonAsync<JsonArray>();


        var resp = await _fhir.GetAsync($"Patient/{id}");

        if (resp.StatusCode != HttpStatusCode.OK)
        {
            return new ContentResult
            {
                Content = await resp.Content.ReadAsStringAsync(),
                ContentType = "application/fhir+json",
                StatusCode = (int)resp.StatusCode
            };
        }

        var participantes = new JsonArray
        {
            new JsonObject { ["member"] = new JsonObject { ["reference"] = $"Patient/{id}" } }
        };

        foreach (var r in referencias!)
        {
            participantes.Add(new JsonObject
            {
                ["member"] = new JsonObject { ["reference"] = (string?)r!["reference"] }
            });
        }

        var careTeam = new JsonObject
        {
            ["resourceType"] = "CareTeam",
            ["id"] = id,                                                  // mismo id que el Patient
            ["status"] = "active",
            ["subject"] = new JsonObject { ["reference"] = $"Patient/{id}" },
            ["participant"] = participantes
        };

        var content = new StringContent(careTeam.ToJsonString(), Encoding.UTF8, "application/fhir+json");

        // PUT con id fijo: crea el Group si no existe (201) o lo reemplaza si existe (200)
        var respCareTeam = await _fhir.PutAsync($"CareTeam/{id}", content);

        return new ContentResult
        {
            Content = await respCareTeam.Content.ReadAsStringAsync(),
            ContentType = "application/fhir+json",
            StatusCode = (int)respCareTeam.StatusCode
        };
    }


    [Function("AsociarDispositivo")]
    public async Task<IActionResult> AsociarDispositivo([HttpTrigger(AuthorizationLevel.Function, "post", Route = "Patient/{id}/device")] HttpRequest req, string id)
    {
        var dto = await req.ReadFromJsonAsync<JsonObject>();


        var resp = await _fhir.GetAsync($"Patient/{id}");

        if (resp.StatusCode != HttpStatusCode.OK)
        {
            return new ContentResult
            {
                Content = await resp.Content.ReadAsStringAsync(),
                ContentType = "application/fhir+json",
                StatusCode = (int)resp.StatusCode
            };
        }

        var device = new JsonObject
        {
            ["resourceType"] = "Device",
            ["id"] = id,                                              // mismo id que el Patient
            ["identifier"] = new JsonArray(new JsonObject
            {
                ["system"] = "urn:app-monitoreo:dispositivo",
                ["value"] = (string?)dto!["deviceId"]                 // id del dispositivo físico
            }),
            ["patient"] = new JsonObject { ["reference"] = $"Patient/{id}" }
        };

        var content = new StringContent(device.ToJsonString(), Encoding.UTF8, "application/fhir+json");

        // PUT con id fijo: crea el Group si no existe (201) o lo reemplaza si existe (200)
        var respDevice = await _fhir.PutAsync($"Device/{id}", content);

        return new ContentResult
        {
            Content = await respDevice.Content.ReadAsStringAsync(),
            ContentType = "application/fhir+json",
            StatusCode = (int)respDevice.StatusCode
        };
    }

    [Function("AsociarUmbrales")]
    public async Task<IActionResult> AsociarUmbrales([HttpTrigger(AuthorizationLevel.Function, "post", Route = "Patient/{id}/thresholds")] HttpRequest req, string id)
    {
        var dto = await req.ReadFromJsonAsync<JsonObject>();


        var resp = await _fhir.GetAsync($"Patient/{id}");

        if (resp.StatusCode != HttpStatusCode.OK)
        {
            return new ContentResult
            {
                Content = await resp.Content.ReadAsStringAsync(),
                ContentType = "application/fhir+json",
                StatusCode = (int)resp.StatusCode
            };
        }

        var goal = new JsonObject
        {
            ["resourceType"] = "Goal",
            ["id"] = id,
            ["lifecycleStatus"] = "active",
            ["description"] = new JsonObject { ["text"] = "Umbrales de signos vitales" },
            ["subject"] = new JsonObject { ["reference"] = $"Patient/{id}" },
            ["note"] = new JsonArray(new JsonObject { ["text"] = (string?)dto!["condition"] }),
            ["target"] = new JsonArray(
             new JsonObject
             {
                 ["measure"] = new JsonObject
                 {
                     ["coding"] = new JsonArray(new JsonObject
                     { ["system"] = "http://loinc.org", ["code"] = "59408-5" })
                 },
                 ["detailRange"] = new JsonObject
                 {
                     ["low"] = new JsonObject
                     {
                         ["value"] = (int?)dto["spo2Min"],        // antes: heartRateMin
                         ["unit"] = "%",                          // antes: "/min"
                         ["system"] = "http://unitsofmeasure.org",
                         ["code"] = "%"
                     }
                 }
             },
              new JsonObject
              {
                  ["measure"] = new JsonObject
                  {
                      ["coding"] = new JsonArray(new JsonObject
                      { ["system"] = "http://loinc.org", ["code"] = "8867-4" })
                  },
                  ["detailRange"] = new JsonObject
                  {
                      ["low"] = new JsonObject
                      {
                          ["value"] = (int?)dto["heartRateMin"],
                          ["unit"] = "/min",
                          ["system"] = "http://unitsofmeasure.org",
                          ["code"] = "/min"
                      },
                      ["high"] = new JsonObject
                      {
                          ["value"] = (int?)dto["heartRateMax"],
                          ["unit"] = "/min",
                          ["system"] = "http://unitsofmeasure.org",
                          ["code"] = "/min"
                      }
                  }
              })
        };

        var content = new StringContent(goal.ToJsonString(), Encoding.UTF8, "application/fhir+json");

        // PUT con id fijo: crea el Group si no existe (201) o lo reemplaza si existe (200)
        var respGoal = await _fhir.PutAsync($"Goal/{id}", content);

        return new ContentResult
        {
            Content = await respGoal.Content.ReadAsStringAsync(),
            ContentType = "application/fhir+json",
            StatusCode = (int)respGoal.StatusCode
        };
    }

}