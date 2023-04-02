using Adyen.Model.Checkout;
using Adyen.Service;
using adyen_dotnet_giftcard_example.Options;
using adyen_dotnet_giftcard_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace adyen_dotnet_giftcard_example.Controllers
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

        [HttpPost("api/paymentMethods")]
        public async Task<ActionResult<string>> PaymentMethods()
        {
            var paymentMethodsRequest = new PaymentMethodsRequest(
                merchantAccount: _merchantAccount, // Required.
                countryCode: "NL", 
                shopperLocale: "en_US", 
                channel: PaymentMethodsRequest.ChannelEnum.Web
            );
            
            try
            {
                var res = await _checkout.PaymentMethodsAsync(paymentMethodsRequest);
                _logger.LogInformation($"Response Payments API:\n{res}\n");
                return res.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request failed for PaymentMethods:\n{e.ResponseBody}\n");
                throw;
            }
        }

        [HttpPost("api/sessions/dropin")]
        public async Task<ActionResult<string>> SessionsDropin()
        {
            var sessionsRequest = new CreateCheckoutSessionRequest();
            sessionsRequest.MerchantAccount = _merchantAccount; // Required.
            sessionsRequest.Channel = CreateCheckoutSessionRequest.ChannelEnum.Web;
            sessionsRequest.Amount = new Amount("EUR", 11000); // Value is 110€ in minor units

            var orderRef = Guid.NewGuid();
            sessionsRequest.Reference = orderRef.ToString(); // Required.

            // Required for 3DS2 redirect flow.
            sessionsRequest.ReturnUrl = $"{_urlService.GetHostUrl()}/dropin/redirect?orderRef={orderRef}"; // Redirect from dropin.

            // Used for klarna, klarna is not supported everywhere, hence why we've defaulted to countryCode "NL" as it supports the following payment methods below:
            // "Pay now", "Pay later" and "Pay over time", see docs for more info: https://docs.adyen.com/payment-methods/klarna#supported-countries
            sessionsRequest.CountryCode = "NL";
            sessionsRequest.LineItems = new List<LineItem>()
            {
                new LineItem(quantity: 1, amountIncludingTax: 5500 , description: "Sunglasses"),
                new LineItem(quantity: 1, amountIncludingTax: 5500, description: "Headphones")
            };

            try
            {
                var res = await _checkout.SessionsAsync(sessionsRequest);
                _logger.LogInformation($"Response Payments API:\n{res}\n");
                return res.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request failed for Sessions:\n{e.ResponseBody}\n");
                throw;
            }
        }

        [HttpPost("api/sessions/giftcardcomponent")]
        public async Task<ActionResult<string>> SessionsGiftcardComponent()
        {
            var sessionsRequest = new CreateCheckoutSessionRequest();
            sessionsRequest.MerchantAccount = _merchantAccount; // Required.
            sessionsRequest.Channel = CreateCheckoutSessionRequest.ChannelEnum.Web;
            sessionsRequest.Amount = new Amount("EUR", 11000); // Value is 110€ in minor units.

            var orderRef = Guid.NewGuid();
            sessionsRequest.Reference = orderRef.ToString(); // Required.

            // Required for 3DS2 redirect flow.
            sessionsRequest.ReturnUrl = $"{_urlService.GetHostUrl()}/giftcardcomponent/redirect?orderRef={orderRef}"; // Redirect from GiftcardComponent.

            // Used for klarna, klarna is not supported everywhere, hence why we've defaulted to countryCode "NL" as it supports the following payment methods below:
            // "Pay now", "Pay later" and "Pay over time", see docs for more info: https://docs.adyen.com/payment-methods/klarna#supported-countries
            sessionsRequest.CountryCode = "NL";
            sessionsRequest.LineItems = new List<LineItem>()
            {
                new LineItem(quantity: 1, amountIncludingTax: 5500 , description: "Sunglasses"),
                new LineItem(quantity: 1, amountIncludingTax: 5500, description: "Headphones")
            };

            try
            {
                var res = await _checkout.SessionsAsync(sessionsRequest);
                _logger.LogInformation($"Response Payments API: \n{res}\n");
                return res.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request failed for Sessions:\n{e.ResponseBody}\n");
                throw;
            }
        }
    }
}