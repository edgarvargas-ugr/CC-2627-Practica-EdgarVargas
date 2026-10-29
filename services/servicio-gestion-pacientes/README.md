# App de monitoreo y alertas diarias de mediciones personalizada de Salud - Servicio-Gestión-Pacientes

**Autor:** _Edgar Camilo Vargas Aldana_ · **Asignatura:** Cloud Computing: Fundamentos e Infraestructuras

---

## 1. Arquitectura

### Componentes

| Componente | Responsabilidad | Almacenamiento |
|---|---|---|
| `servicio-gestion-pacientes` | Gestiona pacientes, familiares y umbrales en C# | Servidor FHIR + base de datos |

## 2. Especificación de la API REST

Todas las rutas van bajo `/api/v1`. Se muestra una versión sintética de los endpoints principales.

### servicio-gestion-pacientes

El servicio debe enviar siempre el header `x-functions-key` con la clave puesta en el archivo secrets-host.json

| Verbo | Ruta | Descripción |
|---|---|---|
| POST | `/{recurso}` | Crear un `Patient`, `Practitioner`, `RelatedPerson` |
| PUT | `/{recurso}/{id}` | Actualizar el recurso |
| GET | `/{recurso}/{id}` | Consultar un recurso (también `CareTeam`, `Device` y `Goal`) |
| POST | `/Patient/{id}/related` | Asociar profesionales y familiares al paciente (crea un `CareTeam`) |
| POST | `/Patient/{id}/device` | Asociar un dispositivo al paciente (crea un `Device`) |
| POST | `/Patient/{id}/thresholds` | Definir umbrales personalizados (crea un `Goal`) |

Ejemplo: crear un paciente. El `id` y `identifier.value` deben coincidir y contener al menos una letra.

```json
POST /api/v1/Patient
x-functions-key: <clave>

{
  "resourceType": "Patient",
  "id": "DNI11111",
  "identifier": [
    {
      "type": {
        "coding": [
          { "system": "http://terminology.hl7.org/CodeSystem/v2-0203", "code": "NI" }
        ]
      },
      "system": "urn:app-monitoreo:identificacion",
      "value": "DNI11111"
    }
  ],
  "name": [ { "text": "Prueba CC Patient" } ]
}
```

Ejemplo: asociar profesionales y familiares (deben existir antes; `Person` no es válido).

```json
POST /api/v1/Patient/DNI11111/related

[
  { "reference": "Practitioner/DNI22222" },
  { "reference": "RelatedPerson/DNI44444" }
]
```

Ejemplo: asociar un dispositivo.

```json
POST /api/v1/Patient/DNI11111/device

{ "deviceId": "watch-p001" }
```

Ejemplo: definir umbrales personalizados.

```json
POST /api/v1/Patient/DNI11111/thresholds

{
  "spo2Min": 90,
  "heartRateMin": 50,
  "heartRateMax": 110,
  "condition": "COPD"
}
```

## 3. Gestión del proyecto
- **Estrategia de ramas:** `main` protegida; el trabajo se integra mediante *feature branches* (`feature/<servicio>-<descripcion>`) y Pull Requests.
- **Commits semánticos:** `feat:`, `fix:`, `docs:`, `refactor:`, con ámbito opcional, por ejemplo `feat(ingestion): valida lecturas fuera de rango`.
- **Tablero:** GitHub Projects con vistas agrupadas por Milestones.
- **Historias de usuario:** registradas como Issues con la etiqueta `user-story`, criterios de aceptación y checklist técnico.
- **Plantillas:** `.github/ISSUE_TEMPLATE/user_story.md` y `.github/PULL_REQUEST_TEMPLATE.md`.

## 4. Implementación

Primero se construye la imagen basado en el Dockerfile para ello se sugiere el siguiente comando

```docker build -t servicio-gestion-pacientes . ```

Se debe usar secrets-host-template.json y generar el propio secrets-host.json, esto con el fin de darle seguridad al servicio. Una vez ello se levanta el contenedor de la siguiente forma.

```docker run -d --name servicio-gestion-pacientes -p 8081:80 -e FHIR_BASE_URL=https://hapi.fhir.org/baseR4 -e AzureWebJobsSecretStorageType=Files -v "${PWD}\secrets-host.json:/azure-functions-host/Secrets/host.json" servicio-gestion-pacientes```

---