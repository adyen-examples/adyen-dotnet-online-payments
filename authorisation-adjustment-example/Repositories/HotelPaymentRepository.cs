using adyen_dotnet_authorisation_adjustment_example.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace adyen_dotnet_authorisation_adjustment_example.Repositories
{
    /// <summary>
    /// Acts as a local (in-memory) repository to store <see cref="HotelPaymentModel"/>s by <see cref="HotelPaymentModel.PspReference"/>.
    /// </summary>
    public interface IHotelPaymentRepository
    {
        /// <summary>
        /// Dictionary of all payments for the hotel bookings by <see cref="HotelPaymentModel.Reference"/>.
        /// Key: <see cref="HotelPaymentModel.Reference"/>  |  
        /// Value: <see cref="List{string, HotelPaymentModel}"/>.
        /// </summary>
        public Dictionary<string, List<HotelPaymentModel>> HotelPayments { get; }

        /// <summary>
        /// Insert <see cref="HotelPaymentModel"/> into the <see cref="HotelPayments"/> dictionary with the <see cref="HotelPaymentModel.PspReference"/> as its key.
        /// </summary>
        /// <param name="hotelPayment"><see cref="HotelPaymentModel"/>.</param>
        /// <returns>True if inserted.</returns>
        bool Insert(HotelPaymentModel hotelPayment);

        /// <summary>
        /// Gets <see cref="HotelPaymentModel"/>s by <see cref="HotelPaymentModel.Reference"/>.
        /// </summary>
        /// <param name="reference"><see cref="HotelPaymentModel.Reference"/>.</param>
        /// <returns><see cref="IEnumerable{HotelPaymentModel}"/>.</returns>
        IEnumerable<HotelPaymentModel> FindByReference(string reference);

        /// <summary>
        /// Gets latest version of <see cref="HotelPaymentModel"/> by <see cref="HotelPaymentModel.Reference"/>.
        /// </summary>
        /// <param name="reference"><see cref="HotelPaymentModel.Reference"/>.</param>
        /// <returns><see cref="HotelPaymentModel"/>.</returns>
        HotelPaymentModel FindLatestHotelPaymentByReference(string reference);

        /// <summary>
        /// Finds the initial preauthorisation <see cref="HotelPaymentModel"/>.
        /// </summary>
        /// <param name="reference"><see cref="HotelPaymentModel.Reference"/>.</param>
        /// <returns><see cref="HotelPaymentModel"/>.</returns>
        HotelPaymentModel FindPreAuthorisationHotelPayment(string reference);
    }

    public class HotelPaymentRepository : IHotelPaymentRepository
    {
        public Dictionary<string, List<HotelPaymentModel>> HotelPayments { get; }

        public HotelPaymentRepository()
        {
            HotelPayments = new Dictionary<string, List<HotelPaymentModel>>();
        }

        public bool Insert(HotelPaymentModel hotelPayment)
        {
            // If `Reference` is not specified, throw an ArgumentNullException.
            if (string.IsNullOrWhiteSpace(hotelPayment.Reference))
            {
                throw new ArgumentNullException(nameof(hotelPayment.Reference));
            }    

            // Check if `Reference` is in the list, do nothing if we've never saved the reference.
            if (!HotelPayments.TryGetValue(hotelPayment.Reference, out var list))
            {
                return false;
            }

            // Check if `PspReference` already exists.
            var existingHotelPayment = list.FirstOrDefault(x => x.PspReference == hotelPayment.PspReference);
            if (existingHotelPayment is null)
            {
                // If it doesn't exists, we add it.
                list.Add(hotelPayment);
                return true;
            }

            // If the values are exactly the same (f.e. when we receive the webhook twice).
            // Consider it a duplicate, and do not add anything to the list.
            if (hotelPayment.IsEqual(existingHotelPayment))
            {
                return false;
            }

            // Add the hotel payment.
            list.Add(hotelPayment);
            return true;
        }

        public IEnumerable<HotelPaymentModel> FindByReference(string reference)
        {
            if (!HotelPayments.TryGetValue(reference, out List<HotelPaymentModel> result))
                return Enumerable.Empty<HotelPaymentModel>();

            return result;
        }

        public HotelPaymentModel FindLatestHotelPaymentByReference(string reference)
        {
            List<HotelPaymentModel> result = FindByReference(reference)
                .OrderBy(x => x.DateTime)
                .ToList();

            return result.LastOrDefault();
        }

        public HotelPaymentModel FindPreAuthorisationHotelPayment(string reference)
        {
            if (!HotelPayments.TryGetValue(reference, out List<HotelPaymentModel> result))
                return null;

            return result.FirstOrDefault();
        }
    }
}