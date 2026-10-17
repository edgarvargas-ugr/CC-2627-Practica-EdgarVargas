# App de monitoreo y alertas diarias de mediciones personalizada de Salud - FHIR Service

**Autor:** _Edgar Camilo Vargas Aldana_ · **Asignatura:** Cloud Computing: Fundamentos e Infraestructuras

---

## 1. Arquitectura

Se usara el docker de HAPI Service ofrecido para crear este servicio de datos y la base de persistencia del servicio.

[Repositorio Github del docker HAPI FHIR R4](https://github.com/hapifhir/hapi-fhir-jpaserver-starter)

[Documentación de HL7 FHIR](https://hl7.org/fhir/R4/resourcelist.html)

### Componentes

| Componente | Responsabilidad | Almacenamiento |
|---|---|---|
| `fhir-service` | Servidor FHIR que permite la gestión de recursos de interoperar con estandar internacional de datos en su release R4 | Docker FHIR Service |

---
