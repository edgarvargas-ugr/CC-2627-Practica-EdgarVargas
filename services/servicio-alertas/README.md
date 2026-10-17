# App de monitoreo y alertas diarias de mediciones personalizada de Salud - Servicio-Alertas

**Autor:** _Edgar Camilo Vargas Aldana_ · **Asignatura:** Cloud Computing: Fundamentos e Infraestructuras

---

## 1. Arquitectura

### Componentes

| Componente | Responsabilidad | Almacenamiento |
|---|---|---|
| `servicio-alertas` | Revisa el cache a ver si en los ultimos 5 minutos se genero una medición asociada a ese paciente que no cumpliera el umbral, de ser así se dispara el servicio de alertas. Decide a quién avisar y por qué canal en C# | Base de datos de alertas |

## 2. Especificación de la API REST

Todas las rutas van bajo `/api/v1`. Se muestra una versión sintética de los endpoints principales.

### servicio-alertas

| Verbo | Ruta | Descripción |
|---|---|---|
| GET | `/alerts?patientId=&status=` | Listar alertas |
| GET | `/alerts/{id}` | Detalle y motivo de la alerta |


## 3. Gestión del proyecto
- **Estrategia de ramas:** `main` protegida; el trabajo se integra mediante *feature branches* (`feature/<servicio>-<descripcion>`) y Pull Requests.
- **Commits semánticos:** `feat:`, `fix:`, `docs:`, `refactor:`, con ámbito opcional, por ejemplo `feat(ingestion): valida lecturas fuera de rango`.
- **Tablero:** GitHub Projects con vistas agrupadas por Milestones.
- **Historias de usuario:** registradas como Issues con la etiqueta `user-story`, criterios de aceptación y checklist técnico.
- **Plantillas:** `.github/ISSUE_TEMPLATE/user_story.md` y `.github/PULL_REQUEST_TEMPLATE.md`.

---