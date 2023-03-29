namespace adyen_dotnet_checkout_example.Dtos.Requests
{
    public class BalanceCheckRequest
    {
        public string Type { get; init; }
        public string Number { get; init; }
        public string Cvc { get; init; }
        public string HolderName { get; init; }
    }
}
