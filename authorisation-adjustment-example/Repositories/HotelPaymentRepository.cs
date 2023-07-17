using adyen_dotnet_authorisation_adjustment_example.Models;
using System.Collections.Concurrent;

namespace adyen_dotnet_authorisation_adjustment_example.Repositories
{
    /// <summary>
    /// Acts as a local (memory) repository to store <see cref="HotelPaymentModel"/>s.
    /// </summary>
    public interface IHotelPaymentRepository
    {        
        /// <summary>
        /// Dictionary of all payments for the hotel bookings by <see cref="HotelPaymentModel.PspReference"/>.
        /// Key: <see cref="HotelPaymentModel.PspReference"/> | Value: <see cref="HotelPaymentModel"/>.
        /// </summary>
        public ConcurrentDictionary<string, HotelPaymentModel> HotelPayments { get; }

        /// <summary>
        /// Insert <see cref="HotelPaymentModel"/> into the <see cref="HotelPayments"/> dictionary with the <see cref="HotelPaymentModel.PspReference"/> as its key.
        /// </summary>
        /// <param name="hotelPayment"><see cref="HotelPaymentModel"/>.</param>
        /// <returns>True if inserted <inheritdocthe/> dictionary.</returns>
        bool Insert(HotelPaymentModel hotelPayment);

        /// <summary>
        /// Gets <see cref="HotelPaymentModel"/> by <see cref="HotelPaymentModel.PspReference"/>.
        /// </summary>
        /// <param name="pspReference"><see cref="HotelPaymentModel.PspReference"/>.</param>
        /// <returns><see cref="HotelPaymentModel"/>.</returns>
        HotelPaymentModel GetByPspReference(string pspReference);

        /// <summary>
        /// Gets <see cref="HotelPaymentModel"/> by <see cref="HotelPaymentModel.Reference"/>.
        /// </summary>
        /// <param name="reference"><see cref="HotelPaymentModel.Reference"/>.</param>
        /// <returns><see cref="HotelPaymentModel"/>.</returns>
        HotelPaymentModel GetByReference(string reference);
    }

    public class HotelPaymentRepository : IHotelPaymentRepository
    {
        public ConcurrentDictionary<string, HotelPaymentModel> HotelPayments { get; }

        public HotelPaymentRepository()
        {
            HotelPayments = new ConcurrentDictionary<string, HotelPaymentModel>();
        }

        public bool Insert(HotelPaymentModel hotelPayment)
        {
            return HotelPayments.TryAdd(hotelPayment.PspReference, hotelPayment);
        }

        public HotelPaymentModel GetByPspReference(string pspReference)
        {
            if (!HotelPayments.TryGetValue(pspReference, out var hotelPaymentModel))
                return null;
            return hotelPaymentModel;
        }

        public HotelPaymentModel GetByReference(string reference)
        {
            foreach (var kvp in HotelPayments)
            {
                if (kvp.Value.Reference == reference)
                {
                    return kvp.Value;
                }
            }
            return null;
        }
    }
}