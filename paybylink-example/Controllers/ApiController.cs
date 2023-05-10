using Adyen.Model.Checkout;
using adyen_dotnet_paybylink_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace adyen_dotnet_paybylink_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IPaymentLinkService _paymentLinkService;

        public ApiController(ILogger<ApiController> logger, IPaymentLinkService paymentLinkService)
        {
            _logger = logger;
            _paymentLinkService = paymentLinkService;
        }

        [HttpPost("api/links")]
        public async Task<ActionResult<string>> CreatePaymentLink(Requests.CreatePaymentLinkRequest request)
        {
            try
            {
                PaymentLinkResponse response = await _paymentLinkService.CreatePaymentLinkAsync(request.Reference, request.Amount, request.IsReusable);
                return Ok(response);
            }
            catch(Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest(e.ToString());
            }
        }
    }
}