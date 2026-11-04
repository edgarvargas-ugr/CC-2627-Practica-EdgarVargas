# App de monitoreo y alertas diarias de mediciones personalizada de Salud - Servicio-Ingesta-Mediciones

**Autor:** _Edgar Camilo Vargas Aldana_ · **Asignatura:** Cloud Computing: Fundamentos e Infraestructuras

---

## 1. Arquitectura

### Componentes

| Componente | Responsabilidad | Almacenamiento |
|---|---|---|
| `servicio-ingesta-mediciones` | Recibe mediciones, valida su calidad, las convierte en recursos FHIR para guardarlos y publica un evento en C#| Servidor FHIR |

## 2. Especificación de la API REST

Todas las rutas van bajo `/api/v1`. Se muestra una versión sintética de los endpoints principales. 

### Dependencias
 
| Dependencia | Uso |
|---|---|
| `fhir-service` | Se consulta que el paciente exista y se guardan las `Observation` |
| Cola de mensajes (Azure Storage Queues; en local, Azurite) | Se publica la cola `mediciones-ingestadas` para que `servicio-evaluacion-mediciones` evalúe la medición |

Según la documentación de Microsoft este sería el endpoint para manejo local de Azurite

`AzureWebJobsStorage=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://host.docker.internal:11000/devstoreaccount1;QueueEndpoint=http://host.docker.internal:11001/devstoreaccount1;TableEndpoint=http://host.docker.internal:11002/devstoreaccount1;`

### servicio-ingesta-mediciones

| Verbo | Ruta | Descripción |
|---|---|---|---|
| POST | `/mediciones/manual` | Registra una medición enviada por un médico |
| POST | `/mediciones/dispositivo` | Registra una medición enviada por el dispositivo (en la nube se podría sustituir por un trigger de mensajería) |
| GET | `/Patient/{id}/mediciones` | Consulta las mediciones de un paciente, de la más reciente a la más antigua |

```json
POST /api/v1/mediciones/dispositivo 
POST /api/v1/mediciones/manual
{
	"pacienteId":"DNI11111",
	"spo2":100,
	"frecuenciaCardiaca":72,
	"fecha":"2026-10-31T15:01:00-05:00"
}
```

```
GET /api/Patient/DNI11111/mediciones?desde=2026-11-01&hasta=2026-11-03&limite=50
```
 
| Parámetro | Descripción |
|---|---|
| `desde` | Fecha inicial (por ejemplo `2026-11-01`). Opcional |
| `hasta` | Fecha final. Si solo se indica la fecha, incluye todo ese día. Opcional |
| `limite` | Máximo de resultados (por defecto 100, máximo 500). Opcional |

La consulta debe enviar el header `x-functions-key` con la clave puesta en el archivo `secrets-host.json`.

## 3. Gestión del proyecto
- **Estrategia de ramas:** `main` protegida; el trabajo se integra mediante *feature branches* (`feature/<servicio>-<descripcion>`) y Pull Requests.
- **Commits semánticos:** `feat:`, `fix:`, `docs:`, `refactor:`, con ámbito opcional, por ejemplo `feat(ingestion): valida lecturas fuera de rango`.
- **Tablero:** GitHub Projects con vistas agrupadas por Milestones.
- **Historias de usuario:** registradas como Issues con la etiqueta `user-story`, criterios de aceptación y checklist técnico.
- **Plantillas:** `.github/ISSUE_TEMPLATE/user_story.md` y `.github/PULL_REQUEST_TEMPLATE.md`.

## 4. Implementación

Primero se construye la imagen basado en el Dockerfile para ello se sugiere el siguiente comando

```docker build -t servicio-ingesta-mediciones . ```

Se debe usar secrets-host-template.json y generar el propio secrets-host.json, esto con el fin de darle seguridad al servicio. Una vez ello se levanta el contenedor de la siguiente forma.

```docker run -d --name servicio-ingesta-mediciones `
  -p 8082:80 `
  -e FHIR_BASE_URL=https://hapi.fhir.org/baseR4 `
  -e "AzureWebJobsStorage=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://host.docker.internal:11000/devstoreaccount1;QueueEndpoint=http://host.docker.internal:11001/devstoreaccount1;TableEndpoint=http://host.docker.internal:11002/devstoreaccount1;" `
  -e AzureWebJobsSecretStorageType=Files `
  -v "${PWD}\secrets-host.json:/azure-functions-host/Secrets/host.json" `
  servicio-ingesta-mediciones```

---