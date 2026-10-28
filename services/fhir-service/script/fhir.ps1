# Cargar las variables del archivo .env
if (-not (Test-Path ..\.env)) { Write-Host "Falta .env: copialo desde .env.example"; exit 1 }
Get-Content ..\.env | Where-Object { $_ -match '^\s*[^#\s][^=]*=' } | ForEach-Object {
    $nombre, $valor = $_ -split '=', 2
    Set-Variable -Name $nombre.Trim() -Value $valor.Trim() -Scope Script
}

docker rm -f bd-fhir fhir-service

docker run -d --name bd-fhir `
  -e POSTGRES_USER=${FHIR_DB_USER} -e POSTGRES_PASSWORD=${FHIR_DB_PASSWORD} -e POSTGRES_DB=${FHIR_DB_NAME} `
  -p "${FHIR_DB_HOST_PORT}:5432" `
  -v bd-fhir-data:/var/lib/postgresql/data `
  postgres:16-alpine

Start-Sleep -Seconds 10

docker run -d --name fhir-service `
  -e FHIR_DB_HOST=host.docker.internal -e FHIR_DB_PORT=${FHIR_DB_HOST_PORT} `
  -e FHIR_DB_NAME=${FHIR_DB_NAME} -e FHIR_DB_USER=${FHIR_DB_USER} -e FHIR_DB_PASSWORD=${FHIR_DB_PASSWORD} `
  -v "$((Resolve-Path ..\config\hapi.application.yaml).Path):/app/config/application.yaml:ro" `
  -p "${FHIR_PORT}:8080" `
  ${HAPI_IMAGE}