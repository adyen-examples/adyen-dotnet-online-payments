#!/usr/bin/env bash
set -euo pipefail

echo "[setup] dotnet info"
dotnet --info || true

echo "[setup] configuring HTTPS developer certificate"
dotnet dev-certs https --clean || true
dotnet dev-certs https || true

echo "[setup] restoring project"
dotnet restore

echo "[setup] done."
echo ""
echo "Before running starting the app, set the following environment variables by exporting them in the terminal:"
echo "   - ADYEN_API_KEY          (https://docs.adyen.com/user-management/how-to-get-the-api-key)"
echo "   - ADYEN_CLIENT_KEY       (https://docs.adyen.com/user-management/client-side-authentication)"
echo "   - ADYEN_MERCHANT_ACCOUNT (https://docs.adyen.com/account/account-structure)"
echo "Optional variable:"
echo "   - ADYEN_HMAC_KEY         (https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures)"
echo ""
echo "Then navigate to your desired example directory and run: dotnet run -project"
