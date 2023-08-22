using Adyen.Model.Nexo;
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

        [HttpPost("api/send-payment-request")]
        public async Task<ActionResult<SaleToPOIResponse>> SendPaymentRequest(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _posPaymentService.SendPaymentRequestAsync(_poiId, _saleId, "EUR", new decimal(42.42), cancellationToken);

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
                var response = await _posPaymentReversalService.SendReversalRequestAsync(ReversalReasonType.CustCancel, "Customer cancelled", "salereferenceId", _poiId, _saleId, new decimal(42.42), cancellationToken);

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