namespace adyen_dotnet_giving_example.Models.Requests
{
    public class DonationAmountRequest
    {
        public long Value { get; init; } // Amount in minor units.
        public string Currency { get; init; } // e.g., "EUR"
        
        // New field to accept the payment method chosen by the user
        public PaymentMethodDetails PaymentMethod { get; set; } 
    }
}
