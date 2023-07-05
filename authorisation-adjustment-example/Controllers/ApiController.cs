using Adyen.Model.Checkout;
using Adyen.Service.Checkout;
using adyen_dotnet_authorisation_adjustment_example.Options;
using adyen_dotnet_authorisation_adjustment_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_authorisation_adjustment_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly IPaymentsService _paymentsService;
        private readonly IUrlService _urlService;
        private readonly ILogger<ApiController> _logger;
        private readonly AdyenOptions _options;

        public ApiController(IPaymentsService paymentsService, IUrlService urlService, ILogger<ApiController> logger, IOptions<AdyenOptions> options)
        {
            _paymentsService = paymentsService;
            _urlService = urlService;
            _logger = logger;
            _options = options.Value;
        }

        [HttpPost("api/getPaymentMethods")]
        public async Task<ActionResult<PaymentMethodsResponse>> GetPaymentMethods(CancellationToken cancellationToken = default)
        {
            var paymentMethodsRequest = new PaymentMethodsRequest()
            {
                MerchantAccount = _options.ADYEN_MERCHANT_ACCOUNT,
                Channel = PaymentMethodsRequest.ChannelEnum.Web
            };

            try
            {
                var res = await _paymentsService.PaymentMethodsAsync(paymentMethodsRequest, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for PaymentMethods:\n{res}\n");
                return res;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for PaymentMethods failed:\n{e.ResponseBody}\n");
                throw;
            }
        }

        /// <summary>
        /// This method creates a pre-authorisation using the Payments API.
        /// </summary>
        [HttpPost("api/pre-authorisation")]
        public async Task<ActionResult<PaymentResponse>> PreAuthorisation(PaymentRequest request, CancellationToken cancellationToken = default)
        {
            var orderRef = Guid.NewGuid();

            var paymentRequest = new PaymentRequest()
            {
                MerchantAccount = _options.ADYEN_MERCHANT_ACCOUNT, // Required.
                Reference = orderRef.ToString(), // Required.
                Channel = PaymentRequest.ChannelEnum.Web,
                Amount = new Amount("EUR", 24999), // Value is 249.99€ in minor units.

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
                Origin = _urlService.GetHostUrl(),
                BrowserInfo = new BrowserInfo() { UserAgent = HttpContext.Request.Headers["user-agent"] }, // Add more browser info here. 
                ShopperIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                PaymentMethod = request.PaymentMethod,

                AdditionalData = new Dictionary<string, string>
                {
                    { "allow3DS2", "true" },            // Required for the 3DS2 flow.
                    { "authorisationType", "PreAuth" }, // Set `authorisationType` to `preAuth`.
                    { "manualCapture", "true" },        // Set `manualCapture` to `true` so we do not finalize the payment until we capture it.
                }
            };

            try
            {
                var res = await _paymentsService.PaymentsAsync(paymentRequest, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for Payment:\n{res}\n");
                return res;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payment failed:\n{e.ResponseBody}\n");
                throw;
            }
        }

        
        [HttpPost("api/submitAdditionalDetails")]
        public async Task<ActionResult<PaymentDetailsResponse>> SubmitAdditionalDetails(DetailsRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var res = await _paymentsService.PaymentsDetailsAsync(request, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for PaymentDetails:\n{res}\n");
                return res;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for PaymentDetails failed:\n{e.ResponseBody}\n");
                throw;
            }
        }

        [HttpGet("api/handleShopperRedirect")]
        public async Task<IActionResult> HandleShoppperRedirect(string payload = null, string redirectResult = null, CancellationToken cancellationToken = default)
        {
            var detailsRequest = new DetailsRequest();
            if (!string.IsNullOrWhiteSpace(redirectResult))
            {
                detailsRequest.Details = new PaymentCompletionDetails() { RedirectResult = redirectResult };
            }

            if (!string.IsNullOrWhiteSpace(payload))
            {
                detailsRequest.Details = new PaymentCompletionDetails() { Payload = payload };
            }

            try
            {
                var res = await _paymentsService.PaymentsDetailsAsync(detailsRequest, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for PaymentDetails:\n{res}\n");
                string redirectUrl = "/result/";
                switch (res.ResultCode)
                {
                    case PaymentDetailsResponse.ResultCodeEnum.Authorised:
                        redirectUrl += "success";
                        break;
                    case PaymentDetailsResponse.ResultCodeEnum.Pending:
                    case PaymentDetailsResponse.ResultCodeEnum.Received:
                        redirectUrl += "pending";
                        break;
                    case PaymentDetailsResponse.ResultCodeEnum.Refused:
                        redirectUrl += "failed";
                        break;
                    default:
                        redirectUrl += "error";
                        break;
                }

                return Redirect(redirectUrl + "?reason=" + res.ResultCode);
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for PaymentDetails failed:\n{e.ResponseBody}\n");
                throw;
            }
        }
    }
}