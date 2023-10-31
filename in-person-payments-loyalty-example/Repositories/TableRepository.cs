using adyen_dotnet_in_person_payments_loyalty_example.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace adyen_dotnet_in_person_payments_loyalty_example.Repositories
{
    public interface ITableRepository
    {
        /// <summary>
        /// List of all <see cref="TableModel"/>s.
        /// </summary>
        List<TableModel> Tables { get; }

        void ApplyDiscount(decimal discount); // TODO
    }

    public class TableRepository : ITableRepository
    {
        public List<TableModel> Tables { get; }

        private readonly string[] PizzaNames = {
            "Pepperoni",
            "Margherita",
            "Vegetarian",
            "Supreme",
            "BBQ Chicken",
            "Four Cheese",
            "Spinach",
            "Chicken",
        };

        public TableRepository()
        {
            Tables = new List<TableModel>();
            for (int i = 0; i < PizzaNames.Length; i++)
            {
                string pizzaName = PizzaNames[i];
                Tables.Add(new TableModel()
                {
                    TableName = pizzaName,
                    Amount = Math.Round((11 * (i + 1)) + 0.99M, 2),
                    OriginalAmount = Math.Round((11 * (i + 1)) + 0.99M, 2),
                    Currency = "EUR",
                    PaymentStatus = PaymentStatus.NotPaid,
                    IsDiscounted = false,
                    PaymentStatusDetails = new PaymentStatusDetails()
                });
            }
        }

        public void ApplyDiscount(decimal discount)
        {
            List<PaymentStatusDetails> details = Tables.Select(x => x.PaymentStatusDetails).ToList();

            Tables.Clear();
            for (int i = 0; i < PizzaNames.Length; i++)
            {
                string pizzaName = PizzaNames[i];
                Tables.Add(new TableModel()
                {
                    TableName = pizzaName,
                    Amount = Math.Round(((11 * (i + 1)) + 0.99M) * discount, 2),
                    OriginalAmount = Math.Round((11 * (i + 1)) + 0.99M, 2),
                    Currency = "EUR",
                    PaymentStatus = PaymentStatus.NotPaid,
                    IsDiscounted = true, 
                    PaymentStatusDetails = details.Count == 0 ? new PaymentStatusDetails() : details[i]
                });
            }
        }
    }
}
