# App de monitoreo y alertas diarias de mediciones personalizada de Salud - Servicio-Evaluación-Mediciones

**Autor:** _Edgar Camilo Vargas Aldana_ · **Asignatura:** Cloud Computing: Fundamentos e Infraestructuras

---

## 1. Arquitectura

Servicio de alertas


### Componentes

| Componente | Responsabilidad | Almacenamiento |
|---|---|---|
| `servicio-evaluacion-mediciones` | Evalúa cada medición frente a los umbrales y la línea base; calcula estado y tendencia; en C# | Caché (Redis) + FHIR (lectura) + DB (umbrales) |

## 2. Especificación de la API REST

Todas las rutas van bajo `/api/v1`. Se muestra una versión sintética de los endpoints principales.

### servicio-evaluacion-mediciones

| Verbo | Ruta | Descripción |
|---|---|---|
| GET | `/Patient/{id}/status` | Estado actual: normal, atención o urgente |
| GET | `/Patient/{id}/trend?days=7` | Tendencia frente a la línea base |

## 3. Gestión del proyecto
- **Estrategia de ramas:** `main` protegida; el trabajo se integra mediante *feature branches* (`feature/<servicio>-<descripcion>`) y Pull Requests.
- **Commits semánticos:** `feat:`, `fix:`, `docs:`, `refactor:`, con ámbito opcional, por ejemplo `feat(ingestion): valida lecturas fuera de rango`.
- **Tablero:** GitHub Projects con vistas agrupadas por Milestones.
- **Historias de usuario:** registradas como Issues con la etiqueta `user-story`, criterios de aceptación y checklist técnico.
- **Plantillas:** `.github/ISSUE_TEMPLATE/user_story.md` y `.github/PULL_REQUEST_TEMPLATE.md`.

---