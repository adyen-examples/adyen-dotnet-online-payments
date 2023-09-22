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
        private readonly ILogger<ApiController> _logger;
        private readonly IPosPaymentService _posPaymentService;
        private readonly IPosReversalService _posPaymentReversalService;
        private readonly IPosAbortService _posAbortService;
        private readonly IPosCardAcquisitionService _posCardAcquisitionService;
        private readonly IPosCardAcquisitionPaymentService _posCardAcquisitionPaymentService;
        private readonly IPosCardAcquisitionAbortService _posCardAcquisitionAbortService;
        private readonly ITableRepository _tableService;

        private readonly string _saleId;
        private readonly string _poiId;

        public CardAcquisitionController(ILogger<ApiController> logger, 
            IPosPaymentService posPaymentService, 
            IPosReversalService posPaymentReversalService, 
            IPosAbortService posAbortService,
            IPosCardAcquisitionService posCardAcquisitionService,
            IPosCardAcquisitionPaymentService posCardAcquisitionPaymentService,
            IPosCardAcquisitionAbortService posCardAcquisitionAbortService,
            ITableRepository tableService, 
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
            
            _poiId = options.Value.ADYEN_POS_POI_ID;
            _saleId = options.Value.ADYEN_POS_SALE_ID;
        }

        [Route("card-acquisition/create")]
        public async Task<ActionResult> CreateCardAcquisition(CancellationToken cancellationToken = default)
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
                string serviceId = IdUtility.GetRandomAlphanumericId(10);
                SaleToPOIResponse response = await _posCardAcquisitionService.SendCardAcquisitionRequestAsync(serviceId,
                    _poiId, _saleId, cancellationToken: cancellationToken);
                CardAcquisitionResponse cardAcquisitionResponse = response.MessagePayload as CardAcquisitionResponse;

                if (cardAcquisitionResponse == null)
                {
                    return NotFound();
                }

                // Decode the base64 encoded string.
                string decodedUTF8JsonString =
                    System.Text.Encoding.UTF8.GetString(
                        Convert.FromBase64String(cardAcquisitionResponse.Response.AdditionalResponse));

                CardAcquisitionRoot json = JsonConvert.DeserializeObject<CardAcquisitionRoot>(decodedUTF8JsonString);

                SaleToPOIResponse abortRequest = await _posCardAcquisitionAbortService.SendAbortRequestAsync(IdUtility.GetRandomAlphanumericId(10), _poiId, _saleId, cancellationToken: cancellationToken);

                return Ok(new CreateCardAcquisitionResponse()
                {
                    // If you are going to continue with a payment, keep the TimeStamp and TransactionID, because you need these card acquisition details in your payment request.
                    PoiTransactionTimeStamp = cardAcquisitionResponse.POIData.POITransactionID.TimeStamp,
                    PoiTransactionId = cardAcquisitionResponse.POIData.POITransactionID.TransactionID,

                    CardCountryCode = cardAcquisitionResponse.PaymentInstrumentData.CardData.CardCountryCode,
                    PaymentToken = cardAcquisitionResponse.PaymentInstrumentData.CardData.PaymentToken.TokenValue,

                    Alias = json.AdditionalData.Alias,
                    PaymentAccountReference = json.AdditionalData.PaymentAccountReference,
                    CardBin = json.AdditionalData.CardBin,
                    IssuerCountry = json.AdditionalData.IssuerCountry,
                    ShopperEmail = json.AdditionalData.ShopperEmail,
                    ShopperReference = json.AdditionalData.ShopperReference
                });
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