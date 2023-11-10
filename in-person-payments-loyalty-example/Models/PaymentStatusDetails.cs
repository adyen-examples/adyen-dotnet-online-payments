using System;

namespace adyen_dotnet_in_person_payments_loyalty_example.Models
{
    public class PaymentStatusDetails
    {
        /// PspReference: It is also possible to get the PspReference from the Response.AdditionalData property:
        /// https://docs.adyen.com/point-of-sale/basic-tapi-integration/verify-transaction-status/#id324618081
        /// public string PspReference { get; set; }

        /// <summary>
        /// The API provides a refusal reason when there's an error. This will get populated when "failure" is sent as a response.
        /// </summary>
        public string RefusalReason { get; set; } = null;

        /// <summary>
        /// The POI Transaction Id (the PspReference is shown after the `.` if the payment request was sent, in this case: 'TG6DVRZ3HVTFWR82').
        /// Example value: CmI6001693237705007.TG6DVRZ3HVTFWR82.
        /// </summary>
        public string PoiTransactionId { get; set; } = null;

        /// <summary>
        /// Date of the POI transaction.
        /// </summary>
        public DateTime? PoiTransactionTimeStamp { get; set; } = null;

        /// <summary>
        /// The SaleTransactionId (SaleReferenceId).
        /// Example value: 6abcb27d-9082-40d9-969d-1c7f283ebd52.
        /// </summary>
        public string SaleTransactionId { get; set; } = null;

        /// <summary>
        /// Date of the Sale transaction.
        /// </summary>
        public DateTime? SaleTransactionTimeStamp { get; set; } = null;

        /// <summary>
        /// The unique ID of a message pair, which processes the transaction. The value is assigned when you initiate a payment transaction to the terminal, and used to cancel/abort the request.
        /// This unique request ID, consisting of 1-10 alphanumeric characters is generated using the <see cref="Utilities.IdUtility.GetRandomAlphanumericId(int)"/> function and must be unique within the last 48 hours for the terminal (POIID) being used.
        /// </summary>
        public string ServiceId { get; set; } = null;
    }
}
