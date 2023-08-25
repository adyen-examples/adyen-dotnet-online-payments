namespace adyen_dotnet_in_person_payments_example.Models
{
    public class TableModel
    {
        public string Name { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
    }
}
