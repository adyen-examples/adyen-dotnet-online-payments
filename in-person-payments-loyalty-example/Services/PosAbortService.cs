using Adyen.Model.Nexo;
using Adyen.Model.Nexo.Message;
using Adyen.Service;
using adyen_dotnet_in_person_payments_loyalty_example.Utilities;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_loyalty_example.Services
{
    public interface IPosAbortService
    {
        /// <summary>
        /// Sends a terminal-api abort request which cancels an in-progress transaction.
        /// See: https://docs.adyen.com/point-of-sale/basic-tapi-integration/cancel-a-transaction/.
        /// </summary>
        /// <param name="serviceId">Your unique ID for this request, consisting of 1-10 alphanumeric characters. Must be unique within the last 48 hours for the terminal (POIID) being used. Generated using <see cref="Utilities.IdUtility.GetRandomAlphanumericId(int0)"/>.</param>
        /// <param name="poiId">Your unique ID of the terminal to send this request to. Format: [device model]-[serial number]. Seealso <seealso cref="Options.AdyenOptions.ADYEN_POS_POI_ID"/></param>
        /// <param name="saleId">Your unique ID for the POS system (cash register) to send this request from. Seealso <see cref="Options.AdyenOptions.ADYEN_POS_SALE_ID"/>.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns><see cref="SaleToPOIResponse"/>.</returns>
        Task<SaleToPOIResponse> SendAbortRequestAsync(string serviceId, string poiId, string saleId, CancellationToken cancellationToken = default);
    }

    public class PosAbortService : IPosAbortService
    {
        private readonly IPosPaymentCloudApi _posPaymentCloudApi;

        public PosAbortService(IPosPaymentCloudApi posPaymentCloudApi)
        {
            _posPaymentCloudApi = posPaymentCloudApi;
        }

        public Task<SaleToPOIResponse> SendAbortRequestAsync(string serviceId, string poiId, string saleId, CancellationToken cancellationToken)
        {
            SaleToPOIRequest request = GetAbortRequest(serviceId, poiId, saleId);
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request);
        }

        private SaleToPOIRequest GetAbortRequest(string serviceId, string poiId, string saleId)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    MessageCategory = MessageCategoryType.Abort,
                    MessageClass = MessageClassType.Service,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,
                    ServiceID = IdUtility.GetRandomAlphanumericId(10)
                },
                MessagePayload = new AbortRequest()
                { 
                    MessageReference = new MessageReference()
                    {
                        MessageCategory = MessageCategoryType.Payment,
                        ServiceID = serviceId,
                        POIID = poiId, 
                        SaleID = saleId
                    },
                    AbortReason = "MerchantAbort"
                }
            };

            return request;
        }
    }
}