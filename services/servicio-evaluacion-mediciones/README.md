# App de monitoreo y alertas diarias de mediciones personalizada de Salud - Servicio-Evaluación-Mediciones

**Autor:** _Edgar Camilo Vargas Aldana_ · **Asignatura:** Cloud Computing: Fundamentos e Infraestructuras

---

## 1. Arquitectura

### Componentes

| Componente | Responsabilidad | Almacenamiento |
|---|---|---|
| `servicio-evaluacion-mediciones` | Evalúa cada medición frente a los umbrales del paciente (o los umbrales por defecto) y, si se superan 3 mediciones fuera de rango en 3 minutos, publica una alerta; en C# | Caché (Redis) |

## 2. Especificación de la mensajería

Este servicio **no expone una API REST**: se activa por mensajes de cola (Azure Functions con `QueueTrigger`). Todas las interacciones se hacen a través de colas.

### Dependencias

| Dependencia | Uso |
|---|---|
| Cola de mensajes (Azure Storage Queues; en local, Azurite) | Se consume `mediciones-ingestadas` y se publica en `alertar` |
| `servicio-gestion-pacientes` | Se consultan las `Observation` de cada medición (`Observation/{id}`) y los umbrales personalizados del paciente (`Goal/{pacienteId}`) |
| Redis | Lleva el conteo de mediciones fuera de rango por paciente dentro de la ventana de tiempo |

Según la documentación de Microsoft este sería el endpoint para manejo local de Azurite

`AzureWebJobsStorage=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://host.docker.internal:11000/devstoreaccount1;QueueEndpoint=http://host.docker.internal:11001/devstoreaccount1;TableEndpoint=http://host.docker.internal:11002/devstoreaccount1;`

### servicio-evaluacion-mediciones

| Cola | Sentido | Descripción |
|---|---|---|
| `mediciones-ingestadas` | Entrada (trigger) | Mensaje publicado por `servicio-ingesta-mediciones` por cada medición registrada |
| `alertar` | Salida | Alerta publicada cuando el paciente acumula 3 o más mediciones fuera de rango en 3 minutos |

Mensaje de entrada (`mediciones-ingestadas`):

```json
{
	"pacienteId": "DNI11111",
	"observationIds": ["obs-spo2-1", "obs-fc-1"]
}
```

Mensaje de salida (`alertar`):

```json
{
	"pacienteId": "DNI11111",
	"spo2": 88,
	"frecuenciaCardiaca": 125,
	"fecha": "2026-10-31T15:01:00-05:00",
	"umbrales": { "spo2Min": 92, "fcMin": 50, "fcMax": 110 },
	"medicionesFueraDeRango": 3
}
```

### Flujo de evaluación

1. Se recibe el mensaje de `mediciones-ingestadas` y se obtienen sus `Observation` (SpO2 `59408-5` y frecuencia cardiaca `8867-4`). Si falta alguna o la fecha, el mensaje se descarta.
2. Se consultan los umbrales personalizados del paciente (`Goal`). Si no existen, se usan los valores por defecto.
3. Si la medición está dentro de los umbrales, termina el proceso.
4. Si está fuera de rango, se registra en Redis (`fuera:{pacienteId}`) y se descartan las anteriores a 3 minutos.
5. Si hay 3 o más mediciones fuera de rango en la ventana, se publica el mensaje en la cola `alertar`.

### Umbrales y parámetros

| Parámetro | Valor |
|---|---|
| SpO2 mínima por defecto | 92 |
| Frecuencia cardiaca mínima / máxima por defecto | 50 / 110 |
| Mediciones fuera de rango para alertar | 3 |
| Ventana de evaluación | 3 minutos |

### Configuración

| Variable | Descripción |
|---|---|
| `AzureWebJobsStorage` | Conexión al Storage / Azurite donde viven las colas |
| `REDIS_CONNECTION` | Conexión a Redis (por defecto `localhost:6379`) |
| `SERVICIO_GESTION_PACIENTES` | URL base del servicio de gestión de pacientes |
| `PACIENTES_FUNCTIONS_KEY` | Clave enviada en el header `x-functions-key` hacia gestión de pacientes |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Opcional. Habilita la telemetría |

## 3. Gestión del proyecto
- **Estrategia de ramas:** `main` protegida; el trabajo se integra mediante *feature branches* (`feature/<servicio>-<descripcion>`) y Pull Requests.
- **Commits semánticos:** `feat:`, `fix:`, `docs:`, `refactor:`, con ámbito opcional, por ejemplo `feat(ingestion): valida lecturas fuera de rango`.
- **Tablero:** GitHub Projects con vistas agrupadas por Milestones.
- **Historias de usuario:** registradas como Issues con la etiqueta `user-story`, criterios de aceptación y checklist técnico.
- **Plantillas:** `.github/ISSUE_TEMPLATE/user_story.md` y `.github/PULL_REQUEST_TEMPLATE.md`.

## 4. Implementación

Primero se construye la imagen basado en el Dockerfile para ello se sugiere el siguiente comando

```docker build -t servicio-evaluacion-mediciones . ```

Se debe usar secrets-host-template.json y generar el propio secrets-host.json, esto con el fin de darle seguridad al servicio. Una vez ello se levanta el contenedor de la siguiente forma.

```docker run -d --name servicio-evaluacion-mediciones `
  -p 8086:8080 `
  -e "AzureWebJobsStorage=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://host.docker.internal:11000/devstoreaccount1;QueueEndpoint=http://host.docker.internal:11001/devstoreaccount1;TableEndpoint=http://host.docker.internal:11002/devstoreaccount1;" `
  -e SERVICIO_GESTION_PACIENTES=http://host.docker.internal:8081/api/v1/ `
  -e REDIS_CONNECTION=host.docker.internal:6379 `
  -e PACIENTES_FUNCTIONS_KEY=clave-master-cc-ugr `
  -e AzureWebJobsSecretStorageType=Files `
  -v "${PWD}\secrets-host.json:/azure-functions-host/Secrets/host.json" `
  servicio-evaluacion-mediciones```

---
