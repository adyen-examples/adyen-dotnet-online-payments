using adyen_dotnet_subscription_example.Models;
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
        /// <summary>
        /// List of all customers that have bought a subscription using one or more payment methods.
        /// This repository is populated when we confirm a valid webhook containing the <see cref="recurringDetailReference"/> payload, see <see cref="Controllers.WebhookController"/>.
        /// </summary>
        Dictionary<string, SubscribedCustomer> SubscribedCustomers { get; }

        bool Remove(string shopperReference, string recurringDetailReference);

        bool Upsert(string pspReference, string shopperReference, string recurringDetailReference);
    }

    public class SubscriptionRepository : ISubscriptionRepository
    {
        /// <inheritdoc/>
        public Dictionary<string, SubscribedCustomer> SubscribedCustomers { get; }

        public SubscriptionRepository()
        {
            SubscribedCustomers = new Dictionary<string, SubscribedCustomer>();
        }

        /// <inheritdoc/>
        public bool Remove(string shopperReference, string recurringDetailReference)
        {
            if (!SubscribedCustomers.TryGetValue(shopperReference, out SubscribedCustomer customer))
            {
                return false; // No Shopper found.
            }

            SubscribedCustomerDetails existingCustomerDetails = customer.SubscribedCustomerDetails.FirstOrDefault(x => x.RecurringDetailReference == recurringDetailReference);

            if (existingCustomerDetails == null)
            {
                return false;
            }

            bool isSuccess = customer.SubscribedCustomerDetails.Remove(existingCustomerDetails);

            // If a shopperReference has no recurringDetailReferences left, remove the shopperReference.
            if (!customer.SubscribedCustomerDetails.Any())
            {
                SubscribedCustomers.Remove(shopperReference);
            }
            return isSuccess;
        }

        /// <inheritdoc/>
        public bool Upsert(string pspReference, string shopperReference, string recurringDetailReference)
        {
            // New customer: add the shopper reference and the recurringDetailReference.
            if (!SubscribedCustomers.ContainsKey(shopperReference))
            {
                SubscribedCustomers.Add(shopperReference,
                    new SubscribedCustomer()
                    {
                        ShopperReference = shopperReference,
                        SubscribedCustomerDetails = new List<SubscribedCustomerDetails>()
                        {
                            new SubscribedCustomerDetails()
                            {
                                RecurringDetailReference = recurringDetailReference,
                                PspReference = pspReference
                            }
                        }
                    });
                return true;
            }

            // Existing customer:
            SubscribedCustomerDetails existingCustomerDetails = SubscribedCustomers[shopperReference].SubscribedCustomerDetails.FirstOrDefault(x => x.RecurringDetailReference == recurringDetailReference);

            // Add token (recurringDetailReference) to their list of payment methods.
            if (existingCustomerDetails != null)
            {
                SubscribedCustomers[shopperReference].SubscribedCustomerDetails.Add(
                    new SubscribedCustomerDetails()
                    {
                        RecurringDetailReference = recurringDetailReference,
                        PspReference = pspReference
                    });
                return true;
            }

            // Token was already added before, update existing details
            return false;
        }
    }
}
