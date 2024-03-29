namespace adyen_dotnet_in_person_payments_example.Options
{
    public class AdyenOptions
    {
        /// <summary>
        /// Your secret API Key.
        /// See https://docs.adyen.com/development-resources/api-credentials#generate-your-api-key.
        /// </summary>
        public string ADYEN_API_KEY { get; set; }
        
        /// <summary>
        /// HMAC Key used to validate your webhook signatures.
        /// See https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures.
        /// </summary>
        public string ADYEN_HMAC_KEY { get; set; }
        
        /// <summary>
        /// The unique ID of your payment terminal for the NEXO Sale to POI protocol.
        /// Format: [device model]-[serial number].
        /// See https://docs.adyen.com/point-of-sale/basic-tapi-integration/make-a-payment/#make-a-payment.
        /// </summary>
        public string ADYEN_POS_POI_ID { get; set; }

        /// <summary>
        /// Your unique ID for the POS system (cash register) to send this request from.
        /// </summary>
        public string ADYEN_POS_SALE_ID { get; set; }

        /// <summary>
        /// Default: null, unless you want to override this to point to a different endpoint based on your region.
        /// See https://docs.adyen.com/point-of-sale/design-your-integration/terminal-api/#cloud.
        /// Optionally, if you do not own an Adyen Terminal/POS (yet), you can test this application using Adyen's Mock Terminal-API Application on GitHub: https://github.com/adyen-examples/adyen-mock-terminal-api (see README).
        /// </summary>
        public string ADYEN_TERMINAL_API_CLOUD_ENDPOINT { get; set; }
    }
}