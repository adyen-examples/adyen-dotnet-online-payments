using System;

namespace adyen_dotnet_authorisation_adjustment_example.Models
{
    public class HotelPaymentModel
    {
        // PspReference provided by Adyen.
        public string PspReference { get; set; }

        // Reference provided by the merchant.
        public string Reference { get; set; }
        
        // Date of payment.
        public DateTime DateTime { get; set; }

        // Amount.
        public long? Amount { get; set; }
        public string Currency { get; set; }

        // Result.
        public string ResultCode { get; set; }
        public string RefusalReason { get; set; } // Populated when resultcode is not authorised.
        
        // Payment method used.
        public string PaymentMethodBrand { get; set; } // PaymentMethod brand (e.g. `mc`).
        public string PaymentMethodType { get; set; } // PaymentMethod type (e.g. `scheme`).
        
        #region Amount/RefusalReasons/PaymentMethod show logic
        
        public bool IsAmountShown()
        {
            return Amount != null && !string.IsNullOrWhiteSpace(Currency);
        }

        public bool IsRefusalReasonShown()
        {
            return !string.IsNullOrWhiteSpace(RefusalReason);
        }

        public bool IsPaymentMethodShown()
        {
            return !string.IsNullOrWhiteSpace(PaymentMethodBrand) && !string.IsNullOrWhiteSpace(PaymentMethodType);
        }
        
        #endregion

        #region ResultCodes logic
        
        public bool IsAuthorised()
        {
            return ResultCode is "Authorised" or "AUTHORISATION";
        }

        public bool IsAuthorisedAdjusted()
        {
            return ResultCode is "AUTHORISATION_ADJUSTMENT";
        }

        public bool IsCaptured()
        {
            return ResultCode is "CAPTURE";
        }
        
        #endregion
    }
}