namespace adyen_dotnet_in_person_payments_example.Models.Requests
{
    public class CreateReversalRequest
    {
        public string PoiTransactionId { get; init; }
        public string SaleReferenceId { get; init; }
    }
}
