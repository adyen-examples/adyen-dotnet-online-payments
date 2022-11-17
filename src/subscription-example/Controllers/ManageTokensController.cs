using Adyen.Model.Recurring;
using adyen_dotnet_subscription_example.Clients;
using adyen_dotnet_subscription_example.Options;
using adyen_dotnet_subscription_example.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace adyen_dotnet_subscription_example.Controllers
{
    public class ManageTokensController : Controller
    {
        private readonly string _clientKey;
        private readonly IRecurringClient _recurringClient;
        private readonly ICheckoutClient _checkoutClient;
        private readonly ISubscriptionRepository _repository;

        public ManageTokensController(IOptions<AdyenOptions> options, IRecurringClient recurringClient, ICheckoutClient checkoutClient, ISubscriptionRepository repository)
        {
            _clientKey = options.Value.ADYEN_CLIENT_KEY;
            _recurringClient = recurringClient;
            _checkoutClient = checkoutClient;
            _repository = repository;
        }

        [Route("managetokens")]
        public async Task<IActionResult> Index()
        {
            var details = new List<RecurringDetailsResult>();
            foreach (var shopperReference in _repository.ShopperReferences)
            {
                details.Add(await _recurringClient.ListRecurringDetailAsync(shopperReference.Key));
            }
            ViewBag.Details = details;
            return View();
        }

        [Route("managetokens/makepayment/{recurringDetailReference}")]
        public async Task<IActionResult> MakePayment(string recurringDetailReference)
        {
            var details = await _checkoutClient.MakePaymentAsync(ShopperReference.Value, recurringDetailReference);
            ViewBag.Message = $"Made a payment using {recurringDetailReference}";
            return Redirect("/managetokens");
        }

        [Route("managetokens/disable/{recurringDetailReference}")]
        public async Task<IActionResult> Disable(string recurringDetailReference)
        {
            var details = await _recurringClient.DisableRecurringDetailAsync(ShopperReference.Value, recurringDetailReference);
            ViewBag.Message = $"Removed {recurringDetailReference}";
            return Redirect("/managetokens");
        }
    }
}
