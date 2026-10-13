## Descripción

<!-- Qué cambia este PR y por qué, en 2 o 3 líneas. -->

## Historia de usuario relacionada

<!-- "Closes #12" cierra el issue automáticamente al fusionar. Usa "Refs #12" si solo avanza parcialmente. -->

Closes #

## Tipo de cambio

- [ ] `feat`: nueva funcionalidad
- [ ] `fix`: corrección de un error
- [ ] `docs`: documentación
- [ ] `refactor`: cambio interno sin alterar el comportamiento
- [ ] `test`: pruebas
- [ ] `chore` / `ci`: configuración, dependencias o pipeline

## Servicios afectados

- [ ] `servicio-ingesta-mediciones`
- [ ] `servicio-evaluacion-mediciones`
- [ ] `servicio-alertas`
- [ ] `servicio-gestion-pacientes`
- [ ] `api-gateway`
- [ ] `fhir-service`
- [ ] `front-app-web`
- [ ] `simulador-dispositivo-medicion`
- [ ] Documentación / configuración del repositorio

## Cambios en contratos

<!-- Marca solo si aplica; si no, deja "Ninguno". -->

- [ ] Ninguno
- [ ] Endpoint REST nuevo o modificado (ruta y método):
- [ ] Mensaje o evento nuevo o modificado (cola / Event Hub):
- [ ] Recursos FHIR afectados:
- [ ] Variables de entorno nuevas (actualizado `.env.example`)

## Cómo se probó

<!-- Comandos ejecutados, casos probados o capturas. -->

```bash
# ejemplo
docker compose up -d
pytest services/servicio-alertas
```

## Checklist

- [ ] La rama sale de `main` y sigue el formato `feature/<servicio>-<descripcion>`
- [ ] Los commits siguen el formato semántico (`feat(alertas): ...`)
- [ ] El código compila y las pruebas pasan en local
- [ ] Se añadieron o actualizaron pruebas
- [ ] Se actualizó la documentación (README o `docs/`) si hacía falta
- [ ] No hay secretos, claves ni datos reales de pacientes en el código
- [ ] Los criterios de aceptación del issue están cumplidos

## Notas para quien revisa

<!-- Decisiones de diseño, partes que te generan dudas, trabajo pendiente. -->