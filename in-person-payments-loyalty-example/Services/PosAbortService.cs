using System.Threading;
using System.Threading.Tasks;
using adyen_dotnet_in_person_payments_loyalty_example.Utilities;
using Adyen.Model.TerminalApi;
using Adyen.Model.TerminalApi.Message;
using Adyen.Service;

namespace adyen_dotnet_in_person_payments_loyalty_example.Services
{
    public interface IPosAbortService
    {
        /// <summary>
        /// Sends a terminal-api abort request which cancels an in-progress card acquisition.
        /// See: https://docs.adyen.com/point-of-sale/basic-tapi-integration/cancel-a-transaction/.
        /// </summary>
        /// <param name="poiId">Your unique ID of the terminal to send this request to. Format: [device model]-[serial number]. Seealso <seealso cref="Options.AdyenOptions.ADYEN_POS_POI_ID"/></param>
        /// <param name="saleId">Your unique ID for the POS system (cash register) to send this request from. Seealso <see cref="Options.AdyenOptions.ADYEN_POS_SALE_ID"/>.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns><see cref="SaleToPOIResponse"/>.</returns>
        Task<SaleToPOIResponse> SendAbortRequestAsync(string poiId, string saleId, CancellationToken cancellationToken = default);
    }

    public class PosAbortService : IPosAbortService
    {
        private readonly ITerminalCloudApi _terminalCloudApi;

        public PosAbortService(ITerminalCloudApi terminalCloudApi)
        {
            _terminalCloudApi = terminalCloudApi;
        }

        public Task<SaleToPOIResponse> SendAbortRequestAsync(string poiId, string saleId, CancellationToken cancellationToken = default)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    MessageCategory = MessageCategoryType.EnableService,
                    MessageClass = MessageClassType.Service,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,
                    ServiceID = IdUtility.GetRandomAlphanumericId(10),
                },
                MessagePayload = new EnableServiceRequest()
                {
                    TransactionAction = TransactionActionType.AbortTransaction
                }
            };

            return _terminalCloudApi.TerminalRequestSynchronousAsync(request);
        }
    }
}