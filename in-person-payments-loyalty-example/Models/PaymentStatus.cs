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
        /// Indicates that the customer has paid for the table, e.g. successful payment request.
        /// </summary>
        Paid = 3,
        
        /// <summary>
        /// A refund is set to <see cref="RefundInProgress"/> when the merchant has initiated the referenced refund process.
        /// Referenced refunds are processed asynchronously.
        /// https://docs.adyen.com/point-of-sale/basic-tapi-integration/refund-payment/referenced/
        /// </summary>
        RefundInProgress = 4,  
        
        /// <summary>
        /// A refund is set to to <see cref="Refunded"/> when the webhook CANCEL_OR_REFUND is successfully received.
        /// https://docs.adyen.com/point-of-sale/basic-tapi-integration/refund-payment/refund-webhooks/#cancel-or-refund-webhook 
        /// </summary>
        Refunded = 5,

        /// <summary>
        /// A refund is set to to <see cref="RefundFailed"/> when the webhook REFUND_FAILED is successfully received.
        /// https://docs.adyen.com/online-payments/refund#refund-failed 
        /// </summary>
        RefundFailed = 6,
        
        /// <summary>
        /// A refund is set to to <see cref="RefundedReversed"/> when the webhook REFUNDED_REVERSED is successfully received.
        /// https://docs.adyen.com/online-payments/refund/#refunded-reversed 
        /// </summary>
        RefundedReversed = 7
    }
}