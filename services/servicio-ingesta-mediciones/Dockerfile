#Traer una imagen de .NET 8.0 para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY servicio-ingesta-mediciones/servicio-ingesta-mediciones.csproj servicio-ingesta-mediciones/
RUN dotnet restore servicio-ingesta-mediciones/servicio-ingesta-mediciones.csproj

COPY servicio-ingesta-mediciones/ servicio-ingesta-mediciones/
RUN dotnet publish servicio-ingesta-mediciones/servicio-ingesta-mediciones.csproj -c Release -o /home/site/wwwroot --no-restore

#Traer imagen para usarla ya para publicar
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
	AzureWebJobsSecretStorageType=Files

COPY --from=build /home/site/wwwroot /home/site/wwwroot

