using Adyen.Model.Checkout;
using adyen_dotnet_paybylink_example.Models;
using adyen_dotnet_paybylink_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
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
                PaymentLinkResponse response = await _paymentLinkService.CreatePaymentLinkAsync(request.Reference, request.Amount);
                return Ok(response);
            }
            catch(Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest(e.ToString());
            }
        }

        // https://github.com/adyen-examples/adyen-java-spring-online-payments/blob/main/paybylink-example/src/main/java/com/adyen/paybylink/service/PaymentLinkService.java
        //[HttpGet("api/links")]
        //public async Task<ActionResult<string>> PaymentLinks(string id)
        //{
        //    var t = UpdatePaymentLinkRequest // GetLink(id);
        //}
    }
}