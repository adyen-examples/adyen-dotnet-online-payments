using Adyen.HttpClient;
using Adyen.Model.Nexo;
using adyen_dotnet_in_person_payments_loyalty_example.Models;
using adyen_dotnet_in_person_payments_loyalty_example.Models.Requests;
using adyen_dotnet_in_person_payments_loyalty_example.Models.Responses;
using adyen_dotnet_in_person_payments_loyalty_example.Options;
using adyen_dotnet_in_person_payments_loyalty_example.Repositories;
using adyen_dotnet_in_person_payments_loyalty_example.Services;
using adyen_dotnet_in_person_payments_loyalty_example.Services.CardAcquisition;
using adyen_dotnet_in_person_payments_loyalty_example.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace adyen_dotnet_in_person_payments_loyalty_example.Controllers
{
    [ApiController]
    public class CardAcquisitionController : ControllerBase
    {
        private readonly ILogger<CardAcquisitionController> _logger;
        private readonly IPosPaymentService _posPaymentService;
        private readonly IPosReversalService _posPaymentReversalService;
        private readonly IPosAbortService _posAbortService;
        private readonly IPosCardAcquisitionService _posCardAcquisitionService;
        private readonly IPosCardAcquisitionPaymentService _posCardAcquisitionPaymentService;
        private readonly IPosCardAcquisitionAbortService _posCardAcquisitionAbortService;
        private readonly ITableRepository _tableService;
        private readonly ICardAcquisitionRepository _cardAcquisitionRepository;

        private readonly string _saleId;
        private readonly string _poiId;

        public CardAcquisitionController(ILogger<CardAcquisitionController> logger, 
            IPosPaymentService posPaymentService, 
            IPosReversalService posPaymentReversalService, 
            IPosAbortService posAbortService,
            IPosCardAcquisitionService posCardAcquisitionService,
            IPosCardAcquisitionPaymentService posCardAcquisitionPaymentService,
            IPosCardAcquisitionAbortService posCardAcquisitionAbortService,
            ITableRepository tableService, 
            ICardAcquisitionRepository cardAcquisitionRepository,
            IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _posPaymentService = posPaymentService;
            _posPaymentReversalService = posPaymentReversalService;
            _posAbortService = posAbortService;
            _posCardAcquisitionService = posCardAcquisitionService;
            _posCardAcquisitionPaymentService = posCardAcquisitionPaymentService;
            _posCardAcquisitionAbortService = posCardAcquisitionAbortService;
            _tableService = tableService;
            _cardAcquisitionRepository = cardAcquisitionRepository;
            _poiId = options.Value.ADYEN_POS_POI_ID;
            _saleId = options.Value.ADYEN_POS_SALE_ID;
        }


        [Route("card-acquisitions")]
        public ActionResult CardAcquisitions(CancellationToken cancellationToken = default)
        {
            return Ok(_cardAcquisitionRepository.CardAcquisitions.Select(x => JsonConvert.SerializeObject(x.ToJson(), Formatting.Indented)));
        }

        [Route("card-acquisition/create/{amount}")]
        public async Task<ActionResult> CreateCardAcquisition(decimal? amount, CancellationToken cancellationToken = default)
        {
            /*
            var table = _tableService.Tables.FirstOrDefault(t => t.TableName == request.TableName);

            if (table == null)
            {
                return NotFound(new CreatePaymentResponse()
                {
                    Result = "failure",
                    RefusalReason = $"Table {request.TableName} not found"
                });
            }*/


            try
            {
                SaleToPOIResponse response = await _posCardAcquisitionService.SendCardAcquisitionRequestAsync(IdUtility.GetRandomAlphanumericId(10), _poiId, _saleId, cancellationToken: cancellationToken);
                CardAcquisitionResponse cardAcquisitionResponse = response.MessagePayload as CardAcquisitionResponse;

                if (cardAcquisitionResponse == null)
                {
                    return NotFound();
                }

                // Decode the base64 encoded string.
                string decodedUTF8JsonString =System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cardAcquisitionResponse.Response.AdditionalResponse));

                CardAcquisitionRoot json = JsonConvert.DeserializeObject<CardAcquisitionRoot>(decodedUTF8JsonString);

                if (json.AdditionalData.GiftcardIndicator)
                {
                    return BadRequest(); // This is a giftcard. Can't attach a giftcard to this.
                }

                SaleToPOIResponse paymentRequest;
                if (string.IsNullOrWhiteSpace(json.AdditionalData.ShopperReference) || string.IsNullOrWhiteSpace(json.AdditionalData.ShopperEmail))
                {
                    // New Customer.
                    paymentRequest = await _posCardAcquisitionPaymentService.SendPaymentRequestNewCustomerAsync(
                        serviceId: IdUtility.GetRandomAlphanumericId(10),
                        poiId: _poiId,
                        saleId: _saleId,
                        currency: "EUR",
                        amount: amount,
                        shopperEmail: Identifiers.ShopperEmail,
                        shopperReference: Identifiers.ShopperReference,
                        cardAcquisitionTimeStamp: cardAcquisitionResponse.POIData.POITransactionID.TimeStamp,
                        cardAcquisitionTransactionId: cardAcquisitionResponse.POIData.POITransactionID.TransactionID,
                        cancellationToken: cancellationToken
                    ); ;
                }
                else
                {
                    // Existing Customer.
                    paymentRequest = await _posCardAcquisitionPaymentService.SendPaymentRequestExistingCustomerAsync(
                        serviceId: IdUtility.GetRandomAlphanumericId(10),
                        poiId: _poiId,
                        saleId: _saleId,
                        currency: "EUR",
                        amount: amount,
                        cardAcquisitionTimeStamp: cardAcquisitionResponse.POIData.POITransactionID.TimeStamp,
                        cardAcquisitionTransactionId: cardAcquisitionResponse.POIData.POITransactionID.TransactionID,
                        cancellationToken: cancellationToken
                    );

                }

                //SaleToPOIResponse abortRequest = await _posCardAcquisitionAbortService.SendAbortRequestAsync(IdUtility.GetRandomAlphanumericId(10), _poiId, _saleId, cancellationToken: cancellationToken);
                var model = new CardAcquisitionModel()
                {
                    // If you are going to continue with a payment, keep the TimeStamp and TransactionID, because you need these card acquisition details in your payment request.
                    POIReconciliationID = cardAcquisitionResponse.POIData.POIReconciliationID,
                    PoiTransactionTimeStamp = cardAcquisitionResponse.POIData.POITransactionID.TimeStamp,
                    PoiTransactionId = cardAcquisitionResponse.POIData.POITransactionID.TransactionID,

                    CardCountryCode = cardAcquisitionResponse.PaymentInstrumentData.CardData.CardCountryCode,
                    MaskedPAN = cardAcquisitionResponse.PaymentInstrumentData.CardData.MaskedPAN,
                    PaymentBrand = cardAcquisitionResponse.PaymentInstrumentData.CardData.PaymentBrand,
                    PaymentToken = cardAcquisitionResponse.PaymentInstrumentData.CardData.PaymentToken.TokenValue, // Provided if the TokenRequestedType == Customer
                    ExpiryDate = cardAcquisitionResponse.PaymentInstrumentData.CardData.SensitiveCardData.ExpiryDate,
                    PaymentInstrumentType = cardAcquisitionResponse.PaymentInstrumentData.PaymentInstrumentType,

                    Alias = json.AdditionalData.Alias,
                    PaymentAccountReference = json.AdditionalData.PaymentAccountReference,
                    CardBin = json.AdditionalData.CardBin,
                    IssuerCountry = json.AdditionalData.IssuerCountry,
                    ShopperEmail = json.AdditionalData.ShopperEmail,
                    ShopperReference = json.AdditionalData.ShopperReference,

                    GiftCardIndicator = json.AdditionalData.GiftcardIndicator,

                    SaleTransactionId = cardAcquisitionResponse.SaleData.SaleTransactionID.TransactionID,
                    SaleTransactionTimeStamp = cardAcquisitionResponse.SaleData.SaleTransactionID.TimeStamp
                };

                _cardAcquisitionRepository.CardAcquisitions.Add(model);

                return Ok(model);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
        }

        [HttpPost("api/create-reversal")]
        public async Task<ActionResult<CreateReversalResponse>> CreateReversal([FromBody] CreateReversalRequest request, CancellationToken cancellationToken = default)
        {
            TableModel table = _tableService.Tables.FirstOrDefault(t => t.TableName == request.TableName);

            if (table == null)
            {
                return NotFound(new CreatePaymentResponse()
                {
                    Result = "failure",
                    RefusalReason = $"Table {request.TableName} not found"
                });
            }
            
            try
            {
                SaleToPOIResponse response = await _posPaymentReversalService.SendReversalRequestAsync(ReversalReasonType.MerchantCancel, table.PaymentStatusDetails.SaleTransactionId, table.PaymentStatusDetails.PoiTransactionId, _poiId, _saleId, cancellationToken);

                ReversalResponse reversalResponse = response?.MessagePayload as ReversalResponse;
                if (reversalResponse == null)
                {
                    return BadRequest(new CreateReversalResponse()
                    {
                        Result = "failure",
                        RefusalReason = $"Empty reversal response"
                    });
                }

                switch (reversalResponse.Response.Result)
                {
                    case ResultType.Success:
                        table.PaymentStatus = PaymentStatus.RefundInProgress;
                        return Ok(new CreateReversalResponse()
                        {
                            Result = "success"
                        });
                    case ResultType.Failure:
                        table.PaymentStatus = PaymentStatus.RefundFailed;
                        return Ok(new CreateReversalResponse()
                        {
                            Result = "failure",
                            RefusalReason = "Payment terminal responded with: " + HttpUtility.UrlDecode(reversalResponse.Response.AdditionalResponse)
                        });
                    case ResultType.Partial:
                        throw new NotImplementedException(nameof(ResultType.Partial));
                    default:
                        return BadRequest(new CreateReversalResponse()
                        {
                            Result = "failure",
                            RefusalReason = _poiId == null 
                                ? "Could not reach payment terminal - POI ID is not set" 
                                : $"Could not reach payment terminal with POI ID {_poiId}"
                        });
                }
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(e.Code, new CreateReversalResponse()
                {
                    Result = "failure",
                    RefusalReason = $"ErrorCode: {e.Code}, see logs"
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
        }

        [HttpGet("card-acquisition/abort/{tableName}")]
        public async Task<ActionResult<SaleToPOIResponse>> Abort(string tableName, CancellationToken cancellationToken = default)
        {
            try
            {
                TableModel table = _tableService.Tables.FirstOrDefault(t => t.TableName == tableName);

                if (table?.PaymentStatusDetails?.ServiceId == null)
                {
                    return NotFound();
                }

                SaleToPOIResponse abortResponse = await _posAbortService.SendAbortRequestAsync(MessageCategoryType.CardAcquisition, table.PaymentStatusDetails.ServiceId, _poiId, _saleId, cancellationToken);
                return Ok(abortResponse);
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(e.Code, new CreateReversalResponse()
                {
                    Result = "failure",
                    RefusalReason = $"ErrorCode: {e.Code}, see logs"
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
        }
    }
}