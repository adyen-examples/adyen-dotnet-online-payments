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
        /// <param name="id">The id of the payment link.</param>
        /// <param name="reference">The reference of the payment link.</param>
        /// <param name="url">The url of payment link.</param>
        /// <param name="expiresAt">Determines when the link expires.</param>
        /// <param name="status">Status of the payment.</param>
        /// <param name="isReusable">Set to true if you'd like to accept multiple payments per payment link.</param>
        /// <returns>True if upserted.</returns>
        bool Upsert(string id, string reference, string url, DateTime expiresAt, string status, bool isReusable);
    }

    public class PaymentLinkRepository : IPaymentLinkRepository
    {
        public ConcurrentDictionary<string, PaymentLinkModel> PaymentLinks { get; }

        public PaymentLinkRepository()
        {
            PaymentLinks = new ConcurrentDictionary<string, PaymentLinkModel>();
        }

        public bool Upsert(string id, string reference, string url, DateTime expiresAt, string status, bool isReusable)
        {
            // New payment link.
            if (!PaymentLinks.TryGetValue(reference, out PaymentLinkModel paymentLink))
            {
                return PaymentLinks.TryAdd(reference,
                    new PaymentLinkModel()
                    {
                        Id = id,
                        Reference = reference,
                        Url = url,
                        ExpiresAt = expiresAt,
                        Status = status,
                        IsReusable = isReusable
                    });
            }

            // Update existing payment link.
            paymentLink.ExpiresAt = expiresAt;
            paymentLink.Status = status;
            paymentLink.Url = url;
            paymentLink.IsReusable = isReusable;

            return false;
        }
    }
}
