using System;
using System.Text;

namespace adyen_dotnet_authorisation_adjustment_example.Models
{
    public class BookingPaymentModel
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
        public string ResultCode { get; set; }    // ResultCode == Authorised.
        public string RefusalReason { get; set; } // Populated when resultcode is not authorised.
        
        // Payment method used.
        public string PaymentMethodBrand { get; set; } // PaymentMethod brand (e.g. `mc`).
        public string PaymentMethodType { get; set; } // PaymentMethod type (e.g. `scheme`).

        // Override for outputting purposes.
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{nameof(PspReference)}: {PspReference}");
            sb.AppendLine($"{nameof(Reference)}: {Reference}");
            sb.AppendLine($"{nameof(DateTime)}: {DateTime}");
            sb.AppendLine($"{nameof(Amount)}: {Amount}");
            sb.AppendLine($"{nameof(Currency)}: {Currency}");
            sb.AppendLine($"{nameof(ResultCode)}: {ResultCode}");
            sb.AppendLine($"{nameof(RefusalReason)}: {RefusalReason}");
            sb.AppendLine($"{nameof(PaymentMethodBrand)}: {PaymentMethodBrand}");
            sb.AppendLine($"{nameof(PaymentMethodType)}: {PaymentMethodType}");
            return sb.ToString();
        }
    }
}