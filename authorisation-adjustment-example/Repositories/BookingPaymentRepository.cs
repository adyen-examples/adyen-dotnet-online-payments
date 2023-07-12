using adyen_dotnet_authorisation_adjustment_example.Models;
using System.Collections.Concurrent;

namespace adyen_dotnet_authorisation_adjustment_example.Repositories
{
    /// <summary>
    /// Acts as a local (memory) repository to store <see cref="BookingPaymentModel"/>s.
    /// </summary>
    public interface IBookingPaymentRepository
    {
        ConcurrentDictionary<string, BookingPaymentModel> BookingPayments { get; }
       
        bool Remove(string pspReference);

        bool Upsert(BookingPaymentModel bookingPaymentModel);
    }

    public class BookingPaymentRepository : IBookingPaymentRepository
    {
        public ConcurrentDictionary<string, BookingPaymentModel> BookingPayments { get; }

        public BookingPaymentRepository()
        {
            BookingPayments = new ConcurrentDictionary<string, BookingPaymentModel>();
        }

        public bool Remove(string pspReference)
        {
            return BookingPayments.TryRemove(pspReference, out var _);
        }

        public bool Upsert(BookingPaymentModel bookingPaymentModel)
        {
            return BookingPayments.TryAdd(bookingPaymentModel.PspReference, bookingPaymentModel);
        }
    }
}