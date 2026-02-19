using Adyen.Checkout.Models;
using Adyen.Checkout.Services;
using adyen_dotnet_checkout_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace adyen_dotnet_checkout_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IUrlService _urlService;
        private readonly IPaymentsService _paymentsService;
        private readonly string _merchantAccount;
        
        public ApiController(IPaymentsService paymentsService, ILogger<ApiController> logger, IUrlService urlService, IConfiguration configuration)
        {
            _logger = logger;
            _urlService = urlService;
            _paymentsService = paymentsService;
            _merchantAccount = configuration["ADYEN_MERCHANT_ACCOUNT"];
        }

        [HttpPost("api/sessions")]
        public async Task<ActionResult<CreateCheckoutSessionResponse>> Sessions(CancellationToken cancellationToken = default)
        {
            var orderRef = Guid.NewGuid();
            var sessionsRequest = new CreateCheckoutSessionRequest()
            {
                MerchantAccount = _merchantAccount, // Required.
                Reference = orderRef.ToString(), // Required.
                Channel = CreateCheckoutSessionRequest.ChannelEnum.Web,
                Amount = new Amount("EUR", 10000), // Value is 100€ in minor units.
                
                // Required for 3DS2 redirect flow.
                ReturnUrl = $"{_urlService.GetHostUrl()}/redirect?orderRef={orderRef}",

                // Used for klarna, klarna is not supported everywhere, hence why we've defaulted to countryCode "NL" as it supports the following payment methods below:
                // "Pay now", "Pay later" and "Pay over time", see docs for more info: https://docs.adyen.com/payment-methods/klarna#supported-countries.
                CountryCode = "NL",
                LineItems = new List<LineItem>()
                {
                    new LineItem(quantity: 1, amountIncludingTax: 5000, description: "Sunglasses"),
                    new LineItem(quantity: 1, amountIncludingTax: 5000, description: "Headphones")
                }
            };

            try
            {
                var response = await _paymentsService.SessionsAsync(sessionsRequest, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for Payments API:\n{response}\n");

           
                if (response.TryDeserializeCreatedResponse(out var result))
                {
                    return result;
                }
                return null;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed:\n{e.ResponseBody}\n");
                throw;
            }
        }
    }
}