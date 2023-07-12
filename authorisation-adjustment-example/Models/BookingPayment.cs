using System;

namespace adyen_dotnet_authorisation_adjustment_example.Models
{
    public class BookingPayment
    {
        public string PspReference { get; set; }
        public string Reference { get; set; }
        public long? Amount { get; set; }
        public string Currency { get; set; }
        public DateTime DateTime { get; set; }
        public string ResultCode { get; set; }
        public string RefusalReason { get; set; }
    }
}