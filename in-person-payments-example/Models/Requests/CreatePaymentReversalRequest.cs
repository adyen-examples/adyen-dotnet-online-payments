namespace adyen_dotnet_in_person_payments_example.Models.Requests
{
    public class CreatePaymentReversalRequest
    {
        public decimal Amount { get; init; }
        public string SaleReferenceId { get; init; }
    }
}
