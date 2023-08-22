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
        /// Your Merchant Account name.
        /// See https://docs.adyen.com/account/account-structure.
        /// </summary>
        public string ADYEN_MERCHANT_ACCOUNT { get; set; }

        /// <summary>
        /// HMAC Key used to validate your webhook signatures.
        /// See https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures.
        /// </summary>
        public string ADYEN_HMAC_KEY { get; set; } // TODO

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

    }
}