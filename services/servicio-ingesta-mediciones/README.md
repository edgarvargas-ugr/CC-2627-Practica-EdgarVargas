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

### servicio-ingesta-mediciones

| Verbo | Ruta | Descripción |
|---|---|---|
| POST | `/Observation` | Recibir una medición (o un lote) |
| GET | `/Patient/{id}/observations` | Historial de mediciones (formato FHIR) |

```json
POST /api/v1/measurements
{
  "patientId": "p001",
  "deviceId": "watch-p001",
  "timestamp": "2026-10-13T08:05:00Z",
  "spo2": 94,
  "heartRate": 72,
  "wearTimeMinutes": 480
}
```

## 3. Gestión del proyecto
- **Estrategia de ramas:** `main` protegida; el trabajo se integra mediante *feature branches* (`feature/<servicio>-<descripcion>`) y Pull Requests.
- **Commits semánticos:** `feat:`, `fix:`, `docs:`, `refactor:`, con ámbito opcional, por ejemplo `feat(ingestion): valida lecturas fuera de rango`.
- **Tablero:** GitHub Projects con vistas agrupadas por Milestones.
- **Historias de usuario:** registradas como Issues con la etiqueta `user-story`, criterios de aceptación y checklist técnico.
- **Plantillas:** `.github/ISSUE_TEMPLATE/user_story.md` y `.github/PULL_REQUEST_TEMPLATE.md`.

---