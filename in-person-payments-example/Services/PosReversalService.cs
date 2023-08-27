using Adyen.Model.Nexo;
using Adyen.Model.Nexo.Message;
using Adyen.Service;
using adyen_dotnet_in_person_payments_example.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_example.Services
{
    public interface IPosReversalService
    {
        /// <summary>
        /// Sends a terminal-api payment reversal request for the specified <paramref name="saleReferenceId"/>, <paramref name="poiId"/> and <paramref name="saleId"/>.
        /// </summary>
        /// <param name="reversalReasonType"><see cref="ReversalReasonType"/>.</param>
        /// <param name="saleReferenceId">Identification of a Sale global transaction for a sequence of related POI transactions: https://docs.adyen.com/point-of-sale/design-your-integration/terminal-api/terminal-api-reference/#comadyennexosaledata.</param>
        /// <param name="poiId">The unique ID of the terminal to send this request to. Format: [device model]-[serial number].</param>
        /// <param name="saleId">Your unique ID for the POS system (cash register) to send this request from.</param>
        /// <param name="reversedAmount">Amount in DECIMAL units (example: 42.99), the terminal API does not use minor units.</param>        
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns><see cref="SaleToPOIResponse"/>.</returns>
        Task<SaleToPOIResponse> SendReversalRequestAsync(ReversalReasonType reversalReasonType, string saleReferenceId, string poiId, string saleId, decimal? reversedAmount, CancellationToken cancellationToken = default);
    }

    public class PosReversalService : IPosReversalService
    {
        private readonly IPosPaymentCloudApi _posPaymentCloudApi;

        public PosReversalService(IPosPaymentCloudApi posPaymentCloudApi)
        {
            _posPaymentCloudApi = posPaymentCloudApi;
        }

        public Task<SaleToPOIResponse> SendReversalRequestAsync(ReversalReasonType reversalReasonType, string saleReferenceId, string poiId, string saleId, decimal? reversedAmount, CancellationToken cancellationToken)
        {
            SaleToPOIRequest request = GetReversalRequest(reversalReasonType, saleReferenceId, poiId, saleId, reversedAmount);
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request); // Missing cancellationToken here & naming here is kinda confusing for devs.
        }

        private SaleToPOIRequest GetReversalRequest(ReversalReasonType reversalReasonType, string saleReferenceId, string poiId, string saleId, decimal? reversedAmount)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    MessageCategory = MessageCategoryType.Reversal,
                    MessageClass = MessageClassType.Service,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,

                    // The unique ID of a message pair, which processes the transaction.
                    // Your unique ID for this request, consisting of 1-10 alphanumeric characters.
                    // Must be unique within the last 48 hours for the terminal (POIID) being used.
                    ServiceID = IdUtility.GetRandomAlphanumericId(10), /// Used in <see cref="IPosPaymentTransactionStatusService"/> and <see cref="IPosAbortService"/> and <see cref="IPosReversalService"/>
                },
                MessagePayload = new ReversalRequest()
                {
                    OriginalPOITransaction = new OriginalPOITransaction()
                    {
                        SaleID = saleId,
                        POIID = poiId,
                    },
                    ReversalReason = reversalReasonType,
                    ReversedAmount = reversedAmount,
                    ReversedAmountSpecified = true,
                    SaleReferenceID = saleReferenceId
                }
            };

            return request;
        }
    }
}