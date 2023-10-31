namespace adyen_dotnet_in_person_payments_loyalty_example.Models
{
    public enum PaymentStatus
    {
        /// <summary>
        /// Indicates that the customer has not paid yet.
        /// </summary>
        NotPaid = 1,
        
        /// <summary>
        /// Indicates that the customer is going to pay, e.g. the payment request is sent to the terminal.
        /// </summary>
        PaymentInProgress = 2,

        /// <summary>
        /// Indicates that the customer has paid, e.g. successful payment request.
        /// </summary>
        Paid = 3
    }
}