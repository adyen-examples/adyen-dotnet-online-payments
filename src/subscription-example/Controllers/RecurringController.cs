using adyen_dotnet_subscription_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace adyen_dotnet_subscription_example.Controllers
{
    [ApiController]
    public class RecurringController : ControllerBase
    {
        private readonly ILogger<RecurringController> _logger;
        private readonly IRecurringClient _subscriptionService;
        private readonly string _shopperReference = "YOUR_UNIQUE_SHOPPER_ID_IOfW3k9G2PvXFu2X"; // It's a unique reference that the merchant uses to uniquely identify the shopper.

        public RecurringController(ILogger<RecurringController> logger, IRecurringClient subscriptionService)
        {
            _logger = logger;
            _subscriptionService = subscriptionService;
        }

        [HttpGet("recurring/listRecurringDetails")]
        public async Task<ActionResult<string>> ListRecurringDetailsAsync()
        {
            var result = await _subscriptionService.ListRecurringDetailAsync(_shopperReference);
            return result.ToJson();
        }

        [Route("recurring/disable/{recurringDetailReference}")]
        public async Task<ActionResult<string>> DisableStoredPaymentMethodAsync(string recurringDetailReference)
        {
            var result = await _subscriptionService.DisableRecurringDetailAsync(_shopperReference, recurringDetailReference);
            return result.Response;
        }
    }
}