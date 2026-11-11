#!/bin/sh
set -e

# Start Azurite (Azure Storage emulator) so the Functions runtime can boot.
azurite --silent \
    --location /opt/azurite \
    --blobHost 0.0.0.0 --blobPort 10000 \
    --queueHost 0.0.0.0 --queuePort 10001 \
    --tableHost 0.0.0.0 --tablePort 10002 &

# Wait until Azurite's blob service accepts connections.
echo "Waiting for Azurite..."
until curl -s --max-time 1 http://localhost:10000/ >/dev/null 2>&1; do
    sleep 1
done
echo "Azurite is ready."

# SWA CLI serves the published Blazor app and proxies /api/* to the Functions host.
exec swa start ./wwwroot \
    --api-location ./api \
    --swa-config-location /app \
    --host 0.0.0.0 \
    --port 4280
