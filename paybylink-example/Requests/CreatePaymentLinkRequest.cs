namespace adyen_dotnet_paybylink_example.Requests
{
    public class CreatePaymentLinkRequest
    {
        public long Amount { get; init; }
        public string Reference { get; init; }
        public bool IsReusable { get; init; }
    }
}
