using System;

namespace adyen_dotnet_authorisation_adjustment_example.Models
{
    public class UpdatePaymentAmountRequest
    {
        public string PspReference { get; init; }
        public long Amount { get; init; }
        public string Currency { get; init; }
    }
}