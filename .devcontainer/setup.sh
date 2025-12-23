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
  # pick one project to run, for example checkout-example
  cd checkout-example
  dotnet run

Before starting the app, set these environment variables (export in terminal):
  ADYEN_API_KEY            https://docs.adyen.com/user-management/how-to-get-the-api-key
  ADYEN_CLIENT_KEY         https://docs.adyen.com/user-management/client-side-authentication
  ADYEN_MERCHANT_ACCOUNT   https://docs.adyen.com/account/account-structure
  ADYEN_HMAC_KEY           https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures

Then navigate to your desired example directory and run:
  dotnet run --project <path-to-csproj>

If you want HTTP only inside the container, run:
  ASPNETCORE_URLS=http://0.0.0.0:8080 dotnet run --no-launch-profile

Other projects:
  subscription-example/
  giftcard-example/
  paybylink-example/
  authorisation-adjustment-example/
  giving-example/
  in-person-payments-example/
  in-person-payments-loyalty-example/
EOF
