using adyen_dotnet_in_person_payments_example.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Adyen.Model.TerminalApi;
using Adyen.Model.TerminalApi.Message;
using Adyen.Service;

namespace adyen_dotnet_in_person_payments_example.Services
{
    public interface IPosReversalService
    {
        /// <summary>
        /// Issue a referenced refund by sending a terminal-api payment reversal request.
        /// See: https://docs.adyen.com/point-of-sale/basic-tapi-integration/refund-payment/referenced/?tab=full_refund_1#referenced-request.
        /// For error-scenarios, see: https://docs.adyen.com/point-of-sale/error-scenarios/#error-conditions.
        /// </summary>
        /// <param name="reversalReasonType"><see cref="ReversalReasonType"/>.</param>
        /// <param name="saleTransactionId">Unique Id of a sale global transaction. Appears as MerchantReference in your Customer Area.</param>
        /// <param name="poiTransactionId">Unique Id of a POI transaction.</param>
        /// <param name="poiId">Your unique ID of the terminal to send this request to. Format: [device model]-[serial number]. Seealso <seealso cref="Options.AdyenOptions.ADYEN_POS_POI_ID"/></param>
        /// <param name="saleId">Your unique ID for the POS system (cash register) to send this request from. Seealso <see cref="Options.AdyenOptions.ADYEN_POS_SALE_ID"/>.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns><see cref="SaleToPOIResponse"/>.</returns>
        Task<SaleToPOIResponse> SendReversalRequestAsync(ReversalReasonType reversalReasonType, string saleTransactionId, string poiTransactionId, string poiId, string saleId, CancellationToken cancellationToken = default);
    }

    public class PosReversalService : IPosReversalService
    {
        private readonly ITerminalCloudApi _terminalCloudApi;

        public PosReversalService(ITerminalCloudApi terminalCloudApi)
        {
            _terminalCloudApi = terminalCloudApi;
        }

        public Task<SaleToPOIResponse> SendReversalRequestAsync(ReversalReasonType reversalReasonType, string saleTransactionId, string poiTransactionId, string poiId, string saleId, CancellationToken cancellationToken)
        {
            SaleToPOIRequest request = GetReversalRequest(reversalReasonType, saleTransactionId, poiTransactionId, poiId, saleId);
            return _terminalCloudApi.TerminalRequestAsynchronousAsync(request);
        }

        private SaleToPOIRequest GetReversalRequest(ReversalReasonType reversalReasonType, string saleTransactionId, string poiTransactionId, string poiId, string saleId)
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
                    ServiceID = IdUtility.GetRandomAlphanumericId(10), 
                },
                MessagePayload = new ReversalRequest()
                {
                    OriginalPOITransaction = new OriginalPOITransaction()
                    {
                        SaleID = saleId,
                        POIID = poiId,
                        POITransactionID = new TransactionIdentification()
                        {
                            TimeStamp = DateTime.UtcNow, 
                            TransactionID = poiTransactionId
                        },
                    },
                    SaleData = new SaleData()
                    {
                        SaleTransactionID = new TransactionIdentification()
                        {
                          TimeStamp = DateTime.UtcNow,
                          TransactionID = saleTransactionId
                        }
                    },
                    ReversalReason = reversalReasonType
                }
            };

            return request;
        }
    }
}