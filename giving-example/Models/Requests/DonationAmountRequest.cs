namespace adyen_dotnet_giving_example.Models.Requests
{
    public class DonationAmountRequest
    {
        public long Value { get; init; } // Amount.
        public string Currency { get; init; }
    }
}
