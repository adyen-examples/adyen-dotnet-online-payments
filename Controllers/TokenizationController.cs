using Adyen;
using Adyen.Model.Checkout;
using Adyen.Service;
using adyen_dotnet_online_payments.Options;
using adyen_dotnet_online_payments.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace adyen_dotnet_online_payments.Controllers
{
    [ApiController]
    public class TokenizationController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IUrlService _urlService;
        private readonly string _merchantAccount;
        private readonly Client _client;
        private readonly Checkout _checkout;

        public TokenizationController(ILogger<ApiController> logger, IUrlService urlService, IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _urlService = urlService;
            _client = new Client(options.Value.ADYEN_API_KEY, Adyen.Model.Enum.Environment.Test); // Test Environment;
            _checkout = new Checkout(_client);
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
        }

        [Route("tokenization/subscription")]
        public ActionResult<string> Subscription()
        {
            var orderRef = Guid.NewGuid();
            var amount = new Amount("USD", 0);
            var details = new DefaultPaymentMethodDetails
            {
                Type = "scheme",
                Number = "4166676667666746",
                ExpiryMonth = "03",
                ExpiryYear = "2030",
                Cvc = "737",
                HolderName = "John Smith"
            };

            var paymentsRequest = new PaymentRequest
            {
                Reference = orderRef.ToString(),
                Amount = amount,
                ReturnUrl = $"{_urlService.GetHostUrl()}/redirect?orderRef={orderRef}",
                MerchantAccount = _merchantAccount,
                ShopperInteraction = PaymentRequest.ShopperInteractionEnum.Ecommerce,
                StorePaymentMethod = true,
                RecurringProcessingModel = PaymentRequest.RecurringProcessingModelEnum.Subscription,
                ShopperReference = "YOUR_UNIQUE_SHOPPER_ID_IOfW3k9G2PvXFu2j",
                PaymentMethod = details
            };

            try
            {
                var paymentResponse = _checkout.Payments(paymentsRequest);
                _logger.LogInformation($"Response for Payment API::\n{paymentResponse}\n");
                return paymentResponse.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed::\n{e.ResponseBody}\n");
                throw e;
            }
        }
    }
}