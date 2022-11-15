using Adyen;
using Adyen.Model.Recurring;
using adyen_dotnet_subscription_example.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Recurring = Adyen.Service.Recurring;

namespace adyen_dotnet_subscription_example.Controllers
{
    [ApiController]
    public class RecurringController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly string _merchantAccount;
        private readonly Client _client;
        private readonly Recurring _recurring;
        private readonly string _shopperReference = "YOUR_UNIQUE_SHOPPER_ID_IOfW3k9G2PvXFu2j"; // It's a unique reference that the merchant uses to uniquely identify the shopper.

        public RecurringController(ILogger<ApiController> logger, IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _client = new Client(options.Value.ADYEN_API_KEY, Adyen.Model.Enum.Environment.Test); // Test Environment;
            _recurring = new Recurring(_client);
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
        }

        [HttpGet("recurring/listRecurringDetails")]
        public async Task<ActionResult<string>> ListRecurringDetails()
        {
            var request = new RecurringDetailsRequest()
            {
                MerchantAccount = _merchantAccount,
                ShopperReference = _shopperReference
            };

            try
            {
                var recurringDetailsResult = await _recurring.ListRecurringDetailsAsync(request);
                _logger.LogInformation($"Response for Recurring API::\n{recurringDetailsResult}\n");
                return recurringDetailsResult.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for listing recurring details failed::\n{e.ResponseBody}\n");
                throw;
            }
        }

        [HttpGet("recurring/disable/{recurringDetailReference}")]
        public async Task<ActionResult<string>> DisableStoredPaymentMethod(string recurringDetailReference)
        {
            var request = new DisableRequest()
            {
                MerchantAccount = _merchantAccount,
                ShopperReference = _shopperReference,
                RecurringDetailReference = recurringDetailReference
            };

            try
            {
                var disableResult = await _recurring.DisableAsync(request);
                _logger.LogInformation($"Response for Recurring API::\n{disableResult}\n");
                return disableResult.Response;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for listing recurring details failed::\n{e.ResponseBody}\n");
                throw;
            }
        }
    }
}