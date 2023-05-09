namespace adyen_dotnet_paybylink_example.Requests
{
    public class CreatePaymentLinkRequest
    {
        public int Amount { get; init; }
        public string Reference { get; init; }
    }
}
