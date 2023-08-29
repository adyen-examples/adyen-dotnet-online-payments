using Adyen.Model.Nexo;
using adyen_dotnet_in_person_payments_example.Models.Requests;
using adyen_dotnet_in_person_payments_example.Models.Responses;
using adyen_dotnet_in_person_payments_example.Options;
using adyen_dotnet_in_person_payments_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using adyen_dotnet_in_person_payments_example.Models;

namespace adyen_dotnet_in_person_payments_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IPosPaymentService _posPaymentService;
        private readonly IPosReversalService _posPaymentReversalService;
        private readonly ITableService _tableService;

        private readonly string _saleId;
        private readonly string _poiId;

        public ApiController(ILogger<ApiController> logger, 
            IPosPaymentService posPaymentService, 
            IPosReversalService posPaymentReversalService, 
            IPosAbortService posAbortService,
            IPosTransactionStatusService posTransactionStatusService,
            ITableService tableService, 
            IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _posPaymentService = posPaymentService;
            _posPaymentReversalService = posPaymentReversalService;
            _tableService = tableService;
            _poiId = options.Value.ADYEN_POS_POI_ID;
            _saleId = options.Value.ADYEN_POS_SALE_ID;
        }

        [HttpPost("api/create-payment")]
        public async Task<ActionResult<CreatePaymentResponse>> CreatePayment([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken = default)
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
                var response = await _posPaymentService.SendPaymentRequestAsync(_poiId, _saleId, request.Currency, request.Amount);

                PaymentResponse paymentResponse = response?.MessagePayload as PaymentResponse;
                if (response == null)
                {
                    return BadRequest(new CreatePaymentResponse()
                    {
                        Result = "failure",
                        RefusalReason = "Empty payment response"
                    });
                }
                
                switch (paymentResponse?.Response?.Result)
                {
                    case ResultType.Success:
                        var res = new CreatePaymentResponse()
                        {
                            Result = "success",
                            PoiTransactionId = paymentResponse.POIData.POITransactionID.TransactionID,
                            PoiTransactionDateTime = paymentResponse.POIData.POITransactionID.TimeStamp,
                            SaleTransactionId = paymentResponse.SaleData.SaleTransactionID.TransactionID,
                            SaleTransactionDateTime = paymentResponse.SaleData.SaleTransactionID.TimeStamp
                        };
                        
                        // Update table.
                        table.TableStatus = TableStatus.Paid;
                        table.PoiTransactionId = res.PoiTransactionId;
                        table.SaleReferenceId = res.SaleTransactionId;
                        table.TransactionDateTime = res.SaleTransactionDateTime.Value;
                        
                        return Ok(res);
                    case ResultType.Failure:
                        return Ok(new CreatePaymentResponse()
                        {
                            Result = "failure",
                            RefusalReason = "Payment terminal responded with: " + paymentResponse?.Response?.ErrorCondition,
                            PoiTransactionId = paymentResponse.POIData.POITransactionID.TransactionID,
                            PoiTransactionDateTime = paymentResponse.POIData.POITransactionID.TimeStamp,
                            SaleTransactionId = paymentResponse.SaleData.SaleTransactionID.TransactionID,
                            SaleTransactionDateTime = paymentResponse.SaleData.SaleTransactionID.TimeStamp
                        });
                    case ResultType.Partial:
                        throw new NotImplementedException(nameof(ResultType.Partial));
                    default:
                        return BadRequest(new CreatePaymentResponse()
                        {
                            Result = "failure",
                            RefusalReason = _poiId == null ? "Could not reach payment terminal - POI ID was not set." : $"Could not reach payment terminal with POI ID {_poiId}."
                        });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(500, new CreatePaymentResponse()
                {
                    Result = "failure",
                    RefusalReason = e.Message
                });
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
                var response = await _posPaymentReversalService.SendReversalRequestAsync(ReversalReasonType.MerchantCancel, table.SaleReferenceId, table.PoiTransactionId, _poiId, _saleId, cancellationToken);

                ReversalResponse reversalResponse = response?.MessagePayload as ReversalResponse;
                if (reversalResponse == null)
                {
                    return BadRequest();
                }

                switch (reversalResponse.Response.Result)
                {
                    case ResultType.Success:
                        table.TableStatus = TableStatus.RefundInProgress;
                        return Ok(new CreateReversalResponse()
                        {
                            Result = "success"
                        });
                    case ResultType.Failure:
                        table.TableStatus = TableStatus.RefundFailed;
                        return Ok(new CreateReversalResponse()
                        {
                            Result = "failure",
                            RefusalReason = "Payment terminal responded with: " + reversalResponse.Response.AdditionalResponse
                        });
                    case ResultType.Partial:
                        throw new NotImplementedException(nameof(ResultType.Partial));
                    default:
                        return BadRequest(new CreateReversalResponse()
                        {
                            Result = "failure",
                            RefusalReason = _poiId == null ? "Could not reach payment terminal - POI ID was not set." : $"Could not reach payment terminal with POI ID {_poiId}."
                        });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(500, new CreatePaymentResponse()
                {
                    Result = "failure",
                    RefusalReason = e.Message
                });
            }
        }

        [Route("api/get-transaction-status")]
        public async Task<ActionResult> Test(CancellationToken cancellationToken = default)
        {
            return Ok();
        }
    }
}