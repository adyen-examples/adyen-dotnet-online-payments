using Adyen.Model.Checkout;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using adyen_dotnet_subscription_example.Services;

namespace adyen_dotnet_subscription_example.Controllers
{
    [ApiController]
    public class TokenizationController : ControllerBase
    {
        private readonly ICheckoutService _checkoutService;

        public TokenizationController(ICheckoutService checkoutService)
        {
            _checkoutService = checkoutService;
        }

        /// <summary>
        /// This method creates a token using the /sessions endpoint.
        /// </summary>
        [HttpPost("api/tokenization/sessions")]
        public async Task<ActionResult<CreateCheckoutSessionResponse>> Sessions(CancellationToken cancellationToken = default)
        {
            var result = await _checkoutService.CheckoutSessionsAsync(ShopperReference.Value, cancellationToken);
            return result;
        }
    }
}