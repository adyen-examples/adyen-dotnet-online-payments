using Adyen.Model.Nexo;
using adyen_dotnet_in_person_payments_example.Models;
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
using System.Web;

namespace adyen_dotnet_in_person_payments_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IPosPaymentService _posPaymentService;
        private readonly IPosReversalService _posPaymentReversalService;
        private readonly IPosAbortService _posAbortService;
        private readonly IPosTransactionStatusService _posTransactionStatusService;
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
            _posAbortService = posAbortService;
            _posTransactionStatusService = posTransactionStatusService;
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
                SaleToPOIResponse response = await _posPaymentService.SendPaymentRequestAsync(_poiId, _saleId, request.Currency, request.Amount, cancellationToken);

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
                SaleToPOIResponse response = await _posPaymentReversalService.SendReversalRequestAsync(ReversalReasonType.MerchantCancel, table.SaleReferenceId, table.PoiTransactionId, _poiId, _saleId, cancellationToken);

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
                            RefusalReason = "Payment terminal responded with: " + HttpUtility.UrlDecode(reversalResponse.Response.AdditionalResponse)
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
                return StatusCode(500, new CreateReversalResponse()
                {
                    Result = "failure",
                    RefusalReason = e.Message
                });
            }
        }

        [HttpPost("api/abort/{serviceId?}")]
        public async Task<ActionResult<SaleToPOIResponse>> Abort(string serviceId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // TODO: Revise abort and transactionstatus, you can queue up multiple payments on the terminal
                if (string.IsNullOrWhiteSpace(serviceId))
                {
                    // In case of a communication or technical issue or when you do not have the ServiceID,
                    // You can get the ServiceID of the original request by sending a transaction status request with an empty TransactionStatusRequest object.
                    // See: https://docs.adyen.com/point-of-sale/basic-tapi-integration/cancel-a-transaction/#get-service-id
                    SaleToPOIResponse response = await _posTransactionStatusService.SendTransactionStatusRequestAsync(null, _poiId, _saleId, cancellationToken);

                    TransactionStatusResponse transactionStatusResponse = response?.MessagePayload as TransactionStatusResponse;

                    if (transactionStatusResponse == null)
                    {
                        return BadRequest();
                    }

                    serviceId = transactionStatusResponse.MessageReference.ServiceID;
                }

                SaleToPOIResponse abortResponse = await _posAbortService.SendAbortRequestAsync(serviceId, _poiId, _saleId, cancellationToken);

                return Ok(abortResponse);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
        }

        [HttpGet("api/get-transaction-status/{serviceId?}")]
        public async Task<ActionResult<SaleToPOIResponse>> GetTransactionStatus(string serviceId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                SaleToPOIResponse response = await _posTransactionStatusService.SendTransactionStatusRequestAsync(serviceId, _poiId, _saleId, cancellationToken);
                return Ok(response);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
        }
    }
}