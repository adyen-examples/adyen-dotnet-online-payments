using Adyen.Model.Checkout;
using Adyen.Service.Checkout;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using adyen_dotnet_checkout_example_advanced.Options;
using adyen_dotnet_checkout_example_advanced.Services;

namespace adyen_dotnet_checkout_example_advanced.Controllers
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

        [HttpPost("api/getPaymentMethods")]
        public async Task<ActionResult<string>> GetPaymentMethods(CancellationToken cancellationToken = default)
        {
            var paymentMethodsRequest = new PaymentMethodsRequest()
            {
                MerchantAccount = _merchantAccount,
                Channel = PaymentMethodsRequest.ChannelEnum.Web
            };
            
            try
            {
                var res = await _paymentsService.PaymentMethodsAsync(paymentMethodsRequest, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for PaymentMethods:\n{res}\n");
                return res.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for PaymentMethods failed:\n{e.ResponseBody}\n");
                throw;
            }
        }

        [HttpPost("api/initiatePayment")]
        public async Task<ActionResult<string>> InitiatePayment(PaymentRequest request, CancellationToken cancellationToken = default)
        {
            var orderRef = Guid.NewGuid();
            var paymentRequest = new PaymentRequest()
            {
                MerchantAccount = _merchantAccount, // Required.
                Reference = orderRef.ToString(), // Required.
                Channel = PaymentRequest.ChannelEnum.Web,
                Amount = new Amount("EUR", 10000), // Value is 100€ in minor units.
                
                // Required for 3DS2 redirect flow.
                ReturnUrl = $"{_urlService.GetHostUrl()}/api/handleShopperRedirect?orderRef={orderRef}",

                // Used for klarna, klarna is not supported everywhere, hence why we've defaulted to countryCode "NL" as it supports the following payment methods below:
                // "Pay now", "Pay later" and "Pay over time", see docs for more info: https://docs.adyen.com/payment-methods/klarna#supported-countries.
                CountryCode = "NL",
                LineItems = new List<LineItem>()
                {
                    new LineItem(quantity: 1, amountIncludingTax: 5000, description: "Sunglasses"),
                    new LineItem(quantity: 1, amountIncludingTax: 5000, description: "Headphones")
                },
                AdditionalData = new Dictionary<string, string>() { {  "allow3DS2", "true" } },
                Origin = _urlService.GetHostUrl(),
                ShopperIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                // We have to pass on the paymentMethodType from the client here.
                // TODO: See https://github.com/Adyen/adyen-java-api-library/blob/18.0.0/src/main/java/com/adyen/model/checkout/PaymentsRequest.java#L248
                PaymentMethod = CheckoutPaymentMethod.FromJson("")
            };

            try
            {
                var res = await _paymentsService.PaymentsAsync(paymentRequest, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for Payments:\n{res}\n");
                return res.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed:\n{e.ResponseBody}\n");
                throw;
            }
        }



        [HttpPost("api/submitAdditionalDetails")]
        public async Task<ActionResult<string>> SubmitAdditionalDetails([FromBody] DetailsRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var res = await _paymentsService.PaymentsDetailsAsync(request, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for PaymentsDetails:\n{res}\n");
                return res.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for PaymentsDetails failed:\n{e.ResponseBody}\n");
                throw;
            }
        }

        [HttpGet("api/handleShopperRedirect")]
        public Task<ActionResult<string>> HandleShoppperRedirect(string orderReference, string payload = null, string redirectResult = null,
            CancellationToken cancellationToken = default)
        {
            var request = new DetailsRequest();
            if (!string.IsNullOrWhiteSpace(redirectResult))
            {
                request.Details = new PaymentCompletionDetails() { RedirectResult = redirectResult };
            }

            if (!string.IsNullOrWhiteSpace(payload))
            {
                request.Details = new PaymentCompletionDetails() {Payload = payload};
            }

            return request.ToJson();
        }
    }
}