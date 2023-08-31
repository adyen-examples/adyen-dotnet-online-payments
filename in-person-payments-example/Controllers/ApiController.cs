using Adyen.Model.Nexo;
using adyen_dotnet_in_person_payments_example.Models;
using adyen_dotnet_in_person_payments_example.Models.Requests;
using adyen_dotnet_in_person_payments_example.Models.Responses;
using adyen_dotnet_in_person_payments_example.Options;
using adyen_dotnet_in_person_payments_example.Services;
using adyen_dotnet_in_person_payments_example.Utilities;
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
            var table = _tableService.Tables.FirstOrDefault(t => t.TableName == request.TableName);

            if (table == null)
            {
                return NotFound(new CreatePaymentResponse()
                {
                    Result = "failure",
                    RefusalReason = $"Table {request.TableName} not found"
                });
            }

            string serviceId = IdUtility.GetRandomAlphanumericId(10);

            table.PaymentStatusDetails.ServiceId = serviceId;
            table.PaymentStatus = PaymentStatus.PaymentInProgress;
            
            try
            {
                SaleToPOIResponse response = await _posPaymentService.SendPaymentRequestAsync(serviceId, _poiId, _saleId, request.Currency, request.Amount, cancellationToken);

                PaymentResponse paymentResponse = response?.MessagePayload as PaymentResponse;
                if (response == null)
                {
                    table.PaymentStatus = PaymentStatus.NotPaid;
                    return BadRequest(new CreatePaymentResponse()
                    {
                        Result = "failure",
                        RefusalReason = "Empty payment response"
                    });
                }

                TableModel result = _tableService.UpdatePaymentStatus(table, paymentResponse);

                if (result == null)
                {
                    table.PaymentStatus = PaymentStatus.NotPaid;
                    return BadRequest(new CreatePaymentResponse()
                    {
                        Result = "failure",
                        RefusalReason = _poiId == null ? "Could not reach payment terminal - POI ID is not set." : $"Could not reach payment terminal with POI ID {_poiId}."
                    });
                }

                if (!string.IsNullOrWhiteSpace(result.PaymentStatusDetails.RefusalReason))
                {
                    return Ok(new CreatePaymentResponse()
                    {
                        Result = "failure",
                        RefusalReason = result.PaymentStatusDetails.RefusalReason,
                        PoiTransactionId = result.PaymentStatusDetails.PoiTransactionId,
                        PoiTransactionDateTime = result.PaymentStatusDetails.PoiTransactionTimeStamp,
                        SaleTransactionId = result.PaymentStatusDetails.SaleTransactionId,
                        SaleTransactionDateTime = result.PaymentStatusDetails.SaleTransactionTimeStamp,
                    });
                }

                return Ok(new CreatePaymentResponse()
                {
                    Result = "success",
                    PoiTransactionId = result.PaymentStatusDetails.PoiTransactionId,
                    PoiTransactionDateTime = result.PaymentStatusDetails.PoiTransactionTimeStamp,
                    SaleTransactionId = result.PaymentStatusDetails.SaleTransactionId,
                    SaleTransactionDateTime = result.PaymentStatusDetails.SaleTransactionTimeStamp,
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                table.PaymentStatus = PaymentStatus.NotPaid;
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
                            RefusalReason = _poiId == null ? "Could not reach payment terminal - POI ID is not set." : $"Could not reach payment terminal with POI ID {_poiId}."
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

        [HttpPost("api/abort/{tableName}")]
        public async Task<ActionResult<SaleToPOIResponse>> Abort(string tableName, CancellationToken cancellationToken = default)
        {
            try
            {
                TableModel table = _tableService.Tables.FirstOrDefault(t => t.TableName == tableName);

                if (table?.PaymentStatusDetails?.ServiceId == null)
                {
                    return NotFound();
                }

                SaleToPOIResponse abortResponse = await _posAbortService.SendAbortRequestAsync(table.PaymentStatusDetails.ServiceId, _poiId, _saleId, cancellationToken);
                table.PaymentStatus = PaymentStatus.NotPaid;
                return Ok(abortResponse);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
        }

        [HttpGet("api/get-transaction-status/{tableName}")]
        public async Task<ActionResult<SaleToPOIResponse>> GetTransactionStatus(string tableName, CancellationToken cancellationToken = default)
        {
            try
            {
                TableModel table = _tableService.Tables.FirstOrDefault(t => t.TableName == tableName);

                if (table?.PaymentStatusDetails?.ServiceId == null)
                {
                    return NotFound();
                }

                SaleToPOIResponse response = await _posTransactionStatusService.SendTransactionStatusRequestAsync(table.PaymentStatusDetails.ServiceId, _poiId, _saleId, cancellationToken);
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