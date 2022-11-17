using System.Collections.Generic;
using System.Linq;

namespace adyen_dotnet_subscription_example.Repositories
{
    /// <summary>
    /// Acts as a local (memory) repository to store shopperReferences and recurringDetailReferences.
    /// These can be used to make future payment requests.
    /// </summary>
    public interface ISubscriptionRepository
    {
        Dictionary<string, List<string>> ShopperReferences { get; }

        bool Remove(string shopperReference, string recurringDetailReference);

        bool Upsert(string shopperReference, string recurringDetailReference);
    }

    public class SubscriptionRepository : ISubscriptionRepository
    {
        public Dictionary<string, List<string>> ShopperReferences { get; }

        public SubscriptionRepository()
        {
            ShopperReferences = new Dictionary<string, List<string>>();
        }

        public bool Remove(string shopperReference, string recurringDetailReference)
        {
            if (!ShopperReferences.TryGetValue(shopperReference, out List<string> list))
            {
                return false; // No ShopperReference found.
            }

            bool isSuccess = list.Remove(recurringDetailReference);

            // If a shopperReference has no recurringDetailReferences left, remove the shopperReference.
            if (!list.Any())
            {
                ShopperReferences.Remove(shopperReference);
            }
            return isSuccess;
        }

        public bool Upsert(string shopperReference, string recurringDetailReference)
        {
            if (!ShopperReferences.ContainsKey(shopperReference))
            {
                // New shopper reference, add the shopper reference and the recurringDetailReference.
                ShopperReferences.Add(shopperReference, new List<string> { recurringDetailReference });
                return true;
            }

            string existingToken = ShopperReferences[recurringDetailReference].FirstOrDefault(token => token == recurringDetailReference);
            if (existingToken == null)
            {
                // Shopper added a new payment method and tokenized it. Append the recurringDetailReference to the existing list of keys.
                ShopperReferences[recurringDetailReference].Add(recurringDetailReference);
                return true;
            }

            // Token was already added don't do anything.
            return false;
        }
    }
}
