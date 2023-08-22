using Adyen.Model.Nexo;
using Adyen.Model.Nexo.Message;
using Adyen.Service;
using adyen_dotnet_in_person_payments_example.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_example.Services
{
    public interface IPosPaymentReversalService
    {
        Task<SaleToPOIResponse> SendReversalRequestAsync(ReversalReasonType reversalReasonType, string saleReferenceId, string poiId, string saleId, decimal? reversedAmount, CancellationToken cancellationToken = default);
    }

    public class PosPaymentReversalService : IPosPaymentReversalService
    {
        private readonly IPosPaymentCloudApi _posPaymentCloudApi;

        public PosPaymentReversalService(IPosPaymentCloudApi posPaymentCloudApi)
        {
            _posPaymentCloudApi = posPaymentCloudApi;
        }

        public Task<SaleToPOIResponse> SendReversalRequestAsync(ReversalReasonType reversalReasonType, string saleReferenceId, string poiId, string saleId, decimal? reversedAmount, CancellationToken cancellationToken)
        {
            SaleToPOIRequest request = GetReversalRequest(reversalReasonType, saleReferenceId, poiId, saleId, reversedAmount, cancellationToken);
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request); // Missing cancellationToken here & naming here is kinda confusing for devs.
        }

        private SaleToPOIRequest GetReversalRequest(ReversalReasonType reversalReasonType, string saleReferenceId, string poiId, string saleId, decimal? reversedAmount, CancellationToken cancellationToken)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    // Not applicable in this case - Optionally, used for Identification of a device message pair. 
                    // Required if MessageClass is Device. The length of the string must be greater than or equal to 1 and less than or equal to 10.
                    // See: https://docs.adyen.com/point-of-sale/design-your-integration/terminal-api/terminal-api-reference/#comadyennexomessageheader.
                    //DeviceID = "YourUnique",

                    MessageCategory = MessageCategoryType.Reversal,
                    MessageClass = MessageClassType.Service,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,

                    // Your unique ID for this request, consisting of 1-10 alphanumeric characters.
                    // Must be unique within the last 48 hours for the terminal (POIID) being used.
                    ServiceID = IdUtility.GetRandomAlphanumericId(10),
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