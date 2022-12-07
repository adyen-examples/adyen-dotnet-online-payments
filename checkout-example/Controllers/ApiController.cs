using Adyen;
using Adyen.Model.Checkout;
using Adyen.Service;
using adyen_dotnet_checkout_example.Options;
using adyen_dotnet_checkout_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace adyen_dotnet_checkout_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IUrlService _urlService;
        private readonly Checkout _checkout;
        private readonly string _merchantAccount;
        
        public ApiController(ILogger<ApiController> logger, IUrlService urlService, IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _urlService = urlService;
            var client = new Client(options.Value.ADYEN_API_KEY, Adyen.Model.Enum.Environment.Test); // Test Environment;
            _checkout = new Checkout(client);
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
        }

        [HttpPost("api/sessions")]
        public async Task<ActionResult<string>> Sessions()
        {
            var sessionsRequest = new CreateCheckoutSessionRequest();
            sessionsRequest.MerchantAccount = _merchantAccount; // required
            sessionsRequest.Channel = CreateCheckoutSessionRequest.ChannelEnum.Web;
            var amount = new Amount("EUR", 10000); // value is 100€ in minor units
            sessionsRequest.Amount = amount;
            var orderRef = Guid.NewGuid();
            sessionsRequest.Reference = orderRef.ToString(); // required

            // required for 3ds2 redirect flow
            sessionsRequest.ReturnUrl = $"{_urlService.GetHostUrl()}/redirect?orderRef={orderRef}";

            // used for klarna, klarna is not supported everywhere, hence why we've defaulted to countryCode "NL" as it supports the following payment methods below:
            // "Pay now", "Pay later" and "Pay over time", see docs for more info: https://docs.adyen.com/payment-methods/klarna#supported-countries
            sessionsRequest.CountryCode = "NL";
            sessionsRequest.LineItems = new List<LineItem>()
            {
                new LineItem(quantity: 1, amountIncludingTax: 5000 , description: "Sunglasses"),
                new LineItem(quantity: 1, amountIncludingTax: 5000, description: "Headphones")
            };
            
            try
            {
                var res = await _checkout.SessionsAsync(sessionsRequest);
                _logger.LogInformation($"Response for Payment API::\n{res}\n");
                return res.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed::\n{e.ResponseBody}\n");
                throw;
            }
        }
    }
}