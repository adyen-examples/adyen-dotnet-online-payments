namespace adyen_dotnet_authorisation_adjustment_example.Models
{
    public class UpdatePaymentAmountRequest
    {
        public string Reference { get; init; }
        public long Amount { get; init; }
    }
}