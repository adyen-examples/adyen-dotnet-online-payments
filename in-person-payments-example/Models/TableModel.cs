namespace adyen_dotnet_in_person_payments_example.Models
{
    public class TableModel
    {
        /// <summary>
        /// Currency used for the <see cref="Amount"/> (e.g. "EUR", "USD).
        /// </summary>
        public string Currency { get; set; }
        
        /// <summary>
        /// The total amount in DECIMAL units (example: 42.99), the terminal API does not use minor units.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Name of the table, used to uniquely identify the table.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Status of the table, used to check if the table has paid.
        /// </summary>
        public TableStatus TableStatus { get; set; } = TableStatus.NotPaid;

        /// <summary>
        /// The Poi Transaction Id, populated when a <see cref="TableStatus"/> is set to <see cref="Models.TableStatus.Paid"/>.
        /// </summary>
        public string PoiTransactionId { get; set; } = null;

        /// <summary>
        /// The Sale Reference Id, populated when a <see cref="TableStatus"/> is set to <see cref="Models.TableStatus.Paid"/>.
        /// </summary>
        public string SaleReferenceId { get; set; } = null;
    }
}
