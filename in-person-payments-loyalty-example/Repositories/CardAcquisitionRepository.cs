using adyen_dotnet_in_person_payments_loyalty_example.Models;
using System.Collections.Generic;
using System.Linq;

namespace adyen_dotnet_in_person_payments_loyalty_example.Repositories
{
    public interface ICardAcquisitionRepository
    {
        /// <summary>
        /// List of all <see cref="CardAcquisitionModel"/>s.
        /// </summary>
        List<CardAcquisitionModel> CardAcquisitions { get; }

        CardAcquisitionModel Get(string paymentToken);
    }

    public class CardAcquisitionRepository : ICardAcquisitionRepository
    {
        public List<CardAcquisitionModel> CardAcquisitions { get; }

        public CardAcquisitionRepository()
        {
            CardAcquisitions = new List<CardAcquisitionModel>();
        }

        public CardAcquisitionModel Get(string paymentToken)
        {
            if (paymentToken == null)
                return null;
            return CardAcquisitions.FirstOrDefault(x => x.PaymentToken == paymentToken);
        }
    }
}
