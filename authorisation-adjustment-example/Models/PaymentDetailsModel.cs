﻿using System;

namespace adyen_dotnet_authorisation_adjustment_example.Models
{
    public class PaymentDetailsModel
    {
        /// <summary>
        /// PspReference (a unique identifier for this payment) provided by Adyen. 
        /// </summary>
        public string PspReference { get; set; }

        /// <summary>
        /// PspReference of the original pre-authorisation, this can be found in the response after calling <see cref="Controllers.ApiController.PreAuthorisation(Adyen.Model.Checkout.PaymentRequest, System.Threading.CancellationToken)"/>. 
        /// </summary>
        public string OriginalReference { get; set; }
        
        /// <summary>
        /// A merchant reference provided by you, randomly generated when <see cref="Controllers.ApiController.PreAuthorisation(Adyen.Model.Checkout.PaymentRequest, System.Threading.CancellationToken)"/> is called.
        /// </summary>
        public string MerchantReference { get; set; }
        
        /// <summary>
        /// Date of payment, the offset is included to allow for local timezone conversion.
        /// </summary>
        public DateTimeOffset DateTime { get; set; }

        /// <summary>
        /// Amount, in minor units (e.g. '4299' would translate into '42.99').
        /// </summary>
        public long? Amount { get; set; }
        
        /// <summary>
        /// Currency, e.g. "EUR", "USD".
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Result code, e.g. 'authorised', 'capture' etc.
        /// </summary>
        public string ResultCode { get; set; }
        
        /// <summary>
        /// We populate the refusal reason when we receive 
        /// 1) an error/refused/cancelled <seealso cref="ResultCode"/> API response or
        /// 2) an unsuccessful webhook.
        /// </summary>
        public string RefusalReason { get; set; } 
        
        /// <summary>
        /// The payment method brand (e.g. "mc", "visa" ) used to create the pre-authorisation.
        /// </summary>
        public string PaymentMethodBrand { get; set; }

        /// <summary>
        /// Compares a <see cref="PaymentDetailsModel"/>.
        /// </summary>
        /// <param name="other"><see cref="PaymentDetailsModel"/>.</param>
        /// <returns>Returns true if all values are equal.</returns>
        public bool IsEqual(PaymentDetailsModel other)
        {
            if (other == null)
            {
                return false;
            }

            return PspReference == other.PspReference
                   && OriginalReference == other.OriginalReference
                   && MerchantReference == other.MerchantReference
                   && DateTime.Equals(other.DateTime)
                   && Amount == other.Amount
                   && Currency == other.Currency
                   && ResultCode == other.ResultCode
                   && RefusalReason == other.RefusalReason
                   && PaymentMethodBrand == other.PaymentMethodBrand;
        }

        /// <summary>
        /// The payment details of the shopper are verified, and the funds are reserved.
        /// https://docs.adyen.com/online-payments/adjust-authorisation/#pre-authorise
        /// </summary>
        /// <returns>True if authorisation was successful.</returns>
        public bool IsAuthorised()
        {
            return ResultCode is "AUTHORISATION" or "Authorised";
        }
        
        /// <summary>
        /// The preauthorisation amount has been adjusted or extended.
        /// https://docs.adyen.com/online-payments/adjust-authorisation/#adjust-the-amount
        /// </summary>
        /// <returns>True if authorisation adjustment was successful.</returns>
        public bool IsAuthorisedAdjusted()
        {
            return ResultCode is "AUTHORISATION_ADJUSTMENT";
        }

        /// <summary>
        /// The reserved funds are transferred from the shopper to your account. 
        /// https://docs.adyen.com/online-payments/adjust-authorisation/#capture-authorisation
        /// </summary>
        /// <returns>True if capture was successful.</returns>
        public bool IsCaptured()
        {
            return ResultCode is "CAPTURE";
        }

        /// <summary>
        /// Adyen's validations were successful and we sent the refund request to the card scheme.
        /// This usually means that the refund will be processed successfully.
        /// However, in rare cases the refund can be rejected by the card scheme, or reversed.
        /// For information about these exceptions, see REFUND_FAILED webhook, and REFUNDED_REVERSED webhook.
        /// https://docs.adyen.com/online-payments/reversal/#cancel-or-refund-webhook
        /// </summary>
        /// <returns>True is reversal was successful.</returns>
        public bool IsReversed()
        {
            return ResultCode is "CANCEL_OR_REFUND";
        }
    }
}