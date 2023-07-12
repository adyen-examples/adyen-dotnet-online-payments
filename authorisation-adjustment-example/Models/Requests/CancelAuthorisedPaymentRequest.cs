using System;

namespace adyen_dotnet_authorisation_adjustment_example.Models
{
    public class CancelAuthorisedPaymentRequest
    {
        public string PspReference { get; init; }
    }
}