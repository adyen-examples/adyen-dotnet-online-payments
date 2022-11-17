using adyen_dotnet_subscription_example.Clients;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace adyen_dotnet_subscription_example.Controllers
{
    [ApiController]
    public class RecurringController : ControllerBase
    {
        private readonly IRecurringClient _recurringClient;

        public RecurringController(IRecurringClient recurringClient)
        {
            _recurringClient = recurringClient;
        }

        [HttpGet("recurring/listRecurringDetails")]
        public async Task<ActionResult<string>> ListRecurringDetailsAsync()
        {
            var result = await _recurringClient.ListRecurringDetailAsync(ShopperReference.Value);
            return result.ToJson();
        }

        [Route("recurring/disable/{recurringDetailReference}")]
        public async Task<ActionResult<string>> DisableStoredPaymentMethodAsync(string recurringDetailReference)
        {
            var result = await _recurringClient.DisableRecurringDetailAsync(ShopperReference.Value, recurringDetailReference);
            return result.Response;
        }
    }
}