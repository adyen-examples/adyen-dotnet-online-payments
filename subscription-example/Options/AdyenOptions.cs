namespace adyen_dotnet_subscription_example.Options
{
    public class AdyenOptions
    {
        /// <summary>
        /// Your secret API Key.
        /// See https://docs.adyen.com/development-resources/api-credentials#generate-your-api-key.
        /// </summary>
        public string ADYEN_API_KEY { get; set; }

        /// <summary>
        /// Public key used for client-side authentication.
        /// See https://docs.adyen.com/development-resources/client-side-authentication.
        /// </summary>
        public string ADYEN_CLIENT_KEY { get; set; }

        /// <summary>
        /// Your Merchant Account name.
        /// See https://docs.adyen.com/account/account-structure.
        /// </summary>
        public string ADYEN_MERCHANT_ACCOUNT { get; set; }

        /// <summary>
        /// HMAC Key used to validate your webhook signatures.
        /// See https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures.
        /// </summary>
        public string ADYEN_HMAC_KEY { get; set; }
    }
}