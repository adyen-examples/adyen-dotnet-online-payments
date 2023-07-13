using adyen_dotnet_authorisation_adjustment_example.Models;
using System.Collections.Concurrent;

namespace adyen_dotnet_authorisation_adjustment_example.Repositories
{
    /// <summary>
    /// Acts as a local (memory) repository to store <see cref="HotelPaymentModel"/>s.
    /// </summary>
    public interface IHotelPaymentRepository
    {
        ConcurrentDictionary<string, HotelPaymentModel> HotelPayments { get; }

        bool Upsert(HotelPaymentModel hotelPayment);
    }

    public class HotelPaymentRepository : IHotelPaymentRepository
    {
        public ConcurrentDictionary<string, HotelPaymentModel> HotelPayments { get; }

        public HotelPaymentRepository()
        {
            HotelPayments = new ConcurrentDictionary<string, HotelPaymentModel>();
        }

        public bool Upsert(HotelPaymentModel hotelPayment)
        {
            return HotelPayments.TryAdd(hotelPayment.PspReference, hotelPayment);
        }
    }
}