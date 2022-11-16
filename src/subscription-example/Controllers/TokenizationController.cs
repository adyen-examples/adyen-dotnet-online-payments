using adyen_dotnet_subscription_example.Clients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace adyen_dotnet_subscription_example.Controllers
{
    [ApiController]
    public class TokenizationController : ControllerBase
    {
        private readonly ILogger<TokenizationController> _logger;
        private readonly ICheckoutClient _checkoutService;
        private readonly string _shopperReference = "YOUR_UNIQUE_SHOPPER_ID_IOfW3k9G2PvXFu2X"; // It's a unique reference that the merchant uses to uniquely identify the shopper.

        public TokenizationController(ILogger<TokenizationController> logger, ICheckoutClient checkoutService)
        {
            _logger = logger;
            _checkoutService = checkoutService;
        }

        /// <summary>
        /// This method makes a Merchant Initiated Transaction (MIT) on behalf of their customer.
        /// </summary>
        /// <param name="recurringDetailReference">The token.</param>
        [Route("tokenization/payment")]
        public async Task<ActionResult<string>> MakePaymentAsync(string recurringDetailReference)
        {
            var result = await _checkoutService.MakePaymentAsync(_shopperReference, recurringDetailReference);
            return result.ToJson();
        }

        /// <summary>
        /// This method creates a token using the /sessions endpoint.
        /// </summary>
        [HttpPost("tokenization/sessions")]
        public async Task<ActionResult<string>> SessionsAsync()
        {
            var result = await _checkoutService.CheckoutSessionsAsync(_shopperReference);
            return result.ToJson();
        }
        
        // Show available payment methods (frontend) // TODO: Remove this.
        //[HttpGet("tokenization/paymentMethods")]
        //public async Task<ActionResult<string>> PaymentMethodsAsync()
        //{
        //    var paymentMethodsRequest = new PaymentMethodsRequest(merchantAccount: _merchantAccount);

        //    var amount = new Amount("USD", 0);
        //    paymentMethodsRequest.Amount = amount;
        //    paymentMethodsRequest.CountryCode = "NL";
        //    paymentMethodsRequest.ShopperReference = _shopperReference;

        //    try
        //    {
        //        var paymentMethodResponse = await _checkout.PaymentMethodsAsync(paymentMethodsRequest);
        //        _logger.LogInformation($"Response for Payments API::\n{paymentMethodResponse}\n");
        //        return paymentMethodResponse.ToJson();
        //    }
        //    catch (Adyen.HttpClient.HttpClientException e)
        //    {
        //        _logger.LogError($"Request for Payments failed::\n{e.ResponseBody}\n");
        //        return e.ResponseBody;
        //    }
        //}
    }
}