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
    }
}