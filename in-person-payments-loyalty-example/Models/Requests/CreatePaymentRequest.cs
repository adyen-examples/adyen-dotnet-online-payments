namespace adyen_dotnet_in_person_payments_loyalty_example.Models.Requests
{
    public class CreatePaymentRequest
    {
        public string PizzaName { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; }
    }
}
