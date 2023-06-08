using Adyen.Model.Checkout;
using Adyen.Service.Checkout;
using adyen_dotnet_giftcard_example.Options;
using adyen_dotnet_giftcard_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_giftcard_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IUrlService _urlService;
        private readonly IPaymentsService _paymentsService;
        private readonly string _merchantAccount;
        
        public ApiController(IPaymentsService paymentsService, ILogger<ApiController> logger, IUrlService urlService, IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _urlService = urlService;
            _paymentsService = paymentsService;
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
        }

        [HttpPost("api/sessions/dropin")]
        public async Task<ActionResult<CreateCheckoutSessionResponse>> SessionsDropin(CancellationToken cancellationToken = default)
        {
            var orderRef = Guid.NewGuid();
            var sessionsRequest = new CreateCheckoutSessionRequest()
            {
                MerchantAccount = _merchantAccount, // Required.
                Reference = orderRef.ToString(), // Required.

                Channel = CreateCheckoutSessionRequest.ChannelEnum.Web,
                Amount = new Amount("EUR", 11000), // Value is 110€ in minor units

                // Required for 3DS2 redirect flow.
                ReturnUrl = $"{_urlService.GetHostUrl()}/dropin/redirect?orderRef={orderRef}", // Redirect from dropin.

                // Used for klarna, klarna is not supported everywhere, hence why we've defaulted to countryCode "NL" as it supports the following payment methods below:
                // "Pay now", "Pay later" and "Pay over time", see docs for more info: https://docs.adyen.com/payment-methods/klarna#supported-countries
                CountryCode = "NL",
                LineItems = new List<LineItem>()
                {
                    new LineItem(quantity: 1, amountIncludingTax: 5500, description: "Sunglasses"),
                    new LineItem(quantity: 1, amountIncludingTax: 5500, description: "Headphones")
                }
            };

            try
            {
                var res = await _paymentsService.SessionsAsync(sessionsRequest, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response Payments API:\n{res}\n");
                return res;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request failed for Sessions:\n{e.ResponseBody}\n");
                throw;
            }
        }

        [HttpPost("api/sessions/giftcardcomponent")]
        public async Task<ActionResult<CreateCheckoutSessionResponse>> SessionsGiftcardComponent(CancellationToken cancellationToken = default)
        {
            var orderRef = Guid.NewGuid();
            var sessionsRequest = new CreateCheckoutSessionRequest()
            {
                MerchantAccount = _merchantAccount, // Required.
                Reference = orderRef.ToString(), // Required.
                
                Channel = CreateCheckoutSessionRequest.ChannelEnum.Web,
                Amount = new Amount("EUR", 11000), // Value is 110€ in minor units.

                // Required for 3DS2 redirect flow.
                ReturnUrl = $"{_urlService.GetHostUrl()}/giftcardcomponent/redirect?orderRef={orderRef}", // Redirect from GiftcardComponent.

                // Used for klarna, klarna is not supported everywhere, hence why we've defaulted to countryCode "NL" as it supports the following payment methods below:
                // "Pay now", "Pay later" and "Pay over time", see docs for more info: https://docs.adyen.com/payment-methods/klarna#supported-countries
                CountryCode = "NL",
                LineItems = new List<LineItem>()
                {
                    new LineItem(quantity: 1, amountIncludingTax: 5500, description: "Sunglasses"),
                    new LineItem(quantity: 1, amountIncludingTax: 5500, description: "Headphones")
                }
            };

            try
            {
                var res = await _paymentsService.SessionsAsync(sessionsRequest, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response Payments API: \n{res}\n");
                return res;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request failed for Sessions:\n{e.ResponseBody}\n");
                throw;
            }
        }
    }
}