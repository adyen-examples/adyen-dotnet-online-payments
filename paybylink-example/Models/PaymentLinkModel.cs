using System;

namespace adyen_dotnet_paybylink_example.Models
{
    public class PaymentLinkModel
    {
        public string Reference { get; init; }
        public string PspReference { get; init; }
        public string Url { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Status { get; set; }
    }
}
