using Adyen.Checkout.Models;
using Adyen.Checkout.Services;
using adyen_dotnet_checkout_example_advanced.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using adyen_dotnet_checkout_example_advanced.Dtos;
using Microsoft.Extensions.Configuration;

namespace adyen_dotnet_checkout_example_advanced.Controllers
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

        [HttpPost("api/paymentMethods")]
        public async Task<ActionResult<PaymentMethodsResponse>> PaymentMethods(CancellationToken cancellationToken = default)
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
                if (res.TryDeserializeOkResponse(out var paymentMethodsResponse))
                {
                    return paymentMethodsResponse;
                }
                return null;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for PaymentMethods failed:\n{e.ResponseBody}\n");
                throw;
            }
        }

        [HttpPost("api/payments")]
        public async Task<ActionResult<PaymentResponse>> Payments([FromBody] PaymentsDto paymentsDto, CancellationToken cancellationToken = default)
        {
            // Map your DTOs here
            RiskData riskData = RiskDataDto.MapToRiskData(paymentsDto.RiskData);
            BrowserInfo browserInfo = BrowserInfoDto.MapToBrowserInfo(paymentsDto.BrowserInfo);
            CheckoutPaymentMethod paymentMethod = paymentsDto.PaymentMethod;
            
            string origin = paymentsDto.Origin; // Unused
            bool clientStateDataIndicator = paymentsDto.ClientStateDataIndicator; // Unused
            
            // Create the Payment Request to Adyen.
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
                
                // We strongly recommend that you the billingAddress in your request. 
                // Card schemes require this for channel web, iOS, and Android implementations.
                //BillingAddress = new BillingAddress() { ... },
                AuthenticationData = new AuthenticationData()
                {
                    AttemptAuthentication = AuthenticationData.AttemptAuthenticationEnum.Always,
                    // Add the following line for Native 3DS2:
                    //ThreeDSRequestData = new ThreeDSRequestData()
                    //{
                    //    NativeThreeDS = ThreeDSRequestData.NativeThreeDSEnum.Preferred
                    //}
                },
                Origin = _urlService.GetHostUrl(),
                ShopperIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                
                // Pass your mapped DTOs
                BrowserInfo = browserInfo, // pass mapped dto
                PaymentMethod = paymentMethod, // pass mapped dto
                RiskData = riskData, // pass mapped dto
            };

            try
            {
                var res = await _paymentsService.PaymentsAsync(paymentRequest, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for Payment:\n{res}\n");
                if (res.TryDeserializeOkResponse(out PaymentResponse response))
                {
                    return response;
                }
                return null;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payment failed:\n{e.ResponseBody}\n");
                throw;
            }
        }

        [HttpPost("api/payments/details")]
        public async Task<ActionResult<PaymentDetailsResponse>> PaymentDetails(PaymentDetailsRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var res = await _paymentsService.PaymentsDetailsAsync(request, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for PaymentDetails:\n{res}\n");
                if (res.TryDeserializeOkResponse(out PaymentDetailsResponse response))
                {
                    return response;
                }
                return null;
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
            var detailsRequest = new PaymentDetailsRequest();
            if (!string.IsNullOrWhiteSpace(redirectResult))
            {
                // For redirect, you are redirected to an Adyen domain to complete the 3DS2 challenge.
                // After completing the 3DS2 challenge, you get the redirect result from Adyen in the returnUrl.
                // We then pass on the redirectResult.
                detailsRequest.Details = new PaymentCompletionDetails() { RedirectResult = redirectResult };
            }

            if (!string.IsNullOrWhiteSpace(payload))
            {
                detailsRequest.Details = new PaymentCompletionDetails() { Payload = payload };
            }

            try
            {
                var res = await _paymentsService.PaymentsDetailsAsync(detailsRequest, cancellationToken: cancellationToken);

                if (res.TryDeserializeOkResponse(out PaymentDetailsResponse response))
                {
                    
                }
                _logger.LogInformation($"Response for PaymentDetails:\n{res}\n");
                string redirectUrl = "/result/";
                if (response.ResultCode == PaymentDetailsResponse.ResultCodeEnum.Authorised)
                    redirectUrl += "success";
                else if (response.ResultCode == PaymentDetailsResponse.ResultCodeEnum.Pending || response.ResultCode == PaymentDetailsResponse.ResultCodeEnum.Received)
                    redirectUrl += "pending";
                else if (response.ResultCode == PaymentDetailsResponse.ResultCodeEnum.Refused)
                    redirectUrl += "failed";
                else
                    redirectUrl += "error";

                return Redirect(redirectUrl + "?reason=" + response.ResultCode);
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for PaymentDetails failed:\n{e.ResponseBody}\n");
                throw;
            }
        }
    }
}