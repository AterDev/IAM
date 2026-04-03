# syntax=docker/dockerfile:1.7

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS final
WORKDIR /app
RUN apk add --no-cache icu-data-full icu-libs tzdata
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Container \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    RUN_MIGRATIONS=true \
    MIGRATION_MAX_ATTEMPTS=20 \
    MIGRATION_RETRY_DELAY_SECONDS=3
COPY .artifacts/docker/api/ ./
COPY .artifacts/docker/migration/ ./migration/
COPY scripts/docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh
EXPOSE 8080
ENTRYPOINT ["/app/docker-entrypoint.sh"]
