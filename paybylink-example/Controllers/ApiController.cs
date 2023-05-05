using Adyen.Model.Checkout;
using Adyen.Service;
using adyen_dotnet_paybylink_example.Options;
using adyen_dotnet_paybylink_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace adyen_dotnet_paybylink_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IUrlService _urlService;
        private readonly Checkout _checkout;
        private readonly string _merchantAccount;
        
        public ApiController(Checkout checkout, ILogger<ApiController> logger, IUrlService urlService, IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _urlService = urlService;
            _checkout = checkout;
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
        }

        [HttpPost("api/links")]
        public async Task<ActionResult<string>> PaymentLinks(string customName0, int amount = 1000)
        {
            var orderRef = Guid.NewGuid();
            var createPayByLinkRequest = new CreatePaymentLinkRequest()
            {
                MerchantAccount = _merchantAccount, // Required.
                Amount = new Amount("EUR", amount), // Value in minor units.
                Reference = orderRef.ToString(),    // Required.
                ReturnUrl = $"{_urlService.GetHostUrl()}/redirect?orderRef={orderRef}", // Required for 3DS2 redirect flow.
            };

            try
            {
                var response = await _checkout.PaymentLinksAsync(createPayByLinkRequest);
                _logger.LogInformation($"Response Payments API:\n{response}\n");
                return response.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request failed for Sessions:\n{e.ResponseBody}\n");
                throw;
            }
        }

        [HttpGet("api/links")]
        public async Task<ActionResult<string>> PaymentLinks()
        {
            // Paymentlinksservice.GetAllLinks();
            throw new NotImplementedException();
        }

        // https://github.com/adyen-examples/adyen-java-spring-online-payments/blob/main/paybylink-example/src/main/java/com/adyen/paybylink/service/PaymentLinkService.java
        //[HttpGet("api/links")]
        //public async Task<ActionResult<string>> PaymentLinks(string id)
        //{
        //    var t = UpdatePaymentLinkRequest // GetLink(id);
        //}
    }
}