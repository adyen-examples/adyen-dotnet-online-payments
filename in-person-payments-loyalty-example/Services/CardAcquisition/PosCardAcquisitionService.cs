﻿using Adyen.Model.Nexo;
using Adyen.Model.Nexo.Message;
using Adyen.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_loyalty_example.Services.CardAcquisition
{
    public interface IPosCardAcquisitionService
    {
        Task<SaleToPOIResponse> SendCardAcquisitionRequestAsync(string serviceId, string poiId, string saleId, string transactionId, decimal? amount, CancellationToken cancellationToken = default);
    }

    public class PosCardAcquisitionService : IPosCardAcquisitionService
    {
        private readonly IPosPaymentCloudApi _posPaymentCloudApi;

        public PosCardAcquisitionService(IPosPaymentCloudApi posPaymentCloudApi)
        {
            _posPaymentCloudApi = posPaymentCloudApi;
        }

        public Task<SaleToPOIResponse> SendCardAcquisitionRequestAsync(string serviceId, string poiId, string saleId, string transactionId, decimal? amount, CancellationToken cancellationToken = default)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    // Not applicable in this case - Optionally, used for Identification of a device message pair. 
                    // Required if MessageClass is Device. The length of the string must be greater than or equal to 1 and less than or equal to 10.
                    // See: https://docs.adyen.com/point-of-sale/design-your-integration/terminal-api/terminal-api-reference/#comadyennexomessageheader.
                    //DeviceID = "YourUniqueDeviceID",

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
                            TransactionID = transactionId,
                            TimeStamp = DateTime.UtcNow
                        },
                        TokenRequestedType = TokenRequestedType.Customer
                    },
                    CardAcquisitionTransaction = new CardAcquisitionTransaction()
                    {
                        TotalAmount = amount,
                        LoyaltyHandling = LoyaltyHandlingType.Allowed, 
                    }
                }
            };
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request);
        }
    }
}