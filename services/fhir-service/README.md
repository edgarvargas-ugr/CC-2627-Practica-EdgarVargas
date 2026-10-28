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

## 2. Implementación

Se usan dos contenedores: una base de datos PostgreSQL y el servidor HAPI FHIR que guarda sus datos en ella.

### Requisitos

- Windows con Docker instalado.
- PowerShell.
- Opcional: pgAdmin para ver las tablas.

### Dockers utilizados

Para este servicio de datos, no se construye una imagen propia. Los dos containers se crean apartir de imagenes oficiales del repositorio Docker Hub.

| Contenedor | Imagen | Origen |
|---|---|---|
| `bd-fhir` | `postgres:16-alpine` | Imagen oficial de PostgreSQL, publicada por el proyecto Docker Official Images |
| `fhir-service` | `hapiproject/hapi` | Imagen oficial del proyecto HAPI FHIR (`hapi-fhir-jpaserver-starter`), publicada por sus mantenedores |

- **No hay código propio que empaquetar.** `fhir-service` es un servidor FHIR ya compilado y `bd-fhir` es un motor de base de datos. Un Dockerfile propio no es necesario para este servicio.
- **Imágenes ya optimizadas.** `postgres:16-alpine` se basa en Alpine, una distribución muy pequeña. La imagen de HAPI incluye solo el entorno de ejecución de Java y la aplicación.


### Estructura de archivos

```
services/fhir-service/
├── .env.example                  # plantilla
├── config/
│   └── hapi.application.yaml     # configuración de HAPI para usar PostgreSQL
└── script/
    └── fhir.ps1                  # levanta los dos contenedores
```


### Despliegue paso a paso

1. Abre Docker Desktop y espera a que esté en ejecución.
2. Copia `.env.example` como `.env` y edita la contraseña.
3. Abre PowerShell en la carpeta `services/fhir-service/script`.
4. Ejecuta (la primera vez puede pedir el flag de política de ejecución):

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\fhir.ps1
   ```

5. Espera unos 40 segundos. HAPI tarda en arrancar y crear las tablas.

6. Abre `http://localhost:8090` (o el valor de `FHIR_PORT`). Debe cargar la interfaz web de HAPI. La API FHIR responde en `http://localhost:8090/fhir/metadata`.


## 3. Gestión de proyecto

**Branch:** feature/fhir-service

---
