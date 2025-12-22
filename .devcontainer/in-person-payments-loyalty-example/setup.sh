#!/usr/bin/env bash
set -euo pipefail

echo "[setup] dotnet info"
dotnet --info || true

echo "[setup] configuring HTTPS developer certificate"
dotnet dev-certs https --clean || true
dotnet dev-certs https || true

echo "[setup] restoring project"
dotnet restore

echo "[setup] done. You can start the app with:"
echo "  dotnet run"


