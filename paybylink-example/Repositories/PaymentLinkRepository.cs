using adyen_dotnet_paybylink_example.Models;
using System;
using System.Collections.Concurrent;

namespace adyen_dotnet_paybylink_example.Repositories
{
    /// <summary>
    /// Acts as a local (memory) repository to store <see cref="PaymentLinkModel"/>s.
    /// </summary>
    public interface IPaymentLinkRepository
    {
        /// <summary>
        /// List of all <see cref="PaymentLinkModel"/>s that are created.
        /// </summary>
        ConcurrentDictionary<string, PaymentLinkModel> PaymentLinks { get; }

        /// <summary>
        /// Inserts new payment link with <paramref name="reference"/>, update <paramref name="expiresAt"/> and <paramref name="status"/> if it already exists.
        /// </summary>
        /// <param name="reference">The unique reference of the payment link.</param>
        /// <param name="pspReference">The psp reference of the payment link.</param>
        /// <param name="url">The url of payment link.</param>
        /// <param name="expiresAt">Determines when the link expires.</param>
        /// <param name="status">Status of the payment.</param>
        /// <returns>True if upserted.</returns>
        bool Upsert(string reference, string pspReference, string url, DateTime expiresAt, string status);
    }

    public class PaymentLinkRepository : IPaymentLinkRepository
    {
        public ConcurrentDictionary<string, PaymentLinkModel> PaymentLinks { get; }

        public PaymentLinkRepository()
        {
            PaymentLinks = new ConcurrentDictionary<string, PaymentLinkModel>();
        }

        public bool Upsert(string reference, string pspReference, string url, DateTime expiresAt, string status)
        {
            // New payment link:
            if (!PaymentLinks.TryGetValue(reference, out PaymentLinkModel paymentlink))
            {
                return PaymentLinks.TryAdd(reference,
                    new PaymentLinkModel()
                    {
                        Reference = reference,
                        PspReference = pspReference,
                        Url = url,
                        ExpiresAt = expiresAt,
                        Status = status
                    });
            }

            // Existing payment link:
            paymentlink.ExpiresAt = expiresAt;
            paymentlink.Status = status;
            paymentlink.Url = url; // TODO: Investigate whether the url can change when you update the existing reference?

            return false;
        }
    }
}
