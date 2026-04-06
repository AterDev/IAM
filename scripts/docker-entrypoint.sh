#!/bin/sh
set -eu

echo "Starting ApiService..."
exec dotnet /app/ApiService.dll
