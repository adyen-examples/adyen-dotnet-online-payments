using adyen_dotnet_subscription_example.Clients;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace adyen_dotnet_subscription_example.Controllers
{
    [ApiController]
    public class RecurringController : ControllerBase
    {
        private readonly IRecurringClient _subscriptionService;

        public RecurringController(IRecurringClient subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet("recurring/listRecurringDetails")]
        public async Task<ActionResult<string>> ListRecurringDetailsAsync()
        {
            var result = await _subscriptionService.ListRecurringDetailAsync(ShopperReference.Value);
            return result.ToJson();
        }

        [Route("recurring/disable/{recurringDetailReference}")]
        public async Task<ActionResult<string>> DisableStoredPaymentMethodAsync(string recurringDetailReference)
        {
            var result = await _subscriptionService.DisableRecurringDetailAsync(ShopperReference.Value, recurringDetailReference);
            return result.Response;
        }
    }
}