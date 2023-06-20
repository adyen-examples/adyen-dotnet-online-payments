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
    public class TokenizationController : ControllerBase
    {
        private readonly IPaymentsService _paymentsService;
        private readonly IUrlService _urlService;
        private readonly ILogger<TokenizationController> _logger;
        private readonly AdyenOptions _options;

        public TokenizationController(IPaymentsService paymentsService, IUrlService urlService, ILogger<TokenizationController> logger, IOptions<AdyenOptions> options)
        {
            _paymentsService = paymentsService;
            _urlService = urlService;
            _logger = logger;
            _options = options.Value;
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

                Amount = new Amount("EUR", 0),
                Channel = PaymentRequest.ChannelEnum.Web,
                ShopperInteraction = PaymentRequest.ShopperInteractionEnum.Ecommerce,
                RecurringProcessingModel = PaymentRequest.RecurringProcessingModelEnum.Subscription,
                EnableRecurring = true,
                ShopperReference = ShopperReference.Value,

                // Required for 3DS2 redirect flow.
                ReturnUrl = $"{_urlService.GetHostUrl()}/redirect?orderRef={orderRef}",
                
                // Specify Payment method.
                PaymentMethod = request.PaymentMethod,

                // Specify that this is a Pre-Authorisation.
                AdditionalData = new Dictionary<string, string>
                {
                    { "authorisationType", "PreAuth" }
                }
            };

            try
            {
                var response = await _paymentsService.PaymentsAsync(paymentRequest, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for Payments API:\n{response}\n");
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError($"Request for Payments API failed:\n{e}\n");
                throw;
            }
        }
    }
}