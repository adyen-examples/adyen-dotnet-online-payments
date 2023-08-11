using Adyen.Model.Nexo;
using Adyen.Model.Nexo.Message;
using Adyen.Service;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_example.Services
{
    public class InPersonPaymentService
    {
        private static string AlphanumericCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        private static Random Random = new Random();

        private readonly IPosPaymentCloudApi _posPaymentCloudApi;

        public InPersonPaymentService(IPosPaymentCloudApi posPaymentCloudApi)
        {
            _posPaymentCloudApi = posPaymentCloudApi;
        }

        public Task<SaleToPOIResponse> SendSaleToPOIRequest(string currency, decimal? amount, CancellationToken cancellationToken = default)
        {
            SaleToPOIRequest request = GetPaymentRequest(currency, amount);
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request); // Missing cancellationToken here.
        }

        private SaleToPOIRequest GetPaymentRequest(string currency, decimal? amount)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    //DeviceID = "", // TODO: What is this used for? Probably some other functionality but not needed for MessageCatevory.Payment
                    MessageCategory = MessageCategoryType.Payment,
                    MessageClass = MessageClassType.Service,
                    MessageType = MessageType.Request,
                    POIID = "", // The unique ID of the terminal to send this request to. Format: [device model]-[serial number].
                    SaleID = "Kwok_POSMachine_V400m_001", // Your unique ID for the POS system component to send this request from.
                    
                    // Is this your idem-potency key?
                    ServiceID = GetRandomAlphanumericId(10), // Your unique ID for this request, consisting of 1-10 alphanumeric characters. Must be unique within the last 48 hours for the terminal (POIID) being used.
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
                        //SaleToAcquirerData = // TODO: What is this used for? Local integration I guess?
                    },
                    PaymentTransaction = new PaymentTransaction()
                    {
                        AmountsReq = new AmountsReq()
                        {
                            Currency = currency,
                            RequestedAmount = amount
                        }
                    }
                },
                //SecurityTrailer = new ContentInformation() { } // TODO: What is this used for?
            };

            return request;
        }

        private string GetRandomAlphanumericId(int length)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                sb.Append(AlphanumericCharacters[Random.Next(length)]);
            }

            return sb.ToString();
        }
    }
}