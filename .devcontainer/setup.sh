#!/usr/bin/env bash
set -euo pipefail

echo "[setup] dotnet info"
dotnet --info || true

echo "[setup] configuring HTTPS developer certificate"
dotnet dev-certs https --clean || true
dotnet dev-certs https || true

echo "[setup] restoring solution"
dotnet restore adyen-dotnet-online-payments.sln

cat <<'EOF'
[setup] next steps:

Before starting the app, set these environment variables (export in terminal):
  ADYEN_API_KEY          (required) (https://docs.adyen.com/user-management/how-to-get-the-api-key)
  ADYEN_CLIENT_KEY       (required) (https://docs.adyen.com/user-management/client-side-authentication)
  ADYEN_MERCHANT_ACCOUNT (required) (https://docs.adyen.com/account/account-structure)
  ADYEN_HMAC_KEY         (optional, recommended) (https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures)

Then navigate to your desired example directory and start the application:
    cd <example-directory> (e.g. checkout-example)
    dotnet run
    visit http://localhost:8080 (or https://localhost:5001)
EOF
