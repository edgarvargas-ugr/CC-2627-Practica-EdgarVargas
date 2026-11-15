# App de monitoreo y alertas diarias de mediciones personalizada de Salud - Servicio-Alertas

**Autor:** _Edgar Camilo Vargas Aldana_ · **Asignatura:** Cloud Computing: Fundamentos e Infraestructuras

---

## 1. Arquitectura

### Componentes

| Componente | Responsabilidad | Almacenamiento |
|---|---|---|
| `servicio-alertas` | Recibe las alertas publicadas por `servicio-evaluacion-mediciones`, busca el grupo (`CareTeam`) del paciente y envía por email el aviso a los miembros que tengan correo registrado; en C# | Ninguno (sin estado) |

## 2. Especificación de la mensajería

Este servicio **no expone una API REST**: se activa por mensajes de cola (Azure Functions con `QueueTrigger`). Todas las interacciones se hacen a través de colas.

### Dependencias

| Dependencia | Uso |
|---|---|
| Cola de mensajes (Azure Storage Queues; en local, Azurite) | Se consume `alertar` |
| `servicio-gestion-pacientes` | Se consulta el grupo del paciente (`CareTeam/{pacienteId}`) y cada uno de sus miembros (`Patient`, `Practitioner`, `RelatedPerson`) para obtener su email |
| Servidor SMTP (Gmail, gratuito) | Envío del correo de alerta |

Según la documentación de Microsoft este sería el endpoint para manejo local de Azurite

`AzureWebJobsStorage=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://host.docker.internal:11000/devstoreaccount1;QueueEndpoint=http://host.docker.internal:11001/devstoreaccount1;TableEndpoint=http://host.docker.internal:11002/devstoreaccount1;`

### servicio-alertas

| Cola | Sentido | Descripción |
|---|---|---|
| `alertar` | Entrada (trigger) | Mensaje publicado por `servicio-evaluacion-mediciones` cuando el paciente acumula 3 o más mediciones fuera de rango en 3 minutos |

Mensaje de entrada (`alertar`):

```json
{
	"pacienteId": "DNI11111",
	"spo2": 62,
	"frecuenciaCardiaca": 72,
	"fecha": "2026-11-10T20:28:00+00:00",
	"umbrales": { "spo2Min": 90, "fcMin": 50, "fcMax": 110 },
	"medicionesFueraDeRango": 3
}
```

Los miembros del grupo deben tener el email registrado en `telecom`, por ejemplo:

```json
{
	"resourceType": "RelatedPerson",
	"id": "DNI44444",
	"telecom": [ { "system": "email", "value": "familiar@correo.com", "use": "work" } ]
}
```

### Flujo de alertamiento

1. Se recibe el mensaje de la cola `alertar`.
2. Se consulta el grupo del paciente (`CareTeam/{pacienteId}`). Si no existe, se registra un aviso y el mensaje se descarta.
3. Se consulta cada miembro del grupo y se toman los `telecom` con `system = email`. Los miembros sin email se omiten y los correos repetidos se eliminan.
4. Si **ningún** miembro tiene email se lanza un error (la cola reintenta y, tras agotar los intentos, el mensaje pasa a `alertar-poison`).
5. Si al menos uno tiene email, se envía **un único correo** a todos los destinatarios con el paciente, la última medición, los umbrales y el número de mediciones fuera de rango. Si el envío falla se lanza el error y la cola reintenta.

### Configuración

| Variable | Descripción |
|---|---|
| `AzureWebJobsStorage` | Conexión al Storage / Azurite donde viven las colas |
| `SERVICIO_GESTION_PACIENTES` | URL base del servicio de gestión de pacientes |
| `PACIENTES_FUNCTIONS_KEY` | Clave enviada en el header `x-functions-key` hacia gestión de pacientes |
| `SMTP_USER` | Cuenta de correo remitente (Gmail) |
| `SMTP_PASSWORD` | Contraseña de aplicación de Gmail (https://myaccount.google.com/apppasswords, requiere verificación en dos pasos) |
| `SMTP_HOST` | Opcional. Servidor SMTP (por defecto `smtp.gmail.com`) |
| `SMTP_PORT` | Opcional. Puerto SMTP con STARTTLS (por defecto `587`) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Opcional. Habilita la telemetría |

## 3. Gestión del proyecto
- **Estrategia de ramas:** `main` protegida; el trabajo se integra mediante *feature branches* (`feature/<servicio>-<descripcion>`) y Pull Requests.
- **Commits semánticos:** `feat:`, `fix:`, `docs:`, `refactor:`, con ámbito opcional, por ejemplo `feat(ingestion): valida lecturas fuera de rango`.
- **Tablero:** GitHub Projects con vistas agrupadas por Milestones.
- **Historias de usuario:** registradas como Issues con la etiqueta `user-story`, criterios de aceptación y checklist técnico.
- **Plantillas:** `.github/ISSUE_TEMPLATE/user_story.md` y `.github/PULL_REQUEST_TEMPLATE.md`.

## 4. Implementación

Primero se construye la imagen basado en el Dockerfile para ello se sugiere el siguiente comando

```docker build -t servicio-alertas . ```

Una vez ello se levanta el contenedor de la siguiente forma.

```docker run -d --name servicio-alertas `
  -p 8087:8080 `
  -e "AzureWebJobsStorage=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://host.docker.internal:11000/devstoreaccount1;QueueEndpoint=http://host.docker.internal:11001/devstoreaccount1;TableEndpoint=http://host.docker.internal:11002/devstoreaccount1;" `
  -e SERVICIO_GESTION_PACIENTES=http://host.docker.internal:8081/api/v1/ `
  -e PACIENTES_FUNCTIONS_KEY=clave-master-cc-ugr `
  -e SMTP_USER=tu-cuenta@gmail.com `
  -e "SMTP_PASSWORD=abcd efgh ijkl mnop" `
  -e AzureWebJobsSecretStorageType=Files `
  servicio-alertas```

---
