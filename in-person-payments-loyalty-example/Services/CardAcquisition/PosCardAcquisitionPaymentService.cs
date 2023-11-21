using Adyen.Model.Nexo;
using Adyen.Model.Nexo.Message;
using Adyen.Model.Terminal;
using Adyen.Service;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_loyalty_example.Services.CardAcquisition
{
    public interface IPosCardAcquisitionPaymentService
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
        /// <param name="cardAcquisitionTimeStamp">The timestamp from the card acquisition, se ealso <see cref="PosCardAcquisition.SendCardAcquisitionRequest(string, string, string, decimal?, CancellationToken)"/>.</param>
        /// <param name="cardAcquisitionTransactionId">The transaction ID from the card acquisition response, see also <seealso cref="PosCardAcquisition.SendCardAcquisitionRequest(string, string, string, decimal?, CancellationToken)"/>.</param>
        /// <param name="transactionId">Your transactionId, this will appear as 'MerchantReference' in your Customer Area.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns><see cref="SaleToPOIResponse"/>.</returns>
        Task<SaleToPOIResponse> SendPaymentRequestExistingCustomerAsync(string serviceId, string poiId, string saleId, string currency, decimal? amount, DateTime cardAcquisitionTimeStamp, string cardAcquisitionTransactionId, string transactionId, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a terminal-api payment request for the specified <paramref name="amount"/> and <paramref name="currency"/>.
        /// See: https://docs.adyen.com/point-of-sale/basic-tapi-integration/make-a-payment/.
        /// </summary>
        /// <param name="serviceId">Your unique ID for this request, consisting of 1-10 alphanumeric characters. Must be unique within the last 48 hours for the terminal (POIID) being used. Generated using <see cref="Utilities.IdUtility.GetRandomAlphanumericId(int0)"/>.</param>
        /// <param name="poiId">Your unique ID of the terminal to send this request to. Format: [device model]-[serial number]. Seealso <seealso cref="Options.AdyenOptions.ADYEN_POS_POI_ID"/></param>
        /// <param name="saleId">Your unique ID for the POS system (cash register) to send this request from. Seealso <see cref="Options.AdyenOptions.ADYEN_POS_SALE_ID"/>.</param>
        /// <param name="currency">Your <see cref="AmountsReq.Currency"/> (example: "EUR", "USD").</param>
        /// <param name="amount">Your <see cref="AmountsReq.RequestedAmount"/> in DECIMAL units (example: 42.99), the terminal API does not use minor units.</param>
        /// <param name="shopperEmail">The email of your shopper, used for card acquisition loyalty points.</param>
        /// <param name="shopperReference">The unique (ID) reference for your shopper, used for card acquisition loyalty points.</param>
        /// <param name="cardAcquisitionTimeStamp">The timestamp from the card acquisition, see also <see cref="PosCardAcquisition.SendCardAcquisitionRequest(string, string, string, decimal?, CancellationToken)"/>.</param>
        /// <param name="cardAcquisitionTransactionId">The transaction ID from the card acquisition response, see also <seealso cref="PosCardAcquisition.SendCardAcquisitionRequest(string, string, string, decimal?, CancellationToken)"/>.</param>
        /// <param name="transactionId">Your transactionId, this will appear as 'MerchantReference' in your Customer Area.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns><see cref="SaleToPOIResponse"/>.</returns>
        Task<SaleToPOIResponse> SendPaymentRequestNewCustomerAsync(string serviceId, string poiId, string saleId, string currency, decimal? amount, string shopperEmail, string shopperReference, DateTime cardAcquisitionTimeStamp, string cardAcquisitionTransactionId, string transactionId, CancellationToken cancellationToken = default);
    }

    public class PosCardAcquisitionPaymentService : IPosCardAcquisitionPaymentService
    {
        private readonly IPosPaymentCloudApi _posPaymentCloudApi;

        public PosCardAcquisitionPaymentService(IPosPaymentCloudApi posPaymentCloudApi)
        {                       
            _posPaymentCloudApi = posPaymentCloudApi;
        }

        public Task<SaleToPOIResponse> SendPaymentRequestNewCustomerAsync(string serviceId, string poiId, string saleId, string currency, decimal? amount, string shopperEmail, string shopperReference, DateTime cardAcquisitionTimeStamp, string cardAcquisitionTransactionId, string transactionId, CancellationToken cancellationToken)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    MessageCategory = MessageCategoryType.Payment,
                    MessageClass = MessageClassType.Service,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,
                    ServiceID = serviceId,
                },
                MessagePayload = new PaymentRequest()
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
                        SaleToAcquirerData = new SaleToAcquirerData()
                        {
                            ShopperEmail = shopperEmail,
                            ShopperReference = shopperReference,
                            RecurringContract = "ONECLICK"
                        },
                        TokenRequestedType = TokenRequestedType.Customer
                    },
                    PaymentTransaction = new PaymentTransaction()
                    {
                        AmountsReq = new AmountsReq()
                        {
                            Currency = currency,
                            RequestedAmount = amount
                        }
                    },
                    PaymentData = new PaymentData()
                    {
                        CardAcquisitionReference = new TransactionIdentification()
                        {
                            TimeStamp = cardAcquisitionTimeStamp,
                            TransactionID = cardAcquisitionTransactionId
                        }
                    }
                }
            };

            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request);
        }

        public Task<SaleToPOIResponse> SendPaymentRequestExistingCustomerAsync(string serviceId, string poiId, string saleId, string currency, decimal? amount, DateTime cardAcquisitionTimeStamp, string cardAcquisitionTransactionId, string transactionId, CancellationToken cancellationToken)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    MessageCategory = MessageCategoryType.Payment,
                    MessageClass = MessageClassType.Service,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,
                    ServiceID = serviceId,
                },
                MessagePayload = new PaymentRequest()
                {
                    SaleData = new SaleData()
                    {
                        SaleTransactionID = new TransactionIdentification()
                        {
                            // Your reference to identify a payment. We recommend using a unique value per payment.
                            // In your Customer Area and Adyen reports, this will show as the merchant reference for the transaction.
                            TransactionID = transactionId,
                            TimeStamp = DateTime.UtcNow,
                        },
                        TokenRequestedType = TokenRequestedType.Customer
                    },
                    PaymentTransaction = new PaymentTransaction()
                    {
                        AmountsReq = new AmountsReq()
                        {
                            Currency = currency,
                            RequestedAmount = amount
                        }
                    },
                    PaymentData = new PaymentData()
                    {
                        CardAcquisitionReference = new TransactionIdentification()
                        {
                            TimeStamp = cardAcquisitionTimeStamp,
                            TransactionID = cardAcquisitionTransactionId
                        }
                    }
                }
            };
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request);
        }
    }
}