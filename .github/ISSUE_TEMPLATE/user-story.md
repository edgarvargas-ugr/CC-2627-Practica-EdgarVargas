---
name: Historia de usuario
about: Describe una funcionalidad desde el punto de vista de un usuario
title: "[HU] "
labels: user-story
assignees: ""
---

## Historia

**Como** <!-- rol: cuidador, médico/clínica, paciente, administrador, dispositivo -->
**quiero** <!-- qué necesita hacer -->
**para** <!-- qué valor o beneficio obtiene -->

## Contexto

<!-- Opcional: por qué es necesaria, enlaces al README o al diagrama de casos de uso. -->

## Criterios de aceptación

<!-- Formato Dado / Cuando / Entonces. Cada criterio debe poder comprobarse. -->

- [ ] **Dado** ... **cuando** ... **entonces** ...
- [ ] **Dado** ... **cuando** ... **entonces** ...
- [ ] **Dado** ... **cuando** ... **entonces** ...

## Servicios afectados

- [ ] `servicio-ingesta-mediciones`
- [ ] `servicio-evaluacion-mediciones`
- [ ] `servicio-alertas`
- [ ] `servicio-gestion-pacientes`
- [ ] `api-gateway`
- [ ] `fhir-service`
- [ ] `front-app-web`
- [ ] `simulador-dispositivo-medicion`

## Checklist técnico

- [ ] Endpoint o evento definido (ruta, método, formato del mensaje)
- [ ] Implementación
- [ ] Pruebas unitarias / de integración
- [ ] Documentación actualizada (README o `docs/`)
- [ ] Revisado y fusionado mediante Pull Request

## Fuera de alcance

<!-- Qué NO incluye esta historia, para evitar que crezca. -->

## Notas

<!-- Dependencias con otras historias, riesgos, dudas abiertas. -->