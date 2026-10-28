#Traer una imagen de .NET 8.0 para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY servicio-gestion-pacientes/servicio-gestion-pacientes.csproj servicio-gestion-pacientes/
RUN dotnet restore servicio-gestion-pacientes/servicio-gestion-pacientes.csproj

COPY servicio-gestion-pacientes/ servicio-gestion-pacientes/
RUN dotnet publish servicio-gestion-pacientes/servicio-gestion-pacientes.csproj -c Release -o /home/site/wwwroot --no-restore

#Traer imagen para usarla ya para publicar
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
	AzureWebJobsSecretStorageType=Files

COPY --from=build /home/site/wwwroot /home/site/wwwroot

