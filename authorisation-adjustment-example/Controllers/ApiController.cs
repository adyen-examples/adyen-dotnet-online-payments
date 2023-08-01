using Adyen.Model.Checkout;
using Adyen.Service.Checkout;
using adyen_dotnet_authorisation_adjustment_example.Models;
using adyen_dotnet_authorisation_adjustment_example.Options;
using adyen_dotnet_authorisation_adjustment_example.Repositories;
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
        private readonly IPaymentRepository _repository;
        private readonly ILogger<ApiController> _logger;
        private readonly string _merchantAccount;

        public ApiController(IPaymentsService paymentsService, IUrlService urlService, IPaymentRepository repository, ILogger<ApiController> logger, IOptions<AdyenOptions> options)
        {
            _paymentsService = paymentsService;
            _urlService = urlService;
            _repository = repository;
            _logger = logger;
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
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
                var response = await _paymentsService.PaymentMethodsAsync(paymentMethodsRequest, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for PaymentMethods:\n{response}\n");
                return Ok(response);
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for PaymentMethods failed:\n{e.ResponseBody}\n");
                return BadRequest();
            }
        }

        [HttpPost("api/submitAdditionalDetails")]
        public async Task<ActionResult<PaymentDetailsResponse>> SubmitAdditionalDetails(DetailsRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _paymentsService.PaymentsDetailsAsync(request, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for PaymentsDetails:\n{response}\n");
                return Ok(response);
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for PaymentsDetails failed:\n{e.ResponseBody}\n");
                return BadRequest();
            }
        }

        [HttpGet("api/handleRedirect")]
        public async Task<IActionResult> HandleRedirect(string payload = null, string redirectResult = null, CancellationToken cancellationToken = default)
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
                _logger.LogInformation($"Response for PaymentsDetails:\n{res}\n");
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
                _logger.LogError($"Request for PaymentsDetails failed:\n{e.ResponseBody}\n");
                return BadRequest();
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
                MerchantAccount = _merchantAccount, // Required.
                Reference = orderRef.ToString(), // Required.
                Channel = PaymentRequest.ChannelEnum.Web,
                Amount = new Amount("EUR", 24999), // Value is 249.99€ in minor units.

                // Required for 3DS2 redirect flow.
                ReturnUrl = $"{_urlService.GetHostUrl()}/api/handleRedirect?orderRef={orderRef}",

                CountryCode = "NL",
                Origin = _urlService.GetHostUrl(),
                BrowserInfo = new BrowserInfo() { UserAgent = HttpContext.Request.Headers["user-agent"] }, // Add more browser info here. 
                ShopperIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                PaymentMethod = request.PaymentMethod,

                AdditionalData = new Dictionary<string, string>
                {
                    { "allow3DS2", "true" },            // Required for the 3DS2 flow.
                    { "authorisationType", "PreAuth" }, // Set `authorisationType` to `preAuth`.
                }
            };

            try
            {
                var response = await _paymentsService.PaymentsAsync(paymentRequest, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for Payments:\n{response}\n");

                var payment = new PaymentModel()
                {
                    PspReference = response.PspReference,
                    OriginalReference = null,
                    Reference = response.MerchantReference,
                    Amount = response.Amount?.Value,
                    Currency = response.Amount?.Currency,
                    DateTime = DateTimeOffset.UtcNow,
                    ResultCode = response.ResultCode.ToString(),
                    RefusalReason = response.RefusalReason,
                    PaymentMethodBrand = response.PaymentMethod?.Brand,
                };

                if (!_repository.Payments.TryGetValue(payment.Reference, out var list))
                {
                    // Reference does not exist, let's add it.
                    _repository.Payments.TryAdd(
                        payment.Reference, /// Key: Reference.
                        new List<PaymentModel>() {
                        {
                            payment        /// Value: <see cref="PaymentModel"/>.
                        }
                    });
                }

                return Ok(response);
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed:\n{e.ResponseBody}\n");
                return BadRequest();
            }
        }
    }
}