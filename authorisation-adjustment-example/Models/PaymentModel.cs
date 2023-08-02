using System;

namespace adyen_dotnet_authorisation_adjustment_example.Models
{
    public class PaymentModel
    {
        /// PspReference provided by Adyen, you can receive this in the response of the <see cref="Controllers.ApiController.PreAuthorisation(Adyen.Model.Checkout.PaymentRequest, System.Threading.CancellationToken)"/> call.
        public string PspReference { get; set; }

        /// This is a `PspReference` that is referring to the initial `PspReference` of the payment.
        /// In this case, it refers to the PspREference of the Pre-Authorisation payment from <see cref="Controllers.ApiController.PreAuthorisation(Adyen.Model.Checkout.PaymentRequest, System.Threading.CancellationToken)"/>.
        public string OriginalReference { get; set; }

        /// Reference provided by the merchant (you), we generate a random GUID when <see cref="Controllers.ApiController.PreAuthorisation(Adyen.Model.Checkout.PaymentRequest, System.Threading.CancellationToken)"/> is called.
        public string Reference { get; set; }
        
        /// Date of payment, the offset is stored with its timezone.
        public DateTimeOffset DateTime { get; set; }

        /// Amount.
        public long? Amount { get; set; }
        /// Currency.
        public string Currency { get; set; }

        /// Result.
        public string ResultCode { get; set; }
        public string RefusalReason { get; set; } // Populated when ResultCode is not authorised (for example: refused), or when a webhook is unsuccessful
        
        /// Payment method.
        public string PaymentMethodBrand { get; set; } // PaymentMethod brand (e.g. `mc`).
        
        public string GetOriginalPspReference()
        {
            return string.IsNullOrWhiteSpace(OriginalReference) ? PspReference : OriginalReference;
        }

        public bool IsEqual(PaymentModel other)
        {
            return PspReference == other.PspReference
                && OriginalReference == other.OriginalReference
                && Reference == other.Reference
                && DateTime.Equals(other.DateTime)
                && Amount == other.Amount
                && Currency == other.Currency
                && ResultCode == other.ResultCode
                && RefusalReason == other.RefusalReason
                && PaymentMethodBrand == other.PaymentMethodBrand;
        }

        #region Amount/RefusalReasons/PaymentMethod - Methods are used to show elements on frontend.
        public bool IsAmountShown()
        {
            return Amount != null && !string.IsNullOrWhiteSpace(Currency);
        }

        public bool IsRefusalReasonShown()
        {
            return !string.IsNullOrWhiteSpace(RefusalReason);
        }

        #endregion

        #region ResultCodes - Methods are used to show elements on frontend.

        // The payment details of the shopper are verified, and the funds are reserved.
        // https://docs.adyen.com/online-payments/adjust-authorisation/#pre-authorise
        public bool IsAuthorised()
        {
            return ResultCode is "Authorised" or "AUTHORISATION";
        }

        // The preauthorisation amount has been adjusted or extended.
        // https://docs.adyen.com/online-payments/adjust-authorisation/#adjust-the-amount
        public bool IsAuthorisedAdjusted()
        {
            return ResultCode is "AUTHORISATION_ADJUSTMENT";
        }

        // The reserved funds are transferred from the shopper to your account. 
        // https://docs.adyen.com/online-payments/adjust-authorisation/#capture-authorisation
        public bool IsCaptured()
        {
            return ResultCode is "CAPTURE";
        }
        #endregion
    }
}