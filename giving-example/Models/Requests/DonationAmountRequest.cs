namespace adyen_dotnet_giving_example.Models.Requests
{
    public class DonationAmountRequest
    {
        public long Amount { get; init; }
        public string Currency { get; init; }
    }
}
