using Adyen.Model.Nexo;
using adyen_dotnet_in_person_payments_example.Models.Requests;
using adyen_dotnet_in_person_payments_example.Models.Responses;
using adyen_dotnet_in_person_payments_example.Options;
using adyen_dotnet_in_person_payments_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IPosPaymentService _posPaymentService;
        private readonly IPosPaymentReversalService _posPaymentReversalService;

        private readonly string _saleId;
        private readonly string _poiId;

        public ApiController(ILogger<ApiController> logger, IPosPaymentService posPaymentService, IPosPaymentReversalService posPaymentReversalService, IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _posPaymentService = posPaymentService;
            _posPaymentReversalService = posPaymentReversalService;

            _poiId = options.Value.ADYEN_POS_POI_ID;
            _saleId = options.Value.ADYEN_POS_SALE_ID;
        }

        [HttpPost("api/create-payment")]
        public async Task<ActionResult<SaleToPOIResponse>> SendPaymentRequest([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _posPaymentService.SendPaymentRequestAsync(_poiId, _saleId, request.Currency, request.Amount, cancellationToken);

                PaymentResponse paymentResponse = response?.MessagePayload as PaymentResponse;
                if (response == null)
                {
                    return BadRequest(new CreatePaymentResponse()
                    {
                        Result = "failure",
                        RefusalReason = "Empty payment response"
                    });
                }

                switch (paymentResponse.Response.Result)
                {
                    case ResultType.Success:
                        return Ok(new CreatePaymentResponse()
                        {
                            Result = "success",
                            PoiTransactionId = paymentResponse.POIData.POITransactionID.TransactionID,
                            PoiTransactionDateTime = paymentResponse.POIData.POITransactionID.TimeStamp,

                            SaleTransactionId = paymentResponse.SaleData.SaleTransactionID.TransactionID,
                            SaleTransactionDateTime = paymentResponse.SaleData.SaleTransactionID.TimeStamp
                        });
                    case ResultType.Failure:
                        return Ok(new CreatePaymentResponse()
                        {
                            Result = "failure",
                            RefusalReason = "Payment terminal responded with: " + paymentResponse.Response.ErrorCondition.ToString(),
                            PoiTransactionId = paymentResponse.POIData.POITransactionID.TransactionID,
                            PoiTransactionDateTime = paymentResponse.POIData.POITransactionID.TimeStamp,

                            SaleTransactionId = paymentResponse.SaleData.SaleTransactionID.TransactionID,
                            SaleTransactionDateTime = paymentResponse.SaleData.SaleTransactionID.TimeStamp
                        });
                    case ResultType.Partial:
                        throw new NotImplementedException(nameof(ResultType.Partial));
                    default:
                        throw new ArgumentOutOfRangeException(nameof(paymentResponse.Response.Result));
                }
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.ToString());
                return StatusCode(500, new CreatePaymentResponse()
                {
                    Result = "failure",
                    RefusalReason = "Check the stacktrace: " + e.Message
                });
            }
        }

        [HttpPost("api/create-payment-reversal")] // TODO response 
        public async Task<ActionResult<SaleToPOIResponse>> SendPaymentReversalRequest([FromBody] CreatePaymentReversalRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _posPaymentReversalService.SendReversalRequestAsync(ReversalReasonType.CustCancel, request.SaleReferenceId, _poiId, _saleId, request.Amount, cancellationToken);

                ReversalResponse reversalResponse = response?.MessagePayload as ReversalResponse;
                if (response == null)
                {
                    return BadRequest();
                }

                switch (reversalResponse.Response.Result)
                {
                    case ResultType.Success:
                        break;
                    case ResultType.Failure:
                        break;
                    case ResultType.Partial:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(reversalResponse.Response.Result));
                }

                return Ok(reversalResponse);
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.ToString());
                throw;
            }
        }
    }
}