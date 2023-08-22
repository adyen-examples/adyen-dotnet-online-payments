namespace adyen_dotnet_in_person_payments_example.Models.Requests
{
    public class CreatePaymentRequest
    {
        public decimal Amount { get; init; }
        public string Currency { get; init; }
    }
}
