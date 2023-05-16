using Adyen.Model.Checkout;
using adyen_dotnet_paybylink_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_paybylink_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly ILinksService _linksService;

        public ApiController(ILogger<ApiController> logger, ILinksService linksService)
        {
            _logger = logger;
            _linksService = linksService;
        }

        [HttpPost("api/links")]
        public async Task<ActionResult<string>> CreatePaymentLink(Requests.CreatePaymentLinkRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                PaymentLinkResponse response = await _linksService.CreatePaymentLinkAsync(request.Reference, request.Amount, request.IsReusable, cancellationToken);
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