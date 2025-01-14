using Adyen.Model.Checkout;
using Adyen.Service.Checkout;
using adyen_dotnet_giving_example.Models.Requests;
using adyen_dotnet_giving_example.Options;
using adyen_dotnet_giving_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PaymentRequest = Adyen.Model.Checkout.PaymentRequest;

namespace adyen_dotnet_giving_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private const string DonationToken = "DonationToken";
        private const string PaymentOriginalPspReference = "PaymentOriginalPspReference";
        
        private readonly ILogger<ApiController> _logger;
        private readonly IUrlService _urlService;
        private readonly IPaymentsService _paymentsService;
        private readonly IDonationsService _donationsService;
        private readonly string _merchantAccount;
        
        public ApiController(IPaymentsService paymentsService, IDonationsService donationsService, IUrlService urlService, IOptions<AdyenOptions> options,  ILogger<ApiController> logger)
        {
            _logger = logger;
            _urlService = urlService;
            _paymentsService = paymentsService;
            _donationsService = donationsService;
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
        }

        [HttpPost("api/donations")]
        public async Task<ActionResult<PaymentMethodsResponse>> Donations([FromBody] DonationAmountRequest amountRequest, CancellationToken cancellationToken = default)
        {
             try
    {
        string pspReference = HttpContext.Session.GetString(PaymentOriginalPspReference);
        string donationToken = HttpContext.Session.GetString(DonationToken);

        if (string.IsNullOrWhiteSpace(pspReference))
        {
            _logger.LogInformation("Could not find the PspReference in the stored session.");
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(donationToken))
        {
            _logger.LogInformation("Could not find the DonationToken in the stored session.");
            return NotFound();
        }

        // Dynamically pass the payment method from the frontend
        var response = await _donationsService.DonationsAsync(new DonationPaymentRequest()
        {
            Amount = new Amount(amountRequest.Currency, amountRequest.Value),
            Reference = Guid.NewGuid().ToString(),
            PaymentMethod = amountRequest.PaymentMethod,  // PaymentMethod is passed dynamically
            DonationToken = donationToken,
            DonationOriginalPspReference = pspReference,
            DonationAccount = "MyCharity_Giving_TEST",  // Set your donation account here
            ReturnUrl = $"{_urlService.GetHostUrl()}",
            MerchantAccount = _merchantAccount,
            ShopperInteraction = DonationPaymentRequest.ShopperInteractionEnum.ContAuth
        }, cancellationToken: cancellationToken);

        HttpContext.Session.Remove(PaymentOriginalPspReference);
        HttpContext.Session.Remove(DonationToken);

        return Ok(response);
    }
    catch (Adyen.HttpClient.HttpClientException e)
    {
        _logger.LogError($"Request for PaymentMethods failed:\n{e.ResponseBody}\n");
        throw;
    }
        }

        [HttpPost("api/getPaymentMethods")]
        public async Task<ActionResult<PaymentMethodsResponse>> GetPaymentMethods(CancellationToken cancellationToken = default)
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
                return res;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for PaymentMethods failed:\n{e.ResponseBody}\n");
                throw;
            }
        }

        [HttpPost("api/initiatePayment")]
        public async Task<ActionResult<PaymentResponse>> InitiatePayment(PaymentRequest request, CancellationToken cancellationToken = default)
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
                
                AdditionalData = new Dictionary<string, string>() { {  "allow3DS2", "true" } },
                Origin = _urlService.GetHostUrl(),
                BrowserInfo = request.BrowserInfo, 
                ShopperIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                PaymentMethod = request.PaymentMethod
            };

            try
            {
                var res = await _paymentsService.PaymentsAsync(paymentRequest, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for Payment:\n{res}\n");
                
                HttpContext.Session.SetString(PaymentOriginalPspReference, res.PspReference);

                if (string.IsNullOrWhiteSpace(res.DonationToken))
                {
                    _logger.LogError("The payments endpoint did not return a donationToken, please enable this in your Customer Area. See README.");
                }
                else
                {
                    HttpContext.Session.SetString(DonationToken, res.DonationToken);
                }

                return res;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payment failed:\n{e.ResponseBody}\n");
                throw;
            }
        }

        [HttpPost("api/submitAdditionalDetails")]
        public async Task<ActionResult<PaymentDetailsResponse>> SubmitAdditionalDetails(PaymentDetailsRequest request, CancellationToken cancellationToken = default)
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
            var detailsRequest = new PaymentDetailsRequest();
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