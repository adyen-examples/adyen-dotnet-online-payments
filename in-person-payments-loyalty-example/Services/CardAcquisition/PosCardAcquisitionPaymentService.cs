using Adyen.Model.Nexo;
using Adyen.Model.Nexo.Message;
using Adyen.Model.Terminal;
using Adyen.Service;
using System;
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
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns><see cref="SaleToPOIResponse"/>.</returns>
        Task<SaleToPOIResponse> SendPaymentRequestExistingCustomerAsync(string serviceId, string poiId, string saleId, string currency, decimal? amount, DateTime cardAcquisitionTimeStamp, string cardAcquisitionTransactionId, CancellationToken cancellationToken);

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
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns><see cref="SaleToPOIResponse"/>.</returns>
        Task<SaleToPOIResponse> SendPaymentRequestNewCustomerAsync(string serviceId, string poiId, string saleId, string currency, decimal? amount, string shopperEmail, string shopperReference, DateTime cardAcquisitionTimeStamp, string cardAcquisitionTransactionId, CancellationToken cancellationToken = default);
        
        Task<SaleToPOIResponse> RegisterCustomerAsync(string serviceId, string poiId, string saleId, CancellationToken cancellationToken = default);
        
        Task<SaleToPOIResponse> EnterEmailAddressAsync(string serviceId, string poiId, string saleId, CancellationToken cancellationToken = default);
    }

    public class PosCardAcquisitionPaymentService : IPosCardAcquisitionPaymentService
    {
        private readonly IPosPaymentCloudApi _posPaymentCloudApi;

        public PosCardAcquisitionPaymentService(IPosPaymentCloudApi posPaymentCloudApi)
        {
            _posPaymentCloudApi = posPaymentCloudApi;
        }

        public Task<SaleToPOIResponse> SendPaymentRequestNewCustomerAsync(string serviceId, string poiId, string saleId, string currency, decimal? amount, string shopperEmail, string shopperReference, DateTime cardAcquisitionTimeStamp, string cardAcquisitionTransactionId, CancellationToken cancellationToken)
        {
            SaleToPOIRequest request = GetPaymentNewCustomerRequest(serviceId, poiId, saleId, currency, amount, shopperEmail, shopperReference, cardAcquisitionTimeStamp, cardAcquisitionTransactionId);
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request);
        }

        public Task<SaleToPOIResponse> RegisterCustomerAsync(string serviceId, string poiId, string saleId, CancellationToken cancellationToken = default)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    MessageCategory = MessageCategoryType.Input,
                    MessageClass = MessageClassType.Device,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,
                    ServiceID = serviceId, 
                },
                MessagePayload = new InputRequest()
                {
                    DisplayOutput = new DisplayOutput()
                    {
                      Device  = DeviceType.CustomerDisplay,
                      InfoQualify = InfoQualifyType.Display,
                      OutputContent = new OutputContent()
                      {
                          OutputFormat = OutputFormatType.Text,
                          PredefinedContent = new PredefinedContent()
                          {
                              ReferenceID = "GetConfirmation"
                          },
                          OutputText = new OutputText[]
                          {
                              new OutputText()
                              {
                                  Text = "Welcome!"
                              },
                              new OutputText()
                              {
                                  Text = "Would you like to join our loyalty program?"
                              },
                              new OutputText()
                              {
                                  Text = "No"
                              },
                              new OutputText()
                              {
                                  Text = "Yes"
                              }
                          }
                      }
                    },
                    InputData = new InputData()
                    {
                        Device = DeviceType.CustomerInput,
                        InfoQualify = InfoQualifyType.Input,
                        InputCommand = InputCommandType.GetConfirmation,
                        MaxInputTime = 30
                    }
                }
            };
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request);
        }

        public Task<SaleToPOIResponse> EnterEmailAddressAsync(string serviceId, string poiId, string saleId, CancellationToken cancellationToken = default)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    MessageCategory = MessageCategoryType.Input,
                    MessageClass = MessageClassType.Device,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,
                    ServiceID = serviceId, 
                },
                MessagePayload = new InputRequest()
                {
                    DisplayOutput = new DisplayOutput()
                    {
                      Device  = DeviceType.CustomerDisplay,
                      InfoQualify = InfoQualifyType.Display,
                      OutputContent = new OutputContent()
                      {
                          OutputFormat = OutputFormatType.Text,
                          PredefinedContent = new PredefinedContent()
                          {
                              ReferenceID = "GetText"
                          },
                          OutputText = new OutputText[]
                          {
                              new OutputText()
                              {
                                  Text = "Enter your email address"
                              }
                          }
                      }
                    },
                    InputData = new InputData()
                    {
                        Device = DeviceType.CustomerInput,
                        InfoQualify = InfoQualifyType.Input,
                        InputCommand = InputCommandType.TextString,
                        MaxInputTime = 120,
                        DefaultInputString = "youremail@domain.com"
                    }
                }
            };
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request);
        }

        private SaleToPOIRequest GetPaymentNewCustomerRequest(string serviceId, string poiId, string saleId, string currency, decimal? amount, string shopperEmail, string shopperReference, DateTime cardAcquisitionTimeStamp, string cardAcquisitionTransactionId)
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
                            TransactionID = Guid.NewGuid().ToString(),
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
                    }, /*
                    LoyaltyData = new LoyaltyData[]
                    {
                        new LoyaltyData()
                        {
                            CardAcquisitionReference = new TransactionIdentification()
                            {
                                TimeStamp = cardAcquisitionTimeStamp,
                                TransactionID = cardAcquisitionTransactionId
                            }, 
                            LoyaltyAmount = new LoyaltyAmount()
                            {
                                Value = 10.1M,
                                LoyaltyUnit = LoyaltyUnitType.Monetary
                            },
                            LoyaltyAccountID = new LoyaltyAccountID()
                            {
                                LoyaltyID = "214124",
                                EntryMode = new EntryModeType[] { EntryModeType.Manual },
                                IdentificationType = IdentificationType.PAN
                            }
                        },
                    }*/
                }
            };

            return request;
        }

        public Task<SaleToPOIResponse> SendPaymentRequestExistingCustomerAsync(string serviceId, string poiId, string saleId, string currency, decimal? amount, DateTime cardAcquisitionTimeStamp, string cardAcquisitionTransactionId, CancellationToken cancellationToken)
        {
            SaleToPOIRequest request = GetPaymentExistingCustomerRequest(serviceId, poiId, saleId, currency, amount, cardAcquisitionTimeStamp, cardAcquisitionTransactionId);
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request);
        }

        private SaleToPOIRequest GetPaymentExistingCustomerRequest(string serviceId, string poiId, string saleId, string currency, decimal? amount, DateTime cardAcquisitionTimeStamp, string cardAcquisitionTransactionId)
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
                            TransactionID = Guid.NewGuid().ToString(),
                            TimeStamp = DateTime.UtcNow
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

            return request;
        }
    }
}