using adyen_dotnet_authorisation_adjustment_example.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace adyen_dotnet_authorisation_adjustment_example.Repositories
{
    /// <summary>
    /// Acts as a local (in-memory) repository to store <see cref="PaymentModel"/>s by <see cref="PaymentModel.PspReference"/>.
    /// </summary>
    public interface IPaymentRepository
    {
        /// <summary>
        /// Dictionary of all payments for the hotel bookings by <see cref="PaymentModel.Reference"/>.
        /// Key: <see cref="PaymentModel.Reference"/>  |  
        /// Value: <see cref="List{string, PaymentModel}"/>.
        /// </summary>
        public Dictionary<string, List<PaymentModel>> Payments { get; }

        /// <summary>
        /// Insert <see cref="PaymentModel"/> into the <see cref="Payments"/> dictionary with the <see cref="PaymentModel.PspReference"/> as its key.
        /// </summary>
        /// <param name="payment"><see cref="PaymentModel"/>.</param>
        /// <returns>True if inserted.</returns>
        bool Insert(PaymentModel payment);

        /// <summary>
        /// Gets <see cref="PaymentModel"/>s by <see cref="PaymentModel.Reference"/>.
        /// </summary>
        /// <param name="reference"><see cref="PaymentModel.Reference"/>.</param>
        /// <returns><see cref="IEnumerable{PaymentModel}"/>.</returns>
        IEnumerable<PaymentModel> FindByReference(string reference);

        /// <summary>
        /// Gets latest version of <see cref="PaymentModel"/> by <see cref="PaymentModel.Reference"/>.
        /// </summary>
        /// <param name="reference"><see cref="PaymentModel.Reference"/>.</param>
        /// <returns><see cref="PaymentModel"/>.</returns>
        PaymentModel FindLatestPaymentByReference(string reference);

        /// <summary>
        /// Finds the initial preauthorisation <see cref="PaymentModel"/>.
        /// </summary>
        /// <param name="reference"><see cref="PaymentModel.Reference"/>.</param>
        /// <returns><see cref="PaymentModel"/>.</returns>
        PaymentModel FindPreAuthorisationPayment(string reference);
    }

    public class PaymentRepository : IPaymentRepository
    {
        public Dictionary<string, List<PaymentModel>> Payments { get; }

        public PaymentRepository()
        {
            Payments = new Dictionary<string, List<PaymentModel>>();
        }

        public bool Insert(PaymentModel payment)
        {
            // If `Reference` is not specified, throw an ArgumentNullException.
            if (string.IsNullOrWhiteSpace(payment.Reference))
            {
                throw new ArgumentNullException(nameof(payment.Reference));
            }    

            // Check if `Reference` is in the list, do nothing if we've never saved the reference.
            if (!Payments.TryGetValue(payment.Reference, out var list))
            {
                return false;
            }

            // Check if `PspReference` already exists.
            var existingPayment = list.FirstOrDefault(x => x.PspReference == payment.PspReference);
            if (existingPayment is null)
            {
                // If it doesn't exists, we add it.
                list.Add(payment);
                return true;
            }

            // If the values are exactly the same (f.e. when we receive the webhook twice).
            // Consider it a duplicate, and do not add anything to the list.
            if (payment.IsEqual(existingPayment))
            {
                return false;
            }

            // Add the payment.
            list.Add(payment);
            return true;
        }

        public IEnumerable<PaymentModel> FindByReference(string reference)
        {
            if (!Payments.TryGetValue(reference, out List<PaymentModel> result))
                return Enumerable.Empty<PaymentModel>();

            return result;
        }

        public PaymentModel FindLatestPaymentByReference(string reference)
        {
            List<PaymentModel> result = FindByReference(reference)
                .OrderBy(x => x.DateTime)
                .ToList();

            return result.LastOrDefault();
        }

        public PaymentModel FindPreAuthorisationPayment(string reference)
        {
            if (!Payments.TryGetValue(reference, out List<PaymentModel> result))
                return null;

            return result.FirstOrDefault();
        }
    }
}