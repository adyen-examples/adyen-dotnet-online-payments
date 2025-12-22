#!/usr/bin/env bash
set -euo pipefail

echo "[setup] dotnet info"
dotnet --info || true

echo "[setup] configuring HTTPS developer certificate"
dotnet dev-certs https --clean || true
dotnet dev-certs https || true

echo "[setup] restoring solution"
dotnet restore adyen-dotnet-online-payments.sln

echo "[setup] done. You can start a project with, e.g.:"
echo "  cd checkout-example && dotnet run"
