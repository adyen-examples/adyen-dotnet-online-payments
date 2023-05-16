using adyen_dotnet_subscription_example.Clients;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_subscription_example.Controllers
{
    [ApiController]
    public class TokenizationController : ControllerBase
    {
        private readonly ICheckoutClient _checkoutService;

        public TokenizationController(ICheckoutClient checkoutService)
        {
            _checkoutService = checkoutService;
        }

        /// <summary>
        /// This method creates a token using the /sessions endpoint.
        /// </summary>
        [HttpPost("api/tokenization/sessions")]
        public async Task<ActionResult<string>> Sessions(CancellationToken cancellationToken = default)
        {
            var result = await _checkoutService.CheckoutSessionsAsync(ShopperReference.Value, cancellationToken);
            return result.ToJson();
        }
    }
}