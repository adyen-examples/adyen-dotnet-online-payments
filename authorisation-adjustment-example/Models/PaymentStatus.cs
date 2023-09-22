namespace adyen_dotnet_authorisation_adjustment_example.Models
{
    public enum PaymentStatus
    {
        /// <summary>
        /// https://docs.adyen.com/development-resources/refusal-reasons/.
        /// </summary>
        Refused = 1,

        /// <summary>
        /// https://docs.adyen.com/get-started-with-adyen/payment-glossary/#authorisation.
        /// </summary>
        Authorised = 2,

        /// <summary>
        /// https://docs.adyen.com/online-payments/capture/.
        /// </summary>
        Captured = 3,

        /// <summary>
        /// https://docs.adyen.com/online-payments/reversal/.
        /// </summary>
        Reversed = 4,
    }
}