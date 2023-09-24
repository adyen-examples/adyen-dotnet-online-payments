using Adyen.Model.Nexo;
using Adyen.Model.Nexo.Message;
using Adyen.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_loyalty_example.Services
{
    public interface IPosPaymentService
    {
        /// <summary>
        /// Sends a terminal-api payment request for the specified <paramref name="amount"/> and <paramref name="currency"/>.
        /// See: https://docs.adyen.com/point-of-sale/basic-tapi-integration/make-a-payment/.
        /// </summary>
        /// <param name="serviceId">Your unique ID for this request, consisting of 1-10 alphanumeric characters. Must be unique within the last 48 hours for the terminal (POIID) being used. Generated using <see cref="Utilities.IdUtility.GetRandomAlphanumericId(int0)"/>.</param>
        /// <param name="poiId">Your unique ID of the terminal to send this request to. Format: [device model]-[serial number]. Seealso <seealso cref="Options.AdyenOptions.ADYEN_POS_POI_ID"/></param>
        /// <param name="saleId">Your unique ID for the POS system (cash register) to send this request from. Seealso <see cref="Options.AdyenOptions.ADYEN_POS_SALE_ID"/>.</param>
        /// <param name="currency">Your <see cref="AmountsReq.Currency"/> (example: "EUR", "USD").</param>
        /// <param name="amount">Your <see cref="AmountsReq.RequestedAmount"/> in DECIMAL units (example: 42.99), the terminal API does not use minor units.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns><see cref="SaleToPOIResponse"/>.</returns>
        Task<SaleToPOIResponse> SendPaymentRequestAsync(string serviceId, string poiId, string saleId, string currency, decimal? amount, CancellationToken cancellationToken = default);
    }

    public class PosPaymentService : IPosPaymentService
    {
        private readonly IPosPaymentCloudApi _posPaymentCloudApi;

        public PosPaymentService(IPosPaymentCloudApi posPaymentCloudApi)
        {
            _posPaymentCloudApi = posPaymentCloudApi;
        }

        public Task<SaleToPOIResponse> SendPaymentRequestAsync(string serviceId, string poiId, string saleId, string currency, decimal? amount, CancellationToken cancellationToken)
        {
            SaleToPOIRequest request = GetPaymentRequest(serviceId, poiId, saleId, currency, amount);
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request);
        }

        private SaleToPOIRequest GetPaymentRequest(string serviceId, string poiId, string saleId, string currency, decimal? amount)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    // Not applicable in this case - Optionally, used for Identification of a device message pair. 
                    // Required if MessageClass is Device. The length of the string must be greater than or equal to 1 and less than or equal to 10.
                    // See: https://docs.adyen.com/point-of-sale/design-your-integration/terminal-api/terminal-api-reference/#comadyennexomessageheader.
                    //DeviceID = "YourUnique",

                    MessageCategory = MessageCategoryType.Payment,
                    MessageClass = MessageClassType.Service,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,
                    ServiceID = serviceId
                },
                MessagePayload = new PaymentRequest()
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
                        // A basic Terminal API payment request includes the amount, currency, TransactionID, and timestamp of the transaction.
                        // If you want to add information that you want Adyen to apply to your request and save on the Adyen payments platform.
                        // Set the SaleToAcquirerData, see: https://docs.adyen.com/point-of-sale/add-data/
                        // Example with SaleToAcquirerData: https://docs.adyen.com/point-of-sale/add-data/sale-to-acquirer-data/.
                        //SaleToAcquirerData = new Adyen.Model.Terminal.SaleToAcquirerData()
                        //{

                        //}
                    },
                    PaymentTransaction = new PaymentTransaction()
                    {
                        AmountsReq = new AmountsReq()
                        {
                            Currency = currency,
                            RequestedAmount = amount
                        }
                    }
                }
            };

            return request;
        }
    }
}