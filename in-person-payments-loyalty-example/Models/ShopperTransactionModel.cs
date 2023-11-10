namespace adyen_dotnet_in_person_payments_loyalty_example.Models
{
    /// <summary>
    /// A simple representation of past transactions (history). Save the pizza name, amount and currency to keep track of what the shopper bought.
    /// </summary>
    public class ShopperTransactionModel
    {
        public string PizzaName { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
    }
}