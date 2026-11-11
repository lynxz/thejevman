# syntax=docker/dockerfile:1

# ---------- Build stage ----------
# Publishes the Blazor WebAssembly frontend and the Azure Functions API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first for better layer caching.
COPY Client/thejevman.Client.csproj Client/
COPY Api/thejevman.Api.csproj Api/
RUN dotnet restore Client/thejevman.Client.csproj
RUN dotnet restore Api/thejevman.Api.csproj

# Copy the rest of the source and publish both projects.
COPY . .

RUN dotnet publish Client/thejevman.Client.csproj -c Release -o /publish/client --no-restore
RUN dotnet publish Api/thejevman.Api.csproj -c Release -o /publish/api --no-restore

# ---------- Runtime stage ----------
# .NET 10 runtime (to run the Functions worker) + Node.js (to run SWA CLI).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install Node.js 20 for the SWA CLI and the Azure Storage emulator.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*

# SWA CLI + Azure Functions Core Tools + Azurite (local storage for the Functions runtime).
RUN npm install -g @azure/static-web-apps-cli azure-functions-core-tools@4 azurite

# Published artifacts.
COPY --from=build /publish/client/wwwroot ./wwwroot
COPY --from=build /publish/api ./api
COPY staticwebapp.config.json ./staticwebapp.config.json

# Functions runtime settings (the API only uses HTTP triggers).
ENV AzureWebJobsStorage=UseDevelopmentStorage=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

COPY docker/entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

EXPOSE 4280

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
