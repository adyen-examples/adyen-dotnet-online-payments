using adyen_dotnet_in_person_payments_loyalty_example.Models;
using System;
using System.Collections.Generic;

namespace adyen_dotnet_in_person_payments_loyalty_example.Repositories
{
    public interface IShopperRepository
    {
        Dictionary<String, ShopperModel> Shoppers { get; }
        ShopperModel AddIfNotExists(string alias, string shopperReference, string shopperEmail, bool isLoyaltyMember, int loyaltyPoints);
        bool AddLoyaltyPoints(string alias, int loyaltyPoints);
        bool AddShopperTransaction(string alias, decimal amount, string currency, string pizzaName, string serviceId);
        bool IsSignedUpForLoyaltyProgram(string alias);
        ShopperModel Get(string alias);
    }
    
    public class ShopperRepository : IShopperRepository
    {
        public Dictionary<String, ShopperModel> Shoppers { get; }

        public ShopperRepository()
        {
            Shoppers = new Dictionary<string, ShopperModel>();
        }

        public ShopperModel AddIfNotExists(string alias, string shopperReference, string shopperEmail, bool isLoyaltyMember, int loyaltyPoints)
        {
            if (Shoppers.ContainsKey(alias))
            {
                return null;
            }

            var shopper = new ShopperModel()
            {
                Alias = alias,
                ShopperReference = shopperReference,
                ShopperEmail = shopperEmail,
                IsSignedUpForLoyaltyProgram = isLoyaltyMember,
                LoyaltyPoints = loyaltyPoints,
                TransactionHistory = new List<ShopperTransactionModel>()
            };
            
            Shoppers.Add(alias, shopper);
            return shopper;
        }

        public bool AddShopperTransaction(string alias, decimal amount, string currency, string pizzaName, string serviceId)
        {
            if (!IsSignedUpForLoyaltyProgram(alias))
            {
                return false;
            }

            var shopper = Get(alias);
            shopper.TransactionHistory.Add(new ShopperTransactionModel()
            {
                Amount = amount,
                Currency = currency,
                PizzaName = pizzaName,
                ServiceId = serviceId
            });
            return true;
        }

        public bool AddLoyaltyPoints(string alias, int loyaltyPoints)
        {
            var shopper = Get(alias);
            
            if (shopper == null)
            {
                return false;
            }

            if (!shopper.IsSignedUpForLoyaltyProgram)
            {
                return false;
            }

            shopper.LoyaltyPoints += loyaltyPoints;
            return true;
        }

        public bool IsSignedUpForLoyaltyProgram(string alias)
        {
            var shopper = Get(alias);

            if (shopper == null)
            {
                return false;
            }

            return shopper.IsSignedUpForLoyaltyProgram;
        }

        public ShopperModel Get(string alias)
        {
            if (alias == null)
            {
                return null;
            }

            if (!Shoppers.TryGetValue(alias, out ShopperModel model))
            {
                return null;
            }

            return model;
        }
    }
}