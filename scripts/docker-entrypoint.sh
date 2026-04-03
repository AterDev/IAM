#!/bin/sh
set -eu

run_migrations=$(printf '%s' "${RUN_MIGRATIONS:-true}" | tr '[:upper:]' '[:lower:]')

if [ "$run_migrations" = "true" ] || [ "$run_migrations" = "1" ] || [ "$run_migrations" = "yes" ]; then
    max_attempts="${MIGRATION_MAX_ATTEMPTS:-20}"
    retry_delay="${MIGRATION_RETRY_DELAY_SECONDS:-3}"
    attempt=1

    while [ "$attempt" -le "$max_attempts" ]; do
        echo "[$attempt/$max_attempts] Running database migrations..."
        if dotnet /app/migration/MigrationService.dll; then
            echo "Database migrations completed successfully."
            break
        fi

        if [ "$attempt" -ge "$max_attempts" ]; then
            echo "Database migrations failed after $max_attempts attempts." >&2
            exit 1
        fi

        echo "Database migrations failed; retrying in ${retry_delay}s..."
        sleep "$retry_delay"
        attempt=$((attempt + 1))
    done
else
    echo "Skipping database migrations because RUN_MIGRATIONS=${RUN_MIGRATIONS:-false}."
fi

echo "Starting ApiService..."
exec dotnet /app/ApiService.dll
