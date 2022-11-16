using adyen_dotnet_subscription_example.Clients;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace adyen_dotnet_subscription_example.Controllers
{
    [ApiController]
    public class TokenizationController : ControllerBase
    {
        private readonly ICheckoutClient _checkoutService;
        private readonly string _shopperReference = "YOUR_UNIQUE_SHOPPER_ID_IOfW3k9G2PvXFu2X"; // It's a unique reference that the merchant uses to uniquely identify the shopper.

        public TokenizationController(ICheckoutClient checkoutService)
        {
            _checkoutService = checkoutService;
        }

        /// <summary>
        /// This method makes a Merchant Initiated Transaction (MIT) using the stored <paramref name="recurringDetailReference"/>.
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
    }
}