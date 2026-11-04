# App de monitoreo y alertas diarias de mediciones personalizada de Salud - Azurite - Servicio de colas

**Autor:** _Edgar Camilo Vargas Aldana_ · **Asignatura:** Cloud Computing: Fundamentos e Infraestructuras

---

## 1. Arquitectura
 
### Componentes
 
| Componente | Responsabilidad | Almacenamiento |
|---|---|---|
| `azurite` | Emulador local de Azure Storage. En este proyecto se usa como **emulador de colas de mensajes** (servicio Queue) para comunicar los microservicios de forma asíncrona. Los servicios de Blob y Table que expone solo los usa el host de Azure Functions (`AzureWebJobsStorage`) | Volumen Docker `azurite-data` |

## 2. Implementación
 
Se levanta como un contenedor Docker con los siguientes comandos (PowerShell):
 
```powershell
docker pull mcr.microsoft.com/azure-storage/azurite
docker rm -f azurite
 
docker run -d --name azurite `
  -p 11000:10000 -p 11001:10001 -p 11002:10002 `
  -v azurite-data:/data `
  mcr.microsoft.com/azure-storage/azurite `
  azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --location /data --skipApiVersionCheck
```

---