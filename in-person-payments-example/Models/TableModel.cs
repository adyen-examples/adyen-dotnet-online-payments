namespace adyen_dotnet_in_person_payments_example.Models
{
    public class TableModel
    {
        /// <summary>
        /// Currency used for the <see cref="Amount"/> (e.g. "EUR", "USD).
        /// </summary>
        public string Currency { get; init; }
        
        /// <summary>
        /// The table amount to-be-paid, in DECIMAL units (example: 42.99), the terminal API does not use minor units.
        /// </summary>
        public decimal Amount { get; init; }

        /// <summary>
        /// Name of the table, used to uniquely identify the table.
        /// </summary>
        public string TableName { get; init; }

        /// <summary>
        /// Status of the table, used to check if the table has paid.
        /// </summary>
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.NotPaid;

        /// <summary>
        /// Object that populated with information when payment process is started.
        /// </summary>
        public PaymentStatusDetails PaymentStatusDetails { get; init; } = new PaymentStatusDetails();
    }
}
