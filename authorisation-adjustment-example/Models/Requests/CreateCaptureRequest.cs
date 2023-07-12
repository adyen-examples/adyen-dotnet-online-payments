using System;

namespace adyen_dotnet_authorisation_adjustment_example.Models
{
    public class CreateCaptureRequest
    {
        public string PspReference { get; init; }
        public long Amount { get; init; }
    }
}