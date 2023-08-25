using System;
using System.Collections.Generic;
using System.Linq;

namespace adyen_dotnet_authorisation_adjustment_example.Models
{
    public class PaymentModel
    {
        /// <summary>
        /// A merchant reference provided by you, randomly generated when <see cref="Controllers.ApiController.PreAuthorisation(Adyen.Model.Checkout.PaymentRequest, System.Threading.CancellationToken)"/> is called.
        /// </summary>
        public string MerchantReference { get; set; }
        
        /// <summary>
        /// PspReference of the original pre-authorisation, this can be found in the response after calling <see cref="Controllers.ApiController.PreAuthorisation(Adyen.Model.Checkout.PaymentRequest, System.Threading.CancellationToken)"/>. 
        /// </summary>
        public string PspReference { get; set; }
        
        /// <summary>
        /// Date of payment, the offset is included to allow for local timezone conversion.
        /// </summary>
        public DateTimeOffset BookingDate { get; set; }

        /// <summary>
        /// Expiry date of the authorisation.
        /// See validity: https://docs.adyen.com/online-payments/adjust-authorisation/#validity.
        /// </summary>
        public DateTimeOffset ExpiryDate { get; set; }
        
        public DateTimeOffset LastUpdated { get; set; }

        /// <summary>
        /// Current pre-authorised amount, in minor units (e.g. '4299' would translate into '42.99').
        /// </summary>
        public long? Amount { get; set; }

        /// <summary>
        /// Currency, e.g. "EUR", "USD".
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// The payment method brand (e.g. "mc", "visa" ) used to create the pre-authorisation.
        /// </summary>
        public string PaymentMethodBrand { get; set; }
        
        /// <summary>
        /// List of <see cref="PaymentDetailsModel"/>s, this is populated through the initial pre-authorisation and the following webhook events.
        /// </summary>
        public List<PaymentDetailsModel> PaymentsHistory { get; set; }

        /// <summary>
        /// Gets the payment status by checking the <see cref="PaymentsHistory"/>.
        /// Check if the payment has been successfully reversed (cancelled or refunded).
        /// Check if the payment has been successfully captured.
        /// Check if the latest authorised/authorisation adjusted payment is successful.
        /// Otherwise return refused.
        /// This function is used to show or hide the "Capture", "Adjust", "Extend" and "Reversal" buttons on the frontend.
        /// </summary>
        /// <returns><see cref="PaymentStatus"/>.</returns>
        public PaymentStatus GetPaymentStatus()
        {
            List<PaymentDetailsModel> orderedPayments = PaymentsHistory.OrderBy(paymentDetails => paymentDetails.DateTime).ToList();

            PaymentDetailsModel reversedPayment = orderedPayments
                .Where(paymentDetails => paymentDetails.IsReversed())?
                .FirstOrDefault(paymentDetails => paymentDetails.IsSuccess());
            
            if (reversedPayment is not null)
            {
                return PaymentStatus.Reversed;
            }
            
            PaymentDetailsModel capturedPayment = orderedPayments
                .Where(paymentDetails => paymentDetails.IsCaptured())?
                .FirstOrDefault(paymentDetails => paymentDetails.IsSuccess());
            
            if (capturedPayment is not null)
            {
                return PaymentStatus.Captured;
            }
            
            PaymentDetailsModel authorisedPayment = orderedPayments
                .Where(paymentDetails => (paymentDetails.IsAuthorisedAdjusted() || paymentDetails.IsAuthorised()) 
                    && paymentDetails.IsSuccess())?
                .LastOrDefault();
            
            if (authorisedPayment is not null)
            {
                return PaymentStatus.Authorised;
            }
            
            return PaymentStatus.Refused;
        }
    }
}