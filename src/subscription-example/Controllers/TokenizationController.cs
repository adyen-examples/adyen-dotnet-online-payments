using Adyen;
using Adyen.Model.Checkout;
using Adyen.Service;
using adyen_dotnet_subscription_example.Options;
using adyen_dotnet_subscription_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace adyen_dotnet_subscription_example.Controllers
{
    [ApiController]
    public class TokenizationController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IUrlService _urlService;
        private readonly string _merchantAccount;
        private readonly Client _client;
        private readonly Checkout _checkout;
        private readonly string _shopperReference = "YOUR_UNIQUE_SHOPPER_ID_IOfW3k9G2PvXFu2j"; // It's a unique reference that the merchant uses to uniquely identify the shopper.

        public TokenizationController(ILogger<ApiController> logger, IUrlService urlService, IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _urlService = urlService;
            _client = new Client(options.Value.ADYEN_API_KEY, Adyen.Model.Enum.Environment.Test); // Test Environment;
            _checkout = new Checkout(_client);
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
        }

        // MOve this function elsewhere.
        [Route("tokenization/subscription/payment/{token}")]
        public async Task<ActionResult<string>> InitiateSubscriptionPayment(string token = "LZ2VWB5WJ6KXWD82")
        {
            var amount = new Amount("USD", 10000);
            var details = new DefaultPaymentMethodDetails
            {
                Type = "scheme",
                StoredPaymentMethodId = token
            };

            var paymentsRequest = new PaymentRequest
            {
                Reference = "YOUR_ORDER_NUMBER",
                Amount = amount,
                MerchantAccount = _merchantAccount,
                ShopperInteraction = PaymentRequest.ShopperInteractionEnum.ContAuth, // improve in docs, should be an enum now
                RecurringProcessingModel = PaymentRequest.RecurringProcessingModelEnum.Subscription, // improve in docs, should be an enum now
                ShopperReference = _shopperReference,
                PaymentMethod = details
            };

            try
            {
                var paymentResponse = await _checkout.PaymentsAsync(paymentsRequest);
                _logger.LogInformation($"Response for Payment API::\n{paymentResponse}\n");
                return paymentResponse.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed::\n{e.ResponseBody}\n");
                throw;
            }
        }

        [HttpPost("tokenization/subscription")]
        public async Task<ActionResult<string>> InitiateTokenizationRequestForSubscription()
        {
            var orderRef = Guid.NewGuid();
            var amount = new Amount("USD", 0); // Some card schemes accept 0, dynamic card validation (in ca) => The dynamic card validation will automatically increase this amount depending on the scheme (and it refunds it automatically)
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
                ShopperReference = _shopperReference,
                PaymentMethod = details
            };

            try
            {
                var paymentResponse = await _checkout.PaymentsAsync(paymentsRequest);
                _logger.LogInformation($"Response for Payment API::\n{paymentResponse}\n");
                return paymentResponse.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed::\n{e.ResponseBody}\n");
                throw;
            }
        }

        //[HttpPost("tokenization/subscription")]
        //public async Task<ActionResult<string>> InitiateTokenizationRequestForSubscription()
        //{
        //    var orderRef = Guid.NewGuid();

        //    var sessionsRequest = new CreateCheckoutSessionRequest();
        //    sessionsRequest.MerchantAccount = _merchantAccount; // required

        //    var amount = new Amount("USD", 1); // Some card schemes accept 0, dynamic card validation (in ca) => The dynamic card validation will automatically increase this amount depending on the scheme (and it refunds it automatically)
        //    sessionsRequest.Amount = amount;
        //    sessionsRequest.Reference = orderRef.ToString(); // required

        //    sessionsRequest.ShopperInteraction = CreateCheckoutSessionRequest.ShopperInteractionEnum.Ecommerce;
        //    sessionsRequest.StorePaymentMethod = true;
        //    sessionsRequest.EnableRecurring = true;
        //    sessionsRequest.RecurringProcessingModel = CreateCheckoutSessionRequest.RecurringProcessingModelEnum.Subscription;
        //    sessionsRequest.ShopperReference = _shopperReference;

        //    // required for 3ds2 redirect flow
        //    sessionsRequest.ReturnUrl = $"{_urlService.GetHostUrl()}/redirect?orderRef={orderRef}";

        //    try
        //    {
        //        var sessionResponse = await _checkout.SessionsAsync(sessionsRequest);
        //        _logger.LogInformation($"Response for Payment API::\n{sessionResponse}\n");
        //        return sessionResponse.ToJson();
        //    }
        //    catch (Adyen.HttpClient.HttpClientException e)
        //    {
        //        _logger.LogError($"Request for Payments failed::\n{e.ResponseBody}\n");
        //        throw e;
        //    }
        //}
    }
}