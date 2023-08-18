using Adyen.Model.Nexo;
using adyen_dotnet_in_person_payments_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private const string PoiId = "V400m-123456789";
        private const string SaleId = "YOUR_UNIQUE_POS_ID_001";

        private readonly ILogger<ApiController> _logger;
        private readonly PosPaymentService _posPaymentService;
        private readonly PosPaymentReversalService _posPaymentReversalService;

        public ApiController(ILogger<ApiController> logger, PosPaymentService posPaymentService, PosPaymentReversalService posPaymentReversalService)
        {
            _logger = logger;
            _posPaymentService = posPaymentService;
            _posPaymentReversalService = posPaymentReversalService;
        }

        [HttpPost("api/send-payment-request")]
        public async Task<ActionResult<SaleToPOIResponse>> SendPaymentRequest(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _posPaymentService.SendPaymentRequestAsync(PoiId, SaleId, "EUR", new decimal(42.42), cancellationToken);

                PaymentResponse paymentResponse = response?.MessagePayload as PaymentResponse;
                if (response == null)
                {
                    return BadRequest();
                }

                switch (paymentResponse.Response.Result)
                {
                    case ResultType.Success:
                        break;
                    case ResultType.Failure:
                        break;
                    case ResultType.Partial:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(paymentResponse.Response.Result));
                }
                

                return Ok(response);
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.ToString());
                throw;
            }
        }

        [HttpPost("api/send-payment-reversal-request")]
        public async Task<ActionResult<SaleToPOIResponse>> SendPaymentReversalRequest(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _posPaymentReversalService.SendReversalRequestAsync(ReversalReasonType.CustCancel, "Customer cancelled", "salereferenceId", PoiId, SaleId, new decimal(42.42), cancellationToken);

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

                return Ok(response);
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.ToString());
                throw;
            }
        }
    }
}