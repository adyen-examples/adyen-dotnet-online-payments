using adyen_dotnet_subscription_example.Clients;
using adyen_dotnet_subscription_example.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace adyen_dotnet_subscription_example.Controllers
{
    public class ManageTokensController : Controller
    {
        private readonly string _clientKey;
        private readonly IRecurringClient _subscriptionService;
        private readonly ICheckoutClient _checkoutClient;

        public ManageTokensController(IOptions<AdyenOptions> options, IRecurringClient subscriptionService, ICheckoutClient checkoutClient)
        {
            _clientKey = options.Value.ADYEN_CLIENT_KEY;
            _subscriptionService = subscriptionService;
            _checkoutClient = checkoutClient;
        }

        [Route("managetokens")]
        public async Task<IActionResult> Index()
        {
            var details = await _subscriptionService.ListRecurringDetailAsync(ShopperReference.Value);
            ViewBag.Details = details;
            return View();
        }

        [Route("managetokens/makepayment/{recurringDetailReference}")]
        public async Task<IActionResult> MakePayment(string recurringDetailReference)
        {
            var details = await _checkoutClient.MakePaymentAsync(ShopperReference.Value, recurringDetailReference);

            // TODO Show something visual to show that the payment is successful, instead of redirecting to same page.
            return Redirect("/managetokens");
        }

        [Route("managetokens/disable/{recurringDetailReference}")]
        public async Task<IActionResult> Disable(string recurringDetailReference)
        {
            var details = await _subscriptionService.DisableRecurringDetailAsync(ShopperReference.Value, recurringDetailReference);
            // TODO: Show a message that says: removed ... token etc.
            return Redirect("/managetokens");
        }
    }
}
