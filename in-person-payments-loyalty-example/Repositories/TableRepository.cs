using System;
using System.Collections.Generic;
using adyen_dotnet_in_person_payments_loyalty_example.Models;

namespace adyen_dotnet_in_person_payments_loyalty_example.Repositories
{
    public interface ITableRepository
    {
        /// <summary>
        /// List of all <see cref="TableModel"/>s.
        /// </summary>
        List<TableModel> Tables { get; }

        void ApplyDiscount(decimal discount);
        void Reset();
    }

    public class TableRepository : ITableRepository
    {
        public List<TableModel> Tables { get; }

        string[] PizzaNames = {
            "Pepperoni",
            "Margherita",
            "Hawai",
            "Vegetarian",
            "Supreme",
            "BBQ Chicken",
            "Mushroom",
            "Spicy Chicken",
            "Pineapple",
            "Four Cheese",
            "Spinach",
            "Chicken",
        };

        public TableRepository()
        {
            Tables = new List<TableModel>();
            Reset();
        }

        public void Reset()
        {
            Tables.Clear();
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
                    IsDiscounted = false
                });
            }
        }

        public void ApplyDiscount(decimal discount)
        {
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
                    IsDiscounted = true
                });
            }
        }
    }
}
