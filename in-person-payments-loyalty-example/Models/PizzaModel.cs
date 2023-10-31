namespace adyen_dotnet_in_person_payments_loyalty_example.Models
{
    public class PizzaModel
    {
        /// <summary>
        /// Currency used for the <see cref="Amount"/> (e.g. "EUR", "USD).
        /// </summary>
        public string Currency { get; init; }

        /// <summary>
        /// The pizza amount to-be-paid, in DECIMAL units (example: 42.99), the terminal API does not use minor units.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Original amount.
        /// </summary>
        public decimal OriginalAmount { get; init; }

        /// <summary>
        /// Is Discounted flag.
        /// </summary>
        public bool IsDiscounted { get => Amount != OriginalAmount; }

        /// <summary>
        /// Name of the pizza, used to uniquely identify the pizza.
        /// </summary>
        public string PizzaName { get; init; }

        /// <summary>
        /// Status of the pizza, used to check if the pizza has paid for.
        /// </summary>
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.NotPaid;

        /// <summary>
        /// Object that populated with information when payment process is started.
        /// </summary>
        public PaymentStatusDetails PaymentStatusDetails { get; init; } = new PaymentStatusDetails();
    }
}
