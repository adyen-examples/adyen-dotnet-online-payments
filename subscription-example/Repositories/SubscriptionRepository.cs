using adyen_dotnet_subscription_example.Models;
using System.Collections.Concurrent;
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
        /// This repository is populated when we confirm a hmac-valid webhook containing the <see cref="recurringDetailReference"/> payload, see <see cref="Controllers.WebhookController.Webhooks(Adyen.Model.Notification.NotificationRequest)"/>.
        /// </summary>
        ConcurrentDictionary<string, SubscribedCustomer> SubscribedCustomers { get; }

        /// <summary>
        /// Removes token (<paramref name="recurringDetailReference"/>).
        /// If the shopperReference doesn't contain any <paramref name="recurringDetailReference"/>s, we remove <paramref name="shopperReference"/>.
        /// </summary>
        /// <param name="shopperReference">The unique shopper reference (usually a GUID to identify your shopper).</param>
        /// <param name="recurringDetailReference">The <paramref name="recurringDetailReference"/> token that is retrieved from <see cref="Controllers.WebhookController.Webhooks(Adyen.Model.Notification.NotificationRequest)"/>.</param>
        /// <returns></returns>
        bool Remove(string shopperReference, string recurringDetailReference);

        /// <summary>
        /// Inserts token (<paramref name="recurringDetailReference"/>) and <paramref name="shopperReference"/>, overwrite if it already exists.
        /// </summary>
        /// <param name="paymentMethod">The payment method from the transaction.</param>
        /// <param name="shopperReference">The unique shopper reference (usually a GUID to identify your shopper).</param>
        /// <param name="recurringDetailReference">The <paramref name="recurringDetailReference"/> token that is retrieved from <see cref="Controllers.WebhookController.Webhooks"/>.</param>
        /// <returns></returns>
        bool Upsert(string paymentMethod, string shopperReference, string recurringDetailReference);
    }

    public class SubscriptionRepository : ISubscriptionRepository
    {
        /// <inheritdoc/>
        public ConcurrentDictionary<string, SubscribedCustomer> SubscribedCustomers { get; }

        public SubscriptionRepository()
        {
            SubscribedCustomers = new ConcurrentDictionary<string, SubscribedCustomer>();
        }

        /// <inheritdoc/>
        public bool Remove(string shopperReference, string recurringDetailReference)
        {
            // Check if subscriber exists.
            if (!SubscribedCustomers.TryGetValue(shopperReference, out SubscribedCustomer customer))
            {
                return false; // No ShopperReference found.
            }

            // Check if token (recurringDetailReference) already exists for the given customer.
            SubscribedCustomerDetails existingCustomerDetails = customer.SubscribedCustomerDetails.FirstOrDefault(x => x.RecurringDetailReference == recurringDetailReference);

            if (existingCustomerDetails == null)
            {
                return false; // No token found for shopper.
            }

            // Removes the token (recurringDetailReference).
            bool isSuccess = customer.SubscribedCustomerDetails.Remove(existingCustomerDetails);

            // If a shopperReference has no recurringDetailReferences left, remove the shopperReference.
            if (!customer.SubscribedCustomerDetails.Any())
            {
                return SubscribedCustomers.TryRemove(shopperReference, out var _);
            }
            return isSuccess;
        }

        /// <inheritdoc/>
        public bool Upsert(string paymentMethod, string shopperReference, string recurringDetailReference)
        {
            // New customer: add the shopper reference and the token (recurringDetailReference).
            if (!SubscribedCustomers.ContainsKey(shopperReference))
            {
                return SubscribedCustomers.TryAdd(shopperReference,
                    new SubscribedCustomer()
                    {
                        ShopperReference = shopperReference,
                        SubscribedCustomerDetails = new List<SubscribedCustomerDetails>()
                        {
                            new SubscribedCustomerDetails()
                            {
                                RecurringDetailReference = recurringDetailReference,
                                PaymentMethod = paymentMethod
                            }
                        }
                    });
            }

            // Existing customer:
            SubscribedCustomerDetails existingCustomerDetails = SubscribedCustomers[shopperReference].SubscribedCustomerDetails.FirstOrDefault(x => x.RecurringDetailReference == recurringDetailReference);

            // Add token (recurringDetailReference) if it doesn't already exist.
            if (existingCustomerDetails == null)
            {
                var details = SubscribedCustomers[shopperReference].SubscribedCustomerDetails;

                if (details.FirstOrDefault(x => x.RecurringDetailReference == recurringDetailReference) == null)
                {
                    details.Add(
                        new SubscribedCustomerDetails()
                        {
                            RecurringDetailReference = recurringDetailReference,
                            PaymentMethod = paymentMethod
                        });
                    return true;
                }
            }

            // Token (recurringDetailReference) was already added before.
            return false;
        }
    }
}
