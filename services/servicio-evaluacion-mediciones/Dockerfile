#Traer una imagen de .NET 8.0 para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY servicio-evaluacion-mediciones/servicio-evaluacion-mediciones.csproj servicio-evaluacion-mediciones/
RUN dotnet restore servicio-evaluacion-mediciones/servicio-evaluacion-mediciones.csproj

COPY servicio-evaluacion-mediciones/ servicio-evaluacion-mediciones/
RUN dotnet publish servicio-evaluacion-mediciones/servicio-evaluacion-mediciones.csproj -c Release -o /home/site/wwwroot --no-restore

#Traer imagen para usarla ya para publicar
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
	AzureWebJobsSecretStorageType=Files

COPY --from=build /home/site/wwwroot /home/site/wwwroot

