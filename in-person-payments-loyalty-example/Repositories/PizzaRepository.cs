using adyen_dotnet_in_person_payments_loyalty_example.Models;
using System;
using System.Collections.Generic;

namespace adyen_dotnet_in_person_payments_loyalty_example.Repositories
{
    public interface IPizzaRepository
    {
        /// <summary>
        /// List of all <see cref="PizzaModel"/>s.
        /// </summary>
        List<PizzaModel> Pizzas { get; }

        /// <summary>
        /// Applies discount percentage on every pizza.
        /// </summary>
        /// <param name="discountPercentage">Discount percentage from 0 to 100.</param>
        void ApplyDiscount(int discountPercentage);

        /// <summary>
        /// Removes discount from every pizza.
        /// </summary>
        void ClearDiscount();
    }

    public class PizzaRepository : IPizzaRepository
    {
        public List<PizzaModel> Pizzas { get; }

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

        public PizzaRepository()
        {
            Pizzas = new List<PizzaModel>();
            for (int i = 0; i < PizzaNames.Length; i++)
            {
                decimal amount = Math.Round((11 * (i + 1)) + 0.99M, 2); // EUR 11.99, 22.99, 33.99 etc...
                Pizzas.Add(new PizzaModel()
                {
                    PizzaName = PizzaNames[i],
                    Amount = amount,
                    OriginalAmount = amount,
                    Currency = "EUR",
                    PaymentStatusDetails = new PaymentStatusDetails()
                });
            }
        }

        public void ApplyDiscount(int discountPercentage)
        {
            if (discountPercentage < 0 || discountPercentage > 100)
            {
                return;
            }

            for (int i = 0; i < Pizzas.Count; i++)
            {
                Pizzas[i].Amount = Math.Round(Pizzas[i].OriginalAmount * ((100 - discountPercentage) / 100M), 2);
            }
        }

        public void ClearDiscount()
        {
            for (int i = 0; i < Pizzas.Count; i++)
            {
                Pizzas[i].Amount = Pizzas[i].OriginalAmount;
            }
        }
    }
}
