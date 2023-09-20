using Adyen.Model.Nexo;
using Adyen.Model.Nexo.Message;
using Adyen.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_loyalty_example.Services
{
    public interface IPosCardAcquisition
    {
        Task<SaleToPOIResponse> SendCardAcquisitionRequest(string serviceId, string poiId, string saleId, decimal? amount = 0.0M, CancellationToken cancellationToken = default);
    }

    public class PosCardAcquisition : IPosCardAcquisition
    {
        private readonly IPosPaymentCloudApi _posPaymentCloudApi;

        public PosCardAcquisition(IPosPaymentCloudApi posPaymentCloudApi)
        {
            _posPaymentCloudApi = posPaymentCloudApi;
        }

        public Task<SaleToPOIResponse> SendCardAcquisitionRequest(string serviceId, string poiId, string saleId, decimal? amount = 0.0M, CancellationToken cancellationToken = default)
        {
            SaleToPOIRequest request = GetCardAcquisitionRequest(serviceId, poiId, saleId, amount);
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request);
        }

        private SaleToPOIRequest GetCardAcquisitionRequest(string serviceId, string poiId, string saleId, decimal? amount)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    // Not applicable in this case - Optionally, used for Identification of a device message pair. 
                    // Required if MessageClass is Device. The length of the string must be greater than or equal to 1 and less than or equal to 10.
                    // See: https://docs.adyen.com/point-of-sale/design-your-integration/terminal-api/terminal-api-reference/#comadyennexomessageheader.
                    //DeviceID = "YourUnique",

                    MessageCategory = MessageCategoryType.CardAcquisition,
                    MessageClass = MessageClassType.Service,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,
                    ServiceID = serviceId, 
                },
                MessagePayload = new CardAcquisitionRequest()
                {
                    SaleData = new SaleData()
                    {
                        SaleTransactionID = new TransactionIdentification()
                        {
                            // Your reference to identify a payment. We recommend using a unique value per payment.
                            // In your Customer Area and Adyen reports, this will show as the merchant reference for the transaction.
                            TransactionID = Guid.NewGuid().ToString(),
                            TimeStamp = DateTime.UtcNow
                        },
                        TokenRequestedType = TokenRequestedType.Customer
                    },
                    CardAcquisitionTransaction = new CardAcquisitionTransaction()
                    {
                        TotalAmount = amount
                    }
                }
            };

            return request;
        }
    }
}